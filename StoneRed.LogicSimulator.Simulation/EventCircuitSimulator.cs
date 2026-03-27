using System.Linq.Expressions;

namespace StoneRed.LogicSimulator.Simulation;

/// <summary>
/// An event-driven circuit simulator that uses queue-based change propagation.
/// Only gates with changed inputs are evaluated, making it efficient for circuits with localized activity.
/// </summary>
/// <remarks>
/// This simulator uses a dirty-marking propagation queue. When a gate's output changes,
/// only the gates connected to it are queued for re-evaluation. This is more efficient
/// than <see cref="CycleCircuitSimulator"/> for sparse circuits where most gates remain stable.
/// </remarks>
public sealed class EventCircuitSimulator : SimulatorBase
{
    private Action<int[], int[], int[], int>[] gateEvaluators = [];
    private readonly Queue<int> activeQueue = new();
    private bool[] inQueue = [];

    /// <summary>
    /// Called when a gate is added to ensure internal arrays are properly sized.
    /// </summary>
    /// <param name="gateId">The ID of the newly added gate.</param>
    protected override void OnGateAdded(int gateId)
    {
        if (inQueue.Length != gateKinds.Count)
        {
            Array.Resize(ref inQueue, gateKinds.Count);
        }
    }

    /// <summary>
    /// Resets the circuit to initial state and queues all gates for initial evaluation.
    /// </summary>
    public override void Reset()
    {
        base.Reset();
        activeQueue.Clear();
        Array.Clear(inQueue);
        for (int i = 0; i < gateKinds.Count; i++)
        {
            Enqueue(i);
        }
    }

    /// <summary>
    /// Called when a source gate's value changes. Queues the source for propagation.
    /// </summary>
    /// <param name="gateId">The ID of the source gate that changed.</param>
    protected override void OnSourceChanged(int gateId)
    {
        Enqueue(gateId);
    }

    private void Enqueue(int gateId)
    {
        if (!inQueue[gateId])
        {
            inQueue[gateId] = true;
            activeQueue.Enqueue(gateId);
        }
    }

    /// <summary>
    /// Executes one simulation step by processing all pending changes in the queue.
    /// Continues until the queue is empty (all changes have propagated).
    /// </summary>
    public override void Step()
    {
        EnsureCompiled();
        if (!initialized)
        {
            Reset();
        }

        while (activeQueue.Count > 0)
        {
            int gateId = activeQueue.Dequeue();
            inQueue[gateId] = false;

            int oldOutput = outputMasks[gateId];
            gateEvaluators[gateId](inputMasks, outputMasks, sourceStates, gateId);

            if (outputMasks[gateId] != oldOutput)
            {
                Propagate(gateId);
            }
        }
    }

    private void Propagate(int fromGate)
    {
        int start = edgeStart[fromGate];
        int end = edgeStart[fromGate + 1];
        int outVal = outputMasks[fromGate] & 1;

        for (int e = start; e < end; e++)
        {
            int toGate = edgeToGate[e];
            int bit = edgeToInputBit[e];
            int oldBit = (inputMasks[toGate] >> bit) & 1;
            if (oldBit != outVal)
            {
                inputMasks[toGate] ^= 1 << bit;
                if (hasAnyWatchers)
                {
                    NotifyGateWatchers(toGate);
                }
                Enqueue(toGate);
            }
        }
    }

    /// <summary>
    /// Runs the simulation until the queue is empty (circuit is stable) or maxSteps is reached.
    /// </summary>
    /// <param name="maxSteps">Maximum number of steps to execute.</param>
    /// <param name="steps">Output parameter containing the number of steps executed.</param>
    /// <returns>True if the circuit stabilized (queue is empty); false if maxSteps was exceeded.</returns>
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

        while (activeQueue.Count > 0 && steps < maxSteps)
        {
            steps++;
            Step();
        }
        return activeQueue.Count == 0;
    }

    /// <summary>
    /// Compiles per-gate evaluators as separate compiled lambda expressions.
    /// Each gate has its own evaluator function for efficient event-driven execution.
    /// </summary>
    protected override void CompileEngine()
    {
        gateEvaluators = new Action<int[], int[], int[], int>[gateKinds.Count];
        ParameterExpression inputsParam = Expression.Parameter(typeof(int[]), "inputs");
        ParameterExpression outputsParam = Expression.Parameter(typeof(int[]), "outputs");
        ParameterExpression sourcesParam = Expression.Parameter(typeof(int[]), "sources");
        ParameterExpression gateIdParam = Expression.Parameter(typeof(int), "gateId");
        ConstantExpression lutDataConst = Expression.Constant(lutData);

        for (int i = 0; i < gateKinds.Count; i++)
        {
            BinaryExpression inMask = Expression.ArrayIndex(inputsParam, gateIdParam);
            Expression logicExpr = GenerateGateLogic(i, inMask, sourcesParam, gateIdParam, lutDataConst);

            gateEvaluators[i] = Expression.Lambda<Action<int[], int[], int[], int>>(
                Expression.Assign(Expression.ArrayAccess(outputsParam, gateIdParam), logicExpr),
                inputsParam, outputsParam, sourcesParam, gateIdParam).Compile();
        }
    }

    /// <summary>
    /// Creates a new instance of EventCircuitSimulator for internal use (e.g., LUT computation).
    /// </summary>
    /// <returns>A new EventCircuitSimulator instance.</returns>
    protected override SimulatorBase CreateInternalSimulator()
    {
        return new EventCircuitSimulator();
    }

    /// <summary>
    /// Ensures internal storage arrays are properly sized for the current gate count.
    /// </summary>
    protected override void EnsureStorage()
    {
        base.EnsureStorage();
        if (inQueue.Length != gateKinds.Count)
        {
            Array.Resize(ref inQueue, gateKinds.Count);
        }
    }
}
