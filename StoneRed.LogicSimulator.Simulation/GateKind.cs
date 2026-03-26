namespace StoneRed.LogicSimulator.Simulation;

/// <summary>
/// Defines the types of logic gates available in the circuit simulator.
/// </summary>
public enum GateKind : byte
{
    /// <summary>
    /// An input gate that can have its value set externally via <see cref="ICircuitSimulator.SetSource"/>.
    /// Used as circuit inputs or constant values.
    /// </summary>
    Source,

    /// <summary>
    /// A NOT gate that inverts its input (0 → 1, 1 → 0).
    /// </summary>
    Not,

    /// <summary>
    /// A 2-input AND gate. Output is 1 only when both inputs are 1.
    /// </summary>
    And2,

    /// <summary>
    /// A 2-input OR gate. Output is 1 when at least one input is 1.
    /// </summary>
    Or2,

    /// <summary>
    /// A buffer gate that passes its input directly to the output.
    /// Used for signal routing or as macro gate input pins.
    /// </summary>
    Buffer,

    /// <summary>
    /// A sink gate that receives input but produces no useful output.
    /// Used as circuit outputs or macro gate output pins.
    /// </summary>
    Sink,

    /// <summary>
    /// A Look-Up Table gate implementing arbitrary combinational logic via a truth table.
    /// Created using <see cref="ICircuitSimulator.AddLutGate"/> instead of <see cref="ICircuitSimulator.AddGate"/>.
    /// </summary>
    Lut,
}
