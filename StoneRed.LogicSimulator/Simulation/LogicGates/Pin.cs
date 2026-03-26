using StoneRed.LogicSimulator.Api;
using StoneRed.LogicSimulator.Api.Attributes;

namespace StoneRed.LogicSimulator.Simulation.LogicGates;

[LogicGateName("Pin")]
[LogicGateDescription("A pin is a input or output.")]
internal class Pin : LogicGate
{
    public override int OutputCount { get; set; } = 1;

    public override int InputCount { get; set; } = 1;

    protected internal override void Register(ICircuitSimulator circuitSimulator)
    {
        SimulatorGateId = circuitSimulator.AddGate(GateKind.Buffer);
    }
}