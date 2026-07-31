namespace Wfc.Screens.MenuManager;

// What the select-slot screen is being opened for. Load is the default because it is
// the harmless reading: picking a slot resumes it, and nothing gets wiped.
public enum SlotPickerMode {
  Load = 0,
  NewGame = 1,
}
