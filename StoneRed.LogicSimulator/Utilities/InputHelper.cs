using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using MonoGame.Extended.Input;

namespace StoneRed.LogicSimulator.Utilities;

internal static class InputHelper
{
    public static Vector2 GetMovementDirection(this KeyboardStateExtended keyboardState)
    {
        Vector2 movementDirection = Vector2.Zero;
        if (keyboardState.IsKeyDown(Keys.S))
        {
            movementDirection += Vector2.UnitY;
        }
        if (keyboardState.IsKeyDown(Keys.W))
        {
            movementDirection -= Vector2.UnitY;
        }
        if (keyboardState.IsKeyDown(Keys.A))
        {
            movementDirection -= Vector2.UnitX;
        }
        if (keyboardState.IsKeyDown(Keys.D))
        {
            movementDirection += Vector2.UnitX;
        }
        return movementDirection;
    }
}