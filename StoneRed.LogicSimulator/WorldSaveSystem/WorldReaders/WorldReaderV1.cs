using FluentResults;

using StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StoneRed.LogicSimulator.WorldSaveSystem.WorldReaders;

internal class WorldReaderV1 : IWorldReader
{
    private readonly Srls srls;

    public WorldReaderV1(Srls srls)
    {
        this.srls = srls;
    }

    public async Task<Result<IEnumerable<LogicGate>>> ReadWorld(string filePath, IProgress<WorldSaveLoadProgress> progress)
    {
        if (!File.Exists(filePath))
        {
            return Result.Fail($"File \"{filePath}\" does not exist");
        }

        return await Task.Run(() => InternalReadWorld(filePath, progress));
    }

    public Result<IEnumerable<LogicGate>> InternalReadWorld(string filePath, IProgress<WorldSaveLoadProgress> progress)
    {
        BinaryReader? reader = null;
        Dictionary<LogicGate, List<(ulong GateRefId, int inputIndex, int outputIndex)>> connections = new();

        try
        {
            progress.Report(new(0, "Opening file"));

            reader = new BinaryReader(File.OpenRead(filePath));
            _ = reader.ReadUInt16(); // File version

            int numberOfLogicGates = reader.ReadInt32();

            for (int gateNumber = 0; gateNumber < numberOfLogicGates; gateNumber++)
            {
                progress.Report(new((int)((double)gateNumber / numberOfLogicGates * 50d), $"Loading logic gates ({gateNumber / numberOfLogicGates})"));

                ulong id = reader.ReadUInt64();
                string typeName = reader.ReadString();
                int inputCount = reader.ReadInt32();
                int outputCount = reader.ReadInt32();
                int numberOfConnections = reader.ReadInt32();

                if (srls.LogicGatesManager.TryCreateLogicGate(typeName, out LogicGate? logicGate))
                {
                    logicGate.Id = id;
                    logicGate.InputCount = inputCount;
                    logicGate.OutputCount = outputCount;
                }
                else
                {
                    return Result.Fail($"Logic gate type \"{typeName}\" does not exist!");
                }

                connections.Add(logicGate, new());

                for (int connectionNumber = 0; connectionNumber < numberOfConnections; connectionNumber++)
                {
                    ulong gateRefId = reader.ReadUInt64();
                    int inputIndex = reader.ReadInt32();
                    int outputIndex = reader.ReadInt32();

                    connections[logicGate].Add((gateRefId, inputIndex, outputIndex));
                }
            }

            int connectionCount = 0;

            foreach (KeyValuePair<LogicGate, List<(ulong gateRefId, int inputIndex, int outputIndex)>> connectionPair in connections)
            {
                progress.Report(new((int)((double)connections.Count / connectionCount * 50d), $"Connecting logic gates ({connections.Count / connectionCount})"));

                LogicGate logicGate = connectionPair.Key;

                foreach ((ulong gateRefId, int inputIndex, int outputIndex) in connectionPair.Value)
                {
                    LogicGate? connectedGate = connections.Keys.FirstOrDefault(g => g.Id == gateRefId);

                    if (connectedGate is null)
                    {
                        return Result.Fail($"Logic gate connection {logicGate.Id} -> {gateRefId} does not exist!");
                    }

                    logicGate.Connect(connectedGate, inputIndex, outputIndex);
                }

                connectionCount++;
            }
        }
        catch (IOException ex)
        {
            return Result.Fail(ex.Message);
        }
        finally
        {
            reader?.Dispose();
        }

        return connections.Keys;
    }
}