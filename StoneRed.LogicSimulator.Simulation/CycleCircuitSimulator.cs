using System.Linq.Expressions;

namespace StoneRed.LogicSimulator.Simulation;

/// <summary>
/// A synchronous cycle-based circuit simulator that evaluates all gates every cycle.
/// Provides deterministic timing and predictable evaluation order (gate 0 to N).
/// </summary>
/// <remarks>
/// This simulator evaluates all gates synchronously each step, making it simpler and more
/// predictable than <see cref="EventCircuitSimulator"/>. It's ideal for synchronous digital
/// designs where deterministic timing is important. Less efficient for sparse circuits
/// but has uniform performance characteristics.
/// </remarks>
public sealed class CycleCircuitSimulator : SimulatorBase
{
    private int[] nextInputMasks = [];
    private Action<int[], int[], int[]> computeOutputs = (_, _, _) => { };
    private int[] previousOutputMasks = [];
    private int[] previousInputMasks = [];

    /// <summary>
    /// Ensures internal storage arrays are properly sized for the current gate count.
    /// </summary>
    protected override void EnsureStorage()
    {
        base.EnsureStorage();
        if (nextInputMasks.Length != gateKinds.Count)
        {
            nextInputMasks = new int[gateKinds.Count];
            previousOutputMasks = new int[gateKinds.Count];
            previousInputMasks = new int[gateKinds.Count];
        }
    }

    /// <summary>
    /// Resets the circuit to initial state, clearing all input and output buffers.
    /// </summary>
    public override void Reset()
    {
        base.Reset();
        Array.Clear(nextInputMasks);
        Array.Clear(previousOutputMasks);
        Array.Clear(previousInputMasks);
    }

    /// <summary>
    /// Executes one simulation cycle by evaluating all gates synchronously,
    /// then propagating outputs to inputs for the next cycle.
    /// </summary>
    public override void Step()
    {
        EnsureCompiled();
        if (!initialized)
        {
            Reset();
        }

        (outputMasks, previousOutputMasks) = (previousOutputMasks, outputMasks);
        computeOutputs(inputMasks, outputMasks, sourceStates);
        if (hasAnyWatchers)
        {
            Array.Copy(inputMasks, previousInputMasks, inputMasks.Length);
        }
        PropagateAndSwap();
        if (hasAnyWatchers)
        {
            NotifyAllWatchers(previousInputMasks);
        }
    }

    private void PropagateAndSwap()
    {
        Array.Clear(nextInputMasks);
        for (int fromGate = 0; fromGate < gateKinds.Count; fromGate++)
        {
            if ((outputMasks[fromGate] & 1) == 0)
            {
                continue;
            }

            int start = edgeStart[fromGate];
            int end = edgeStart[fromGate + 1];
            for (int e = start; e < end; e++)
            {
                nextInputMasks[edgeToGate[e]] |= 1 << edgeToInputBit[e];
            }
        }
        (inputMasks, nextInputMasks) = (nextInputMasks, inputMasks);
    }

    private bool PropagateAndSwapDetectChange()
    {
        Array.Clear(nextInputMasks);
        for (int fromGate = 0; fromGate < gateKinds.Count; fromGate++)
        {
            if ((outputMasks[fromGate] & 1) == 0)
            {
                continue;
            }

            int start = edgeStart[fromGate];
            int end = edgeStart[fromGate + 1];
            for (int e = start; e < end; e++)
            {
                nextInputMasks[edgeToGate[e]] |= 1 << edgeToInputBit[e];
            }
        }

        bool changed = false;
        for (int i = 0; i < inputMasks.Length; i++)
        {
            if (inputMasks[i] != nextInputMasks[i]) { changed = true; break; }
        }

        (inputMasks, nextInputMasks) = (nextInputMasks, inputMasks);
        return changed;
    }

    /// <summary>
    /// Runs the simulation until inputs stabilize (no changes between cycles) or maxSteps is reached.
    /// </summary>
    /// <param name="maxSteps">Maximum number of cycles to execute.</param>
    /// <param name="steps">Output parameter containing the number of cycles executed.</param>
    /// <returns>True if the circuit stabilized; false if maxSteps was exceeded.</returns>
    public override bool TryRunUntilStable(int maxSteps, out int steps)
    {
        steps = 0;
        if (maxSteps <= 0)
        {
            return false;
        }

        EnsureCompiled();
        if (!initialized)
        {
            Reset();
        }

        bool changed = true;
        while (changed && steps < maxSteps)
        {
            steps++;
            (outputMasks, previousOutputMasks) = (previousOutputMasks, outputMasks);
            computeOutputs(inputMasks, outputMasks, sourceStates);
            if (hasAnyWatchers)
            {
                Array.Copy(inputMasks, previousInputMasks, inputMasks.Length);
            }
            changed = PropagateAndSwapDetectChange();
            if (hasAnyWatchers)
            {
                NotifyAllWatchers(previousInputMasks);
            }
        }
        return !changed;
    }

    /// <summary>
    /// Compiles a single evaluator that computes outputs for all gates in one call.
    /// All gate logic is combined into a single compiled lambda expression.
    /// </summary>
    protected override void CompileEngine()
    {
        ParameterExpression inputsParam = Expression.Parameter(typeof(int[]), "inputs");
        ParameterExpression outputsParam = Expression.Parameter(typeof(int[]), "outputs");
        ParameterExpression sourcesParam = Expression.Parameter(typeof(int[]), "sources");
        ConstantExpression lutDataConst = Expression.Constant(lutData);

        List<Expression> body = [];
        for (int i = 0; i < gateKinds.Count; i++)
        {
            ConstantExpression indexExpr = Expression.Constant(i);
            BinaryExpression inMask = Expression.ArrayIndex(inputsParam, indexExpr);
            Expression logicExpr = GenerateGateLogic(i, inMask, sourcesParam, indexExpr, lutDataConst);

            body.Add(Expression.Assign(Expression.ArrayAccess(outputsParam, indexExpr), logicExpr));
        }

        computeOutputs = Expression.Lambda<Action<int[], int[], int[]>>(Expression.Block(body), inputsParam, outputsParam, sourcesParam).Compile();
    }

    /// <summary>
    /// Creates a new instance of CycleCircuitSimulator for internal use (e.g., LUT computation).
    /// </summary>
    /// <returns>A new CycleCircuitSimulator instance.</returns>
    protected override SimulatorBase CreateInternalSimulator()
    {
        return new CycleCircuitSimulator();
    }
}
