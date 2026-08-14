namespace Wfc.Screens.MenuManager;

using EventHandler = Wfc.Core.Event.EventHandler;

public enum MenuAction {
  Play = 0,
  GoToSettings = 1,
  Quit = 2,
  GoToCredits = 3,
  GoBack = 5,
  NewGame = 6,
  GoToLevelSelect = 10,
  SelectSlot = 12,
  ContinueGame = 13,
  ShowDialog = 14,
  // The player accepted a dialog. Backing out reports DismissDialog instead: the two
  // used to share this value, so a cancel was indistinguishable from a confirm.
  ConfirmDialog = 15,
  ExitClearedLevel = 16,
  DismissDialog = 17,
  LoadGame = 18,
  // A settings row opened or closed its list of options. No screen answers these -
  // they are here so opening a list is heard. Reusing ShowDialog for it meant the
  // settings screen answered by putting its own dialog up.
  OpenDropdown = 19,
  CloseDropdown = 20,
}

public static partial class MenuActionExtensions {
  public static void Emit(this MenuAction action) => EventHandler.Instance.EmitMenuActionPressed(action);
}
