using Microsoft.VisualStudio.TestTools.UnitTesting;

using StoneRed.LogicSimulator.Simulation;

namespace StoneRed.LogicSimulator.Tests;

[TestClass]
public class CycleAdvancedTests : AdvancedCircuitTestsBase
{
    protected override ICircuitSimulator CreateSimulator()
    {
        return new CycleCircuitSimulator();
    }
}

[TestClass]
public class EventAdvancedTests : AdvancedCircuitTestsBase
{
    protected override ICircuitSimulator CreateSimulator()
    {
        return new EventCircuitSimulator();
    }
}

public abstract class AdvancedCircuitTestsBase
{
    protected abstract ICircuitSimulator CreateSimulator();

    [TestMethod]
    public void TestSRLatch()
    {
        // SR Latch using NOR gates
        // Q  = NOR(R, Q')
        // Q' = NOR(S, Q)
        ICircuitSimulator sim = CreateSimulator();

        int s = sim.AddGate(GateKind.Source);
        int r = sim.AddGate(GateKind.Source);

        int orQ = sim.AddGate(GateKind.Or2);
        int norQ = sim.AddGate(GateKind.Not); // Q

        int orQNot = sim.AddGate(GateKind.Or2);
        int norQNot = sim.AddGate(GateKind.Not); // Q'

        // Q = NOR(R, Q')
        sim.ConnectGates(r, orQ, 0);
        sim.ConnectGates(norQNot, orQ, 1);
        sim.ConnectGates(orQ, norQ, 0);

        // Q' = NOR(S, Q)
        sim.ConnectGates(s, orQNot, 0);
        sim.ConnectGates(norQ, orQNot, 1);
        sim.ConnectGates(orQNot, norQNot, 0);

        int qSink = sim.AddGate(GateKind.Sink);
        int qNotSink = sim.AddGate(GateKind.Sink);
        sim.ConnectGates(norQ, qSink, 0);
        sim.ConnectGates(norQNot, qNotSink, 0);

        // Reset state (R=1, S=0) -> Q=0, Q'=1
        sim.SetSource(r, true);
        sim.SetSource(s, false);
        _ = sim.RunUntilStable(10000);
        Assert.IsFalse(sim.GetOutput(qSink), "Q should be 0 after Reset (R=1)");
        Assert.IsTrue(sim.GetOutput(qNotSink), "Q' should be 1 after Reset (R=1)");

        // Hold (R=0, S=0) -> Q=0
        sim.SetSource(r, false);
        _ = sim.RunUntilStable(10000);
        Assert.IsFalse(sim.GetOutput(qSink), "Q should stay 0");

        // Set state (R=0, S=1) -> Q=1, Q'=0
        sim.SetSource(s, true);
        _ = sim.RunUntilStable(10000);
        Assert.IsTrue(sim.GetOutput(qSink), "Q should be 1 after Set (S=1)");
        Assert.IsFalse(sim.GetOutput(qNotSink), "Q' should be 0 after Set (S=1)");

        // Hold (R=0, S=0) -> Q=1
        sim.SetSource(s, false);
        _ = sim.RunUntilStable(10000);
        Assert.IsTrue(sim.GetOutput(qSink), "Q should stay 1");
    }

