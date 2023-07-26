using Microsoft.Xna.Framework;

namespace StoneRed.LogicSimulator.Simulation.LogicGates;

internal record LogicGateMetadata
{
    public Vector2 Position { get; set; }

    public Vector2 Size { get; set; } = new Vector2(100, 100);

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}