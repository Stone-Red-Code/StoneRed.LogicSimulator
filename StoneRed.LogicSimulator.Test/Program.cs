namespace StoneRed.LogicSimulator.Test;

internal static class Program
{
    public static void Main()
    {
        var sim = new ExprCircuitSimulator();

        var inverter = new CircuitDefinition();
        int invIn = inverter.AddInputPin();   // Source inside definition
        int invNot = inverter.AddGate(GateKind.Not);
        int invOut = inverter.AddOutputPin(); // Sink inside definition
        inverter.Connect(invIn, invNot, toInputBit: 0);
        inverter.Connect(invNot, invOut, toInputBit: 0);

        sim.RegisterMacroGate("INV", inverter);
        sim.ComputeLut("INV");

        var doubleInverter = new CircuitDefinition();
        int inv2In = doubleInverter.AddInputPin();
        CircuitDefinition.MacroInstanceDef invA = doubleInverter.AddMacroInstance("INV", inputCount: 1, outputCount: 1);
        CircuitDefinition.MacroInstanceDef invB = doubleInverter.AddMacroInstance("INV", inputCount: 1, outputCount: 1);
        int inv2Out = doubleInverter.AddOutputPin();
        doubleInverter.Connect(inv2In, invA.Inputs[0], toInputBit: 0);
        doubleInverter.Connect(invA.Outputs[0], invB.Inputs[0], toInputBit: 0);
        doubleInverter.Connect(invB.Outputs[0], inv2Out, toInputBit: 0);

        sim.RegisterMacroGate("INV2", doubleInverter);
        sim.ComputeLut("INV2");

        int a = sim.AddGate(GateKind.Source);
        ExprCircuitSimulator.MacroInstance inv2 = sim.AddMacroGate("INV2");
        int lamp = sim.AddGate(GateKind.Sink);

        sim.ConnectGates(a, inv2.Inputs[0], toInputBit: 0);
        sim.ConnectGates(inv2.Outputs[0], lamp, toInputBit: 0);

        sim.SetSource(a, value: false);
        sim.RunUntilStable();
        Console.WriteLine($"A=0 => Lamp={sim.GetOutput(lamp)} (expected False)");

        sim.SetSource(a, value: true);
        sim.RunUntilStable();
        Console.WriteLine($"A=1 => Lamp={sim.GetOutput(lamp)} (expected True)");
    }
}
