namespace StoneRed.LogicSimulator.WorldSaveSystem;

internal class WorldSaveLoadProgress
{
    public int Percentage { get; }

    public string Message { get; }

    public WorldSaveLoadProgress(int percentage, string message)
    {
        Percentage = percentage;
        Message = message;
    }
}