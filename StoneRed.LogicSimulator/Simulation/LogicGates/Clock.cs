using MonoGame.Extended.Input;

using StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces;

using System;

namespace StoneRed.LogicSimulator.Simulation.LogicGates;

internal class Clock : LogicGate, IInteractable
{
    private int count = 0;
    private int tickRate = 0;
    public override int OutputCount => 1;

    public override int InputCount => 0;

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

    public Clock()
    {
        Metadata.Name = "Clock";
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

    protected override void Execute()
    {
        if (tickRate <= 0)
        {
            SetOutputBit(0, 0);
            return;
        }

        if (count >= tickRate * 2)
        {
            count = 0;
        }

        count++;

        if (count > tickRate)
        {
            SetOutputBit(1, 0);
        }
        else
        {
            SetOutputBit(0, 0);
        }
    }
}