using Microsoft.Xna.Framework;

using StoneRed.LogicSimulator.Simulation.LogicGates.Attributes;
using StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces;

namespace StoneRed.LogicSimulator.Simulation.LogicGates;

[LogicGateName("Lamp")]
[LogicGateDescription("A lamp is a light source that can be turned on and off.")]
internal class Lamp : LogicGate, IColorable
{
    public override int OutputCount { get; set; } = 0;

    public override int InputCount { get; set; } = 1;

    public Color Color { get; set; } = Color.CadetBlue;

    protected override void Execute()
    {
        Color = GetInputBit(0) == 0 ? Color.CadetBlue : Color.Yellow;
    }
}