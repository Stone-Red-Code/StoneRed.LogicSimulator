﻿using Microsoft.Xna.Framework;

using Myra.Graphics2D.UI;

using StoneRed.LogicSimulator.UserInterface.Windows;

namespace StoneRed.LogicSimulator.UserInterface.Screens;

internal class StartScreen : SrlsScreen<VerticalStackPanel>
{
    protected override string XmmpPath => "StartScreen.xmmp";

    protected override void LoadContent()
    {
        VerticalMenu menu = Root.FindChildById<VerticalMenu>("menu");
        menu.FindMenuItemById("menuQuit").Selected += (s, a) => srls.Exit();
        menu.FindMenuItemById("menuSettings").Selected += (s, a) => srls.ShowWindow<SettingsWindow>();
        menu.FindMenuItemById("menuStartNew").Selected += (s, a) => srls.LoadScreen<WorldScreen>();
        menu.FindMenuItemById("menuLoad").Selected += (s, a) => srls.LoadScreen(new LoadingScreen());
    }

    protected override void Draw(GameTime gameTime)
    {
    }

    protected override void Update(GameTime gameTime)
    {
    }
}