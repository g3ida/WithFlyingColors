namespace Wfc.Screens.MenuManager;

public interface IMenuManager {
  void SwitchScene(string scenePath);
  void GoToMenu(GameMenus nextMenu);
  GameMenus GetCurrentMenu();
  GameMenus GetPreviousMenu();
  string? GetCurrentLevelScenePath();
  void SetCurrentLevel(string levelScenePath);
}
