using Microsoft.Xna.Framework;

using MonoGame.Extended.Input;

using StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces;

namespace StoneRed.LogicSimulator.Simulation.LogicGates;

internal class Button : LogicGate, IInteractable, IColorable
{
    public override int OutputCount => 1;

    public override int InputCount => 0;

    public bool IsPressed { get; set; }
    public Color Color { get; set; } = Color.Purple;
    public string Info { get; set; } = "OFF";

    public Button()
    {
        Metadata.Name = "Button";
    }

    public void OnInteraction(MouseStateExtended mouseState, MouseStateExtended previousMouseState, KeyboardStateExtended keyboardStateExtended)
    {
        IsPressed = mouseState.IsButtonDown(MouseButton.Left);
        Color = IsPressed ? Color.Green : Color.Purple;
        Info = IsPressed ? "ON" : "OFF";
    }

    protected override void Execute()
    {
        if (IsPressed)
        {
            SetOutputBit(1, 0);
        }
        else
        {
            SetOutputBit(0, 0);
        }
    }
}