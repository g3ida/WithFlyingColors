namespace Wfc.Core.Persistence;

using System;

// Questions the menus ask across every slot rather than the selected one: whether
// the play button needs a sub-menu at all, and which slot Continue should resume.
// Extension methods so the fake save manager in the tests answers them through the
// same code.
public static class SaveSlotQueries {

  // Occupied slots, whatever their progress: a save created moments ago counts,
  // because it is already something Continue can resume and Load can pick.
  public static int CountFilledSlots(this ISaveManager saveManager) {
    var count = 0;
    for (var i = 0; i < saveManager.SlotCount; i++) {
      if (saveManager.IsSLotFilled(i)) {
        count++;
      }
    }
    return count;
  }

  // The question behind the Play button: with nothing saved anywhere there is no game
  // to continue and none to load, so Play can only mean "start one".
  public static bool HasNoSaves(this ISaveManager saveManager) => saveManager.CountFilledSlots() == 0;

  // Overall game completion for a slot card: cleared levels against the whole level
  // list, unlike Progress which only tracks checkpoints inside the current level.
  public static int CompletionPercent(this SlotMetaData metaData, int totalLevels) {
    if (totalLevels <= 0) {
      return 0;
    }
    return Math.Clamp(metaData.ClearedLevels.Count * 100 / totalLevels, 0, 100);
  }

  // The slot Continue resumes: the filled slot written to most recently. Null when
  // every slot is empty, which is what sends Play straight into a new game.
  public static int? MostRecentlyPlayedSlotIndex(this ISaveManager saveManager) {
    int? bestIndex = null;
    ulong bestTimestamp = 0;
    for (var i = 0; i < saveManager.SlotCount; i++) {
      if (!saveManager.IsSLotFilled(i)) {
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
