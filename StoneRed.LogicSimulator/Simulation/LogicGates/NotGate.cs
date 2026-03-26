using StoneRed.LogicSimulator.Api;
using StoneRed.LogicSimulator.Api.Attributes;

namespace StoneRed.LogicSimulator.Simulation.LogicGates;

[LogicGateName("Not Gate")]
[LogicGateDescription("A not gate is a gate that inverts the input.")]
internal class NotGate : LogicGate
{
    public override int InputCount { get; set; } = 1;

    public override int OutputCount { get; set; } = 1;

    protected internal override void Register(ICircuitSimulator circuitSimulator)
    {
        SimulatorGateId = circuitSimulator.AddGate(GateKind.Not);
    }
}