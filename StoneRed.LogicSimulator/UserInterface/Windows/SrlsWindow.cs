using Microsoft.Xna.Framework;

using Myra.Graphics2D.UI;

using StoneRed.LogicSimulator.Misc;

using System;
using System.IO;

namespace StoneRed.LogicSimulator.UserInterface.Windows;

internal abstract class SrlsWindow
{
    public event EventHandler? Closed;

    protected Srls srls = null!;
    protected Window window = null!;

    public virtual bool ScalingEnabled { get; } = true;
    public Vector2 Scale { get => window.Scale; set => window.Scale = value; }

    protected abstract string XmmpPath { get; }

    public void Load(Srls srls)
    {
        this.srls = srls;

        string data = File.ReadAllText(Paths.GetContentPath(XmmpPath));
        window = (Window)Project.LoadFromXml(data, srls.AssetManager).Root;
        window.Closed += (_, _) => Closed?.Invoke(this, EventArgs.Empty);
    }

    public void Close()
    {
        window.Close();
    }

    public abstract void Initialize();

    public void Show()
    {
        window.ShowModal(srls.Desktop);
    }
}