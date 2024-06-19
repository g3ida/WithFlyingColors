using Godot;
using System;
using System.Xml.Serialization;

public partial class LevelClearedMenu : GameMenu
{
  public override void _Ready()
  {
    base._Ready();
  }

  public override void _Input(InputEvent ev)
  {
    base._Input(ev);
    // Handle input based on input type
    if (ev is InputEventKey)
    {
      Event.Instance.EmitMenuButtonPressed(MenuButtons.EXIT_LEVEL_CLEAR);
    }
  }

  public override bool on_menu_button_pressed(MenuButtons menuButton)
  {
    base.on_menu_button_pressed(menuButton);
    if (menuButton == MenuButtons.EXIT_LEVEL_CLEAR)
    {
      NavigateToScreen(MenuManager.Menus.MAIN_MENU);
      return true;
    }
    return false;
  }
}
