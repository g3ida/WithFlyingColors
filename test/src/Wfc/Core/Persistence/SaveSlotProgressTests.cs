namespace Wfc.Core.Persistence.Test;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Persistence;
using Wfc.Screens.Levels;

// Nothing in the game used to write Progress or LevelId anywhere outside the SlotMetaData
// constructor, so the Continue button never appeared and quitting lost the run. These are the rules
// the new write path follows; none of them need a scene tree or a file on disk.
public class SaveSlotProgressTests(Node testScene) : TestClass(testScene) {
  [Test]
  public void RecordsTheLevelAndProgressOnASlotThatHasNoneYet() {
    var slot = new SaveSlot(1);

    slot.RecordProgress(LevelId.Level1, 40);

    slot.MetaData.ShouldNotBeNull();
    slot.MetaData!.LevelId.ShouldBe(LevelId.Level1);
    slot.MetaData.Progress.ShouldBe(40);
  }

  // Dying back to an earlier checkpoint re-raises CheckpointReached from further back in the level.
  // That is not the player losing progress.
  [Test]
  public void ProgressWithinALevelNeverGoesBackwards() {
    var slot = new SaveSlot(1);
    slot.RecordProgress(LevelId.Level1, 60);

    slot.RecordProgress(LevelId.Level1, 20);

    slot.MetaData!.Progress.ShouldBe(60);
  }

  // Progress is measured inside a level, so arriving in the next one starts it over rather than
  // reading as a completed level that has somehow gone backwards.
  [Test]
  public void ANewLevelStartsItsProgressOver() {
    var slot = new SaveSlot(1);
    slot.RecordProgress(LevelId.Tutorial, 100);

    slot.RecordProgress(LevelId.Level1, 10);

    slot.MetaData!.LevelId.ShouldBe(LevelId.Level1);
    slot.MetaData.Progress.ShouldBe(10);
  }

  [Test]
  public void ProgressIsAPercentage() {
    var slot = new SaveSlot(1);

    slot.RecordProgress(LevelId.Level1, 250);
    slot.MetaData!.Progress.ShouldBe(100);

    var other = new SaveSlot(2);
    other.RecordProgress(LevelId.Level1, -10);
    other.MetaData!.Progress.ShouldBe(0);
  }
}
