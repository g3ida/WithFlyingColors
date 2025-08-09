namespace Wfc.Screens.MenuManager;

using System;
using Godot;
using Wfc.Screens.Levels;
using Wfc.Screens.MenuManager.Menus.MainMenu;
using Wfc.Screens.SettingsMenu;
using Wfc.Utils.Attributes;

public class MenuManager : IMenuManager {

  private readonly Node _rootNode;
  private Node _currentScene = null!;

  private GameMenus _currentMenu = GameMenus.MAIN_MENU;

  private GameMenus _previousMenu = GameMenus.GAME;

  public MenuManager(Node rootNode) {
    _rootNode = rootNode;
  }


  public LevelId? LevelId {
    get; private set;
  }

  public string? GetMenuScenePath(GameMenus menu) {
    if (menu == GameMenus.GAME) {
      return GetScenePath<SceneOrchester>();
    }
    else {
      LevelId = null;
      return menu switch {
        GameMenus.SETTINGS_MENU => GetScenePath<SettingsMenu>(),
        GameMenus.STATS_MENU => GetScenePath<StatsMenu>(),
        GameMenus.MAIN_MENU => GetScenePath<MainMenu>(),
        GameMenus.LEVEL_CLEAR_MENU => GetScenePath<LevelClearedMenu>(),
        GameMenus.SELECT_SLOT => GetScenePath<SelectSlotMenu>(),
        GameMenus.LEVEL_SELECT_MENU => GetScenePath<LevelSelectMenu>(),
        _ => null,
      };
    }
  }

  private static string? GetScenePath<T>() where T : class {
    var attribute = Attribute.GetCustomAttribute(typeof(T), typeof(ScenePathAttribute)) as ScenePathAttribute;
    return attribute?.Path;
  }

  public void GoToMenu(GameMenus nextMenu) {
    if (_currentMenu != nextMenu) {
      _previousMenu = _currentMenu;
      _currentMenu = nextMenu;
      var scenePath = GetMenuScenePath(nextMenu);
      if (scenePath != null) {
        SwitchScene(scenePath);
      }
    }
  }

  public GameMenus GetCurrentMenu() {
    return _currentMenu;
  }

  public GameMenus GetPreviousMenu() {
    return _previousMenu;
  }

  public LevelId? GetCurrentLevelId() {
    return LevelId;
  }

  public void GoToPreviousMenu() {
    GoToMenu(_previousMenu);
  }

  public void SetCurrentLevel(LevelId levelId) {
    LevelId = levelId;
  }

  public void SwitchScene(string scenePath) {
    _currentScene?.QueueFree();
    var newScene = GD.Load<PackedScene>(scenePath).Instantiate();
    _rootNode.AddChild(newScene);
    newScene.Owner = _rootNode;
    _currentScene = newScene;
  }
}
