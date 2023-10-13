using MonoGame.Extended.Input;

namespace StoneRed.LogicSimulator.Api.Interfaces;

internal interface IInteractable
{
    public string Info { get; }

    public void OnInteraction(MouseStateExtended mouseState, MouseStateExtended previousMouseState, KeyboardStateExtended keyboardStateExtended);
}