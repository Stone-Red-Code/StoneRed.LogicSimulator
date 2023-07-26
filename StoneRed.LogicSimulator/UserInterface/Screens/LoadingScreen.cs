using Microsoft.Xna.Framework;

using Myra.Graphics2D.UI;

using StoneRed.LogicSimulator.Simulation.LogicGates;
using StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StoneRed.LogicSimulator.UserInterface.Screens;

internal class LoadingScreen : SrlsScreen<VerticalStackPanel>
{
    private Label label = null!;
    private HorizontalProgressBar horizontalProgressBar = null!;

    protected override string XmmpPath => "LoadingScreen.xmmp";

    protected override void Initialize()
    {
        label = Root.FindChildById<Label>("label");
        horizontalProgressBar = Root.FindChildById<HorizontalProgressBar>("progressBar");
        horizontalProgressBar.Maximum = 100;
    }

    protected override void LoadContent()
    {
        _ = Task.Run(async () =>
        {
            for (int i = 0; i < 100; i++)
            {
                horizontalProgressBar.Value++;
                await Task.Delay(1);
            }
        });
    }

    protected override void Update(GameTime gameTime)
    {
        label.Text = $"Loading... {Math.Round(100 / horizontalProgressBar.Maximum * horizontalProgressBar.Value, 0)}%";
        if (horizontalProgressBar.Value >= horizontalProgressBar.Maximum)
        {
            Clock button = new Clock()
            {
                Id = 0,
                Metadata = new LogicGateMetadata()
                {
                    Name = "Clock",
                    Position = new Vector2(0, 0)
                }
            };

            Switch @switch = new Switch()
            {
                Id = 1,
                Metadata = new LogicGateMetadata()
                {
                    Name = "Switch",
                    Position = new Vector2(0, 150)
                }
            };

            TestLogicGate testLogicGate1 = new TestLogicGate()
            {
                Id = 2,
                Metadata = new LogicGateMetadata()
                {
                    Name = "L1",
                    Position = new Vector2(150, 150)
                }
            };

            Pin testLogicGate2 = new Pin()
            {
                Id = 3,
                Metadata = new LogicGateMetadata()
                {
                    Name = "Pin",
                    Position = new Vector2(300, 300)
                }
            };

            Pin testLogicGate3 = new Pin()
            {
                Id = 4,
                Metadata = new LogicGateMetadata()
                {
                    Name = "Pin",
                    Position = new Vector2(450, 450)
                }
            };

            Lamp lamp = new Lamp()
            {
                Id = 5,
                Metadata = new LogicGateMetadata()
                {
                    Name = "Lamp",
                    Position = new Vector2(600, 450)
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
}