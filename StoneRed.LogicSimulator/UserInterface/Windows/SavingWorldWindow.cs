using FluentResults;

using Myra.Graphics2D.UI;

using StoneRed.LogicSimulator.WorldSaveSystem;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace StoneRed.LogicSimulator.UserInterface.Windows;

internal class SavingWorldWindow : SrlsWindow
{
    private readonly WorldData worldData;
    private bool windowClosable = false;
    private Label label = null!;
    private HorizontalProgressBar horizontalProgressBar = null!;
    protected override string XmmpPath => "SavingWorldWindow.xmmp";

    public SavingWorldWindow(WorldData worldData)
    {
        this.worldData = worldData;
    }

    public override void Initialize()
    {
        window.Closing += (_, e) => e.Cancel = !windowClosable;

        label = window.FindChildById<Label>("label");
        horizontalProgressBar = window.FindChildById<HorizontalProgressBar>("progressBar");
        horizontalProgressBar.Maximum = 100;

        SaveWorld();
    }

    private void SaveWorld()
    {
        Progress<WorldSaveLoadProgress> progress = new Progress<WorldSaveLoadProgress>();
        progress.ProgressChanged += Progress_ProgressChanged;

        WorldSaver worldLoader = new WorldSaver(srls);

        _ = Task.Run(async () =>
        {
            Result<WorldData> result = await worldLoader.SaveWorld(worldData, progress);

            windowClosable = true;

            if (result.IsSuccess)
            {
                Close();
            }
            else
            {
                srls.ShowWindow(new InfoDialog(string.Join(',', result.Errors.Select(e => e.Message))));
            }
        });
    }

    private void Progress_ProgressChanged(object? sender, WorldSaveLoadProgress e)
    {
        horizontalProgressBar.Value = e.Percentage;
        label.Text = e.Message;
    }
}