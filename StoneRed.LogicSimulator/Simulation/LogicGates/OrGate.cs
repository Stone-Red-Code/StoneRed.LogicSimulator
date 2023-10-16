using StoneRed.LogicSimulator.Api;
using StoneRed.LogicSimulator.Api.Attributes;

namespace StoneRed.LogicSimulator.Simulation.LogicGates;

[LogicGateName("Or Gate")]
[LogicGateDescription("A or gate is a gate that returns true if at least one of the inputs is true.")]
internal class OrGate : LogicGate
{
    public override int InputCount { get; set; } = 1;

    public override int OutputCount { get; set; } = 1;

    protected override void Execute()
    {
        SetOutputBit(GetInputBit(0), 0);
    }
}