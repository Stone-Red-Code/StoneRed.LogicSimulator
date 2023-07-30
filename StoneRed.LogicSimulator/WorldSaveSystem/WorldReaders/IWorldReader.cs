using FluentResults;

using System;
using System.Threading.Tasks;

namespace StoneRed.LogicSimulator.WorldSaveSystem.WorldReaders;

internal interface IWorldReader
{
    Task<Result<WorldData>> ReadWorld(string saveName, IProgress<WorldSaveLoadProgress> progress);
}