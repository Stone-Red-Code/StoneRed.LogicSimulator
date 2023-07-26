using StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces;

namespace StoneRed.LogicSimulator.Simulation.LogicGates;

internal class TestLogicGate : LogicGate
{
    public override int InputCount => 1;

    public override int OutputCount => 1;

    public TestLogicGate()
    {
        Metadata.Name = "Test";
    }

    protected override void Execute()
    {
        SetOutputBit(GetInputBit(0) == 1 ? 0 : 1, 0);
    }
}