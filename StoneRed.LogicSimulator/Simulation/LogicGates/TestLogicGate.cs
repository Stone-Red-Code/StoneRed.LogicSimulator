#if DEBUG

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StoneRed.LogicSimulator.Api;
using StoneRed.LogicSimulator.Api.Attributes;

using System.Linq;

namespace StoneRed.LogicSimulator.Simulation.LogicGates;

[LogicGateName("Test")]
internal class TestLogicGate : LogicGate, Api.Interfaces.IDrawable
{
    private Color[] colors = new Color[15 * 15];
    private Texture2D texture = null!;
    public override int OutputCount { get; set; } = 0;
    public override int InputCount { get; set; } = 7;

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
        colors = Enumerable.Repeat(Color.Gray, 15 * 15).ToArray();
        texture = CreateTexture(15, 15);
    }

    protected override void Execute()
    {
        // A
        SetRangeWithSpacing(20, 24, 1, GetInputBit(0) == 1 ? Color.Red : Color.Gray);

        // B
        SetRangeWithSpacing(40, 100, 15, GetInputBit(1) == 1 ? Color.Red : Color.Gray);

        // C
        SetRangeWithSpacing(130, 190, 15, GetInputBit(2) == 1 ? Color.Red : Color.Gray);

        // D
        SetRangeWithSpacing(200, 204, 1, GetInputBit(3) == 1 ? Color.Red : Color.Gray);

        // E
        SetRangeWithSpacing(124, 184, 15, GetInputBit(4) == 1 ? Color.Red : Color.Gray);

        // F
        SetRangeWithSpacing(34, 94, 15, GetInputBit(5) == 1 ? Color.Red : Color.Gray);

        // G
        SetRangeWithSpacing(110, 114, 1, GetInputBit(6) == 1 ? Color.Red : Color.Gray);
    }

    private void SetRangeWithSpacing(int start, int end, int spacing, Color value)
    {
        for (int i = start; i <= end; i += spacing)
        {
            colors[i] = value;
        }
    }
}

#endif