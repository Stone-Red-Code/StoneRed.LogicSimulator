using BenchmarkDotNet.Attributes;
using StoneRed.LogicSimulator.Simulation;

namespace StoneRed.LogicSimulator.Benchmarks;

[MemoryDiagnoser]
public class SimulatorBenchmarks
{
    private CycleCircuitSimulator cycleSim = null!;
    private EventCircuitSimulator eventSim = null!;
    private int cycleSource;
    private int eventSource;
    private bool state;

    [Params(0.01, 0.10, 1.00)]
    public double ActivityPercentage;

    [Params(true, false)]
    public bool UseLut;

    [GlobalSetup]
    public void Setup()
    {
        cycleSim = new CycleCircuitSimulator();
        eventSim = new EventCircuitSimulator();

        SetupCircuit(cycleSim, out cycleSource);
        SetupCircuit(eventSim, out eventSource);
    }

    private void SetupCircuit(ICircuitSimulator sim, out int source)
    {
        var chain10 = new CircuitDefinition();
        int input = chain10.AddInputPin();
        int last = input;
        for (int i = 0; i < 10; i++)
        {
            int not = chain10.AddGate(GateKind.Not);
            chain10.Connect(last, not, 0);
            last = not;
        }
        int output = chain10.AddOutputPin();
        chain10.Connect(last, output, 0);

        sim.RegisterMacroGate("CHAIN10", chain10);
        if (UseLut)
        {
            sim.ComputeLut("CHAIN10");
        }

        const int macroCount = 100;
        int activeMacroCount = Math.Max(1, (int)(macroCount * ActivityPercentage));
        int idleMacroCount = macroCount - activeMacroCount;

        source = sim.AddGate(GateKind.Source);
        int constantSource = sim.AddGate(GateKind.Source);
        int sink = sim.AddGate(GateKind.Sink);

        for (int i = 0; i < activeMacroCount; i++)
        {
            var inst = sim.AddMacroGate("CHAIN10");
            sim.ConnectGates(source, inst.Inputs[0], 0);
            sim.ConnectGates(inst.Outputs[0], sink, 0);
        }

        for (int i = 0; i < idleMacroCount; i++)
        {
            var inst = sim.AddMacroGate("CHAIN10");
            sim.ConnectGates(constantSource, inst.Inputs[0], 0);
            sim.ConnectGates(inst.Outputs[0], sink, 0);
        }

        sim.Reset();
        sim.SetSource(constantSource, false);
        sim.RunUntilStable();
    }

    [Benchmark]
    public void CycleBased()
    {
        state = !state;
        cycleSim.SetSource(cycleSource, state);
        cycleSim.Step();
    }

    [Benchmark]
    public void EventDriven()
    {
        state = !state;
        eventSim.SetSource(eventSource, state);
        eventSim.Step();
    }
}
