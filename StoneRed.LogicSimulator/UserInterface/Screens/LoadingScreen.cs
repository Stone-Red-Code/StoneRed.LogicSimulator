using FluentResults;

using Microsoft.Xna.Framework;

using Myra.Graphics2D.UI;

using StoneRed.LogicSimulator.UserInterface.Windows;
using StoneRed.LogicSimulator.WorldSaveSystem;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace StoneRed.LogicSimulator.UserInterface.Screens;

internal class LoadingScreen : SrlsScreen<VerticalStackPanel>
{
    private readonly string saveName;
    private Label label = null!;
    private HorizontalProgressBar horizontalProgressBar = null!;
    protected override string XmmpPath => "LoadingScreen.xmmp";

    public LoadingScreen(string saveName)
    {
        this.saveName = saveName;
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
            Result<WorldData> result = await worldLoader.LoadWorld(saveName, progress);

            if (result.IsSuccess)
            {
                srls.LoadScreen(new WorldScreen(result.Value));
            }
            else
            {
                srls.LoadScreen<StartScreen>();
                srls.ShowWindow(new InfoDialog(string.Join(',', result.Errors.Select(e => e.Message))));
            }
        });
    }

    protected override void Update(GameTime gameTime)
    {
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