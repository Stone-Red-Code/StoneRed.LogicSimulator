namespace StoneRed.LogicSimulator.Simulation;

/// <summary>
/// Represents a digital logic simulator.
/// </summary>
public interface ICircuitSimulator
{
    int GateCount { get; }
    int AddGate(GateKind kind);
    int AddLutGate(int inputCount, int[] table);
    void ConnectGates(int fromGate, int toGate, int toInputBit);
    void RegisterMacroGate(string name, CircuitDefinition definition);
    bool ComputeLut(string name, int maxSteps = 4096);
    MacroInstance AddMacroGate(string name);

    /// <summary>
    /// Returns the circuit to its initial state (all signals at 0) and 
    /// kickstarts the simulation logic (evaluating gates like NOT).
    /// This is automatically called on the first Step if not called manually.
    /// </summary>
    void Reset();

    void SetSource(int gateId, bool value);
    bool GetOutput(int gateId);
    void Step();
    int RunUntilStable(int maxSteps = 1024);
    bool TryRunUntilStable(int maxSteps, out int steps);
    IDisposable WatchGate(int gateId, Action<int, int> callback);
}
