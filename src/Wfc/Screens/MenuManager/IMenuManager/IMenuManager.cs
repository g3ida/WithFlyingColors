namespace Wfc.Screens.MenuManager;

using Wfc.Screens.Levels;

public interface IMenuManager {
  // Shows a screen and records the visit.
  //
  // Navigation is a history rather than a single "where I came from" slot. A screen
  // the player has not been to is pushed on top; a screen already in the history
  // unwinds back to it, which is what makes leaving the pause menu for the main menu
  // drop the game screen in between rather than bury it.
  //
  // False means nothing happened: either that screen is already showing, or its scene
  // could not be loaded. The history is only touched once the swap has succeeded.
  bool GoToMenu(GameMenus nextMenu);

  GameMenus GetCurrentMenu();

  // The screen back would return to, or null at the root of the history.
  GameMenus? PeekBack();

  // The screen just left, whichever direction it was left in. The menu box uses it to
  // come back facing the button the player went in through, which is a different
  // question from what is underneath the current screen in the history.
  GameMenus GetLastVisitedMenu();

  LevelId? GetCurrentLevelId();

  void SetCurrentLevel(LevelId levelId);

  string? GetMenuScenePath(GameMenus menu);
}
