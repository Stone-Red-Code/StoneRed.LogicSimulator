using StoneRed.LogicSimulator.Utilities;

using System;
using System.Collections.Generic;

namespace StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces;
#pragma warning disable S112 // General exceptions should never be thrown

internal abstract class LogicGate
{
    private readonly List<LogicGateConnection> logicGateConnections = new();
    private int currentInput;
    private int newInput;
    private int output;
    private int cachedOutput;

    public LogicGateMetadata Metadata { get; set; } = new LogicGateMetadata();
    public int Id { get; set; }
    public IReadOnlyList<LogicGateConnection> LogicGateConnections => logicGateConnections.AsReadOnly();
    public abstract int OutputCount { get; }

    public abstract int InputCount { get; }

    public void NextTick()
    {
        currentInput = newInput;
        newInput = 0;
        output = 0;
    }

    public void SetInput(int value, int index)
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

    public void Connect(LogicGate logicGate, int inputIndex, int outputIndex)
    {
        if (inputIndex >= logicGate.InputCount)
        {
            throw new IndexOutOfRangeException();
        }

        if (outputIndex >= OutputCount)
        {
            throw new IndexOutOfRangeException();
        }

        logicGateConnections.Add(new LogicGateConnection(logicGate, inputIndex, outputIndex));
    }

    public void Disconnect(LogicGate logicGate)
    {
        int index = logicGateConnections.FindIndex(c => c.LogicGate.Id == logicGate.Id);
        logicGateConnections.RemoveAt(index);
    }

    public void Update()
    {
        Execute();
        cachedOutput = output;
        PublishOutput();
    }

    public int GetCachedOutputBit(int index)
    {
        if (index < 0 || index >= OutputCount)
        {
            throw new IndexOutOfRangeException();
        }

        return cachedOutput.GetBit(index);
    }

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

    private void PublishOutput()
    {
        foreach (LogicGateConnection connection in logicGateConnections)
        {
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