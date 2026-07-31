namespace Wfc.Core.Persistence;

// Questions the menus ask across every slot rather than the selected one: whether a
// Continue button should exist at all, and which slot it should resume. Extension
// methods so the fake save manager in the tests answers them through the same code.
public static class SaveSlotQueries {

  // A slot the player has actually taken somewhere, as opposed to one that was merely
  // created by picking it in a menu. Only these are worth continuing or loading.
  public static bool IsSlotPlayed(this ISaveManager saveManager, int slotIndex) {
    var metaData = saveManager.GetSlotMetaData(slotIndex);
    return metaData != null && (metaData.Progress > 0 || metaData.ClearedLevels.Count > 0);
  }

  public static int CountPlayedSlots(this ISaveManager saveManager) {
    var count = 0;
    for (var i = 0; i < saveManager.SlotCount; i++) {
      if (saveManager.IsSlotPlayed(i)) {
        count++;
      }
    }
    return count;
  }

  // The slot Continue resumes: the one written to most recently. Null when nothing
  // has been played, which is what hides Continue in the first place.
  public static int? MostRecentlyPlayedSlotIndex(this ISaveManager saveManager) {
    int? bestIndex = null;
    ulong bestTimestamp = 0;
    for (var i = 0; i < saveManager.SlotCount; i++) {
      if (!saveManager.IsSlotPlayed(i)) {
        continue;
      }
      var timestamp = saveManager.GetSlotMetaData(i)!.SaveTimestamp;
      if (bestIndex == null || timestamp > bestTimestamp) {
        bestIndex = i;
        bestTimestamp = timestamp;
      }
    }
    return bestIndex;
  }
}
