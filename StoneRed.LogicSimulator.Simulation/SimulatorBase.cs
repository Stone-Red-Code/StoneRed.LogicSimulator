using System.Linq.Expressions;

namespace StoneRed.LogicSimulator.Simulation;

/// <summary>
/// Abstract base class providing common functionality for circuit simulator implementations.
/// Handles gate storage, connections, macro gates, LUT compilation, and gate watching.
/// </summary>
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
    protected bool hasAnyWatchers;
    protected int nextWatcherId;

    /// <summary>
    /// Internal record representing a gate watcher subscription.
    /// </summary>
    /// <param name="Id">Unique identifier for this watcher.</param>
    /// <param name="GateId">The gate being watched.</param>
    /// <param name="Callback">The callback to invoke on changes.</param>
    protected sealed record GateWatcherEntry(int Id, int GateId, Action<int, int> Callback);

    /// <summary>
    /// Internal record representing a compiled LUT for a macro gate.
    /// </summary>
    /// <param name="InputCount">Number of input pins.</param>
    /// <param name="OutputCount">Number of output pins.</param>
    /// <param name="OutputTables">Truth tables for each output (indexed by input pattern).</param>
    protected sealed record MacroLut(int InputCount, int OutputCount, int[][] OutputTables);

    /// <summary>
    /// Internal record storing macro gate definition and optional compiled LUT.
    /// </summary>
    /// <param name="Definition">The circuit definition of the macro.</param>
    /// <param name="Lut">Optional compiled LUT representation for optimization.</param>
    protected sealed record MacroInfo(CircuitDefinition Definition, MacroLut? Lut);

    /// <inheritdoc/>
    public int GateCount => gateKinds.Count;

    /// <inheritdoc/>
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

    /// <summary>
    /// Called when a gate is added. Override to perform implementation-specific initialization.
    /// </summary>
    /// <param name="gateId">The ID of the newly added gate.</param>
    protected virtual void OnGateAdded(int gateId) { }

    /// <inheritdoc/>
    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public void RegisterMacroGate(string name, CircuitDefinition definition)
    {
        definition.Validate();
        macroGates[name] = new MacroInfo(definition, Lut: null);
        compiled = false;
        initialized = false;
    }

    /// <inheritdoc/>
    public virtual void Reset()
    {
        EnsureStorage();
        Array.Clear(inputMasks);
        Array.Clear(outputMasks);
        Array.Clear(sourceInitialized);
        initialized = true;
    }

    /// <inheritdoc/>
    public abstract void Step();

    /// <inheritdoc/>
    public abstract bool TryRunUntilStable(int maxSteps, out int steps);

    /// <inheritdoc/>
    public int RunUntilStable(int maxSteps = 1024)
    {
        if (!TryRunUntilStable(maxSteps, out int steps))
        {
            throw new InvalidOperationException($"Circuit did not stabilize within {maxSteps} steps.");
        }
        return steps;
    }

    /// <inheritdoc/>
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

    /// <summary>
    /// Called when a source gate's value changes. Override to perform implementation-specific handling.
    /// </summary>
    /// <param name="gateId">The ID of the source gate that changed.</param>
    protected virtual void OnSourceChanged(int gateId) { }

    /// <inheritdoc/>
    public bool GetOutput(int gateId)
    {
        EnsureStorage();
        return (outputMasks[gateId] & 1) != 0;
    }

    /// <inheritdoc/>
    /// <inheritdoc/>
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
        compiled = false;
        return new GateWatcherSubscription(this, id);
    }

    private void RemoveWatcher(int id)
    {
        _ = allWatchers.RemoveAll(w => w.Id == id);
        RebuildWatcherCache();
        compiled = false;
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
        hasAnyWatchers = allWatchers.Count > 0;
    }

    /// <summary>
    /// Notifies all registered watchers of gates that changed between the previous and current output states.
    /// </summary>
    /// <param name="previousOutputMasks">The output states from before the change.</param>
    protected void NotifyAllWatchers(int[] previousOutputMasks)
    {
        for (int i = 0; i < gatesWithWatchers.Length; i++)
        {
            int gateId = gatesWithWatchers[i];
            if (outputMasks[gateId] != previousOutputMasks[gateId])
            {
                Action<int, int>[] callbacks = watcherCache[gateId];
                int val = outputMasks[gateId];
                for (int j = 0; j < callbacks.Length; j++)
                {
                    callbacks[j](gateId, val);
                }
            }
        }
    }

    /// <summary>
    /// Notifies watchers of a specific gate that its output has changed.
    /// </summary>
    /// <param name="gateId">The ID of the gate that changed.</param>
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

    /// <summary>
    /// Ensures that internal storage arrays are allocated and sized correctly for the current gate count.
    /// </summary>
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

    /// <summary>
    /// Ensures the simulator is compiled (netlist, LUTs, and engine are ready).
    /// Triggers compilation if not already done.
    /// </summary>
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

    /// <summary>
    /// Compiles the simulation engine. Implemented by derived classes to build their specific evaluation logic.
    /// </summary>
    protected abstract void CompileEngine();

    /// <summary>
    /// Generates the expression tree for evaluating a single gate's logic.
    /// Used during compilation to build gate evaluators.
    /// </summary>
    /// <param name="gateId">The ID of the gate to generate logic for.</param>
    /// <param name="inMask">Expression representing the gate's input mask.</param>
    /// <param name="sourcesParam">Expression representing the source states array.</param>
    /// <param name="indexExpr">Expression representing the gate index.</param>
    /// <param name="lutDataConst">Expression representing the LUT data array.</param>
    /// <returns>An expression that evaluates to the gate's output value.</returns>
    /// <summary>
    /// Generates the expression tree for evaluating a single gate's logic.
    /// Used during compilation to build gate evaluators.
    /// </summary>
    /// <param name="gateId">The ID of the gate to generate logic for.</param>
    /// <param name="inMask">Expression representing the gate's input mask.</param>
    /// <param name="sourcesParam">Expression representing the source states array.</param>
    /// <param name="indexExpr">Expression representing the gate index.</param>
    /// <param name="lutDataConst">Expression representing the LUT data array.</param>
    /// <returns>An expression that evaluates to the gate's output value.</returns>
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
            _ => throw new InvalidOperationException($"Unknown gate kind: {gateKinds[gateId]}")
        };
    }

    /// <summary>
    /// Compiles the connection netlist into optimized adjacency list structures for fast propagation.
    /// Creates edgeStart, edgeToGate, and edgeToInputBit arrays.
    /// </summary>
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

    /// <summary>
    /// Compiles LUT gate data into flat arrays for efficient lookup during simulation.
    /// Creates lutOffsets, lutMasks, and lutData arrays.
    /// </summary>
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

    /// <inheritdoc/>
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

    /// <summary>
    /// Creates an internal simulator instance for LUT computation or other internal operations.
    /// Implemented by derived classes to return the appropriate simulator type.
    /// </summary>
    /// <returns>A new simulator instance of the same type as the current implementation.</returns>
    protected abstract SimulatorBase CreateInternalSimulator();

    /// <summary>
    /// Attempts to build a LUT representation of a macro gate by simulating all input patterns.
    /// </summary>
    /// <param name="definition">The circuit definition to convert to LUT.</param>
    /// <param name="maxSteps">Maximum steps per pattern simulation.</param>
    /// <returns>A MacroLut if successful; null if the circuit didn't stabilize for any pattern.</returns>
    /// <summary>
    /// Attempts to build a LUT representation of a macro gate by simulating all input patterns.
    /// </summary>
    /// <param name="definition">The circuit definition to convert to LUT.</param>
    /// <param name="maxSteps">Maximum steps per pattern simulation.</param>
    /// <returns>A MacroLut if successful; null if the circuit didn't stabilize for any pattern.</returns>
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

    /// <inheritdoc/>
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

    /// <summary>
    /// Adds a macro gate instance using its pre-computed LUT representation.
    /// Creates buffer gates for inputs and LUT gates for each output.
    /// </summary>
    /// <param name="name">The name of the macro gate.</param>
    /// <param name="lut">The compiled LUT data.</param>
    /// <returns>A MacroInstance with the input and output gate IDs.</returns>
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

    /// <summary>
    /// Copies gates and connections from a circuit definition to a simulator instance.
    /// Handles macro instances recursively and maps gate IDs appropriately.
    /// </summary>
    /// <param name="destination">The simulator to copy gates and connections to.</param>
    /// <param name="definition">The circuit definition to copy from.</param>
    /// <param name="mapKind">Function to transform gate kinds during copying (e.g., Source to Buffer).</param>
    /// <returns>An array mapping original gate IDs to new gate IDs in the destination.</returns>
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

    /// <summary>
    /// Maps a list of pin IDs from one gate ID space to another using a mapping array.
    /// </summary>
    /// <param name="pins">The original pin IDs.</param>
    /// <param name="map">The ID mapping array.</param>
    /// <returns>An array of mapped pin IDs.</returns>
    protected static int[] MapPins(IReadOnlyList<int> pins, int[] map)
    {
        int[] result = new int[pins.Count];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = map[pins[i]];
        }

        return result;
    }

    /// <summary>
    /// Internal class implementing IDisposable for gate watcher unsubscription.
    /// </summary>
    private sealed class GateWatcherSubscription(SimulatorBase simulator, int id) : IDisposable
    {
        public void Dispose()
        {
            simulator.RemoveWatcher(id);
        }
    }
}
