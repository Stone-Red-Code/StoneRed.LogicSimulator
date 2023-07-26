using Microsoft.Xna.Framework;

using MonoGame.Extended.Screens;

using Myra.Graphics2D.UI;

using System;
using System.IO;

namespace StoneRed.LogicSimulator.UserInterface.Screens;

internal abstract class SrlsScreen
{
    protected Srls srls = null!;

    public virtual bool ScalingEnabled => true;
    protected abstract string XmmpPath { get; }

    public GameScreen Load(Srls srls)
    {
        this.srls = srls;

        string data = File.ReadAllText(Path.Combine(srls.ContentPath, XmmpPath));
        srls.Desktop.Root = Project.LoadFromXml(data, srls.AssetManager).Root;

        GameScreenWrapper gameScreenWrapper = new GameScreenWrapper(srls);
        gameScreenWrapper.OnInitialize += (_, _) => Initialize();
        gameScreenWrapper.OnLoadContent += (_, _) => LoadContent();
        gameScreenWrapper.OnUnloadContent += (_, _) => UnloadContent();
        gameScreenWrapper.OnUpdate += (_, e) => Update(e.GameTime);
        gameScreenWrapper.OnDraw += (_, e) => Draw(e.GameTime);

        return gameScreenWrapper;
    }

    protected abstract void Draw(GameTime gameTime);

    protected abstract void Update(GameTime gameTime);

    protected virtual void UnloadContent()
    { }

    protected virtual void LoadContent()
    { }

    protected virtual void Initialize()
    { }

    private sealed class GameScreenWrapper : GameScreen
    {
        public event EventHandler? OnInitialize;

        public event EventHandler? OnLoadContent;

        public event EventHandler? OnUnloadContent;

        public event EventHandler<GameScreenWrapperEventArgs>? OnUpdate;

        public event EventHandler<GameScreenWrapperEventArgs>? OnDraw;

        public GameScreenWrapper(Game game) : base(game)
        {
        }

        public override void Draw(GameTime gameTime)
        {
            OnDraw?.Invoke(this, new(gameTime));
        }

        public override void Initialize()
        {
            OnInitialize?.Invoke(this, EventArgs.Empty);
        }

        public override void LoadContent()
        {
            OnLoadContent?.Invoke(this, EventArgs.Empty);
        }

        public override void UnloadContent()
        {
            OnUnloadContent?.Invoke(this, EventArgs.Empty);
        }

        public override void Update(GameTime gameTime)
        {
            OnUpdate?.Invoke(this, new(gameTime));
        }
    }

    private sealed class GameScreenWrapperEventArgs : EventArgs
    {
        public GameTime GameTime { get; }

        public GameScreenWrapperEventArgs(GameTime gameTime)
        {
            GameTime = gameTime;
        }
    }
}

internal abstract class SrlsScreen<T> : SrlsScreen where T : Widget
{
    protected T Root => (T)srls.Desktop.Root;
}