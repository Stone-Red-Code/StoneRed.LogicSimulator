using FluentResults;

using StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StoneRed.LogicSimulator.WorldSaveSystem.WorldWriters;

internal interface IWorldWriter
{
    Task<Result> WriteWorld(string filePath, IEnumerable<LogicGate> logicGates, IProgress<WorldSaveLoadProgress> progress);
}