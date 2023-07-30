using FluentResults;

using System;
using System.Threading.Tasks;

namespace StoneRed.LogicSimulator.WorldSaveSystem.WorldWriters;

internal interface IWorldWriter
{
    Task<Result> WriteWorld(WorldData worldData, IProgress<WorldSaveLoadProgress> progress);
}