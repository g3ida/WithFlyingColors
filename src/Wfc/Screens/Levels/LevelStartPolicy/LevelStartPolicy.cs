namespace Wfc.Screens.Levels;

// Which level the orchestrator boots into, kept apart from the node so the rule can be
// asserted without a scene tree. A level picked from a menu always wins, a slot that
// has actually been played resumes, everything else is a new game. Splitting the
// decision from the loading is what stopped the "resume" branch being the only one
// that remembered the level it had just created.
public static class LevelStartPolicy {
  public static LevelStartDecision Choose(
    LevelId? queuedLevelId,
    LevelId? savedLevelId,
    int savedProgress,
    bool savedSlotHasClearedLevels,
    LevelId firstLevelId
  ) {
    if (queuedLevelId != null) {
      return new LevelStartDecision(queuedLevelId.Value, false);
    }

    // A run parked in the hub is a run between levels: it opens the hub again rather than
    // walking into whatever level the pointer happens to name, and there is no checkpoint
    // in a hub to restore.
    if (savedLevelId == LevelId.Hub) {
      return new LevelStartDecision(LevelId.Hub, false);
    }

    if (savedLevelId != null && savedProgress > 0) {
      return new LevelStartDecision(savedLevelId.Value, true);
    }

    // Clearing a level parks the slot at the start of the next one with no progress
    // yet. That is not an untouched slot: Continue must land there, from the top of
    // that level rather than from a checkpoint save that does not exist yet.
    if (savedLevelId != null && savedSlotHasClearedLevels) {
      return new LevelStartDecision(savedLevelId.Value, false);
    }

    return new LevelStartDecision(firstLevelId, false);
  }
}
