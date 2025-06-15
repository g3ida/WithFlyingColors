namespace Wfc.Screens.MenuManager;

using System;
using Wfc.Screens.Levels;

public interface IMenuManager {
  void SwitchScene(string scenePath);
  void GoToMenu(GameMenus nextMenu);
  GameMenus GetCurrentMenu();
  GameMenus GetPreviousMenu();
  LevelId? GetCurrentLevelId();
  void SetCurrentLevel(LevelId levelId);
  string? GetMenuScenePath(GameMenus menu);
}
