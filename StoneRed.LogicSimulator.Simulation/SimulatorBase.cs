using System.Linq.Expressions;

namespace StoneRed.LogicSimulator.Simulation;

public abstract class SimulatorBase : ICircuitSimulator
{
    protected readonly List<GateKind> gateKinds = [];
    protected readonly List<(int FromGate, int ToGate, byte ToInputBit)> connections = [];
    protected readonly List<int[]?> lutTableByGate = [];
    protected readonly Dictionary<string, MacroInfo> macroGates = new(StringComparer.Ordinal);

    protected int[] inputMasks = [];
    protected int[] outputMasks = [];
    protected int[] sourceStates = [];
    protected bool[] sourceInitialized = [];

    protected int[] edgeStart = [];
    protected int[] edgeToGate = [];
    protected byte[] edgeToInputBit = [];

    protected int[] lutOffsets = [];
    protected int[] lutMasks = [];
    protected int[] lutData = [];

    protected bool compiled;
    protected bool initialized;

    private readonly List<GateWatcherEntry> allWatchers = [];
    private Action<int, int>[][] watcherCache = [];
    private int[] gatesWithWatchers = [];
    protected int nextWatcherId;

    protected sealed record GateWatcherEntry(int Id, int GateId, Action<int, int> Callback);
    protected sealed record MacroLut(int InputCount, int OutputCount, int[][] OutputTables);
    protected sealed record MacroInfo(CircuitDefinition Definition, MacroLut? Lut);

    public int GateCount => gateKinds.Count;

    public int AddGate(GateKind kind)
    {
        if (kind == GateKind.Lut)
        {
            throw new InvalidOperationException("Use AddLutGate() to create LUT gates.");
        }

        int id = gateKinds.Count;
        gateKinds.Add(kind);
        lutTableByGate.Add(null);
        OnGateAdded(id);
        compiled = false;
        initialized = false;
        return id;
    }

    protected virtual void OnGateAdded(int gateId) { }

    public int AddLutGate(int inputCount, int[] table)
    {
        if (inputCount is < 0 or > 30)
        {
            throw new ArgumentOutOfRangeException(nameof(inputCount));
        }

        ArgumentNullException.ThrowIfNull(table);
        if (table.Length != (1 << inputCount))
        {
            throw new ArgumentException("Invalid table length.");
        }

        int id = gateKinds.Count;
        gateKinds.Add(GateKind.Lut);
        lutTableByGate.Add(table);
        OnGateAdded(id);
        compiled = false;
        initialized = false;
        return id;
    }

    public void ConnectGates(int fromGate, int toGate, int toInputBit)
    {
        if ((uint)fromGate >= (uint)gateKinds.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(fromGate));
        }

