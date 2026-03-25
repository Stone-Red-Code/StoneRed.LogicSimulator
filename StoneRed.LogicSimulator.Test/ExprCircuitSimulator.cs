using System.Linq.Expressions;

namespace StoneRed.LogicSimulator.Test;

public enum GateKind : byte
{
    Source,
    Not,
    And2,
    Or2,
    Buffer,
    Sink,
    Lut,
}

public sealed class ExprCircuitSimulator
{
    private readonly List<GateKind> gateKinds = [];
    private readonly List<(int FromGate, int ToGate, byte ToInputBit)> connections = [];
    private readonly List<int[]?> lutTableByGate = [];
    private readonly Dictionary<string, MacroInfo> macroGates = new(StringComparer.Ordinal);

    private int[] inputMasks = Array.Empty<int>();
    private int[] nextInputMasks = Array.Empty<int>();
    private int[] outputMasks = Array.Empty<int>();
    private int[] sourceStates = Array.Empty<int>();

    private int[] edgeStart = Array.Empty<int>();
    private int[] edgeToGate = Array.Empty<int>();
    private byte[] edgeToInputBit = Array.Empty<byte>();

    private int[] lutOffsets = Array.Empty<int>();
    private int[] lutMasks = Array.Empty<int>();
    private int[] lutData = Array.Empty<int>();

    private Action<int[], int[], int[]>? computeOutputs;
    private bool compiled;

    public int GateCount => gateKinds.Count;

    public sealed record MacroInstance(string Name, int[] Inputs, int[] Outputs);

    private sealed record MacroLut(int InputCount, int OutputCount, int[][] OutputTables);

    private sealed record MacroInfo(CircuitDefinition Definition, MacroLut? Lut);

    public int AddGate(GateKind kind)
    {
        if (kind == GateKind.Lut)
        {
            throw new InvalidOperationException("Use AddLutGate() to create LUT gates.");
        }

        int id = gateKinds.Count;
        gateKinds.Add(kind);
        lutTableByGate.Add(null);
        compiled = false;
        return id;
    }

    public int AddLutGate(int inputCount, int[] table)
    {
        if (inputCount is < 0 or > 30)
        {
            throw new ArgumentOutOfRangeException(nameof(inputCount), "LUT input count must be between 0 and 30.");
        }

        if (table is null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        int expected = 1 << inputCount;
        if (table.Length != expected)
        {
            throw new ArgumentException($"LUT table length must be {expected} for {inputCount} inputs.", nameof(table));
        }

        int id = gateKinds.Count;
        gateKinds.Add(GateKind.Lut);
        lutTableByGate.Add(table);
        compiled = false;
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
    }

    public void RegisterMacroGate(string name, CircuitDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Macro name must not be empty.", nameof(name));
        }

        definition.Validate();
        macroGates[name] = new MacroInfo(definition, Lut: null);
        compiled = false;
    }

