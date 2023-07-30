using Microsoft.Xna.Framework;

using Myra.Graphics2D.UI;

using StoneRed.LogicSimulator.UserInterface.Windows;

namespace StoneRed.LogicSimulator.UserInterface.Screens;

internal class StartScreen : SrlsScreen<VerticalStackPanel>
{
    protected override string XmmpPath => "StartScreen.xmmp";

    protected override void Initialize()
    {
        VerticalMenu menu = Root.FindChildById<VerticalMenu>("menu");
        menu.FindMenuItemById("menuQuit").Selected += (s, e) => srls.Exit();
        menu.FindMenuItemById("menuSettings").Selected += (s, e) => srls.ShowWindow<SettingsWindow>();
        menu.FindMenuItemById("menuStartNew").Selected += (s, e) => srls.LoadScreen<WorldScreen>();
        menu.FindMenuItemById("menuLoad").Selected += (s, e) => srls.ShowWindow<LoadWorldWindow>();
    }

    protected override void Draw(GameTime gameTime)
    {
    }

    protected override void Update(GameTime gameTime)
    {
    }
}