        if ((uint)toGate >= (uint)gateKinds.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(toGate));
        }

        if ((uint)toInputBit >= 32u)
        {
            throw new ArgumentOutOfRangeException(nameof(toInputBit));
        }

        connections.Add((fromGate, toGate, (byte)toInputBit));
        compiled = false;
        initialized = false;
    }

    public void RegisterMacroGate(string name, CircuitDefinition definition)
    {
        definition.Validate();
        macroGates[name] = new MacroInfo(definition, Lut: null);
        compiled = false;
        initialized = false;
    }

    public virtual void Reset()
    {
        EnsureStorage();
        Array.Clear(inputMasks);
        Array.Clear(outputMasks);
        Array.Clear(sourceInitialized);
        initialized = true;
    }

    public abstract void Step();
    public abstract bool TryRunUntilStable(int maxSteps, out int steps);

    public int RunUntilStable(int maxSteps = 1024)
    {
        if (!TryRunUntilStable(maxSteps, out int steps))
        {
            throw new InvalidOperationException($"Circuit did not stabilize within {maxSteps} steps.");
        }
        return steps;
    }

    public virtual void SetSource(int gateId, bool value)
    {
        EnsureStorage();
        if ((uint)gateId >= (uint)gateKinds.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(gateId));
        }

        if (gateKinds[gateId] != GateKind.Source)
        {
            throw new InvalidOperationException("Gate is not a source.");
        }

        int bit = value ? 1 : 0;
        if (sourceStates[gateId] != bit || !sourceInitialized[gateId])
        {
            sourceStates[gateId] = bit;
            sourceInitialized[gateId] = true;
            OnSourceChanged(gateId);
        }
    }

    protected virtual void OnSourceChanged(int gateId) { }

    public bool GetOutput(int gateId)
    {
        EnsureStorage();
        return (outputMasks[gateId] & 1) != 0;
    }

    public IDisposable WatchGate(int gateId, Action<int, int> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        if ((uint)gateId >= (uint)gateKinds.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(gateId));
        }

        int id = nextWatcherId++;
        GateWatcherEntry entry = new GateWatcherEntry(id, gateId, callback);
        allWatchers.Add(entry);
        RebuildWatcherCache();
        return new GateWatcherSubscription(this, id);
    }

    private void RemoveWatcher(int id)
    {
        _ = allWatchers.RemoveAll(w => w.Id == id);
        RebuildWatcherCache();
    }

    private void RebuildWatcherCache()
    {
        int n = gateKinds.Count;
        watcherCache = new Action<int, int>[n][];

        IEnumerable<IGrouping<int, GateWatcherEntry>> groups = allWatchers.GroupBy(w => w.GateId);
        List<int> activeGates = [];

        foreach (IGrouping<int, GateWatcherEntry> group in groups)
        {
            watcherCache[group.Key] = [.. group.Select(w => w.Callback)];
            activeGates.Add(group.Key);
        }

        gatesWithWatchers = [.. activeGates];
    }

    protected void NotifyAllWatchers()
    {
        for (int i = 0; i < gatesWithWatchers.Length; i++)
        {
            int gateId = gatesWithWatchers[i];
            Action<int, int>[] callbacks = watcherCache[gateId];
            int val = outputMasks[gateId];
            for (int j = 0; j < callbacks.Length; j++)
            {
                callbacks[j](gateId, val);
            }
        }
    }

    protected void NotifyGateWatchers(int gateId)
    {
        if (gateId >= watcherCache.Length)
        {
            return;
        }

        Action<int, int>[] callbacks = watcherCache[gateId];
        if (callbacks == null)
        {
            return;
        }

        int val = outputMasks[gateId];
        for (int i = 0; i < callbacks.Length; i++)
        {
            callbacks[i](gateId, val);
        }
    }

    protected virtual void EnsureStorage()
    {
        int n = gateKinds.Count;
        if (inputMasks.Length == n)
        {
            return;
        }

        inputMasks = new int[n];
        outputMasks = new int[n];
        sourceStates = new int[n];
        sourceInitialized = new bool[n];
    }

    protected void EnsureCompiled()
    {
        EnsureStorage();
        if (compiled)
        {
            return;
        }

        CompileNetlist();
        CompileLuts();
        CompileEngine();
        compiled = true;
    }

    protected abstract void CompileEngine();

    protected Expression GenerateGateLogic(
        int gateId,
        Expression inMask,
        Expression sourcesParam,
        Expression indexExpr,
        Expression lutDataConst)
    {
        return gateKinds[gateId] switch
        {
            GateKind.Source => Expression.And(Expression.ArrayIndex(sourcesParam, indexExpr), Expression.Constant(1)),
            GateKind.Not => Expression.Condition(Expression.Equal(Expression.And(inMask, Expression.Constant(1)), Expression.Constant(0)), Expression.Constant(1), Expression.Constant(0)),
            GateKind.And2 => Expression.Condition(Expression.Equal(Expression.And(inMask, Expression.Constant(0b11)), Expression.Constant(0b11)), Expression.Constant(1), Expression.Constant(0)),
            GateKind.Or2 => Expression.Condition(Expression.NotEqual(Expression.And(inMask, Expression.Constant(0b11)), Expression.Constant(0)), Expression.Constant(1), Expression.Constant(0)),
            GateKind.Buffer => Expression.Condition(Expression.NotEqual(Expression.And(inMask, Expression.Constant(1)), Expression.Constant(0)), Expression.Constant(1), Expression.Constant(0)),
            GateKind.Sink => Expression.Condition(Expression.NotEqual(Expression.And(inMask, Expression.Constant(1)), Expression.Constant(0)), Expression.Constant(1), Expression.Constant(0)),
            GateKind.Lut => Expression.ArrayIndex(lutDataConst, Expression.Add(Expression.Constant(lutOffsets[gateId]), Expression.And(inMask, Expression.Constant(lutMasks[gateId])))),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private void CompileNetlist()
    {
        int n = gateKinds.Count;
        edgeStart = new int[n + 1];
        foreach ((int FromGate, int _, byte _) in connections)
        {
            edgeStart[FromGate + 1]++;
        }

        for (int i = 1; i < edgeStart.Length; i++)
        {
            edgeStart[i] += edgeStart[i - 1];
        }

        edgeToGate = new int[connections.Count];
        edgeToInputBit = new byte[connections.Count];
        int[] cursor = (int[])edgeStart.Clone();
        foreach ((int FromGate, int ToGate, byte ToInputBit) in connections)
        {
            int at = cursor[FromGate]++;
            edgeToGate[at] = ToGate;
            edgeToInputBit[at] = ToInputBit;
        }
    }

    private void CompileLuts()
    {
        int n = gateKinds.Count;
        lutOffsets = new int[n];
        lutMasks = new int[n];
        int total = 0;
        for (int i = 0; i < n; i++)
        {
            if (gateKinds[i] != GateKind.Lut)
            {
                continue;
            }

            int[] table = lutTableByGate[i]!;
            lutOffsets[i] = total;
            lutMasks[i] = table.Length - 1;
            total += table.Length;
        }
        lutData = new int[total];
        int cursor = 0;
        for (int i = 0; i < n; i++)
        {
            if (gateKinds[i] != GateKind.Lut)
            {
                continue;
            }

            int[] table = lutTableByGate[i]!;
            Array.Copy(table, 0, lutData, cursor, table.Length);
            cursor += table.Length;
        }
    }

    public bool ComputeLut(string name, int maxSteps = 4096)
    {
        if (!macroGates.TryGetValue(name, out MacroInfo? macro))
        {
            throw new KeyNotFoundException();
        }

        MacroLut? lut = TryBuildMacroLut(macro.Definition, maxSteps);
        macroGates[name] = macro with { Lut = lut };
        compiled = false;
        initialized = false;
        return lut is not null;
    }

    protected abstract SimulatorBase CreateInternalSimulator();

    private MacroLut? TryBuildMacroLut(CircuitDefinition definition, int maxSteps)
    {
        int inputCount = definition.InputPins.Count;
        int outputCount = definition.OutputPins.Count;
        if (inputCount < 0 || outputCount <= 0 || inputCount > 30)
        {
            return null;
        }

        int patterns = 1 << inputCount;
        SimulatorBase sim = CreateInternalSimulator();
        foreach (KeyValuePair<string, MacroInfo> pair in macroGates)
        {
            sim.macroGates[pair.Key] = pair.Value;
        }

        int[] map = CopyDefinitionGatesAndConnections(sim, definition, static (_, kind) => kind);
        int[] inGates = MapPins(definition.InputPins, map);
        int[] outGates = MapPins(definition.OutputPins, map);

        int[][] outputTables = new int[outputCount][];
        for (int o = 0; o < outputCount; o++)
        {
            outputTables[o] = new int[patterns];
        }

        for (int pattern = 0; pattern < patterns; pattern++)
        {
            sim.Reset();
            for (int i = 0; i < inputCount; i++)
            {
                sim.SetSource(inGates[i], ((pattern >> i) & 1) != 0);
            }

            if (!sim.TryRunUntilStable(maxSteps, out _))
            {
                return null;
            }

            for (int o = 0; o < outputCount; o++)
            {
                outputTables[o][pattern] = sim.GetOutput(outGates[o]) ? 1 : 0;
            }
        }
        return new MacroLut(inputCount, outputCount, outputTables);
    }

    public MacroInstance AddMacroGate(string name)
    {
        if (!macroGates.TryGetValue(name, out MacroInfo? macro))
        {
            throw new KeyNotFoundException();
        }

        if (macro.Lut is not null)
        {
            return AddMacroGateFromLut(name, macro.Lut);
        }

        int[] map = CopyDefinitionGatesAndConnections(this, macro.Definition, (gateId, kind) =>
            kind == GateKind.Source ? (macro.Definition.InputPins.Contains(gateId) ? GateKind.Buffer : throw new InvalidOperationException()) : kind);

        return new MacroInstance(name, MapPins(macro.Definition.InputPins, map), MapPins(macro.Definition.OutputPins, map));
    }

    private MacroInstance AddMacroGateFromLut(string name, MacroLut lut)
    {
        int[] inputs = new int[lut.InputCount];
        for (int i = 0; i < inputs.Length; i++)
        {
            inputs[i] = AddGate(GateKind.Buffer);
        }

        int[] outputs = new int[lut.OutputCount];
        for (int o = 0; o < outputs.Length; o++)
        {
            int lutGate = AddLutGate(lut.InputCount, lut.OutputTables[o]);
            for (int i = 0; i < inputs.Length; i++)
            {
                ConnectGates(inputs[i], lutGate, i);
            }

            int sink = AddGate(GateKind.Sink);
            ConnectGates(lutGate, sink, 0);
            outputs[o] = sink;
        }
        return new MacroInstance(name, inputs, outputs);
    }

    protected static int[] CopyDefinitionGatesAndConnections(SimulatorBase destination, CircuitDefinition definition, Func<int, GateKind, GateKind> mapKind)
    {
        int gateCount = definition.GateKinds.Count;
        int[] map = new int[gateCount];
        Array.Fill(map, -1);

        for (int i = 0; i < definition.MacroInstances.Count; i++)
        {
            CircuitDefinition.MacroInstanceDef instanceDef = definition.MacroInstances[i];
            MacroInstance instance = destination.AddMacroGate(instanceDef.Name);
            for (int p = 0; p < instanceDef.Inputs.Length; p++)
            {
                map[instanceDef.Inputs[p]] = instance.Inputs[p];
            }

            for (int p = 0; p < instanceDef.Outputs.Length; p++)
            {
                map[instanceDef.Outputs[p]] = instance.Outputs[p];
            }
        }

        for (int i = 0; i < gateCount; i++)
        {
            if (map[i] == -1)
            {
                map[i] = destination.AddGate(mapKind(i, definition.GateKinds[i]));
            }
        }

        foreach ((int FromGate, int ToGate, byte ToInputBit) in definition.Connections)
        {
            destination.ConnectGates(map[FromGate], map[ToGate], ToInputBit);
        }

        return map;
    }

    protected static int[] MapPins(IReadOnlyList<int> pins, int[] map)
    {
        int[] result = new int[pins.Count];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = map[pins[i]];
        }

        return result;
    }

    private sealed class GateWatcherSubscription(SimulatorBase simulator, int id) : IDisposable
    {
        public void Dispose()
        {
            simulator.RemoveWatcher(id);
        }
    }
}
