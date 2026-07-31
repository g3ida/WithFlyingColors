namespace Wfc.Screens.Levels;

using System.Collections.Generic;

// Which chain levels the level select lets the player jump to, kept pure like
// LevelStartPolicy so the rule is testable without a scene tree. A level is reachable
// once the one before it has been cleared; the level the slot is parked on is always
// reachable, or a mid-level save could lock the player out of their own game.
public static class LevelUnlockPolicy {
  public static bool IsUnlocked(
    LevelId levelId,
    IReadOnlyList<LevelId> chain,
    IReadOnlySet<LevelId> clearedLevels,
    LevelId? resumeLevelId
  ) {
    if (levelId == resumeLevelId) {
      return true;
    }

    for (var index = 0; index < chain.Count; index++) {
      if (chain[index] == levelId) {
        return index == 0 || clearedLevels.Contains(chain[index - 1]);
      }
    }

    // A level the chain does not know cannot be reasoned about, so it stays locked.
    return false;
  }
}
