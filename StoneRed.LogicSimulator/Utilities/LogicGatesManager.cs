using StoneRed.LogicSimulator.Simulation.LogicGates.Attributes;
using StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace StoneRed.LogicSimulator.Utilities;

internal class LogicGatesManager
{
    private readonly Dictionary<LogicGateInfo, Type> logicGates = new Dictionary<LogicGateInfo, Type>();

    public void LoadLogicGates()
    {
        IEnumerable<Type> logicGateTypes = typeof(LogicGate)
            .Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(LogicGate)) && !t.IsAbstract);

        foreach (Type type in logicGateTypes)
        {
            LogicGateDescriptionAttribute? descriptionAttribute = type.GetCustomAttributes(typeof(LogicGateDescriptionAttribute), false).FirstOrDefault() as LogicGateDescriptionAttribute;
            if (type.GetCustomAttributes(typeof(LogicGateNameAttribute), false).FirstOrDefault() is LogicGateNameAttribute nameAttribute)
            {
                LogicGateInfo logicGateInfo = new LogicGateInfo(nameAttribute.Name, descriptionAttribute?.Description);

                if (logicGates.ContainsKey(logicGateInfo))
                {
                    logicGateInfo.TypeName += "#";
                }

                logicGates.Add(logicGateInfo, type);
            }
        }
    }

    public IEnumerable<LogicGateInfo> GetNativeLogicGatesInfos()
    {
        return logicGates.Keys;
    }

    public LogicGate CreateLogicGate(string typeName)
    {
        Type type = logicGates[new LogicGateInfo(typeName, null)];

        return (LogicGate)Activator.CreateInstance(type)!;
    }

    public string GetTypeName(Type type)
    {
        LogicGateNameAttribute nameAttribute = (LogicGateNameAttribute)type.GetCustomAttributes(typeof(LogicGateNameAttribute), false)[0];
        return nameAttribute.Name;
    }

    public bool TryGetTypeName(Type type, [NotNullWhen(true)] out string? typeName)
    {
        if (type.GetCustomAttributes(typeof(LogicGateNameAttribute), false).FirstOrDefault() is not LogicGateNameAttribute nameAttribute)
        {
            typeName = null;
            return false;
        }

        typeName = nameAttribute.Name;
        return true;
    }

    public bool TryCreateLogicGate(string typeName, [NotNullWhen(true)] out LogicGate? logicGate)
    {
        if (!logicGates.ContainsKey(new LogicGateInfo(typeName, null)))
        {
            logicGate = null;
            return false;
        }

        Type type = logicGates[new LogicGateInfo(typeName, null)];

        logicGate = (LogicGate)Activator.CreateInstance(type)!;
        return true;
    }
}

internal class LogicGateInfo
{
    public string TypeName { get; set; }

    public string? Description { get; set; }

    public LogicGateInfo(string name, string? description)
    {
        TypeName = name;
        Description = description;
    }

    public override int GetHashCode()
    {
        return TypeName.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (obj is LogicGateInfo logicGateInfo)
        {
            return logicGateInfo.TypeName == TypeName;
        }
        return false;
    }
}