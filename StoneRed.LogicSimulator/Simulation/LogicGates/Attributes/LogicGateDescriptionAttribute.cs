using System;

namespace StoneRed.LogicSimulator.Simulation.LogicGates.Attributes;

[AttributeUsage(AttributeTargets.All)]
internal class LogicGateDescriptionAttribute : Attribute
{
    public string Description { get; }

    public LogicGateDescriptionAttribute(string description)
    {
        Description = description;
    }
}