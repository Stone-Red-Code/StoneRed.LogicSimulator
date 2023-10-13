namespace StoneRed.LogicSimulator.Api.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class LogicGateNameAttribute : Attribute
{
    public string Name { get; }

    public LogicGateNameAttribute(string name)
    {
        Name = name;
    }
}