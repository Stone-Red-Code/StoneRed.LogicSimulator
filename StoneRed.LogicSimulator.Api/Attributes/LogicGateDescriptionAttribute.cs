namespace StoneRed.LogicSimulator.Api.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class LogicGateDescriptionAttribute : Attribute
{
    public string Description { get; }

    public LogicGateDescriptionAttribute(string description)
    {
        Description = description;
    }
}