using StoneRed.LogicSimulator.Simulation.LogicGates.Attributes;
using StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces;

namespace StoneRed.LogicSimulator.Simulation.LogicGates;

[LogicGateName("Not Gate")]
[LogicGateDescription("A not gate is a gate that inverts the input.")]
internal class NotGate : LogicGate
{
    public override int InputCount { get; set; } = 1;

    public override int OutputCount { get; set; } = 1;

    protected override void Execute()
    {
        SetOutputBit(GetInputBit(0) == 1 ? 0 : 1, 0);
    }
}