using Microsoft.Xna.Framework.Graphics;

using StoneRed.LogicSimulator.Api;
using StoneRed.LogicSimulator.Api.Attributes;
using StoneRed.LogicSimulator.Misc;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

namespace StoneRed.LogicSimulator.Utilities;

internal class LogicGatesManager
{
    private readonly GraphicsDevice graphicsDevice;

    private readonly Dictionary<LogicGateInfo, Type> logicGates = new Dictionary<LogicGateInfo, Type>();

    public LogicGatesManager(GraphicsDevice graphicsDevice)
    {
        this.graphicsDevice = graphicsDevice;
    }

    public static string GetTypeLogicGateName(Type type)
    {
        LogicGateNameAttribute nameAttribute = (LogicGateNameAttribute)type.GetCustomAttributes(typeof(LogicGateNameAttribute), false)[0];
        return nameAttribute.Name;
    }

    public static bool TryGetTypeLogicGateName(Type type, [NotNullWhen(true)] out string? typeName)
    {
        if (type.GetCustomAttributes(typeof(LogicGateNameAttribute), false).FirstOrDefault() is not LogicGateNameAttribute nameAttribute)
        {
            typeName = null;
            return false;
        }

        typeName = nameAttribute.Name;
        return true;
    }

    public void LoadLogicGates()
    {
        IEnumerable<Type> logicGateTypes = GetLogicGateTypesFromAssembly(GetType().Assembly);

        foreach (string directory in Directory.GetDirectories(Paths.GetModsPath()))
        {
            string modName = Path.GetFileName(directory) ?? string.Empty;
            byte[] assemblyBytes = File.ReadAllBytes(Paths.GetModFilePath(modName));

            Assembly assembly = Assembly.Load(assemblyBytes);
            logicGateTypes = logicGateTypes.Concat(GetLogicGateTypesFromAssembly(assembly));
        }

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

        LogicGate logicGate = (LogicGate)Activator.CreateInstance(type)!;
        logicGate.GraphicsDevice = graphicsDevice;
        logicGate.Initialize();
        return logicGate;
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
        logicGate.GraphicsDevice = graphicsDevice;
        logicGate.Initialize();
        return true;
    }

    private IEnumerable<Type> GetLogicGateTypesFromAssembly(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(LogicGate)) && !t.IsAbstract);
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