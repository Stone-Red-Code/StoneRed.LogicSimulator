using Myra.Utility;

using System;
using System.IO;

namespace StoneRed.LogicSimulator.Misc;

internal static class Paths
{
    public static string GetAppDataPath()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string srlsAppDataPath = Path.Combine(appDataPath, "StoneRed", "LogicSimulator");

        if (!Directory.Exists(srlsAppDataPath))
        {
            _ = Directory.CreateDirectory(srlsAppDataPath);
        }

        return srlsAppDataPath;
    }

    public static string GetAppDataPath(params string[] paths)
    {
        return Path.Combine(GetAppDataPath(), Path.Combine(paths));
    }

    public static string GetContentPath()
    {
        return Path.Combine(PathUtils.ExecutingAssemblyDirectory, "Content");
    }

    public static string GetContentPath(string fileName)
    {
        return Path.Combine(GetContentPath(), fileName);
    }

    public static string GetContentPath(params string[] paths)
    {
        return Path.Combine(GetContentPath(), Path.Combine(paths));
    }

    public static string GetSettingsPath()
    {
        return Path.Combine(GetAppDataPath(), "settings.json");
    }

    public static string GetWorldSavesPath()
    {
        string savesPath = GetAppDataPath("Saves");

        if (!Directory.Exists(savesPath))
        {
            _ = Directory.CreateDirectory(savesPath);
        }

        return savesPath;
    }

    public static string GetWorldSaveDirectoryPath(string saveName)
    {
        string savePath = Path.Combine(GetWorldSavesPath(), saveName);

        if (!Directory.Exists(savePath))
        {
            _ = Directory.CreateDirectory(savePath);
        }

        return savePath;
    }

    public static string GetWorldSaveFilePath(string saveName)
    {
        return Path.Combine(GetWorldSaveDirectoryPath(saveName), saveName + ".srls");
    }

    public static string GetModsPath()
    {
        string modsPath = GetAppDataPath("Mods");

        if (!Directory.Exists(modsPath))
        {
            _ = Directory.CreateDirectory(modsPath);
        }

        return modsPath;
    }

    public static string GetModDirectoryPath(string modName)
    {
        string modPath = Path.Combine(GetWorldSavesPath(), modName);

        if (!Directory.Exists(modPath))
        {
            _ = Directory.CreateDirectory(modPath);
        }

        return modPath;
    }

    public static string GetModFilePath(string modName)
    {
        return Path.Combine(GetWorldSaveDirectoryPath(modName), modName + ".dll");
    }
}