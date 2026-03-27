using StoneRed.LogicSimulator.Api;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace StoneRed.LogicSimulator.Simulation;

internal class LogicGateSimulator
{
    private readonly ConcurrentDictionary<ulong, LogicGate> logicGates = new ConcurrentDictionary<ulong, LogicGate>();
    private readonly List<int> globalClockTargets = [];
    private DateTime dateTime;
    private bool logicGatesUpdated = false;
    private ulong logicGateId = 0;
    private ICircuitSimulator? circuitSimulator;
    private int globalClockSourceGateId = -1;
    private bool globalClockState;

    public int TargetTicksPerSecond { get; set; } = 100;

    public int ActualTicksPerSecond { get; private set; }

    public bool IsRunning { get; private set; }

    public bool ClockCalibrating { get; private set; }

    public bool HighPerformanceClock { get; set; }

    public SimulatorType SimulatorType
    {
        get;
        set
        {
            field = value;
            logicGatesUpdated = true;
        }
    } = SimulatorType.Cycle;

    public LogicGateSimulator(IEnumerable<LogicGate> logicGates)
    {
        foreach (LogicGate logicGate in logicGates)
        {
            AddLogicGate(logicGate);
        }
    }

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        IsRunning = true;

        Thread thread = new Thread(SimulationThread)
        {
            IsBackground = true
        };

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

    public void LogicGatesUpdated()
    {
        logicGatesUpdated = true;
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

    public bool GetGateOutput(LogicGate gate)
    {
        if (circuitSimulator == null || gate.SimulatorGateId < 0)
        {
            return false;
        }

        return circuitSimulator.GetOutput(gate.SimulatorGateId);
    }

    private void SimulationThread()
    {
        int tps = 0;
        int sleepDelayIterations = 10000;
        int sleepDelayMs = 10;

        circuitSimulator = CreateSimulator();

        Timer timeCheckTimer = new Timer(_ =>
        {
            // Compensate for any time drift by calculating actual TPS and adjusting sleep delay accordingly
            ActualTicksPerSecond = (int)Math.Round(tps / (DateTime.Now - dateTime).TotalSeconds);
            dateTime = DateTime.Now;
            tps = 0;

            if (HighPerformanceClock)
            {
                float percentage = Math.Abs((TargetTicksPerSecond - (float)ActualTicksPerSecond) / Math.Abs((float)ActualTicksPerSecond) * 100);

                sleepDelayIterations = Math.Max(sleepDelayIterations, 1);
                sleepDelayIterations *= (int)((float)ActualTicksPerSecond / TargetTicksPerSecond);

                ClockCalibrating = percentage > 5;
            }
            else
            {
                // Update cached sleep delay when target TPS changes
                sleepDelayMs = Math.Max((int)(1000f / TargetTicksPerSecond), 1);
            }
        }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

        logicGatesUpdated = true;

        while (IsRunning)
        {
            tps++;

            if (logicGatesUpdated)
            {
                circuitSimulator = CreateSimulator();
                globalClockTargets.Clear();
                globalClockSourceGateId = circuitSimulator.AddGate(GateKind.Source);
                globalClockState = false;
                circuitSimulator.SetSource(globalClockSourceGateId, false);

                // Register all gates first
                foreach (LogicGate gate in logicGates.Values)
                {
                    gate.Register(circuitSimulator);
                    if (gate is LogicGates.Clock)
                    {
                        globalClockTargets.Add(gate.SimulatorGateId);
                    }
                }

                // Connect the global clock source to all clock gates
                for (int i = 0; i < globalClockTargets.Count; i++)
                {
                    circuitSimulator.ConnectGates(globalClockSourceGateId, globalClockTargets[i], 0);
                }

                // Then connect them based on LogicGate connections
                foreach (LogicGate gate in logicGates.Values)
                {
                    foreach (LogicGateConnection connection in gate.LogicGateConnections)
                    {
                        // Connect: this gate's output → connected gate's input
                        circuitSimulator.ConnectGates(
                            gate.SimulatorGateId,
                            connection.LogicGate.SimulatorGateId,
                            connection.InputIndex
                        );
                    }
                }

                circuitSimulator.Reset();

                logicGatesUpdated = false;
            }

            globalClockState = !globalClockState;
            circuitSimulator.SetSource(globalClockSourceGateId, globalClockState);

            circuitSimulator.Step();

            if (HighPerformanceClock)
            {
                Thread.SpinWait(sleepDelayIterations);
            }
            else
            {
                Thread.Sleep(sleepDelayMs);
            }
        }

        _ = timeCheckTimer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    private ICircuitSimulator CreateSimulator()
    {
        return SimulatorType switch
        {
            SimulatorType.Event => new EventCircuitSimulator(),
            SimulatorType.Cycle => new CycleCircuitSimulator(),
            _ => throw new NotImplementedException()
        };
    }
}
