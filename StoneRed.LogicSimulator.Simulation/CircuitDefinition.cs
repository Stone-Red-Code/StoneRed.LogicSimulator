namespace StoneRed.LogicSimulator.Simulation;

public sealed class CircuitDefinition
{
    private readonly List<GateKind> gateKinds = [];
    private readonly List<(int FromGate, int ToGate, byte ToInputBit)> connections = [];
    private readonly List<int> inputPins = [];
    private readonly List<int> outputPins = [];
    private readonly List<MacroInstanceDef> macroInstances = [];

    public IReadOnlyList<GateKind> GateKinds => gateKinds;
    public IReadOnlyList<(int FromGate, int ToGate, byte ToInputBit)> Connections => connections;
    public IReadOnlyList<int> InputPins => inputPins;
    public IReadOnlyList<int> OutputPins => outputPins;
    public IReadOnlyList<MacroInstanceDef> MacroInstances => macroInstances;

    public sealed record MacroInstanceDef(string Name, int[] Inputs, int[] Outputs);

    public int AddGate(GateKind kind)
    {
        int id = gateKinds.Count;
        gateKinds.Add(kind);
        return id;
    }

    public int AddInputPin()
    {
        int id = AddGate(GateKind.Source);
        inputPins.Add(id);
        return id;
    }

    public int AddOutputPin()
    {
        int id = AddGate(GateKind.Sink);
        outputPins.Add(id);
        return id;
    }

    public MacroInstanceDef AddMacroInstance(string name, int inputCount, int outputCount)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Macro name must not be empty.", nameof(name));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(inputCount);

        ArgumentOutOfRangeException.ThrowIfNegative(outputCount);

        int[] inputs = new int[inputCount];
        for (int i = 0; i < inputCount; i++)
        {
            inputs[i] = AddGate(GateKind.Buffer);
        }

        int[] outputs = new int[outputCount];
        for (int i = 0; i < outputCount; i++)
        {
            outputs[i] = AddGate(GateKind.Sink);
        }

        MacroInstanceDef instance = new MacroInstanceDef(name, inputs, outputs);
        macroInstances.Add(instance);
        return instance;
    }

    public void Connect(int fromGate, int toGate, int toInputBit)
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
    }

    public void Validate()
    {
        bool[] hasIncoming = new bool[gateKinds.Count];
        for (int i = 0; i < connections.Count; i++)
        {
            (_, int to, _) = connections[i];
            hasIncoming[to] = true;
        }

        HashSet<int> inputPinSet = [.. inputPins];
        for (int i = 0; i < gateKinds.Count; i++)
        {
            if (gateKinds[i] == GateKind.Source && !inputPinSet.Contains(i))
            {
                throw new InvalidOperationException("Only Source gates marked as InputPins are allowed inside a circuit definition.");
            }

            if (gateKinds[i] == GateKind.Lut)
            {
                throw new InvalidOperationException("Circuit definitions must not contain LUT gates.");
            }
        }

        HashSet<int> macroPinGates = [];
        foreach (MacroInstanceDef instance in macroInstances)
        {
            if (string.IsNullOrWhiteSpace(instance.Name))
            {
                throw new InvalidOperationException("Macro instance name must not be empty.");
            }

            foreach (int pin in instance.Inputs)
            {
                if ((uint)pin >= (uint)gateKinds.Count)
                {
                    throw new InvalidOperationException("Macro instance input pin is out of range.");
                }

                if (gateKinds[pin] != GateKind.Buffer)
                {
                    throw new InvalidOperationException("Macro instance inputs must be Buffer gates.");
                }

                if (!macroPinGates.Add(pin))
                {
                    throw new InvalidOperationException("Macro instance pin is used more than once.");
                }
            }

            foreach (int pin in instance.Outputs)
            {
                if ((uint)pin >= (uint)gateKinds.Count)
                {
                    throw new InvalidOperationException("Macro instance output pin is out of range.");
                }

                if (gateKinds[pin] != GateKind.Sink)
                {
                    throw new InvalidOperationException("Macro instance outputs must be Sink gates.");
                }

                if (!macroPinGates.Add(pin))
                {
                    throw new InvalidOperationException("Macro instance pin is used more than once.");
                }
            }
        }

        foreach (int pin in macroPinGates)
        {
            if (inputPinSet.Contains(pin) || outputPins.Contains(pin))
            {
                throw new InvalidOperationException("Macro instance pins must not be listed as InputPins/OutputPins.");
            }
        }

        foreach (int input in inputPins)
        {
            if (gateKinds[input] != GateKind.Source)
            {
                throw new InvalidOperationException("Input pins must be Source gates.");
            }

            if (hasIncoming[input])
            {
                throw new InvalidOperationException("Input pins must not have incoming connections.");
            }
        }

        foreach (int output in outputPins)
        {
            if (gateKinds[output] != GateKind.Sink)
            {
                throw new InvalidOperationException("Output pins must be Sink gates.");
            }
        }

        if (inputPins.Distinct().Count() != inputPins.Count)
        {
            throw new InvalidOperationException("Input pins contain duplicates.");
        }

        if (outputPins.Distinct().Count() != outputPins.Count)
        {
            throw new InvalidOperationException("Output pins contain duplicates.");
        }
    }
}
