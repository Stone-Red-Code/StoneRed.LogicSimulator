using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StoneRed.LogicSimulator.Simulation.LogicGates.Attributes;
using StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces;

namespace StoneRed.LogicSimulator.Simulation.LogicGates;

#if DEBUG

[LogicGateName("Test")]
internal class TestLogicGate : LogicGate, Interfaces.IDrawable
{
    private readonly Color[] colors = new Color[9];
    private Texture2D texture = null!;
    public override int OutputCount { get; set; } = 0;
    public override int InputCount { get; set; } = 9;

    public Texture2D Texture
    {
        get
        {
            texture.SetData(colors);
            return texture;
        }
    }

    protected internal override void Initialize()
    {
        texture = CreateTexture(3, 3);
    }

    protected override void Execute()
    {
        for (int i = 0; i < InputCount; i++)
        {
            colors[i] = GetInputBit(i) == 1 ? Color.Red : Color.Gray;
        }
    }
}

#endif