using StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace StoneRed.LogicSimulator.Simulation;

internal class LogicGateSimulator
{
    private readonly ConcurrentDictionary<ulong, LogicGate> logicGates = new ConcurrentDictionary<ulong, LogicGate>();
    private DateTime dateTime;
    private bool logicGatesUpdated = false;
    private ulong logicGateId = 0;
    public int TargetTicksPerSecond { get; set; } = 100;

    public int ActualTicksPerSecond { get; private set; }

    public bool IsRunning { get; private set; }

    public bool ClockCalibrating { get; private set; }

    public bool HighPerformanceClock { get; set; }

    public LogicGateSimulator(IEnumerable<LogicGate> logicGates)
    {
        foreach (LogicGate logicGate in logicGates)
        {
            AddLogicGate(logicGate);
        }
    }

    public void Start()
    {
        Thread thread = new Thread(SimulationThread)
        {
            IsBackground = true
        };

        IsRunning = true;

        thread.Start();
    }

    public void Stop()
    {
        IsRunning = false;
    }

    public void AddLogicGate(LogicGate gate)
    {
        gate.Id = logicGateId++;
        logicGatesUpdated = logicGates.TryAdd(gate.Id, gate);
    }

    public LogicGate GetLogicGate(ulong id)
    {
        return logicGates[id];
    }

    public IEnumerable<LogicGate> GetLogicGates()
    {
        return logicGates.Values;
    }

    public void RemoveLogicGate(LogicGate logicGate)
    {
        foreach (LogicGate otherLogicGate in logicGates.Values.Where(l => l.IsConnectedTo(logicGate)))
        {
            otherLogicGate.Disconnect(logicGate);
        }

        logicGatesUpdated = logicGates.TryRemove(logicGate.Id, out _);
    }

    private void SimulationThread()
    {
        int tps = 0;
        float iterations = 10000;

        LogicGate[] logicGatesArray = logicGates.Values.ToArray();

        while (IsRunning)
        {
            tps++;

            if (logicGatesUpdated)
            {
                logicGatesArray = logicGates.Values.ToArray();
                logicGatesUpdated = false;
            }

            if (DateTime.Now > dateTime.AddSeconds(1))
            {
                ActualTicksPerSecond = tps;
                dateTime = DateTime.Now;
                tps = 0;

                if (HighPerformanceClock)
                {
                    float percentage = Math.Abs((TargetTicksPerSecond - (float)ActualTicksPerSecond) / Math.Abs((float)ActualTicksPerSecond) * 100);

                    if (ActualTicksPerSecond / 2 > TargetTicksPerSecond)
                    {
                        iterations *= 2;
                    }
                    else if (ActualTicksPerSecond * 2 < TargetTicksPerSecond)
                    {
                        iterations /= 2;
                    }
                    else if (percentage > 5)
                    {
                        iterations *= (float)ActualTicksPerSecond / TargetTicksPerSecond;
                    }

                    ClockCalibrating = percentage > 5;
                }
            }

            for (int i = 0; i < logicGatesArray.Length; i++)
            {
                logicGatesArray[i].NextTick();
            }

            for (int i = 0; i < logicGatesArray.Length; i++)
            {
                logicGatesArray[i].Update();
            }

            if (HighPerformanceClock)
            {
                Thread.SpinWait((int)iterations);
            }
            else
            {
                Thread.Sleep(Math.Max((int)(1f / TargetTicksPerSecond * 1000), 1));
            }
        }
    }
}