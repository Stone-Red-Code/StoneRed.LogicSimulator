using Myra.Graphics2D.UI;
using StoneRed.LogicSimulator.UserInterface.Screens;

namespace StoneRed.LogicSimulator.UserInterface.Windows;

internal class QuickMenu : SrlsWindow
{
    protected override string XmmpPath => "QuickMenu.xmmp";

    public override void Initialize()
    {
        VerticalMenu menu = window.FindChildById<VerticalMenu>("menu");

        menu.FindMenuItemById("menuMainMenu").Selected += (s, a) => srls.LoadScreen<StartScreen>();
        menu.FindMenuItemById("menuSettings").Selected += (s, a) => srls.ShowWindow<SettingsWindow>();
        menu.FindMenuItemById("menuQuit").Selected += QuickMenu_Selected;
    }

    private void QuickMenu_Selected(object? sender, System.EventArgs e)
    {
        Dialog dialog = Dialog.CreateMessageBox("Quit?", "Would you like to quit?");

        dialog.Closed += (s, a) =>
        {
            if (!dialog.Result)
            {
                return;
            }

            srls.Exit();
        };

        dialog.ShowModal(srls.Desktop);
    }
}