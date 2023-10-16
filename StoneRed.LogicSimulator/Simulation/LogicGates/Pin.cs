using StoneRed.LogicSimulator.Api;
using StoneRed.LogicSimulator.Api.Attributes;

namespace StoneRed.LogicSimulator.Simulation.LogicGates;

[LogicGateName("Pin")]
[LogicGateDescription("A pin is a input or output.")]
internal class Pin : LogicGate
{
    public override int OutputCount { get; set; } = 1;

    public override int InputCount { get; set; } = 1;

    protected override void Execute()
    {
        for (int i = 0; i < InputCount; i++)
        {
            SetOutputBit(GetInputBit(i), i);
        }
    }
}