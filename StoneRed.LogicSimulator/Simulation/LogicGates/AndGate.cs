using StoneRed.LogicSimulator.Api;
using StoneRed.LogicSimulator.Api.Attributes;

namespace StoneRed.LogicSimulator.Simulation.LogicGates;

[LogicGateName("And Gate")]
[LogicGateDescription("A and gate is a gate that returns true if both inputs are true.")]
internal class AndGate : LogicGate
{
    public override int InputCount { get; set; } = 2;

    public override int OutputCount { get; set; } = 1;

    protected internal override void Register(ICircuitSimulator circuitSimulator)
    {
        SimulatorGateId = circuitSimulator.AddGate(GateKind.And2);
    }
}