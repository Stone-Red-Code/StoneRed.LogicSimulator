using FluentResults;

using StoneRed.LogicSimulator.WorldSaveSystem.WorldWriters;

using System;
using System.Threading.Tasks;

namespace StoneRed.LogicSimulator.WorldSaveSystem;

internal class WorldSaver
{
    public WorldSaver(Srls _)
    {

    }

    public Task<Result> SaveWorld(WorldData worldData, IProgress<WorldSaveLoadProgress> progress)
    {
        IWorldWriter? worldWriter = worldData.SaveVersion switch
        {
            1 => new WorldWriterV1(),
            _ => null
        };

        if (worldWriter is null)
        {
            return Task.FromResult(Result.Fail("Invalid file version!"));
        }

        return worldWriter.WriteWorld(worldData, progress);
    }
}