using System.Linq.Expressions;

namespace StoneRed.LogicSimulator.Simulation;

public sealed class CycleCircuitSimulator : SimulatorBase
{
    private int[] nextInputMasks = [];
    private Action<int[], int[], int[]> computeOutputs = (_, _, _) => { };

    protected override void EnsureStorage()
    {
        base.EnsureStorage();
        if (nextInputMasks.Length != gateKinds.Count)
        {
            nextInputMasks = new int[gateKinds.Count];
        }
    }

    public override void Reset()
    {
        base.Reset();
        Array.Clear(nextInputMasks);
    }

    protected override void OnSourceChanged(int gateId) { }

    public override void Step()
    {
        EnsureCompiled();
        if (!initialized)
        {
            Reset();
        }

        computeOutputs(inputMasks, outputMasks, sourceStates);
        PropagateAndSwap();
        NotifyAllWatchers();
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
            computeOutputs(inputMasks, outputMasks, sourceStates);
            changed = PropagateAndSwapDetectChange();
            NotifyAllWatchers();
        }
        return !changed;
    }

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

    protected override SimulatorBase CreateInternalSimulator()
    {
        return new CycleCircuitSimulator();
    }
}
