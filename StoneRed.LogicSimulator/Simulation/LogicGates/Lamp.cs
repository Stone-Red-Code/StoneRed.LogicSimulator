using Microsoft.Xna.Framework;
using StoneRed.LogicSimulator.Api;
using StoneRed.LogicSimulator.Api.Attributes;
using StoneRed.LogicSimulator.Api.Interfaces;

namespace StoneRed.LogicSimulator.Simulation.LogicGates;

[LogicGateName("Lamp")]
[LogicGateDescription("A lamp is a light source that can be turned on and off.")]
internal class Lamp : LogicGate, IColorable
{
    public override int OutputCount { get; set; } = 0;

    public override int InputCount { get; set; } = 1;

    public Color Color { get; set; } = Color.CadetBlue;

    protected internal override void Register(ICircuitSimulator circuitSimulator)
    {
        SimulatorGateId = circuitSimulator.AddGate(GateKind.Sink);
        
        // Watch input to update lamp color
        circuitSimulator.WatchGate(SimulatorGateId, (oldMask, newMask) =>
        {
            bool isOn = (newMask & 1) != 0;
            Color = isOn ? Color.Yellow : Color.CadetBlue;
        });
    }
}