using FluentResults;

using Myra.Graphics2D.UI;

using StoneRed.LogicSimulator.Misc;
using StoneRed.LogicSimulator.WorldSaveSystem;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StoneRed.LogicSimulator.UserInterface.Windows;

internal class SaveWorldWindow : SrlsWindow
{
    private readonly WorldData worldData;
    private ListBox savesListBox = null!;
    private TextButton saveButton = null!;

    protected override string XmmpPath => "SaveWorldWindow.xmmp";

    public SaveWorldWindow(WorldData worldData)
    {
        this.worldData = worldData;
    }

    public override void Initialize()
    {
        savesListBox = window.FindChildById<ListBox>("saves");
        saveButton = window.FindChildById<TextButton>("save");
        TextButton newSaveButton = window.FindChildById<TextButton>("newSave");

        saveButton.Click += SaveButton_Clicked;
        newSaveButton.Click += NewSaveButton_Click;

        int index = 0;
        foreach (string directories in Directory.GetDirectories(Paths.GetWorldSavesPath()))
        {
            string saveName = Path.GetFileName(directories) ?? string.Empty;
            savesListBox.Items.Add(new(saveName));

            if (saveName == worldData.SaveName)
            {
                savesListBox.SelectedIndex = index;
                saveButton.Enabled = true;
            }
            index++;
        }

        savesListBox.SelectedIndexChanged += SavesListBox_SelectedIndexChanged;
    }

    private void SavesListBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        saveButton.Enabled = savesListBox.SelectedItem is not null;
    }

    private void NewSaveButton_Click(object? sender, EventArgs e)
    {
        InputDialog inputDialog = new InputDialog("Enter a name for the new save");
        inputDialog.Ok += NewSaveInputDialog_Ok;

        srls.ShowWindow(inputDialog);
    }

    private async void NewSaveInputDialog_Ok(object? sender, InputDialogEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Text))
        {
            srls.ShowWindow(new InfoDialog("Please enter a valid name"));
            return;
        }

        worldData.SaveName = e.Text;
        await Save();
    }

    private async void SaveButton_Clicked(object? sender, EventArgs e)
    {
        await Save();
    }

    private async Task Save()
    {
        WorldSaver worldSaver = new WorldSaver(srls);
        Result result = await worldSaver.SaveWorld(worldData, new Progress<WorldSaveLoadProgress>());

        if (result.IsSuccess)
        {
            _ = Dialog.CreateMessageBox("Success", "Saved world!");
        }
        else
        {
            _ = Dialog.CreateMessageBox("Error", string.Join(',', result.Errors.Select(e => e.Message)));
        }

        Close();
    }
}