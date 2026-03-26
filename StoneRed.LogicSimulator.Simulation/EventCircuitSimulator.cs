using System.Linq.Expressions;

namespace StoneRed.LogicSimulator.Simulation;

public sealed class EventCircuitSimulator : SimulatorBase
{
    private Action<int[], int[], int[], int>[] gateEvaluators = [];
    private readonly Queue<int> activeQueue = new();
    private bool[] inQueue = [];

    protected override void OnGateAdded(int gateId)
    {
        if (inQueue.Length != gateKinds.Count)
        {
            Array.Resize(ref inQueue, gateKinds.Count);
        }
    }

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
                NotifyGateWatchers(gateId);
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
                Enqueue(toGate);
            }
        }
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

        while (activeQueue.Count > 0 && steps < maxSteps)
        {
            steps++;
            Step();
        }
        return activeQueue.Count == 0;
    }

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

    protected override SimulatorBase CreateInternalSimulator()
    {
        return new EventCircuitSimulator();
    }

    protected override void EnsureStorage()
    {
        base.EnsureStorage();
        if (inQueue.Length != gateKinds.Count)
        {
            Array.Resize(ref inQueue, gateKinds.Count);
        }
    }
}
