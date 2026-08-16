namespace Wfc.Core.Persistence;

// Where the save slots live on disk. Held here rather than on either of the two types that
// write there, because the manager owns the file naming the selected slot and the slots own
// their own contents, and both have to agree on the directory above them.
//
// Settable for the same reason GameSettings.ConfigFilePath is: the instrumented tests point it
// at a scratch directory, so a suite that writes or deletes saves can never reach the player's
// own. Read live rather than captured, so pointing it somewhere takes effect on managers that
// already exist.
public static class SavePaths {
  public const string DEFAULT_ROOT = "user://slots";

  public static string Root { get; set; } = DEFAULT_ROOT;

  public static string SlotsInfo => $"{Root}/slots_info.save";

  public static string SlotDirectory(int slotIndex) => $"{Root}/{slotIndex}";
}
