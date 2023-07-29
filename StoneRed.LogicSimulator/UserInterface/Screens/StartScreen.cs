using Microsoft.Xna.Framework;

using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.File;

using StoneRed.LogicSimulator.UserInterface.Windows;

using System.Threading.Tasks;

namespace StoneRed.LogicSimulator.UserInterface.Screens;

internal class StartScreen : SrlsScreen<VerticalStackPanel>
{
    protected override string XmmpPath => "StartScreen.xmmp";

    protected override void Initialize()
    {
        VerticalMenu menu = Root.FindChildById<VerticalMenu>("menu");
        menu.FindMenuItemById("menuQuit").Selected += (s, a) => srls.Exit();
        menu.FindMenuItemById("menuSettings").Selected += (s, a) => srls.ShowWindow<SettingsWindow>();
        menu.FindMenuItemById("menuStartNew").Selected += (s, a) => srls.LoadScreen<WorldScreen>();
        menu.FindMenuItemById("menuLoad").Selected += MenuLoad_Selected;
    }

    protected override void Draw(GameTime gameTime)
    {
    }

    protected override void Update(GameTime gameTime)
    {
    }

    private void MenuLoad_Selected(object? sender, System.EventArgs e)
    {
        FileDialog fileDialog = new FileDialog(FileDialogMode.OpenFile);

        fileDialog.Show(srls.Desktop);
        fileDialog.Scale = new Vector2(srls.Scale / 3);
        fileDialog.Closed += async (s, a) =>
        {
            if (!fileDialog.Result)
            {
                return;
            }

            // Very cheap workaround to make sure the file dialog is closed before loading the screen
            await Task.Delay(100);

            srls.LoadScreen(new LoadingScreen(fileDialog.FilePath));
        };
    }
}