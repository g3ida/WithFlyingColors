namespace Wfc.Screens.MenuManager;

using Godot;

public class MenuManager : IMenuManager {

  private readonly Node _rootNode;
  private Node _currentScene = null!;

  private GameMenus _currentMenu = GameMenus.MAIN_MENU;

  private GameMenus _previousMenu = GameMenus.GAME;

  public MenuManager(Node rootNode) {
    _rootNode = rootNode;
  }


  public string? LevelScenePath {
    get; private set;
  }

  private string? GetMenuScenePath(GameMenus menu) {
    if (menu == GameMenus.GAME) {
      return MenuScenes.SCENE_ORCHESTER_SCENE;
    }
    else {
      LevelScenePath = "";
      return menu switch {
        GameMenus.SETTINGS_MENU => MenuScenes.SETTINGS_MENU_SCENE,
        GameMenus.STATS_MENU => MenuScenes.STATS_MENU_SCENE,
        GameMenus.MAIN_MENU => MenuScenes.MAIN_MENU_SCENE,
        GameMenus.LEVEL_CLEAR_MENU => MenuScenes.LEVEL_CLEAR_SCENE,
        GameMenus.SELECT_SLOT => MenuScenes.SELECT_SLOT_SCENE,
        GameMenus.LEVEL_SELECT_MENU => MenuScenes.LEVEL_SELECT_SCENE,
        _ => null,
      };
    }
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

  public string? GetCurrentLevelScenePath() {
    return LevelScenePath;
  }

  public void GoToPreviousMenu() {
    GoToMenu(_previousMenu);
  }

  public void SetCurrentLevel(string levelScenePath) {
    LevelScenePath = levelScenePath;
  }

  public void SwitchScene(string scenePath) {
    _currentScene?.QueueFree();
    var newScene = GD.Load<PackedScene>(scenePath).Instantiate();
    _rootNode.AddChild(newScene);
    _currentScene = newScene;
  }
}
