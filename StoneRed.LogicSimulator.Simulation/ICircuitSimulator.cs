namespace StoneRed.LogicSimulator.Simulation;

/// <summary>
/// Represents a digital logic circuit simulator that supports combinational logic gates,
/// look-up tables (LUTs), and hierarchical macro gates.
/// </summary>
public interface ICircuitSimulator
{
    /// <summary>
    /// Gets the total number of gates in the circuit.
    /// </summary>
    int GateCount { get; }

    /// <summary>
    /// Adds a logic gate to the circuit.
    /// </summary>
    /// <param name="kind">The type of gate to add (NOT, AND, OR, etc.).</param>
    /// <returns>The unique identifier of the newly created gate.</returns>
    /// <exception cref="InvalidOperationException">Thrown when attempting to add a LUT gate using this method. Use <see cref="AddLutGate"/> instead.</exception>
    int AddGate(GateKind kind);

    /// <summary>
    /// Adds a Look-Up Table (LUT) gate that implements arbitrary combinational logic.
    /// </summary>
    /// <param name="inputCount">The number of inputs (0-30).</param>
    /// <param name="table">The truth table array. Length must be 2^inputCount. Each element is the output (0 or 1) for the corresponding input pattern.</param>
    /// <returns>The unique identifier of the newly created LUT gate.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when inputCount is not between 0 and 30.</exception>
    /// <exception cref="ArgumentNullException">Thrown when table is null.</exception>
    /// <exception cref="ArgumentException">Thrown when table length doesn't match 2^inputCount.</exception>
    int AddLutGate(int inputCount, int[] table);

    /// <summary>
    /// Connects the output of one gate to the input of another gate.
    /// A single output can be connected to multiple inputs (fan-out).
    /// </summary>
    /// <param name="fromGate">The gate ID whose output will be connected.</param>
    /// <param name="toGate">The gate ID that will receive the signal.</param>
    /// <param name="toInputBit">The input bit position (0-31) on the destination gate.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when gate IDs are invalid or toInputBit is not between 0 and 31.</exception>
    void ConnectGates(int fromGate, int toGate, int toInputBit);

    /// <summary>
    /// Registers a reusable circuit definition as a macro gate that can be instantiated multiple times.
    /// </summary>
    /// <param name="name">The unique name for this macro gate.</param>
    /// <param name="definition">The circuit definition containing gates and connections.</param>
    void RegisterMacroGate(string name, CircuitDefinition definition);

    /// <summary>
    /// Computes and caches a Look-Up Table representation of a registered macro gate for optimization.
    /// This converts the macro's combinational logic into a truth table for faster simulation.
    /// </summary>
    /// <param name="name">The name of the registered macro gate.</param>
    /// <param name="maxSteps">Maximum simulation steps allowed to compute each output pattern. Default is 4096.</param>
    /// <returns>True if the LUT was successfully computed; false if the circuit didn't stabilize within maxSteps.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the macro name is not registered.</exception>
    bool ComputeLut(string name, int maxSteps = 4096);

    /// <summary>
    /// Adds an instance of a registered macro gate to the circuit.
    /// If a LUT has been computed for this macro, the optimized version is used.
    /// </summary>
    /// <param name="name">The name of the registered macro gate.</param>
    /// <returns>A <see cref="MacroInstance"/> containing the gate IDs of the instance's inputs and outputs.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the macro name is not registered.</exception>
    MacroInstance AddMacroGate(string name);

    /// <summary>
    /// Returns the circuit to its initial state (all signals at 0) and 
    /// kickstarts the simulation logic (evaluating gates like NOT).
    /// This is automatically called on the first Step if not called manually.
    /// </summary>
    void Reset();

    /// <summary>
    /// Sets the input signal value on a Source gate.
    /// Only gates of type <see cref="GateKind.Source"/> can have their values set.
    /// </summary>
    /// <param name="gateId">The gate ID of the source gate.</param>
    /// <param name="value">The boolean value to set (true = 1, false = 0).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when gateId is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the gate is not a Source gate.</exception>
    void SetSource(int gateId, bool value);

    /// <summary>
    /// Reads the current output signal of a gate.
    /// </summary>
    /// <param name="gateId">The gate ID to read from.</param>
    /// <returns>True if the output is 1, false if 0.</returns>
    bool GetOutput(int gateId);

    /// <summary>
    /// Executes one simulation step. The behavior depends on the implementation:
    /// - EventCircuitSimulator: Processes all pending changes in the propagation queue.
    /// - CycleCircuitSimulator: Evaluates all gates once synchronously.
    /// </summary>
    void Step();

    /// <summary>
    /// Runs the simulation until all signals stabilize (no more changes occur).
    /// </summary>
    /// <param name="maxSteps">Maximum number of steps to execute before timing out. Default is 1024.</param>
    /// <returns>The number of steps executed before stabilization.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the circuit does not stabilize within maxSteps.</exception>
    int RunUntilStable(int maxSteps = 1024);

    /// <summary>
    /// Attempts to run the simulation until all signals stabilize.
    /// </summary>
    /// <param name="maxSteps">Maximum number of steps to execute.</param>
    /// <param name="steps">Output parameter containing the number of steps executed.</param>
    /// <returns>True if the circuit stabilized; false if maxSteps was exceeded.</returns>
    bool TryRunUntilStable(int maxSteps, out int steps);

    /// <summary>
    /// Subscribes to input mask changes on a specific gate.
    /// The callback is invoked whenever the gate's input mask changes.
    /// </summary>
    /// <param name="gateId">The gate ID to watch.</param>
    /// <param name="callback">Action to invoke on change. Parameters are (gateId, newInputMask).</param>
    /// <returns>An <see cref="IDisposable"/> that unsubscribes the watcher when disposed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when callback is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when gateId is invalid.</exception>
    IDisposable WatchGate(int gateId, Action<int, int> callback);
}
