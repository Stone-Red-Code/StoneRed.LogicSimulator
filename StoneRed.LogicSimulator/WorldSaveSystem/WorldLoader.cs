using FluentResults;

using StoneRed.LogicSimulator.Misc;
using StoneRed.LogicSimulator.WorldSaveSystem.WorldReaders;

using System;
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

    public Task<Result<WorldData>> LoadWorld(string saveName, IProgress<WorldSaveLoadProgress> progress)
    {
        ushort fileVersion;
        BinaryReader? binaryReader = null;

        try
        {
            binaryReader = new BinaryReader(File.OpenRead(Paths.GetWorldSaveFilePath(saveName)));
            fileVersion = binaryReader.ReadUInt16();
        }
        catch (IOException ex)
        {
            return Task.FromResult(Result.Fail<WorldData>(ex.Message));
        }
        finally
        {
            binaryReader?.Close();
        }

        IWorldReader? worldReader = fileVersion switch
        {
            1 => new WorldReaderV1(srls),
            _ => null
        };

        if (worldReader is null)
        {
            return Task.FromResult(Result.Fail<WorldData>("Invalid file version!"));
        }

        return worldReader.ReadWorld(saveName, progress);
    }
}