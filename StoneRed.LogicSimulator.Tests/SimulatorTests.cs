using Microsoft.VisualStudio.TestTools.UnitTesting;

using StoneRed.LogicSimulator.Simulation;

namespace StoneRed.LogicSimulator.Tests;

[TestClass]
public class CycleSimulatorTests : SimulatorTestsBase
{
    protected override ICircuitSimulator CreateSimulator()
    {
        return new CycleCircuitSimulator();
    }
}

[TestClass]
public class EventSimulatorTests : SimulatorTestsBase
{
    protected override ICircuitSimulator CreateSimulator()
    {
        return new EventCircuitSimulator();
    }
}

public abstract class SimulatorTestsBase
{
    protected abstract ICircuitSimulator CreateSimulator();

    [TestMethod]
    public void TestAndGate()
    {
        ICircuitSimulator sim = CreateSimulator();
        int s1 = sim.AddGate(GateKind.Source);
        int s2 = sim.AddGate(GateKind.Source);
        int and = sim.AddGate(GateKind.And2);
        int sink = sim.AddGate(GateKind.Sink);
        sim.ConnectGates(s1, and, 0);
        sim.ConnectGates(s2, and, 1);
        sim.ConnectGates(and, sink, 0);

        void Check(bool i1, bool i2, bool expected)
        {
            sim.SetSource(s1, i1);
            sim.SetSource(s2, i2);
            _ = sim.RunUntilStable();
            Assert.AreEqual(expected, sim.GetOutput(sink), $"AND2 failed for {i1} & {i2}");
        }

        Check(false, false, false);
        Check(false, true, false);
        Check(true, false, false);
        Check(true, true, true);
    }

    [TestMethod]
    public void TestOrGate()
    {
        ICircuitSimulator sim = CreateSimulator();
        int s1 = sim.AddGate(GateKind.Source);
        int s2 = sim.AddGate(GateKind.Source);
        int or = sim.AddGate(GateKind.Or2);
        int sink = sim.AddGate(GateKind.Sink);
        sim.ConnectGates(s1, or, 0);
        sim.ConnectGates(s2, or, 1);
        sim.ConnectGates(or, sink, 0);

        void Check(bool i1, bool i2, bool expected)
        {
            sim.SetSource(s1, i1);
            sim.SetSource(s2, i2);
            _ = sim.RunUntilStable();
            Assert.AreEqual(expected, sim.GetOutput(sink), $"OR2 failed for {i1} | {i2}");
        }

        Check(false, false, false);
        Check(false, true, true);
        Check(true, false, true);
        Check(true, true, true);
    }

    [TestMethod]
    public void TestNotGate()
    {
        ICircuitSimulator sim = CreateSimulator();
        int s1 = sim.AddGate(GateKind.Source);
        int not = sim.AddGate(GateKind.Not);
        int sink = sim.AddGate(GateKind.Sink);
        sim.ConnectGates(s1, not, 0);
        sim.ConnectGates(not, sink, 0);

        sim.SetSource(s1, false);
        _ = sim.RunUntilStable();
        Assert.IsTrue(sim.GetOutput(sink), "NOT(0) should be 1");

        sim.SetSource(s1, true);
        _ = sim.RunUntilStable();
        Assert.IsFalse(sim.GetOutput(sink), "NOT(1) should be 0");
    }

    [TestMethod]
    public void TestLutGate()
    {
        ICircuitSimulator sim = CreateSimulator();
        int s1 = sim.AddGate(GateKind.Source);
        int s2 = sim.AddGate(GateKind.Source);
        int s3 = sim.AddGate(GateKind.Source);

        // Majority function (2 or more high)
        int[] table = { 0, 0, 0, 1, 0, 1, 1, 1 };
        int lut = sim.AddLutGate(3, table);
        int sink = sim.AddGate(GateKind.Sink);

        sim.ConnectGates(s1, lut, 0);
        sim.ConnectGates(s2, lut, 1);
        sim.ConnectGates(s3, lut, 2);
        sim.ConnectGates(lut, sink, 0);

        _ = sim.RunUntilStable();

        sim.SetSource(s1, true); sim.SetSource(s2, true); sim.SetSource(s3, false);
        _ = sim.RunUntilStable();
        Assert.IsTrue(sim.GetOutput(sink), "Majority(1,1,0) should be 1");

        sim.SetSource(s1, false); sim.SetSource(s2, true); sim.SetSource(s3, false);
        _ = sim.RunUntilStable();
        Assert.IsFalse(sim.GetOutput(sink), "Majority(0,1,0) should be 0");
    }

    [TestMethod]
    public void TestMacroCorrectness()
    {
        ICircuitSimulator sim = CreateSimulator();
        CircuitDefinition def = new CircuitDefinition();
        int inPin = def.AddInputPin();
        int not = def.AddGate(GateKind.Not);
        int outPin = def.AddOutputPin();
        def.Connect(inPin, not, 0);
        def.Connect(not, outPin, 0);

        sim.RegisterMacroGate("NOT", def);
        MacroInstance inst = sim.AddMacroGate("NOT");

        int src = sim.AddGate(GateKind.Source);
        int sink = sim.AddGate(GateKind.Sink);
        sim.ConnectGates(src, inst.Inputs[0], 0);
        sim.ConnectGates(inst.Outputs[0], sink, 0);

        sim.SetSource(src, true);
        _ = sim.RunUntilStable();
        Assert.IsFalse(sim.GetOutput(sink), "Macro NOT(1) should be 0");
    }

    [TestMethod]
    public void TestWatcherNotification()
    {
        ICircuitSimulator sim = CreateSimulator();
        int source = sim.AddGate(GateKind.Source);
        int not = sim.AddGate(GateKind.Not);
        sim.ConnectGates(source, not, 0);

        int watcher1Count = 0;
        int watcher2Count = 0;

        IDisposable sub1 = sim.WatchGate(not, (id, val) => watcher1Count++);
        IDisposable sub2 = sim.WatchGate(not, (id, val) => watcher2Count++);

        // Initial state
        sim.SetSource(source, false);
        _ = sim.RunUntilStable();
        Assert.AreEqual(0, watcher1Count, "Watcher 1 should not fire when input mask does not change");
        Assert.AreEqual(0, watcher2Count, "Watcher 2 should not fire when input mask does not change");

        // No change
        sim.SetSource(source, false);
        _ = sim.RunUntilStable();
        Assert.AreEqual(0, watcher1Count, "Watcher 1 should not fire if no change");
        Assert.AreEqual(0, watcher2Count, "Watcher 2 should not fire if no change");

        // Change
        sim.SetSource(source, true);
        _ = sim.RunUntilStable();
        Assert.AreEqual(1, watcher1Count, "Watcher 1 should fire on input change");
        Assert.AreEqual(1, watcher2Count, "Watcher 2 should fire on input change");

        // Dispose one watcher
        sub2.Dispose();

        // Change again
        sim.SetSource(source, false);
        _ = sim.RunUntilStable();
        Assert.AreEqual(2, watcher1Count, "Watcher 1 should fire after sub2 is disposed");
        Assert.AreEqual(1, watcher2Count, "Watcher 2 should NOT fire after being disposed");
    }
}
