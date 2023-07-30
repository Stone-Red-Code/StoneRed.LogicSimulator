using Myra.Graphics2D.UI;

using System;

namespace StoneRed.LogicSimulator.UserInterface.Windows;

internal class InfoDialog : SrlsWindow
{
    public event EventHandler? Ok;

    private readonly string infoText;
    protected override string XmmpPath => "InfoDialog.xmmp";

    public InfoDialog(string infoText)
    {
        this.infoText = infoText;
    }

    public override void Initialize()
    {
        Label label = window.FindChildById<Label>("info");
        TextButton okButton = window.FindChildById<TextButton>("ok");

        label.Text = infoText;

        okButton.Click += OkButton_Click;
    }

    private void OkButton_Click(object? sender, EventArgs e)
    {
        Ok?.Invoke(this, EventArgs.Empty);
        Close();
    }
}