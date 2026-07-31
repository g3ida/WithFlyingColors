namespace Wfc.Entities.Ui.Test;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.Ui;
using Wfc.Screens.Levels;
using Wfc.test.Helpers.Fakes;

// The conditions that decide which buttons the play sub-menu offers: Continue once
// any slot has been played, Load Game once there is a second game to choose between.
// They look across every slot - which slot happens to be selected is irrelevant.
public class ButtonDefTests(Node testScene) : TestClass(testScene) {
  [Test]
  public void AnyPlayedSlot_IsFoundWhereverItLives() {
    var playedElsewhere = new FakeSaveManager(selectedSlot: 0).WithFilledSlot(2, progress: 40);
    var nothingPlayed = new FakeSaveManager(selectedSlot: 0).WithFilledSlot(0, progress: 0);

    ButtonDef.ButtonCondition.HasAnyPlayedSlot.Verify(playedElsewhere).ShouldBeTrue();
    ButtonDef.ButtonCondition.HasAnyPlayedSlot.Verify(nothingPlayed).ShouldBeFalse();
  }

  // Clearing a level resets Progress for the next one, so right after a clear the
  // only trace of play is the cleared set. Continue must not vanish at that moment.
  [Test]
  public void AClearedOnlySlot_CountsAsPlayed() {
    var saveManager = new FakeSaveManager(selectedSlot: 0).WithClearedLevel(0, LevelId.Tutorial);

    ButtonDef.ButtonCondition.HasAnyPlayedSlot.Verify(saveManager).ShouldBeTrue();
  }

  [Test]
  public void MultiplePlayedSlots_NeedsASecondGame() {
    var onePlayed = new FakeSaveManager(selectedSlot: 0).WithFilledSlot(0, progress: 40);
    var twoPlayed = new FakeSaveManager(selectedSlot: 0)
      .WithFilledSlot(0, progress: 40)
      .WithFilledSlot(1, progress: 10);

    ButtonDef.ButtonCondition.HasMultiplePlayedSlots.Verify(onePlayed).ShouldBeFalse();
    ButtonDef.ButtonCondition.HasMultiplePlayedSlots.Verify(twoPlayed).ShouldBeTrue();
  }

  // None has to mean opposite things to the two questions asked of it: a button with
  // no display condition is always offered, a button with no disable condition is
  // never greyed out. Both used to go through Verify, which answers "no" to both, so
  // the play sub-menu's slot button was dropped before it was ever built.
  [Test]
  public void NoneCondition_ShowsTheButtonAndLeavesItEnabled() {
    var saveManager = new FakeSaveManager(selectedSlot: 0);

    ButtonDef.ButtonCondition.None.ShouldDisplay(saveManager).ShouldBeTrue();
    ButtonDef.ButtonCondition.None.ShouldDisable(saveManager).ShouldBeFalse();
  }

  [Test]
  public void RealCondition_DecidesBothDisplayAndDisable() {
    var played = new FakeSaveManager(selectedSlot: 0).WithFilledSlot(0, progress: 40);

    ButtonDef.ButtonCondition.HasAnyPlayedSlot.ShouldDisplay(played).ShouldBeTrue();
    ButtonDef.ButtonCondition.HasAnyPlayedSlot.ShouldDisable(played).ShouldBeTrue();
    ButtonDef.ButtonCondition.HasMultiplePlayedSlots.ShouldDisplay(played).ShouldBeFalse();
    ButtonDef.ButtonCondition.HasMultiplePlayedSlots.ShouldDisable(played).ShouldBeFalse();
  }
}
