namespace StoneRed.LogicSimulator.Simulation;

/// <summary>
/// Represents an instance of a macro gate added to the circuit.
/// Contains the gate IDs of the instance's input and output pins.
/// </summary>
/// <param name="Name">The name of the macro gate definition.</param>
/// <param name="Inputs">Array of gate IDs representing the input pins of this instance.</param>
/// <param name="Outputs">Array of gate IDs representing the output pins of this instance.</param>
public sealed record MacroInstance(string Name, int[] Inputs, int[] Outputs);
