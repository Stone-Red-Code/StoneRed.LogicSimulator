using Microsoft.Xna.Framework;

using MonoGame.Extended.Input;
using StoneRed.LogicSimulator.Api;
using StoneRed.LogicSimulator.Api.Attributes;
using StoneRed.LogicSimulator.Api.Interfaces;

namespace StoneRed.LogicSimulator.Simulation.LogicGates;

[LogicGateName("Button")]
[LogicGateDescription("A button is a momentary switch that can be turned on and off.")]
internal class Button : LogicGate, IInteractable, IColorable
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
        isPressed = mouseState.IsButtonDown(MouseButton.Left);
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