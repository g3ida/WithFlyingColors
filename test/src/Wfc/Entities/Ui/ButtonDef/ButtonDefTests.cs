namespace Wfc.Entities.Ui.Test;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Persistence;
using Wfc.Entities.Ui;
using Wfc.test.Helpers.Fakes;

// The conditions that decide which buttons the play sub-menu offers: "continue" only
// once there is progress to continue, "new game" only while there isn't.
public class ButtonDefTests(Node testScene) : TestClass(testScene) {
  [Test]
  public void DirtySlot_IsASlotWithProgress() {
    var withProgress = new FakeSaveManager(selectedSlot: 0).WithFilledSlot(0, progress: 40);
    var untouched = new FakeSaveManager(selectedSlot: 0);

    ButtonDef.ButtonCondition.IsDirtySlot.Verify(withProgress).ShouldBeTrue();
    ButtonDef.ButtonCondition.IsDirtySlot.Verify(untouched).ShouldBeFalse();
  }

  [Test]
  public void VirginSlot_IsASlotWithoutProgress() {
    var withProgress = new FakeSaveManager(selectedSlot: 0).WithFilledSlot(0, progress: 40);
    var untouched = new FakeSaveManager(selectedSlot: 0);

    ButtonDef.ButtonCondition.IsVirginSlot.Verify(untouched).ShouldBeTrue();
    ButtonDef.ButtonCondition.IsVirginSlot.Verify(withProgress).ShouldBeFalse();
  }

  // A slot that has been saved into but never progressed still counts as untouched,
  // so the player is offered a new game rather than a continue that goes nowhere.
  [Test]
  public void SavedButUnplayedSlot_CountsAsVirgin() {
    var saveManager = new FakeSaveManager(selectedSlot: 0).WithFilledSlot(0, progress: 0);

    ButtonDef.ButtonCondition.IsVirginSlot.Verify(saveManager).ShouldBeTrue();
    ButtonDef.ButtonCondition.IsDirtySlot.Verify(saveManager).ShouldBeFalse();
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
    var withProgress = new FakeSaveManager(selectedSlot: 0).WithFilledSlot(0, progress: 40);

    ButtonDef.ButtonCondition.IsDirtySlot.ShouldDisplay(withProgress).ShouldBeTrue();
    ButtonDef.ButtonCondition.IsDirtySlot.ShouldDisable(withProgress).ShouldBeTrue();
    ButtonDef.ButtonCondition.IsVirginSlot.ShouldDisplay(withProgress).ShouldBeFalse();
    ButtonDef.ButtonCondition.IsVirginSlot.ShouldDisable(withProgress).ShouldBeFalse();
  }
}
