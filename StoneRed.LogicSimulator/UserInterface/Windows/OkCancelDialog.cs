using Myra.Graphics2D.UI;

using System;

namespace StoneRed.LogicSimulator.UserInterface.Windows;

internal class OkCancelDialog : SrlsWindow
{
    public event EventHandler? Ok;

    public event EventHandler? Cancel;

    private readonly string labelText;
    private readonly string okButtonText;
    private readonly string cancelButtonText;
    protected override string XmmpPath => "OkCancelDialog.xmmp";

    public OkCancelDialog(string labelText)
    {
        this.labelText = labelText;
        okButtonText = "Ok";
        cancelButtonText = "Cancel";
    }

    public OkCancelDialog(string labelText, string okButtonText, string cancelButtonText)
    {
        this.labelText = labelText;
        this.okButtonText = okButtonText;
        this.cancelButtonText = cancelButtonText;
    }

    public override void Initialize()
    {
        Label label = window.FindChildById<Label>("label");
        TextButton okButton = window.FindChildById<TextButton>("ok");
        TextButton cancelButton = window.FindChildById<TextButton>("cancel");

        label.Text = labelText;
        okButton.Text = okButtonText;
        cancelButton.Text = cancelButtonText;

        okButton.Click += OkButton_Click;
        cancelButton.Click += CancelButton_Click;
    }

    private void OkButton_Click(object? sender, EventArgs e)
    {
        Ok?.Invoke(this, EventArgs.Empty);
        Close();
    }

    private void CancelButton_Click(object? sender, EventArgs e)
    {
        Cancel?.Invoke(this, EventArgs.Empty);
        Close();
    }
}