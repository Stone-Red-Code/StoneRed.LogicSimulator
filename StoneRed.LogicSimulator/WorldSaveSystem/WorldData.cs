using StoneRed.LogicSimulator.Api;
using System.Collections.Generic;

namespace StoneRed.LogicSimulator.WorldSaveSystem;

internal class WorldData
{
    public IEnumerable<LogicGate> LogicGates { get; set; }

    public ushort SaveVersion { get; set; }

    public string SaveName { get; set; }

    public WorldData(IEnumerable<LogicGate> logicGates, ushort saveVersion, string saveName)
    {
        LogicGates = logicGates;
        SaveVersion = saveVersion;
        SaveName = saveName;
    }
}