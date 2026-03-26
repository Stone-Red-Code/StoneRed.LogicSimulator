using Microsoft.Xna.Framework;

using MonoGame.Extended.Input;

using StoneRed.LogicSimulator.Api;
using StoneRed.LogicSimulator.Api.Attributes;
using StoneRed.LogicSimulator.Api.Interfaces;

namespace StoneRed.LogicSimulator.Simulation.LogicGates;

[LogicGateName("Switch")]
[LogicGateDescription("A switch is a toggleable switch that can be turned on and off.")]
internal class Switch : LogicGate, IInteractable, IColorable
{
    private ICircuitSimulator? circuitSimulator;
    private int gateId;
    private bool isPressed;
    public override int OutputCount { get; set; } = 1;

    public override int InputCount { get; set; } = 0;
    public Color Color { get; set; } = Color.Purple;
    public string Info { get; set; } = "OFF";

    public void OnInteraction(MouseStateExtended mouseState, MouseStateExtended previousMouseState, KeyboardStateExtended keyboardStateExtended)
    {
        if (mouseState.IsButtonDown(MouseButton.Left) && !previousMouseState.IsButtonDown(MouseButton.Left))
        {
            isPressed = !isPressed;
        }

        Color = isPressed ? Color.Green : Color.Purple;
        Info = isPressed ? "ON" : "OFF";

        circuitSimulator?.SetSource(gateId, isPressed);
    }

    protected internal override void Register(ICircuitSimulator circuitSimulator)
    {
        this.circuitSimulator = circuitSimulator;

        SimulatorGateId = circuitSimulator.AddGate(GateKind.Source);
        gateId = SimulatorGateId;
    }
}