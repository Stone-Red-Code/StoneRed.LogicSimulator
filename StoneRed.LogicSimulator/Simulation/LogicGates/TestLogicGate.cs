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

    protected internal override void Register(ICircuitSimulator circuitSimulator)
    {
        SimulatorGateId = circuitSimulator.AddGate(GateKind.Sink);
        
        // Watch all 7 inputs to update display
        circuitSimulator.WatchGate(SimulatorGateId, (oldMask, newMask) =>
        {
            // Extract each bit from the input mask
            bool inputA = (newMask & (1 << 0)) != 0;
            bool inputB = (newMask & (1 << 1)) != 0;
            bool inputC = (newMask & (1 << 2)) != 0;
            bool inputD = (newMask & (1 << 3)) != 0;
            bool inputE = (newMask & (1 << 4)) != 0;
            bool inputF = (newMask & (1 << 5)) != 0;
            bool inputG = (newMask & (1 << 6)) != 0;
            
            // Update 7-segment display
            // A
            SetRangeWithSpacing(20, 24, 1, inputA ? Color.Red : Color.Gray);
            // B
            SetRangeWithSpacing(40, 100, 15, inputB ? Color.Red : Color.Gray);
            // C
            SetRangeWithSpacing(130, 190, 15, inputC ? Color.Red : Color.Gray);
            // D
            SetRangeWithSpacing(200, 204, 1, inputD ? Color.Red : Color.Gray);
            // E
            SetRangeWithSpacing(124, 184, 15, inputE ? Color.Red : Color.Gray);
            // F
            SetRangeWithSpacing(34, 94, 15, inputF ? Color.Red : Color.Gray);
            // G
            SetRangeWithSpacing(110, 114, 1, inputG ? Color.Red : Color.Gray);
        });
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