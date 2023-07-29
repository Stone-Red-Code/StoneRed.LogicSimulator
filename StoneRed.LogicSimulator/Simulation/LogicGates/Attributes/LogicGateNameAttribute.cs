using System;

namespace StoneRed.LogicSimulator.Simulation.LogicGates.Attributes;

[AttributeUsage(AttributeTargets.Class)]
internal class LogicGateNameAttribute : Attribute
{
    public string Name { get; }

    public LogicGateNameAttribute(string name)
    {
        Name = name;
    }
}