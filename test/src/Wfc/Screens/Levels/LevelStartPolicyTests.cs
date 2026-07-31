namespace Wfc.Screens.Levels.Test;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Screens.Levels;

// SceneOrchester had this rule spread over three branches and only one of them kept the
// level it had just loaded, so the level-clear screen could never be shown. The rule is
// a handful of inputs and one answer; keeping it here means the orchestrator has nothing
// left to get wrong but the loading itself.
public class LevelStartPolicyTests(Node testScene) : TestClass(testScene) {
  [Test]
  public void PicksTheLevelQueuedByTheMenu() {
    var decision = LevelStartPolicy.Choose(LevelId.Level1, null, 0, false, LevelId.Tutorial);

    decision.LevelId.ShouldBe(LevelId.Level1);
    decision.ShouldRestoreSavedGame.ShouldBeFalse();
  }

  // Choosing a level from the level select is deliberate: it must not be overruled by,
  // or mixed with, whatever the save slot was in the middle of.
  [Test]
  public void PrefersTheQueuedLevelOverASavedOne() {
    var decision = LevelStartPolicy.Choose(LevelId.Tutorial, LevelId.Level1, 80, true, LevelId.Tutorial);

    decision.LevelId.ShouldBe(LevelId.Tutorial);
    decision.ShouldRestoreSavedGame.ShouldBeFalse();
  }

  [Test]
  public void ResumesASlotThatHasProgress() {
    var decision = LevelStartPolicy.Choose(null, LevelId.Level1, 40, false, LevelId.Tutorial);

    decision.LevelId.ShouldBe(LevelId.Level1);
    decision.ShouldRestoreSavedGame.ShouldBeTrue();
  }

  [Test]
  public void StartsANewGameWhenThereIsNoSlot() {
    var decision = LevelStartPolicy.Choose(null, null, 0, false, LevelId.Tutorial);

    decision.LevelId.ShouldBe(LevelId.Tutorial);
    decision.ShouldRestoreSavedGame.ShouldBeFalse();
  }

  // A freshly created slot has a LevelId but no progress, and restoring from it would
  // load an empty save over a level that has not been played yet.
  [Test]
  public void TreatsAnUntouchedSlotAsANewGame() {
    var decision = LevelStartPolicy.Choose(null, LevelId.Level1, 0, false, LevelId.Tutorial);

    decision.LevelId.ShouldBe(LevelId.Tutorial);
    decision.ShouldRestoreSavedGame.ShouldBeFalse();
  }

  // Clearing a level parks the slot at the start of the next one with no progress yet.
  // Quitting there must resume that level from its top - not restart the whole game,
  // and not try to restore a checkpoint save that does not exist yet.
  [Test]
  public void ResumesTheLevelAdvancedToByAClear() {
    var decision = LevelStartPolicy.Choose(null, LevelId.FourColors, 0, true, LevelId.Tutorial);

    decision.LevelId.ShouldBe(LevelId.FourColors);
    decision.ShouldRestoreSavedGame.ShouldBeFalse();
  }
}
