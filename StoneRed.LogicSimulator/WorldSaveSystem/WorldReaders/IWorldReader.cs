using FluentResults;

using StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StoneRed.LogicSimulator.WorldSaveSystem.WorldReaders;

internal interface IWorldReader
{
    Task<Result<IEnumerable<LogicGate>>> ReadWorld(string filePath, IProgress<WorldSaveLoadProgress> progress);
}