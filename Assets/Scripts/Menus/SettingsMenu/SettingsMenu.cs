using Godot;
using System;

public partial class SettingsMenu : GameMenu
{
    private TabContainer tabContainer;
    private int tabsCount;
    private DialogContainer DialogContainerNode;

    public override void _Ready()
    {
        base._Ready();
        tabContainer = GetNode<TabContainer>("TabContainer");
        tabsCount = tabContainer.GetChildCount();
        DialogContainerNode = GetNode<DialogContainer>("DialogContainer");
    }

    public override void _Input(InputEvent ev)
    {
        base._Input(ev);
        if (Input.IsActionJustPressed("ui_left"))
        {
            tabContainer.CurrentTab = Mathf.Clamp(tabContainer.CurrentTab - 1, 0, tabsCount - 1);
        }
        else if (Input.IsActionJustPressed("ui_right"))
        {
            tabContainer.CurrentTab = Mathf.Clamp(tabContainer.CurrentTab + 1, 0, tabsCount - 1);
        }
    }

    public override bool on_menu_button_pressed(MenuButtons menuButton)
    {
        base.on_menu_button_pressed(menuButton);
        switch (menuButton)
        {
            case MenuButtons.SHOW_DIALOG:
                DialogContainerNode.ShowDialog();
                return true;
            case MenuButtons.BACK:
                if (IsValidState())
                {
                    GameSettings.Instance().SaveGameSettings();
                    return false; // We don't return true here because we want the default behaviour to be called
                }
                else
                {
                    Event.Instance().EmitMenuButtonPressed(MenuButtons.SHOW_DIALOG);
                    return true;
                }
            default:
                return false;
        }
    }

    private bool IsValidState()
    {
        return GameSettings.Instance().AreActionKeysValid();
    }

    private void _on_BackButton_pressed()
    {
        if (!IsInTransitionState())
        {
            if (IsValidState())
            {
                Event.Instance().EmitMenuButtonPressed(MenuButtons.BACK);
            }
            else
            {
                Event.Instance().EmitMenuButtonPressed(MenuButtons.SHOW_DIALOG);
            }
        }
    }
}