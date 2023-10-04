using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StoneRed.LogicSimulator.Simulation.LogicGates.Attributes;
using StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces;

namespace StoneRed.LogicSimulator.Simulation.LogicGates;

#if DEBUG

[LogicGateName("Test")]
internal class TestLogicGate : LogicGate, Interfaces.IDrawable
{
    public override int OutputCount { get; set; } = 1;
    public override int InputCount { get; set; } = 1;

    public Texture2D Texture => CreateTexture(1, 1);

    protected override void Execute()
    {
        Texture.SetData(new Color[] { Color.Red });

        for (int i = 0; i < InputCount; i++)
        {
            SetOutputBit(GetInputBit(i), i);
        }
    }
}
#endif