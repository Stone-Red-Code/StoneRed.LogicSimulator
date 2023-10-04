using FontStashSharp;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.Extended.Screens;

using Myra;
using Myra.Assets;
using Myra.Graphics2D.UI;

using StoneRed.LogicSimulator.Misc;
using StoneRed.LogicSimulator.UserInterface.Screens;
using StoneRed.LogicSimulator.UserInterface.Windows;
using StoneRed.LogicSimulator.Utilities;

using System.IO;

namespace StoneRed.LogicSimulator;

internal class Srls : Game
{
    private readonly ScreenManager screenManager;
    private SrlsWindow? currentSrlsWindow = null;
    private SrlsScreen? currentSrlsScreen = null;
    public float Scale { get; private set; }
    public GraphicsDeviceManager Graphics { get; }

    public Desktop Desktop { get; private set; } = null!;
    public SpriteBatch SpriteBatch { get; private set; } = null!;

    public FontSystem FontSystem { get; private set; } = null!;
    public AssetManager AssetManager { get; private set; } = null!;

    public LogicGatesManager LogicGatesManager { get; private set; } = null!;

    public Settings Settings { get; set; }

    public const ushort CurrentSaveVersion = 1;

    public Srls()
    {
        Content.RootDirectory = "Content";

        Settings = Settings.Load(Paths.GetSettingsPath()) ?? new Settings()
        {
            Resolution = new Resolution(GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height),
            Scale = 1
        };

        Graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = Settings.Resolution.Width,
            PreferredBackBufferHeight = Settings.Resolution.Height,
        };

        Window.AllowUserResizing = true;
        IsMouseVisible = true;

        screenManager = new ScreenManager();
        Components.Add(screenManager);
    }

    public void LoadScreen<T>() where T : SrlsScreen, new()
    {
        LoadScreen(new T());
    }

    public void LoadScreen(SrlsScreen screen)
    {
        GameScreen gameScreen = screen.Load(this);

        currentSrlsScreen = screen;

        screenManager.LoadScreen(gameScreen);
    }

    public void ShowWindow<T>() where T : SrlsWindow, new()
    {
        ShowWindow(new T());
    }

    public void ShowWindow(SrlsWindow window)
    {
        currentSrlsWindow?.Close();

        window.Load(this);
        window.Initialize();
        window.Closed += (s, a) => currentSrlsWindow = null;
        window.Scale = new Vector2(Scale / 2, Scale / 2);

        window.Show();

        currentSrlsWindow = window;
    }

    public TextButton ShowContextMenu(string title, Point position, MenuItem[] menuItems, bool showButton = false)
    {
        string data = File.ReadAllText(Paths.GetContentPath("ContextMenu.xmmp"));
        VerticalStackPanel contextMenu = (VerticalStackPanel)Project.LoadFromXml(data, AssetManager).Root;

        contextMenu.FindChildById<Label>("title").Text = title;

        TextButton button = contextMenu.FindChildById<TextButton>("button");

        button.Visible = showButton;

        VerticalMenu menu = contextMenu.FindChildById<VerticalMenu>("menu");

        foreach (MenuItem menuItem in menuItems)
        {
            menu.Items.Add(menuItem);
        }

        Desktop.ShowContextMenu(contextMenu, position);
        Desktop.ContextMenu.Scale = new Vector2(Scale / 3, Scale / 3);

        return button;
    }

    protected override void Update(GameTime gameTime)
    {
        Scale = 1f / 800 * Window.ClientBounds.Width * Settings.Scale;
        Desktop.Root.Scale = currentSrlsScreen?.ScalingEnabled == true ? new Vector2(Scale, Scale) : Vector2.One;

        if (Desktop.ContextMenu is not null)
        {
            Desktop.ContextMenu.Scale = new Vector2(Scale / 3, Scale / 3);
        }

        if (currentSrlsWindow is not null)
        {
            currentSrlsWindow.Scale = currentSrlsWindow.ScalingEnabled ? new Vector2(Scale / 2, Scale / 2) : Vector2.One;
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        base.Draw(gameTime);

        Desktop.Render();
    }

    protected override void Initialize()
    {
        MyraEnvironment.Game = this;
        MyraEnvironment.SmoothText = true;

        FontSystemDefaults.FontResolutionFactor = 10.0f;
        FontSystemDefaults.KernelWidth = 2;
        FontSystemDefaults.KernelHeight = 2;

        SpriteBatch = new SpriteBatch(GraphicsDevice);

        Desktop = new Desktop();

        FontSystem = new FontSystem();
        FontSystem.AddFont(File.ReadAllBytes(Paths.GetContentPath("ManoloMono.ttf")));

        FileAssetResolver assetResolver = new FileAssetResolver(Paths.GetContentPath());
        AssetManager = new AssetManager(assetResolver);

        LogicGatesManager = new LogicGatesManager(GraphicsDevice);
        LogicGatesManager.LoadLogicGates();

        LoadScreen<StartScreen>();

        base.Initialize();

        SdlMethods.MaximizeWindow(Window.Handle);
    }
}