namespace Wfc.Screens.MenuManager;

using System;
using System.Collections.Generic;
using Godot;
using Wfc.Screens.Levels;
using Wfc.Screens.MenuManager.Menus.MainMenu;
using Wfc.Screens.SettingsMenu;
using Wfc.Utils.Attributes;

public class MenuManager : IMenuManager {

  private readonly Node _rootNode;
  private Node? _currentScene;

  // Where the player has been, oldest first. Empty until the first screen is shown.
  private readonly List<GameMenus> _history = [];

  private GameMenus _lastVisitedMenu = GameMenus.MAIN_MENU;

  private LevelId? _levelId;

  public MenuManager(Node rootNode) {
    _rootNode = rootNode;
  }

  public GameMenus GetCurrentMenu() => _history.Count > 0 ? _history[^1] : GameMenus.MAIN_MENU;

  public GameMenus? PeekBack() => _history.Count > 1 ? _history[^2] : null;

  public GameMenus GetLastVisitedMenu() => _lastVisitedMenu;

  public LevelId? GetCurrentLevelId() => _levelId;

  public void SetCurrentLevel(LevelId levelId) => _levelId = levelId;

  // Pure: which scene stands for this screen. It used to clear the queued level as a
  // side effect, so merely asking where a screen lived changed what the game screen
  // would load.
  public string? GetMenuScenePath(GameMenus menu) => menu switch {
    GameMenus.GAME => GetScenePath<SceneOrchester>(),
    GameMenus.SETTINGS_MENU => GetScenePath<SettingsMenu>(),
    GameMenus.STATS_MENU => GetScenePath<StatsMenu>(),
    GameMenus.MAIN_MENU => GetScenePath<MainMenu>(),
    GameMenus.LEVEL_CLEAR_MENU => GetScenePath<LevelClearedMenu>(),
    GameMenus.SELECT_SLOT => GetScenePath<SelectSlotMenu>(),
    GameMenus.LEVEL_SELECT_MENU => GetScenePath<LevelSelectMenu>(),
    _ => null,
  };

  public bool GoToMenu(GameMenus nextMenu) {
    if (_history.Count > 0 && _history[^1] == nextMenu) {
      return false;
    }

    var scenePath = GetMenuScenePath(nextMenu);
    if (scenePath == null) {
      GD.PushError($"No scene registered for menu {nextMenu}");
      return false;
    }

    if (!_switchScene(scenePath)) {
      return false;
    }

    // Only the game screen carries a level. Leaving for anything else forgets it, so
    // returning to a menu can't leave a stale level queued up behind it.
    if (nextMenu != GameMenus.GAME) {
      _levelId = null;
    }

    _recordVisit(nextMenu);
    return true;
  }

  private void _recordVisit(GameMenus menu) {
    if (_history.Count > 0) {
      _lastVisitedMenu = _history[^1];
    }

    var alreadyVisited = _history.LastIndexOf(menu);
    if (alreadyVisited >= 0) {
      // Been here before: unwind rather than stack a second copy, so the history can't
      // grow without bound as the player moves between two screens.
      _history.RemoveRange(alreadyVisited + 1, _history.Count - alreadyVisited - 1);
    }
    else {
      _history.Add(menu);
    }
  }

  private bool _switchScene(string scenePath) {
    var packedScene = GD.Load<PackedScene>(scenePath);
    if (packedScene == null) {
      GD.PushError($"Could not load menu scene at {scenePath}");
      return false;
    }

    // The outgoing screen is freed first, but QueueFree is deferred, so both are alive
    // for the rest of this frame.
    _currentScene?.QueueFree();
    var newScene = packedScene.Instantiate();
    _rootNode.AddChild(newScene);
    newScene.Owner = _rootNode;
    _currentScene = newScene;
    return true;
  }

  private static string? GetScenePath<T>() where T : class {
    var attribute = Attribute.GetCustomAttribute(typeof(T), typeof(ScenePathAttribute)) as ScenePathAttribute;
    return attribute?.Path;
  }
}