    public bool ComputeLut(string name, int maxSteps = 4096)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Macro name must not be empty.", nameof(name));
        }

        if (maxSteps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSteps));
        }

        if (!macroGates.TryGetValue(name, out MacroInfo? macro))
        {
            throw new KeyNotFoundException($"Macro gate '{name}' is not registered.");
        }

        MacroLut? lut = TryBuildMacroLut(macro.Definition, maxSteps: maxSteps);
        macroGates[name] = macro with { Lut = lut };
        compiled = false;
        return lut is not null;
    }

    public MacroInstance AddMacroGate(string name)
    {
        if (!macroGates.TryGetValue(name, out MacroInfo? macro))
        {
            throw new KeyNotFoundException($"Macro gate '{name}' is not registered.");
        }

        CircuitDefinition definition = macro.Definition;

        if (macro.Lut is not null)
        {
            return AddMacroGateFromLut(name, macro.Lut);
        }

        // Inline (flatten) definition into this simulator by copying its gates and connections.
        // Input pins are represented as Source gates in the definition, but Source gates cannot be driven by wires.
        // For each input pin we substitute a Buffer gate, which can be driven externally and fans out internally.
        int[] map = CopyDefinitionGatesAndConnections(
            destination: this,
            definition: definition,
            mapKind: (gateId, kind) =>
                kind == GateKind.Source
                    ? (definition.InputPins.Contains(gateId) ? GateKind.Buffer : throw new InvalidOperationException("Only Source gates marked as InputPins are allowed inside a macro definition."))
                    : kind);

        int[] inputs = MapPins(definition.InputPins, map);
        int[] outputs = MapPins(definition.OutputPins, map);

        return new MacroInstance(name, inputs, outputs);
    }

    public void ClearSignals()
    {
        EnsureStorage();
        Array.Clear(inputMasks);
        Array.Clear(nextInputMasks);
        Array.Clear(outputMasks);
    }

    public void SetSource(int gateId, bool value)
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

        sourceStates[gateId] = value ? 1 : 0;
    }

    public bool GetOutput(int gateId)
    {
        EnsureStorage();
        if ((uint)gateId >= (uint)gateKinds.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(gateId));
        }

        return (outputMasks[gateId] & 1) != 0;
    }

    public void Step()
    {
        EnsureCompiled();
        computeOutputs!(inputMasks, outputMasks, sourceStates);
        Propagate();
    }

    public int RunUntilStable(int maxSteps = 1024)
    {
        if (!TryRunUntilStable(maxSteps, out int steps))
        {
            throw new InvalidOperationException($"Circuit did not stabilize within {maxSteps} steps.");
        }

        return steps;
    }

    private void Propagate()
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
                int toGate = edgeToGate[e];
                nextInputMasks[toGate] |= 1 << edgeToInputBit[e];
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
                int toGate = edgeToGate[e];
                nextInputMasks[toGate] |= 1 << edgeToInputBit[e];
            }
        }

        bool changed = false;
        for (int i = 0; i < inputMasks.Length; i++)
        {
            if (inputMasks[i] != nextInputMasks[i])
            {
                changed = true;
                break;
            }
        }

        (inputMasks, nextInputMasks) = (nextInputMasks, inputMasks);
        return changed;
    }

    private void EnsureStorage()
    {
        int n = gateKinds.Count;
        if (inputMasks.Length == n)
        {
            return;
        }

        inputMasks = new int[n];
        nextInputMasks = new int[n];
        outputMasks = new int[n];
        sourceStates = new int[n];
    }

    private void EnsureCompiled()
    {
        EnsureStorage();

        if (compiled)
        {
            return;
        }

        CompileNetlist();
        CompileLuts();
        computeOutputs = CompileComputeOutputsExpr();
        compiled = true;
    }

    private void CompileNetlist()
    {
        int n = gateKinds.Count;
        edgeStart = new int[n + 1];

        for (int i = 0; i < connections.Count; i++)
        {
            (int from, _, _) = connections[i];
            edgeStart[from + 1]++;
        }

        for (int i = 1; i < edgeStart.Length; i++)
        {
            edgeStart[i] += edgeStart[i - 1];
        }

        edgeToGate = new int[connections.Count];
        edgeToInputBit = new byte[connections.Count];

        int[] cursor = (int[])edgeStart.Clone();
        for (int i = 0; i < connections.Count; i++)
        {
            (int from, int to, byte bit) = connections[i];
            int at = cursor[from]++;
            edgeToGate[at] = to;
            edgeToInputBit[at] = bit;
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

            int[]? table = lutTableByGate[i];
            if (table is null)
            {
                throw new InvalidOperationException("LUT gate is missing its truth table.");
            }

            if (!IsPowerOfTwo(table.Length))
            {
                throw new InvalidOperationException("LUT table length must be a power of two.");
            }

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

    private Action<int[], int[], int[]> CompileComputeOutputsExpr()
    {
        ParameterExpression inputsParam = Expression.Parameter(typeof(int[]), "inputs");
        ParameterExpression outputsParam = Expression.Parameter(typeof(int[]), "outputs");
        ParameterExpression sourcesParam = Expression.Parameter(typeof(int[]), "sources");

        ConstantExpression lutDataConst = Expression.Constant(lutData);

        Expression[] block = new Expression[gateKinds.Count];

        for (int i = 0; i < gateKinds.Count; i++)
        {
            ConstantExpression idx = Expression.Constant(i);
            Expression inMask = Expression.ArrayIndex(inputsParam, idx);

            Expression outExpr = gateKinds[i] switch
            {
                GateKind.Source => Expression.And(Expression.ArrayIndex(sourcesParam, idx), Expression.Constant(1)),

                GateKind.Not => Expression.Condition(
                    Expression.Equal(Expression.And(inMask, Expression.Constant(1)), Expression.Constant(0)),
                    Expression.Constant(1),
                    Expression.Constant(0)),

                GateKind.And2 => Expression.Condition(
                    Expression.Equal(Expression.And(inMask, Expression.Constant(0b11)), Expression.Constant(0b11)),
                    Expression.Constant(1),
                    Expression.Constant(0)),

                GateKind.Or2 => Expression.Condition(
                    Expression.NotEqual(Expression.And(inMask, Expression.Constant(0b11)), Expression.Constant(0)),
                    Expression.Constant(1),
                    Expression.Constant(0)),

                GateKind.Buffer => Expression.Condition(
                    Expression.NotEqual(Expression.And(inMask, Expression.Constant(1)), Expression.Constant(0)),
                    Expression.Constant(1),
                    Expression.Constant(0)),

                GateKind.Sink => Expression.Condition(
                    Expression.NotEqual(Expression.And(inMask, Expression.Constant(1)), Expression.Constant(0)),
                    Expression.Constant(1),
                    Expression.Constant(0)),

                GateKind.Lut => Expression.ArrayIndex(
                    lutDataConst,
                    Expression.Add(
                        Expression.Constant(lutOffsets[i]),
                        Expression.And(inMask, Expression.Constant(lutMasks[i])))),

                _ => throw new ArgumentOutOfRangeException(),
            };

            block[i] = Expression.Assign(Expression.ArrayAccess(outputsParam, idx), outExpr);
        }

        BlockExpression body = Expression.Block(block);
        return Expression.Lambda<Action<int[], int[], int[]>>(body, inputsParam, outputsParam, sourcesParam).Compile();
    }

    private static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;

    private MacroLut? TryBuildMacroLut(CircuitDefinition definition, int maxSteps)
    {
        int inputCount = definition.InputPins.Count;
        int outputCount = definition.OutputPins.Count;

        if (inputCount < 0 || outputCount <= 0)
        {
            return null;
        }

        if (inputCount > 30)
        {
            throw new InvalidOperationException("Cannot build a LUT for macros with more than 30 inputs (would overflow 32-bit indexing).");
        }

        int patterns = 1 << inputCount;

        var sim = new ExprCircuitSimulator();
        foreach ((string macroName, MacroInfo macroInfo) in macroGates)
        {
            sim.macroGates[macroName] = macroInfo;
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
            sim.ClearSignals();

            for (int i = 0; i < inputCount; i++)
            {
                bool bit = ((pattern >> i) & 1) != 0;
                sim.SetSource(inGates[i], bit);
            }

            if (!sim.TryRunUntilStable(maxSteps: maxSteps, out _))
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
                ConnectGates(inputs[i], lutGate, toInputBit: i);
            }

            int sink = AddGate(GateKind.Sink);
            ConnectGates(lutGate, sink, toInputBit: 0);
            outputs[o] = sink;
        }

        return new MacroInstance(name, inputs, outputs);
    }

    public bool TryRunUntilStable(int maxSteps, out int steps)
    {
        steps = 0;
        if (maxSteps <= 0)
        {
            return false;
        }

        EnsureCompiled();

        while (steps < maxSteps)
        {
            steps++;
            computeOutputs!(inputMasks, outputMasks, sourceStates);
            if (!PropagateAndSwapDetectChange())
            {
                return true;
            }
        }

        return false;
    }

    private static int[] CopyDefinitionGatesAndConnections(
        ExprCircuitSimulator destination,
        CircuitDefinition definition,
        Func<int, GateKind, GateKind> mapKind)
    {
        int gateCount = definition.GateKinds.Count;
        int[] map = new int[gateCount];
        Array.Fill(map, -1);

        // Expand nested macro instances by mapping their placeholder pin gates directly to the instantiated sub-macro pins.
        for (int i = 0; i < definition.MacroInstances.Count; i++)
        {
            CircuitDefinition.MacroInstanceDef instanceDef = definition.MacroInstances[i];
            MacroInstance instance = destination.AddMacroGate(instanceDef.Name);

            if (instance.Inputs.Length != instanceDef.Inputs.Length || instance.Outputs.Length != instanceDef.Outputs.Length)
            {
                throw new InvalidOperationException($"Macro instance '{instanceDef.Name}' pin counts do not match the referenced macro definition.");
            }

            for (int p = 0; p < instanceDef.Inputs.Length; p++)
            {
                int gateId = instanceDef.Inputs[p];
                if (map[gateId] != -1) throw new InvalidOperationException("Macro pin gate was mapped more than once.");
                map[gateId] = instance.Inputs[p];
            }

            for (int p = 0; p < instanceDef.Outputs.Length; p++)
            {
                int gateId = instanceDef.Outputs[p];
                if (map[gateId] != -1) throw new InvalidOperationException("Macro pin gate was mapped more than once.");
                map[gateId] = instance.Outputs[p];
            }
        }

        for (int i = 0; i < gateCount; i++)
        {
            if (map[i] != -1)
            {
                continue;
            }

            map[i] = destination.AddGate(mapKind(i, definition.GateKinds[i]));
        }

        for (int i = 0; i < definition.Connections.Count; i++)
        {
            (int from, int to, byte bit) = definition.Connections[i];
            destination.ConnectGates(map[from], map[to], bit);
        }

        return map;
    }

    private static int[] MapPins(IReadOnlyList<int> pins, int[] map)
    {
        int[] result = new int[pins.Count];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = map[pins[i]];
        }

        return result;
    }
}
