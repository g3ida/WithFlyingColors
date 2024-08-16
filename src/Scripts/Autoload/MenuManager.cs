using System;
using Godot;

public partial class MenuManager : Node2D
{
  private static MenuManager _instance = null;

  public override void _Ready()
  {
    base._Ready();
    _instance = GetTree().Root.GetNode<MenuManager>("MenuManagerCS");
    SetProcess(false);
  }

  public static MenuManager Instance()
  {
    return _instance;
  }

  public const string SETTINGS_MENU_SCENE = "res://Assets/Screens/SettingsMenu.tscn";
  public const string STATS_MENU_SCENE = "res://Assets/Screens/StatsMenu.tscn";
  public const string MAIN_MENU_SCENE = "res://src/Wfc/Entities/Ui/Menu/MainMenu.tscn";
  public const string SELECT_SLOT_SCENE = "res://Assets/Screens/SelectSlotMenu.tscn";
  public const string LEVEL_SELECT_SCENE = "res://Assets/Screens/LevelSelectMenu.tscn";

  //public const string START_LEVEL_MENU_SCENE = "res://Levels/Level1.tscn";
  public const string START_LEVEL_MENU_SCENE = "res://Levels/TutorialLevel.tscn";
  private const string SCENE_ORCHESTER_SCENE = "res://Assets/Scenes/SceneOrchester.tscn";
  private const string LEVEL_CLEAR_SCENE = "res://Assets/Screens/LevelClearedMenu.tscn";

  public enum Menus
  {
    SETTINGS_MENU,
    SELECT_SLOT,
    STATS_MENU,
    MAIN_MENU,
    LEVEL_SELECT_MENU,
    LEVEL_CLEAR_MENU,
    GAME,
    QUIT,
    LOAD
  }

  private Menus currentMenu = Menus.MAIN_MENU;
  private Menus previousMenu = Menus.GAME;

  public Menus PreviousMenu
  {
    get { return previousMenu; }
  }

  public string levelScenePath = "";

  private string GetMenuScenePath(Menus menu)
  {
    if (menu == Menus.GAME)
    {
      return SCENE_ORCHESTER_SCENE;
    }
    else
    {
      levelScenePath = "";
      switch (menu)
      {
        case Menus.SETTINGS_MENU:
          return SETTINGS_MENU_SCENE;
        case Menus.STATS_MENU:
          return STATS_MENU_SCENE;
        case Menus.MAIN_MENU:
          return MAIN_MENU_SCENE;
        case Menus.LEVEL_CLEAR_MENU:
          return LEVEL_CLEAR_SCENE;
        case Menus.SELECT_SLOT:
          return SELECT_SLOT_SCENE;
        case Menus.LEVEL_SELECT_MENU:
          return LEVEL_SELECT_SCENE;
        default:
          return null;
      }
    }
  }

  public void GoToMenu(Menus nextMenu)
  {
    if (currentMenu != nextMenu)
    {
      previousMenu = currentMenu;
      currentMenu = nextMenu;
      string scenePath = GetMenuScenePath(nextMenu);
      if (scenePath != null)
      {
        GetTree().ChangeSceneToFile(scenePath);
      }
    }
  }

  public void SetCurrentLevel(string _levelScenePath)
  {
    levelScenePath = _levelScenePath;
  }
}
