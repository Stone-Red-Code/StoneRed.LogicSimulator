using Myra.Graphics2D.UI;

using System;

namespace StoneRed.LogicSimulator.UserInterface.Windows;

internal class InputDialog : SrlsWindow
{
    public event EventHandler<InputDialogEventArgs>? Ok;

    public event EventHandler? Cancel;

    private readonly string labelText;
    private readonly string okButtonText;
    private readonly string cancelButtonText;
    private TextBox textBox = null!;

    protected override string XmmpPath => "InputDialog.xmmp";

    public InputDialog(string labelText)
    {
        this.labelText = labelText;
        okButtonText = "Ok";
        cancelButtonText = "Cancel";
    }

    public InputDialog(string labelText, string okButtonText, string cancelButtonText)
    {
        this.labelText = labelText;
        this.okButtonText = okButtonText;
        this.cancelButtonText = cancelButtonText;
    }

    public override void Initialize()
    {
        textBox = window.FindChildById<TextBox>("textBox");
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
        Ok?.Invoke(this, new(textBox.Text));
        Close();
    }

    private void CancelButton_Click(object? sender, EventArgs e)
    {
        Cancel?.Invoke(this, EventArgs.Empty);
        Close();
    }
}

internal class InputDialogEventArgs : EventArgs
{
    public string Text { get; set; }

    public InputDialogEventArgs(string text)
    {
        Text = text;
    }
}