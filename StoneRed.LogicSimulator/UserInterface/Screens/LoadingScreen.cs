using FluentResults;

using Microsoft.Xna.Framework;

using Myra.Graphics2D.UI;

using StoneRed.LogicSimulator.Simulation.LogicGates;
using StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces;
using StoneRed.LogicSimulator.WorldSaveSystem;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StoneRed.LogicSimulator.UserInterface.Screens;

internal class LoadingScreen : SrlsScreen<VerticalStackPanel>
{
    private readonly string filePath;
    private Label label = null!;
    private HorizontalProgressBar horizontalProgressBar = null!;
    protected override string XmmpPath => "LoadingScreen.xmmp";

    public LoadingScreen(string filePath)
    {
        this.filePath = filePath;
    }

    protected override void Initialize()
    {
        label = Root.FindChildById<Label>("label");
        horizontalProgressBar = Root.FindChildById<HorizontalProgressBar>("progressBar");
        horizontalProgressBar.Maximum = 100;
    }

    protected override void LoadContent()
    {
        Progress<WorldSaveLoadProgress> progress = new Progress<WorldSaveLoadProgress>();
        progress.ProgressChanged += Progress_ProgressChanged;

        WorldLoader worldLoader = new WorldLoader(srls);

        _ = Task.Run(async () =>
        {
            Result<IEnumerable<LogicGate>> result = await worldLoader.LoadWorld(filePath, progress);
            if (result.IsFailed)
            {
                Dialog.CreateMessageBox("Error", string.Join(',', result.Errors.Select(e => e.Message))).Show(srls.Desktop);
            }
        });
    }

    protected override void Update(GameTime gameTime)
    {
        return;

        label.Text = $"Loading... {Math.Round(100 / horizontalProgressBar.Maximum * horizontalProgressBar.Value, 0)}%";
        if (horizontalProgressBar.Value >= horizontalProgressBar.Maximum)
        {
            Clock button = new Clock()
            {
                Id = 0,
                WorldData = new LogicGateWorldData()
                {
                    Name = "Clock",
                    Position = new Vector2(0, 0)
                }
            };

            Switch @switch = new Switch()
            {
                Id = 1,
                WorldData = new LogicGateWorldData()
                {
                    Name = "Switch",
                    Position = new Vector2(0, 200)
                }
            };

            NotGate testLogicGate1 = new NotGate()
            {
                Id = 2,
                WorldData = new LogicGateWorldData()
                {
                    Name = "Not Gate",
                    Position = new Vector2(200, 200)
                }
            };

            Pin testLogicGate2 = new Pin()
            {
                Id = 3,
                WorldData = new LogicGateWorldData()
                {
                    Name = "Pin",
                    Position = new Vector2(400, 400)
                }
            };

            Pin testLogicGate3 = new Pin()
            {
                Id = 4,
                WorldData = new LogicGateWorldData()
                {
                    Name = "Pin",
                    Position = new Vector2(500, 500)
                }
            };

            Lamp lamp = new Lamp()
            {
                Id = 5,
                WorldData = new LogicGateWorldData()
                {
                    Name = "Lamp",
                    Position = new Vector2(600, 600)
                }
            };

            button.Connect(testLogicGate1, 0, 0);
            @switch.Connect(testLogicGate1, 0, 0);
            testLogicGate1.Connect(testLogicGate2, 0, 0);
            testLogicGate2.Connect(testLogicGate3, 0, 0);
            testLogicGate3.Connect(lamp, 0, 0);

            List<LogicGate> logicGates = new List<LogicGate>
            {
                testLogicGate3,
                button,
                testLogicGate1,
                testLogicGate2,
                lamp,
                @switch
            };

            srls.LoadScreen(new WorldScreen(logicGates));
        }
    }

    protected override void Draw(GameTime gameTime)
    {
    }

    private void Progress_ProgressChanged(object? sender, WorldSaveLoadProgress e)
    {
        horizontalProgressBar.Value = e.Percentage;
        label.Text = e.Message;
    }
}