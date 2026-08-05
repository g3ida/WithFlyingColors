namespace Wfc.Screens.Levels;

using System.Collections.Generic;
using System.Linq;

// Which door the hub opens on, kept pure like the other level policies so the rule can be
// asserted without a scene tree. The hub is a room the player walks around, so where they
// are standing when it appears is the whole of what it says to them: coming out of a level
// puts them at that level's door, and opening a run puts them at the door the run is on.
//
// The chain is the levels the hub has a door for, in play order - not every level there is.
// The intro is played once on the way in and never offered again, so it is not somewhere a
// run can be stood.
public static class HubSpawnPolicy {
  public static LevelId? DoorToStandAt(
    LevelId? levelJustLeft,
    IReadOnlyList<LevelId> chain,
    IReadOnlySet<LevelId> clearedLevels
  ) {
    if (levelJustLeft is { } left && left != LevelId.Hub && chain.Contains(left)) {
      return left;
    }

    foreach (var levelId in chain) {
      if (!clearedLevels.Contains(levelId)) {
        return levelId;
      }
    }

    // Every level finished: the run stands at the last door rather than at the first,
    // which is where it left off and the only door still worth walking back into.
    return chain.Count == 0 ? null : chain[chain.Count - 1];
  }
}
