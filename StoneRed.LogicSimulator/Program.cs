using Myra.Utility;

using System;
using System.IO;

AppDomain.CurrentDomain.UnhandledException += (_, e) => File.WriteAllText(Path.Combine(PathUtils.ExecutingAssemblyDirectory, "error.txt"), e.ExceptionObject.ToString());

using StoneRed.LogicSimulator.Srls srls = new StoneRed.LogicSimulator.Srls();
srls.Run();