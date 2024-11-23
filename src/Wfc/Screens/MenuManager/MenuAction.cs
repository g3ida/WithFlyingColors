namespace Wfc.Screens.MenuManager;

using EventHandler = Wfc.Core.Event.EventHandler;

public enum MenuAction {
  Play = 0,
  GoToSettings = 1,
  Quit = 2,
  GoToStats = 3,
  GoToCredits = 4,
  GoBack = 5,
  NewGame = 6,
  LoadGame = 7,
  SaveGame = 8,
  GoToSlotSelect = 9,
  GoToLevelSelect = 10,
  DeleteSlot = 11,
  SelectSlot = 12,
  ContinueGame = 13,
  ShowDialog = 14,
  ConfirmDialog = 15,
  ExitClearedLevel = 16,
}

public static partial class MenuActionExtensions {
  public static void Emit(this MenuAction action) => EventHandler.Instance.EmitMenuActionPressed(action);
}
