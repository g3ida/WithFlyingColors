namespace Wfc.Core.Persistence.Test;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Persistence;
using Wfc.Screens.Levels;
using Wfc.test.Helpers.Fakes;

// The all-slots questions behind the play sub-menu: Continue exists when any slot has
// been played and resumes the most recent one, Load Game needs a second played slot
// to be worth offering. Run against the fake so the definitions stay in lock-step
// with what the menu tests exercise.
public class SaveSlotQueriesTests(Node testScene) : TestClass(testScene) {
  [Test]
  public void ASlotCreatedButNeverPlayedDoesNotCount() {
    var saveManager = new FakeSaveManager().WithFilledSlot(0, progress: 0);

    saveManager.IsSlotPlayed(0).ShouldBeFalse();
    saveManager.CountPlayedSlots().ShouldBe(0);
    saveManager.MostRecentlyPlayedSlotIndex().ShouldBeNull();
  }

  [Test]
  public void ProgressMakesASlotPlayed() {
    var saveManager = new FakeSaveManager().WithFilledSlot(1, progress: 30);

    saveManager.IsSlotPlayed(1).ShouldBeTrue();
    saveManager.CountPlayedSlots().ShouldBe(1);
  }

  // Clearing a level resets Progress for the next one, so right after a clear the
  // only trace of play is the cleared set. Continue must not vanish at that moment.
  [Test]
  public void AClearedLevelAloneMakesASlotPlayed() {
    var saveManager = new FakeSaveManager().WithClearedLevel(0, LevelId.Tutorial);

    saveManager.IsSlotPlayed(0).ShouldBeTrue();
    saveManager.CountPlayedSlots().ShouldBe(1);
  }

  [Test]
  public void MostRecentlyPlayedIsTheLatestSave() {
    var saveManager = new FakeSaveManager()
      .WithFilledSlot(0, progress: 80, timestamp: 100UL)
      .WithFilledSlot(2, progress: 10, timestamp: 200UL);

    saveManager.MostRecentlyPlayedSlotIndex().ShouldBe(2);
  }

  [Test]
  public void MostRecentlyPlayedIgnoresUnplayedSlots() {
    var saveManager = new FakeSaveManager()
      .WithFilledSlot(0, progress: 40, timestamp: 100UL)
      .WithFilledSlot(1, progress: 0, timestamp: 900UL);

    saveManager.MostRecentlyPlayedSlotIndex().ShouldBe(0);
  }
}
