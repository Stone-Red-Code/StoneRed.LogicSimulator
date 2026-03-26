namespace StoneRed.LogicSimulator.Simulation;

/// <summary>
/// Defines a reusable circuit component that can be registered as a macro gate.
/// Circuit definitions specify gates, connections, input pins, output pins, and nested macro instances.
/// </summary>
public sealed class CircuitDefinition
{
    private readonly List<GateKind> gateKinds = [];
    private readonly List<(int FromGate, int ToGate, byte ToInputBit)> connections = [];
    private readonly List<int> inputPins = [];
    private readonly List<int> outputPins = [];
    private readonly List<MacroInstanceDef> macroInstances = [];

    /// <summary>
    /// Gets the read-only list of gate types in this circuit definition.
    /// </summary>
    public IReadOnlyList<GateKind> GateKinds => gateKinds;

    /// <summary>
    /// Gets the read-only list of connections between gates.
    /// </summary>
    public IReadOnlyList<(int FromGate, int ToGate, byte ToInputBit)> Connections => connections;

    /// <summary>
    /// Gets the read-only list of gate IDs designated as input pins.
    /// </summary>
    public IReadOnlyList<int> InputPins => inputPins;

    /// <summary>
    /// Gets the read-only list of gate IDs designated as output pins.
    /// </summary>
    public IReadOnlyList<int> OutputPins => outputPins;

    /// <summary>
    /// Gets the read-only list of nested macro instances within this definition.
    /// </summary>
    public IReadOnlyList<MacroInstanceDef> MacroInstances => macroInstances;

    /// <summary>
    /// Represents a nested macro gate instance within a circuit definition.
    /// </summary>
    /// <param name="Name">The name of the macro gate to instantiate.</param>
    /// <param name="Inputs">Array of gate IDs representing the input connections.</param>
    /// <param name="Outputs">Array of gate IDs representing the output connections.</param>
    public sealed record MacroInstanceDef(string Name, int[] Inputs, int[] Outputs);

    /// <summary>
    /// Adds a logic gate to the circuit definition.
    /// </summary>
    /// <param name="kind">The type of gate to add.</param>
    /// <returns>The gate ID assigned to the newly created gate.</returns>
    public int AddGate(GateKind kind)
    {
        int id = gateKinds.Count;
        gateKinds.Add(kind);
        return id;
    }

    /// <summary>
    /// Adds a Source gate and marks it as an input pin of this circuit.
    /// </summary>
    /// <returns>The gate ID of the newly created input pin.</returns>
    public int AddInputPin()
    {
        int id = AddGate(GateKind.Source);
        inputPins.Add(id);
        return id;
    }

    /// <summary>
    /// Adds a Sink gate and marks it as an output pin of this circuit.
    /// </summary>
    /// <returns>The gate ID of the newly created output pin.</returns>
    public int AddOutputPin()
    {
        int id = AddGate(GateKind.Sink);
        outputPins.Add(id);
        return id;
    }

    /// <summary>
    /// Adds a nested macro gate instance to the circuit definition.
    /// Creates Buffer gates for inputs and Sink gates for outputs.
    /// </summary>
    /// <param name="name">The name of the macro gate to instantiate.</param>
    /// <param name="inputCount">The number of input pins the macro requires.</param>
    /// <param name="outputCount">The number of output pins the macro provides.</param>
    /// <returns>A <see cref="MacroInstanceDef"/> containing the gate IDs for the instance's pins.</returns>
    /// <exception cref="ArgumentException">Thrown when name is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when inputCount or outputCount is negative.</exception>
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

    /// <summary>
    /// Connects the output of one gate to the input of another gate.
    /// </summary>
    /// <param name="fromGate">The gate ID whose output will be connected.</param>
    /// <param name="toGate">The gate ID that will receive the signal.</param>
    /// <param name="toInputBit">The input bit position (0-31) on the destination gate.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when gate IDs are invalid or toInputBit is not between 0 and 31.</exception>
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

    /// <summary>
    /// Validates the circuit definition for correctness.
    /// Ensures proper gate types, valid connections, and no structural errors.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the circuit definition contains invalid structure or gate usage.</exception>
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
