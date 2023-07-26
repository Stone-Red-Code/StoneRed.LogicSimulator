using StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces;

namespace StoneRed.LogicSimulator.Simulation.LogicGates;

internal class Pin : LogicGate
{
    public override int OutputCount => 1;

    public override int InputCount => 1;

    protected override void Execute()
    {
        for (int i = 0; i < InputCount; i++)
        {
            SetOutputBit(GetInputBit(i), i);
        }
    }
}