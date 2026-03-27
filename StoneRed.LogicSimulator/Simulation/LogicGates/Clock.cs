using StoneRed.LogicSimulator.Api;
using StoneRed.LogicSimulator.Api.Attributes;

namespace StoneRed.LogicSimulator.Simulation.LogicGates;

[LogicGateName("Clock")]
[LogicGateDescription("A clock is a circuit that oscillates between a high and a low state.")]
internal class Clock : LogicGate
{
    public override int OutputCount { get; set; } = 1;

    public override int InputCount { get; set; } = 0;

    protected internal override void Register(ICircuitSimulator circuitSimulator)
    {
        SimulatorGateId = circuitSimulator.AddGate(GateKind.Buffer);
    }
}
