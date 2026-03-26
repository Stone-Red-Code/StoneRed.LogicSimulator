using MonoGame.Extended.Input;
using StoneRed.LogicSimulator.Api;
using StoneRed.LogicSimulator.Api.Attributes;
using StoneRed.LogicSimulator.Api.Interfaces;

using System;

namespace StoneRed.LogicSimulator.Simulation.LogicGates;

[LogicGateName("Clock")]
[LogicGateDescription("A clock is a circuit that oscillates between a high and a low state.")]
internal class Clock : LogicGate, IInteractable
{
    private ICircuitSimulator? circuitSimulator;
    private int gateId;
    private int count = 0;
    private int tickRate = 0;
    private bool currentState = false;
    
    public override int OutputCount { get; set; } = 1;

    public override int InputCount { get; set; } = 0;

    public string Info
    {
        get
        {
            if (tickRate <= 0)
            {
                return "Disabled";
            }

            return tickRate + "\n" + ((count > tickRate) ? count - tickRate : count);
        }
    }

    public void OnInteraction(MouseStateExtended mouseState, MouseStateExtended previousMouseState, KeyboardStateExtended keyboardStateExtended)
    {
        if (mouseState.DeltaScrollWheelValue == 0 || !keyboardStateExtended.IsShiftDown())
        {
            return;
        }

        if (mouseState.DeltaScrollWheelValue < 0 && tickRate < int.MaxValue - 10)
        {
            tickRate += keyboardStateExtended.IsControlDown() ? 10 : 1;
        }
        else if (tickRate >= 1)
        {
            tickRate -= keyboardStateExtended.IsControlDown() ? 10 : 1;
        }

        tickRate = Math.Clamp(tickRate, 0, int.MaxValue);
    }

    protected internal override void Register(ICircuitSimulator circuitSimulator)
    {
        this.circuitSimulator = circuitSimulator;
        SimulatorGateId = circuitSimulator.AddGate(GateKind.Source);
        gateId = SimulatorGateId;
        
        // Watch own output to count ticks and toggle
        circuitSimulator.WatchGate(SimulatorGateId, (oldMask, newMask) =>
        {
            if (tickRate <= 0)
            {
                count = 0;
                currentState = false;
                circuitSimulator.SetSource(gateId, false);
                return;
            }

            // Count the tick
            count++;

            if (count >= tickRate * 2)
            {
                count = 0;
            }

            // Toggle state based on count
            bool newState = count > tickRate;
            if (newState != currentState)
            {
                currentState = newState;
                circuitSimulator.SetSource(gateId, newState);
            }
        });
        
        // Initialize to off state
        circuitSimulator.SetSource(gateId, false);
    }
}