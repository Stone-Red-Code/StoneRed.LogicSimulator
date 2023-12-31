﻿using Microsoft.Xna.Framework.Graphics;

using StoneRed.LogicSimulator.Api.Utilities;

namespace StoneRed.LogicSimulator.Api;
#pragma warning disable S112 // General exceptions should never be thrown

internal abstract class LogicGate
{
    private readonly List<LogicGateConnection> logicGateConnections = new();
    private bool logicGateConnectionsChnaged = false;
    private int currentInput;
    private int newInput;
    private int output;
    private int cachedOutput;
    private LogicGateConnection[] logicGateConnectionsArray = Array.Empty<LogicGateConnection>();
    public abstract int OutputCount { get; set; }
    public abstract int InputCount { get; set; }
    internal IReadOnlyList<LogicGateConnection> LogicGateConnections => logicGateConnections.AsReadOnly();
    internal GraphicsDevice? GraphicsDevice { get; set; }
    internal LogicGateWorldData WorldData { get; init; } = new LogicGateWorldData();
    internal ulong Id { get; set; }

    public override bool Equals(object? obj)
    {
        return obj is LogicGate gate &&
                Id == gate.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    internal void NextTick()
    {
        currentInput = newInput;
        newInput = 0;
        output = 0;
    }

    internal void SetInput(int value, int index)
    {
        if (index >= InputCount)
        {
            throw new IndexOutOfRangeException();
        }

        // Inputs should act as or gates

        if (value == 1)
        {
            newInput.SetBit(1, index);
        }
    }

    internal void Connect(LogicGate logicGate, int inputIndex, int outputIndex)
    {
        if (inputIndex >= logicGate.InputCount)
        {
            throw new IndexOutOfRangeException();
        }

        if (outputIndex >= OutputCount)
        {
            throw new IndexOutOfRangeException();
        }

        lock (logicGateConnections)
        {
            logicGateConnections.Add(new LogicGateConnection(logicGate, inputIndex, outputIndex));
            logicGateConnectionsChnaged = true;
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
            logicGateConnectionsChnaged = true;
        }
    }

    internal void Update()
    {
        Execute();
        cachedOutput = output;
        PublishOutput();
    }

    internal int GetCachedOutputBit(int index)
    {
        if (index < 0 || index >= OutputCount)
        {
            throw new IndexOutOfRangeException();
        }

        return cachedOutput.GetBit(index);
    }

    protected internal virtual void Initialize()
    { }

    protected abstract void Execute();

    protected int GetInputBit(int index)
    {
        if (index < 0 || index >= InputCount)
        {
            throw new IndexOutOfRangeException();
        }

        return currentInput.GetBit(index);
    }

    protected void SetOutputBit(int value, int index)
    {
        if (index < 0 || index >= OutputCount)
        {
            throw new IndexOutOfRangeException();
        }

        output.SetBit(value, index);
    }

    protected Texture2D CreateTexture(int width, int height)
    {
        return new Texture2D(GraphicsDevice, width, height);
    }

    private void PublishOutput()
    {
        if (logicGateConnectionsChnaged)
        {
            lock (logicGateConnections)
            {
                logicGateConnectionsArray = logicGateConnections.ToArray();
                logicGateConnectionsChnaged = false;
            }
        }

        for (int i = 0; i < logicGateConnectionsArray.Length; i++)
        {
            LogicGateConnection connection = logicGateConnectionsArray[i];
            connection.LogicGate.SetInput(output.GetBit(connection.OutputIndex), connection.InputIndex);
        }
    }
}

#pragma warning restore S112 // General exceptions should never be thrown

internal class LogicGateConnection
{
    public LogicGate LogicGate { get; }

    public int InputIndex { get; }
    public int OutputIndex { get; }

    public LogicGateConnection(LogicGate logicGate, int inputIndex, int outputIndex)
    {
        LogicGate = logicGate;
        InputIndex = inputIndex;
        OutputIndex = outputIndex;
    }
};