using StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces;

using System.Collections.Generic;

namespace StoneRed.LogicSimulator.WorldSaveSystem;

internal class WorldData
{
    public IEnumerable<LogicGate> LogicGates { get; set; }

    public int SaveVersion { get; set; }

    public string SaveName { get; set; }

    public WorldData(IEnumerable<LogicGate> logicGates, int saveVersion, string saveName)
    {
        LogicGates = logicGates;
        SaveVersion = saveVersion;
        SaveName = saveName;
    }
}