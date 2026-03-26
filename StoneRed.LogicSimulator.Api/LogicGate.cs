using Microsoft.Xna.Framework.Graphics;

using StoneRed.LogicSimulator.Api.Utilities;
using StoneRed.LogicSimulator.Simulation;

namespace StoneRed.LogicSimulator.Api;

internal abstract class LogicGate
{
    private readonly List<LogicGateConnection> logicGateConnections = [];
    public abstract int OutputCount { get; set; }
    public abstract int InputCount { get; set; }
    internal IReadOnlyList<LogicGateConnection> LogicGateConnections => logicGateConnections.AsReadOnly();
    internal GraphicsDevice? GraphicsDevice { get; set; }
    internal LogicGateWorldData WorldData { get; init; } = new LogicGateWorldData();
    internal ulong Id { get; set; }
    internal int SimulatorGateId { get; set; } = -1;

    public override bool Equals(object? obj)
    {
        return obj is LogicGate gate &&
                Id == gate.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    internal void Connect(LogicGate logicGate, int inputIndex, int outputIndex)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(inputIndex, logicGate.InputCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(outputIndex, OutputCount);

        lock (logicGateConnections)
        {
            logicGateConnections.Add(new LogicGateConnection(logicGate, inputIndex, outputIndex));
        }
    }

    internal bool IsConnectedTo(LogicGate logicGate, int? inputIndex = null, int? outputIndex = null)
    {
        lock (logicGateConnections)
        {
            return logicGateConnections.Exists(c =>
            c.LogicGate.Id == logicGate.Id
            && (!inputIndex.HasValue || c.InputIndex == inputIndex)
            && (!outputIndex.HasValue || c.OutputIndex == outputIndex));
        }
    }

    internal void Disconnect(LogicGate logicGate, int? inputIndex = null, int? outputIndex = null)
    {
        lock (logicGateConnections)
        {
            int index = logicGateConnections.FindIndex(c =>
            c.LogicGate.Id == logicGate.Id &&
            (!inputIndex.HasValue || c.InputIndex == inputIndex) &&
            (!outputIndex.HasValue || c.OutputIndex == outputIndex));
            logicGateConnections.RemoveAt(index);
        }
    }

    protected internal virtual void Initialize()
    { }

    protected internal abstract void Register(ICircuitSimulator circuitSimulator);

    protected Texture2D CreateTexture(int width, int height)
    {
        return new Texture2D(GraphicsDevice, width, height);
    }
}

internal class LogicGateConnection(LogicGate logicGate, int inputIndex, int outputIndex)
{
    public LogicGate LogicGate { get; } = logicGate;

    public int InputIndex { get; } = inputIndex;
    public int OutputIndex { get; } = outputIndex;
};