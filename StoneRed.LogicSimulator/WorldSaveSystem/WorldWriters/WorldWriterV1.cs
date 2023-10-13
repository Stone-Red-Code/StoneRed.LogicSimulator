using FluentResults;

using StoneRed.LogicSimulator.Api.Attributes;
using StoneRed.LogicSimulator.Api.Interfaces;
using StoneRed.LogicSimulator.Misc;
using StoneRed.LogicSimulator.Utilities;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StoneRed.LogicSimulator.WorldSaveSystem.WorldWriters;

internal class WorldWriterV1 : IWorldWriter
{
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

            writer.Write((ushort)1); // Write file version
            writer.Write(numberOfLogicGates);

            int gateNumber = 0;

            foreach (LogicGate logicGate in worldData.LogicGates)
            {
                gateNumber++;
                progress.Report(new((int)((double)gateNumber / numberOfLogicGates * 50d), $"Saving logic gates ({gateNumber}/{numberOfLogicGates})"));

                writer.Write(logicGate.Id);

                if (!LogicGatesManager.TryGetTypeLogicGateName(logicGate.GetType(), out string? typeName))
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