using System.IO;
using System.Text.Json;
using StoneRed.LogicSimulator.Utilities;

namespace StoneRed.LogicSimulator.Misc;

internal class Settings
{
    public Resolution Resolution { get; set; } = new(0, 0);
    public float Scale { get; set; } = 1;
    public bool Fullscreen { get; set; } = false;

    public static Settings? Load(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        return JsonSerializer.Deserialize<Settings>(File.ReadAllText(path));
    }

    public void Save(string path)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(this));
    }
}