    [TestMethod]
    public void TestFullAdder()
    {
        ICircuitSimulator sim = CreateSimulator();

        int a = sim.AddGate(GateKind.Source);
        int b = sim.AddGate(GateKind.Source);
        int cin = sim.AddGate(GateKind.Source);

        int[] xorTable = { 0, 1, 1, 0 };
        int xor1 = sim.AddLutGate(2, xorTable);
        int xor2 = sim.AddLutGate(2, xorTable);

        int and1 = sim.AddGate(GateKind.And2);
        int and2 = sim.AddGate(GateKind.And2);
        int or1 = sim.AddGate(GateKind.Or2);

        sim.ConnectGates(a, xor1, 0);
        sim.ConnectGates(b, xor1, 1);

        sim.ConnectGates(xor1, xor2, 0);
        sim.ConnectGates(cin, xor2, 1);

        sim.ConnectGates(a, and1, 0);
        sim.ConnectGates(b, and1, 1);

        sim.ConnectGates(xor1, and2, 0);
        sim.ConnectGates(cin, and2, 1);

        sim.ConnectGates(and1, or1, 0);
        sim.ConnectGates(and2, or1, 1);

        int sumSink = sim.AddGate(GateKind.Sink);
        int coutSink = sim.AddGate(GateKind.Sink);
        sim.ConnectGates(xor2, sumSink, 0);
        sim.ConnectGates(or1, coutSink, 0);

        void Check(bool iA, bool iB, bool iC, bool expectedSum, bool expectedCout)
        {
            sim.SetSource(a, iA);
            sim.SetSource(b, iB);
            sim.SetSource(cin, iC);
            _ = sim.RunUntilStable(10000);
            Assert.AreEqual(expectedSum, sim.GetOutput(sumSink), $"Sum failed for {iA},{iB},{iC}");
            Assert.AreEqual(expectedCout, sim.GetOutput(coutSink), $"Cout failed for {iA},{iB},{iC}");
        }

        Check(false, false, false, false, false);
        Check(true, false, false, true, false);
        Check(false, true, false, true, false);
        Check(true, true, false, false, true);
        Check(false, false, true, true, false);
        Check(true, false, true, false, true);
        Check(false, true, true, false, true);
        Check(true, true, true, true, true);
    }

    [TestMethod]
    public void TestDLatch()
    {
        ICircuitSimulator sim = CreateSimulator();

        int dSource = sim.AddGate(GateKind.Source);
        int enSource = sim.AddGate(GateKind.Source);

        int notD = sim.AddGate(GateKind.Not);
        sim.ConnectGates(dSource, notD, 0);

        int sAnd = sim.AddGate(GateKind.And2);
        sim.ConnectGates(dSource, sAnd, 0);
        sim.ConnectGates(enSource, sAnd, 1);

        int rAnd = sim.AddGate(GateKind.And2);
        sim.ConnectGates(notD, rAnd, 0);
        sim.ConnectGates(enSource, rAnd, 1);

        int orQ = sim.AddGate(GateKind.Or2);
        int norQ = sim.AddGate(GateKind.Not);

        int orQNot = sim.AddGate(GateKind.Or2);
        int norQNot = sim.AddGate(GateKind.Not);

        sim.ConnectGates(rAnd, orQ, 0);
        sim.ConnectGates(norQNot, orQ, 1);
        sim.ConnectGates(orQ, norQ, 0);

        sim.ConnectGates(sAnd, orQNot, 0);
        sim.ConnectGates(norQ, orQNot, 1);
        sim.ConnectGates(orQNot, norQNot, 0);

        int qSink = sim.AddGate(GateKind.Sink);
        sim.ConnectGates(norQ, qSink, 0);

        // 1. Transparent mode (EN=1) -> Q follows D
        sim.SetSource(enSource, true);
        sim.SetSource(dSource, true);
        _ = sim.RunUntilStable(10000);
        Assert.IsTrue(sim.GetOutput(qSink), "Q should be 1 when D=1, EN=1");

        sim.SetSource(dSource, false);
        _ = sim.RunUntilStable(10000);
        Assert.IsFalse(sim.GetOutput(qSink), "Q should be 0 when D=0, EN=1");

        // 2. Latch mode (EN=0) -> Q stays same
        sim.SetSource(dSource, true);
        sim.SetSource(enSource, true);
        _ = sim.RunUntilStable(10000);
        Assert.IsTrue(sim.GetOutput(qSink));

        sim.SetSource(enSource, false); // LATCH
        _ = sim.RunUntilStable(10000);

        sim.SetSource(dSource, false); // Change D while latched
        _ = sim.RunUntilStable(10000);
        Assert.IsTrue(sim.GetOutput(qSink), "Q should stay 1 even if D changes while EN=0");
    }
}
