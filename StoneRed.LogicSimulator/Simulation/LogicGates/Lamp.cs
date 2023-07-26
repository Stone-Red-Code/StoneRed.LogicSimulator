using Microsoft.Xna.Framework;

using StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces;

namespace StoneRed.LogicSimulator.Simulation.LogicGates;

internal class Lamp : LogicGate, IColorable
{
    public override int OutputCount => 0;

    public override int InputCount => 1;

    public Color Color { get; set; } = Color.CadetBlue;

    protected override void Execute()
    {
        Color = GetInputBit(0) == 0 ? Color.CadetBlue : Color.Yellow;
    }
}