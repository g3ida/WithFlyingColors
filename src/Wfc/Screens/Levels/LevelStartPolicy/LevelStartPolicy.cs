namespace Wfc.Screens.Levels;

// Which level the orchestrator boots into, kept apart from the node so the rule can be
// asserted without a scene tree. Three inputs, one answer: a level picked from a menu
// always wins, a slot that has actually been played resumes, everything else is a new
// game. Splitting the decision from the loading is what stopped the "resume" branch
// being the only one that remembered the level it had just created.
public static class LevelStartPolicy {
  public static LevelStartDecision Choose(
    LevelId? queuedLevelId,
    LevelId? savedLevelId,
    int savedProgress,
    LevelId firstLevelId
  ) {
    if (queuedLevelId != null) {
      return new LevelStartDecision(queuedLevelId.Value, false);
    }

    if (savedLevelId != null && savedProgress > 0) {
      return new LevelStartDecision(savedLevelId.Value, true);
    }

    return new LevelStartDecision(firstLevelId, false);
  }
}
