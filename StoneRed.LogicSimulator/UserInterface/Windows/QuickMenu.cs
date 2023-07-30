using Myra.Graphics2D.UI;

using StoneRed.LogicSimulator.UserInterface.Screens;
using StoneRed.LogicSimulator.WorldSaveSystem;

namespace StoneRed.LogicSimulator.UserInterface.Windows;

internal class QuickMenu : SrlsWindow
{
    private readonly WorldData worldData;

    protected override string XmmpPath => "QuickMenu.xmmp";

    public QuickMenu(WorldData worldData)
    {
        this.worldData = worldData;
    }

    public override void Initialize()
    {
        VerticalMenu menu = window.FindChildById<VerticalMenu>("menu");

        menu.FindMenuItemById("menuMainMenu").Selected += MenuMainMenu_Selected;
        menu.FindMenuItemById("menuSettings").Selected += (s, a) => srls.ShowWindow<SettingsWindow>();
        menu.FindMenuItemById("menuSave").Selected += (s, a) => srls.ShowWindow(new SaveWorldWindow(worldData));
        menu.FindMenuItemById("menuLoad").Selected += (s, a) => srls.ShowWindow<LoadWorldWindow>();
        menu.FindMenuItemById("menuQuit").Selected += MenuQuit_Selected;
    }

    private void MenuMainMenu_Selected(object? sender, System.EventArgs e)
    {
        OkCancelDialog okCancelDialog = new OkCancelDialog("Do you really want to go to the main menu?", "Yes", "No");

        okCancelDialog.Ok += (s, a) => srls.LoadScreen<StartScreen>();

        srls.ShowWindow(okCancelDialog);
    }

    private void MenuQuit_Selected(object? sender, System.EventArgs e)
    {
        OkCancelDialog okCancelDialog = new OkCancelDialog("Do you really want to quit?", "Yes", "No");

        okCancelDialog.Ok += (s, a) => srls.Exit();

        srls.ShowWindow(okCancelDialog);
    }
}