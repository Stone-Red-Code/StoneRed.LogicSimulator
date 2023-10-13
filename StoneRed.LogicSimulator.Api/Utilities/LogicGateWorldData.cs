using Microsoft.Xna.Framework;

namespace StoneRed.LogicSimulator.Api.Utilities;

internal record LogicGateWorldData
{
    public Vector2 Position { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}