using Godot;
using System.Collections.Generic;

public partial class PauseMenu : GameMenu
{
    private List<PauseMenuButtons> buttons;

    public override void _Ready()
    {
        base._Ready();
        buttons = new List<PauseMenuButtons>
        {
            GetNode<PauseMenuButtons>("CenterContainer/VBoxContainer/ResumeButton"),
            GetNode<PauseMenuButtons>("CenterContainer/VBoxContainer/LevelSelectButton"),
            GetNode<PauseMenuButtons>("CenterContainer/VBoxContainer/BackButton")
        };

        HandleBackEvent = false;
        Global.Instance().PauseMenu = this;
    }

    public void _Hide()
    {
        foreach (var button in buttons)
        {
            button._Hide();
        }
    }

    public void _Show()
    {
        buttons[0].GrabFocus();
        foreach (var button in buttons)
        {
            button._Show();
        }
    }

    public void GoToMainMenu()
    {
        NavigateToScreen(MenuManager.Menus.MAIN_MENU);
    }

    public void GoToLevelSelectMenu()
    {
        NavigateToScreen(MenuManager.Menus.LEVEL_SELECT_MENU);
    }
}