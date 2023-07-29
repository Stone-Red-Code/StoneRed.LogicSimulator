using FluentResults;

using StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces;
using StoneRed.LogicSimulator.WorldSaveSystem.WorldReaders;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace StoneRed.LogicSimulator.WorldSaveSystem;

internal class WorldLoader
{
    private readonly Srls srls;

    public WorldLoader(Srls srls)
    {
        this.srls = srls;
    }

    public Task<Result<IEnumerable<LogicGate>>> LoadWorld(string filePath, IProgress<WorldSaveLoadProgress> progress)
    {
        BinaryReader binaryReader = new BinaryReader(File.OpenRead(filePath));
        ushort fileVersion = binaryReader.ReadUInt16();
        binaryReader.Close();

        IWorldReader? worldReader = fileVersion switch
        {
            1 => new WorldReaderV1(srls),
            _ => null
        };

        if (worldReader is null)
        {
            return Task.FromResult(Result.Fail<IEnumerable<LogicGate>>("Invalid file version!"));
        }

        return worldReader.ReadWorld(filePath, progress);
    }
}