using Microsoft.Xna.Framework.Graphics;

using Myra.Graphics2D.UI;

using StoneRed.LogicSimulator.Utilities;

using System.Collections.Generic;

namespace StoneRed.LogicSimulator.UserInterface.Windows;

internal class SettingsWindow : SrlsWindow
{
    private ListBox resolutionsListBox = null!;
    private SpinButton scaleSpinButton = null!;
    private List<Resolution> resolutions = null!;
    protected override string XmmpPath => "SettingsWindow.xmmp";

    public override void Initialize()
    {
        window.ClipToBounds = true;
        scaleSpinButton = window.FindChildById<SpinButton>("scale");
        resolutionsListBox = window.FindChildById<ListBox>("resolutions");
        window.FindChildById<TextButton>("apply").Click += ApplyButton_Click;
        window.FindChildById<TextButton>("cancel").Click += CancelButton_Click;

        resolutions = ResolutionCalculator.GetResolutions(new Resolution(GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height));

        scaleSpinButton.Value = srls.Settings.Scale;

        int index = 0;

        foreach (Resolution resolution in resolutions)
        {
            resolutionsListBox.Items.Add(new ListItem($"{resolution.Width}x{resolution.Height}"));

            if (resolution.Width == srls.Graphics.PreferredBackBufferWidth && resolution.Height == srls.Graphics.PreferredBackBufferHeight)
            {
                resolutionsListBox.SelectedIndex = index;
            }

            index++;
        }
    }

    private void ApplyButton_Click(object? sender, System.EventArgs e)
    {
        Resolution newResolution = resolutions[resolutionsListBox.SelectedIndex.GetValueOrDefault()];

        srls.Graphics.PreferredBackBufferWidth = newResolution.Width;
        srls.Graphics.PreferredBackBufferHeight = newResolution.Height;

        srls.Graphics.ApplyChanges();

        srls.Settings.Resolution = newResolution;
        srls.Settings.Scale = scaleSpinButton.Value.GetValueOrDefault(1);
        srls.Settings.Save(srls.SettingsPath);

        window.Close();

        SdlMethods.RestoreWindow(srls.Window.Handle);
        SdlMethods.MaximizeWindow(srls.Window.Handle);
    }

    private void CancelButton_Click(object? sender, System.EventArgs e)
    {
        window.Close();
    }
}