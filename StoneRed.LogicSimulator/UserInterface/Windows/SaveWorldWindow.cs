using Myra.Graphics2D.UI;

using StoneRed.LogicSimulator.Misc;
using StoneRed.LogicSimulator.WorldSaveSystem;

using System;
using System.IO;

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
        foreach (string directory in Directory.GetDirectories(Paths.GetWorldSavesPath()))
        {
            string saveName = Path.GetFileName(directory) ?? string.Empty;
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

    private void NewSaveInputDialog_Ok(object? sender, InputDialogEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Text))
        {
            srls.ShowWindow(new InfoDialog("Please enter a valid name"));
            return;
        }

        worldData.SaveName = e.Text;
        Save();
    }

    private void SaveButton_Clicked(object? sender, EventArgs e)
    {
        Save();
    }

    private void Save()
    {
        srls.ShowWindow(new SavingWorldWindow(worldData));
    }
}