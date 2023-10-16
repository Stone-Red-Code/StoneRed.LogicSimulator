using Myra.Graphics2D.UI;

using StoneRed.LogicSimulator.Misc;
using StoneRed.LogicSimulator.UserInterface.Screens;

using System;
using System.IO;

namespace StoneRed.LogicSimulator.UserInterface.Windows;

internal class LoadWorldWindow : SrlsWindow
{
    private ListBox savesListBox = null!;
    private TextButton loadButton = null!;

    protected override string XmmpPath => "LoadWorldWindow.xmmp";

    public override void Initialize()
    {
        savesListBox = window.FindChildById<ListBox>("saves");
        loadButton = window.FindChildById<TextButton>("load");

        loadButton.Click += LoadButton_Clicked;

        foreach (string directory in Directory.GetDirectories(Paths.GetWorldSavesPath()))
        {
            string saveName = Path.GetFileName(directory) ?? string.Empty;
            savesListBox.Items.Add(new(saveName));
        }

        savesListBox.SelectedIndexChanged += SavesListBox_SelectedIndexChanged;
    }

    private void SavesListBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        loadButton.Enabled = savesListBox.SelectedItem is not null;
    }

    private void LoadButton_Clicked(object? sender, EventArgs e)
    {
        srls.LoadScreen(new LoadingScreen(savesListBox.SelectedItem.Text));
    }
}