namespace Wfc.Core.Persistence.Test;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using Wfc.Screens.Levels;

// Nothing in the game used to write Progress or LevelId anywhere outside the SlotMetaData
// constructor, so the Continue button never appeared and quitting lost the run. These are the rules
// the new write path follows; none of them need a scene tree or a file on disk.
public class SaveSlotProgressTests(Node testScene) : TestClass(testScene) {
  // Well past the slots the game offers, so a test that writes files cannot land on a slot
  // somebody is actually playing.
  private const int SCRATCH_SLOT = 99;

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

  [Test]
  public void RecordsACompletionOnASlotThatHasNoneYet() {
    var slot = new SaveSlot(1);

    slot.RecordCompletion(LevelId.Tutorial);

    slot.MetaData.ShouldNotBeNull();
    slot.MetaData!.ClearedLevels.ShouldBe([LevelId.Tutorial]);
  }

  // The whole point of keeping completion apart from the resume pointer: moving on to
  // the next level starts Progress over, but what was cleared stays cleared.
  [Test]
  public void AClearedLevelStaysClearedWhenProgressMovesOn() {
    var slot = new SaveSlot(1);
    slot.RecordProgress(LevelId.Tutorial, 100);
    slot.RecordCompletion(LevelId.Tutorial);

    slot.RecordProgress(LevelId.Level1, 10);

    slot.MetaData!.LevelId.ShouldBe(LevelId.Level1);
    slot.MetaData.Progress.ShouldBe(10);
    slot.MetaData.ClearedLevels.ShouldContain(LevelId.Tutorial);
  }

  [Test]
  public void ClearingTheSameLevelTwiceRecordsItOnce() {
    var slot = new SaveSlot(1);

    slot.RecordCompletion(LevelId.Tutorial);
    slot.RecordCompletion(LevelId.Tutorial);

    slot.MetaData!.ClearedLevels.Count.ShouldBe(1);
  }

  [Test]
  public void RecordsGemsOnASlotThatHasNoneYet() {
    var slot = new SaveSlot(1);

    slot.RecordCollectedGems(LevelId.Level1, ["blue", "pink"]);

    slot.MetaData.ShouldNotBeNull();
    slot.MetaData!.GemsCollectedIn(LevelId.Level1).ShouldBe(["blue", "pink"], ignoreOrder: true);
  }

  // A replay that ends short of an already-banked gem must not take it off the door:
  // banked gems union in, they never reset.
  [Test]
  public void BankedGemsOnlyEverAccumulate() {
    var slot = new SaveSlot(1);
    slot.RecordCollectedGems(LevelId.Level1, ["blue", "pink"]);

    slot.RecordCollectedGems(LevelId.Level1, ["yellow"]);
    slot.RecordCollectedGems(LevelId.Level1, ["blue"]);

    slot.MetaData!.GemsCollectedIn(LevelId.Level1).ShouldBe(["blue", "pink", "yellow"], ignoreOrder: true);
  }

  [Test]
  public void GemsAreBankedPerLevel() {
    var slot = new SaveSlot(1);

    slot.RecordCollectedGems(LevelId.Tutorial, ["purple"]);
    slot.RecordCollectedGems(LevelId.Level1, ["blue"]);

    slot.MetaData!.GemsCollectedIn(LevelId.Tutorial).ShouldBe(["purple"]);
    slot.MetaData.GemsCollectedIn(LevelId.Level1).ShouldBe(["blue"]);
    slot.MetaData.GemsCollectedIn(LevelId.FourColors).ShouldBeEmpty();
  }

  // The one record that materializes nothing: its siblings are handed the level they are
  // recording, while a slot invented here would have to guess a resume pointer - and a run with
  // nowhere to be remembered is simply shown the room again.
  [Test]
  public void RecordingTheHubArrivalOnASlotThatHasNoRecordWritesNothing() {
    var slot = new SaveSlot(1);

    slot.RecordHubArrivalSeen();

    slot.MetaData.ShouldBeNull("the hub arrival invented a save the player never asked for");
  }

  // The arrival says where the room has been shown, not where the player is: it must leave the
  // resume pointer exactly where the run left it.
  [Test]
  public void RecordingTheHubArrivalMovesNothingElse() {
    var slot = new SaveSlot(1);
    slot.RecordProgress(LevelId.Level1, 40);

    slot.RecordHubArrivalSeen();

    slot.MetaData!.HasSeenHubArrival.ShouldBeTrue();
    slot.MetaData.LevelId.ShouldBe(LevelId.Level1, "the arrival moved the level the run resumes into");
    slot.MetaData.Progress.ShouldBe(40, "the arrival moved how far the run had got");
  }

  // Starting a new game over an old one deletes the slot and writes a blank one in its place.
  // Deleting only ever took the files: the record stayed in memory, the blank write found it
  // and kept it, and the fresh run opened with the finished one's cleared levels and gems -
  // the tutorial's four already on its door before it had been played.
  [Test]
  public void DeletingASlotForgetsWhatItWasHolding() {
    var serializer = new SimpleJsonSerializer();
    var slot = new SaveSlot(SCRATCH_SLOT);
    slot.RecordCompletion(LevelId.Tutorial);
    slot.RecordCollectedGems(LevelId.Tutorial, ["blue", "pink", "purple", "yellow"]);
    slot.Save(serializer, TestScene.GetTree());

    slot.Delete();
    slot.MetaData.ShouldBeNull("the deleted slot is still holding its own record");

    // What starting a new game in this slot does next.
    slot.Save(serializer, TestScene.GetTree());

    slot.MetaData!.ClearedLevels.ShouldBeEmpty("the new game inherited the old one's cleared levels");
    slot.MetaData.GemsCollectedIn(LevelId.Tutorial).ShouldBeEmpty("the new game inherited the old one's gems");
    slot.Delete();
  }

  // Re-read after every write, so a slot whose files have gone must come back empty rather
  // than keeping the record it was last handed.
  [Test]
  public void RereadingASlotWithNoFilesEmptiesIt() {
    var slot = new SaveSlot(SCRATCH_SLOT);
    slot.RecordCompletion(LevelId.Tutorial);

    slot.LoadMetaData(new SimpleJsonSerializer());

    slot.MetaData.ShouldBeNull("a slot with nothing on disk still claims a record");
  }
}
