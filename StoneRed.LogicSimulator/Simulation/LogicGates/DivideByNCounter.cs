using MonoGame.Extended.Input;

using StoneRed.LogicSimulator.Api;
using StoneRed.LogicSimulator.Api.Attributes;
using StoneRed.LogicSimulator.Api.Interfaces;

using System;

namespace StoneRed.LogicSimulator.Simulation.LogicGates;

[LogicGateName("Divide By N Counter")]
[LogicGateDescription("A divide by N counter is a counter that divides the input frequency by N. The output is high for one tick every N ticks.")]
internal class DivideByNCounter : LogicGate, IInteractable
{
    private ICircuitSimulator? circuitSimulator;
    private int gateId;
    private int count = 0;
    private int tickRate = 0;
    private bool currentState = false;
    private bool inputInitialized = false;
    private bool lastInputState = false;

    public override int OutputCount { get; set; } = 1;

    public override int InputCount { get; set; } = 1;

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

        if (tickRate <= 0)
        {
            count = 0;
            currentState = false;
            circuitSimulator?.SetSource(gateId, false);
        }
    }

    protected internal override void Register(ICircuitSimulator circuitSimulator)
    {
        this.circuitSimulator = circuitSimulator;
        SimulatorGateId = circuitSimulator.AddGate(GateKind.Source);
        gateId = SimulatorGateId;
        count = 0;
        currentState = false;
        inputInitialized = false;
        lastInputState = false;
        circuitSimulator.SetSource(gateId, false);
        _ = circuitSimulator.WatchGate(SimulatorGateId, (_, newInputMask) => OnInputChanged((newInputMask & 1) != 0));
    }

    private void OnInputChanged(bool inputState)
    {
        if (circuitSimulator == null)
        {
            return;
        }

        if (!inputInitialized)
        {
            inputInitialized = true;
            lastInputState = inputState;
            return;
        }

        if (lastInputState == inputState)
        {
            return;
        }

        lastInputState = inputState;

        if (tickRate <= 0)
        {
            if (count != 0 || currentState)
            {
                count = 0;
                currentState = false;
                circuitSimulator.SetSource(gateId, false);
            }

            return;
        }

        count++;

        if (count >= tickRate * 2)
        {
            count = 0;
        }

        bool newState = count > tickRate;
        if (newState != currentState)
        {
            currentState = newState;
            circuitSimulator.SetSource(gateId, newState);
        }
    }
}
