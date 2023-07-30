using FluentResults;

using StoneRed.LogicSimulator.Misc;
using StoneRed.LogicSimulator.Simulation.LogicGates.Attributes;
using StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces;

using System;
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

    public Task<Result> WriteWorld(WorldData worldData, IProgress<WorldSaveLoadProgress> progress)
    {
        return Task.Run(() => InternalWriteWorld(worldData, progress));
    }

    private Result InternalWriteWorld(WorldData worldData, IProgress<WorldSaveLoadProgress> progress)
    {
        BinaryWriter? writer = null;

        try
        {
            string filePath = Paths.GetWorldSaveFilePath(worldData.SaveName);

            writer = new BinaryWriter(File.Open(filePath, FileMode.Create));

            progress.Report(new(0, "Counting logic gates"));

            int numberOfLogicGates = worldData.LogicGates.Count();

            writer.Write((short)1); // Write file version
            writer.Write(numberOfLogicGates);

            foreach (LogicGate logicGate in worldData.LogicGates)
            {
                writer.Write(logicGate.Id);

                if (!srls.LogicGatesManager.TryGetTypeName(logicGate.GetType(), out string? typeName))
                {
                    return Result.Fail($"Logic gate type \"{logicGate.GetType().FullName}\" has no \"{nameof(LogicGateNameAttribute)}\" attribute!");
                }

                writer.Write(typeName);
                writer.Write(logicGate.InputCount);
                writer.Write(logicGate.OutputCount);
                writer.Write(logicGate.WorldData.Name);
                writer.Write(logicGate.WorldData.Description);
                writer.Write(logicGate.WorldData.Position.X);
                writer.Write(logicGate.WorldData.Position.Y);
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