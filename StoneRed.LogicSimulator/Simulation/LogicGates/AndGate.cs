using StoneRed.LogicSimulator.Api;
using StoneRed.LogicSimulator.Api.Attributes;

namespace StoneRed.LogicSimulator.Simulation.LogicGates;

[LogicGateName("And Gate")]
[LogicGateDescription("A and gate is a gate that returns true if both inputs are true.")]
internal class AndGate : LogicGate
{
    public override int InputCount { get; set; } = 2;

    public override int OutputCount { get; set; } = 1;

    protected override void Execute()
    {
        SetOutputBit(GetInputBit(0) == 1 && GetInputBit(1) == 1 ? 1 : 0, 0);
    }
}