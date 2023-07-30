using FluentResults;

using StoneRed.LogicSimulator.WorldSaveSystem.WorldWriters;

using System;
using System.Threading.Tasks;

namespace StoneRed.LogicSimulator.WorldSaveSystem;

internal class WorldSaver
{
    private readonly Srls srls;

    public WorldSaver(Srls srls)
    {
        this.srls = srls;
    }

    public Task<Result> SaveWorld(WorldData worldData, IProgress<WorldSaveLoadProgress> progress)
    {
        IWorldWriter? worldWriter = worldData.SaveVersion switch
        {
            1 => new WorldWriterV1(srls),
            _ => null
        };

        if (worldWriter is null)
        {
            return Task.FromResult(Result.Fail("Invalid file version!"));
        }

        return worldWriter.WriteWorld(worldData, progress);
    }
}