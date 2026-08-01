namespace Wfc.Core.Persistence.Test;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Persistence;
using Wfc.Screens.Levels;
using Wfc.test.Helpers.Fakes;

// The all-slots questions behind the main menu's Play button: an occupied slot -
// however fresh - is enough for the sub-menu to exist, and Continue resumes the one
// written to most recently. Run against the fake so the definitions stay in
// lock-step with what the menu tests exercise.
public class SaveSlotQueriesTests(Node testScene) : TestClass(testScene) {
  [Test]
  public void NoFilledSlotsOnAFreshInstall() {
    var saveManager = new FakeSaveManager();

    saveManager.CountFilledSlots().ShouldBe(0);
    saveManager.MostRecentlyPlayedSlotIndex().ShouldBeNull();
  }

  // A save created moments ago, with zero progress, is already something Continue
  // can resume: Play must stop skipping straight into a new game the moment any
  // slot exists.
  [Test]
  public void ASlotCreatedButNeverPlayedCounts() {
    var saveManager = new FakeSaveManager().WithFilledSlot(0, progress: 0);

    saveManager.CountFilledSlots().ShouldBe(1);
    saveManager.MostRecentlyPlayedSlotIndex().ShouldBe(0);
  }

  [Test]
  public void EveryFilledSlotCounts() {
    var saveManager = new FakeSaveManager()
      .WithFilledSlot(0, progress: 40)
      .WithFilledSlot(2, progress: 0);

    saveManager.CountFilledSlots().ShouldBe(2);
  }

  [Test]
  public void MostRecentlyPlayedIsTheLatestSave() {
    var saveManager = new FakeSaveManager()
      .WithFilledSlot(0, progress: 80, timestamp: 100UL)
      .WithFilledSlot(2, progress: 10, timestamp: 200UL);

    saveManager.MostRecentlyPlayedSlotIndex().ShouldBe(2);
  }

  // Completion is what the slot card shows: cleared levels against the whole chain,
  // not the in-level checkpoint progress.
  [Test]
  public void CompletionIsZeroWithNothingCleared() {
    var saveManager = new FakeSaveManager().WithFilledSlot(0, progress: 90);

    saveManager.GetSlotMetaData(0)!.CompletionPercent(3).ShouldBe(0);
  }

  [Test]
  public void CompletionCountsClearedLevelsAgainstTheTotal() {
    var saveManager = new FakeSaveManager().WithClearedLevel(0, LevelId.Tutorial);

    saveManager.GetSlotMetaData(0)!.CompletionPercent(3).ShouldBe(33);
  }

  [Test]
  public void CompletionCapsAtFullEvenIfTheChainShrank() {
    var saveManager = new FakeSaveManager()
      .WithClearedLevel(0, LevelId.Tutorial)
      .WithClearedLevel(0, LevelId.FourColors);

    saveManager.GetSlotMetaData(0)!.CompletionPercent(1).ShouldBe(100);
    saveManager.GetSlotMetaData(0)!.CompletionPercent(0).ShouldBe(0);
  }
}
