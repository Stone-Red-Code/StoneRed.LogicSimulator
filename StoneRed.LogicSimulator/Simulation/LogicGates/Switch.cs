using Microsoft.Xna.Framework;

using MonoGame.Extended.Input;

using StoneRed.LogicSimulator.Simulation.LogicGates.Attributes;
using StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces;

namespace StoneRed.LogicSimulator.Simulation.LogicGates;

[LogicGateName("Switch")]
[LogicGateDescription("A switch is a toggleable switch that can be turned on and off.")]
internal class Switch : LogicGate, IInteractable, IColorable
{
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
    }

    protected override void Execute()
    {
        if (isPressed)
        {
            SetOutputBit(1, 0);
        }
        else
        {
            SetOutputBit(0, 0);
        }
    }
}