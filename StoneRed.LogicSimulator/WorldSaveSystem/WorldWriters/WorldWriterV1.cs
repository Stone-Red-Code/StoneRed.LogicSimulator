using FluentResults;

using StoneRed.LogicSimulator.Simulation.LogicGates.Attributes;
using StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StoneRed.LogicSimulator.WorldSaveSystem.WorldWriters;

internal class WorldWriterV1 : IWorldWriter
{
    private readonly Srls srls;

    public WorldWriterV1(Srls srls)
    {
        this.srls = srls;
    }

    public Task<Result> WriteWorld(string filePath, IEnumerable<LogicGate> logicGates, IProgress<WorldSaveLoadProgress> progress)
    {
        return Task.Run(() => InternalWriteWorld(filePath, logicGates, progress));
    }

    private Result InternalWriteWorld(string filePath, IEnumerable<LogicGate> logicGates, IProgress<WorldSaveLoadProgress> progress)
    {
        BinaryWriter? writer = null;

        try
        {
            writer = new BinaryWriter(File.OpenWrite(filePath));

            progress.Report(new(0, "Counting logic gates"));

            int numberOfLogicGates = logicGates.Count();

            writer.Write(1); // Write file version
            writer.Write(numberOfLogicGates);

            foreach (LogicGate logicGate in logicGates)
            {
                writer.Write(logicGate.Id);

                if (!srls.LogicGatesManager.TryGetTypeName(logicGate.GetType(), out string? typeName))
                {
                    return Result.Fail($"Logic gate type \"{logicGate.GetType().FullName}\" has no \"{nameof(LogicGateNameAttribute)}\" attribute!");
                }

                writer.Write(typeName);
                writer.Write(logicGate.InputCount);
                writer.Write(logicGate.OutputCount);
                writer.Write(logicGate.LogicGateConnections.Count);

                foreach (LogicGateConnection connection in logicGate.LogicGateConnections)
                {
                    writer.Write(connection.LogicGate.Id);
                    writer.Write(connection.InputIndex);
                    writer.Write(connection.OutputIndex);
                }
            }
        }
        catch (IOException ex)
        {
            return Result.Fail(ex.Message);
        }
        finally
        {
            writer?.Dispose();
        }

        return Result.Ok();
    }
}