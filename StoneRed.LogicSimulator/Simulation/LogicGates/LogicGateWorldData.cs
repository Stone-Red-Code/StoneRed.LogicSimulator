using Microsoft.Xna.Framework;

namespace StoneRed.LogicSimulator.Simulation.LogicGates;

internal record LogicGateWorldData
{
    public Vector2 Position { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}