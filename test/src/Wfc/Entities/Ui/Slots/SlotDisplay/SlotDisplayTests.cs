namespace Wfc.Entities.Ui.Slots.Test;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Persistence;
using Wfc.Entities.Ui.Slots;
using Wfc.test.Helpers.Fakes;

// Slots are stored 0-based and shown 1-based, and there may be no slot at all. The
// main menu used to print the raw index, so it showed "0" for the slot the play
// sub-menu called "1" and "-1" once the selected slot had been deleted.
public class SlotDisplayTests(Node testScene) : TestClass(testScene) {
  private const string NO_SLOT_KEY = "menu.label.no_slot";
  private const string CURRENT_SLOT_KEY = "menu.label.current_slot";

  private readonly FakeLocalizationService _localization = new();

  [Test]
  public void ShowsTheSlotNumberOneBased() {
    new FakeSaveManager(selectedSlot: 0).GetSelectedSlotText(_localization).ShouldBe("1");
    new FakeSaveManager(selectedSlot: 2).GetSelectedSlotText(_localization).ShouldBe("3");
  }

  [Test]
  public void ShowsNoSlotWhenThereIsNoneSelected() {
    var saveManager = new FakeSaveManager(selectedSlot: ISaveManager.NO_SLOT);

    saveManager.GetSelectedSlotText(_localization).ShouldBe(NO_SLOT_KEY);
  }

  // The state the crash came out of: delete the slot you are on, and there is no
  // longer one to name.
  [Test]
  public void ReportsNoSlotAfterTheSelectedOneIsDeleted() {
    var saveManager = new FakeSaveManager(selectedSlot: 1).WithFilledSlot(1);

    saveManager.RemoveSaveSlot(1);

    saveManager.HasSelectedSlot().ShouldBeFalse();
    saveManager.GetSelectedSlotIndex().ShouldBe(ISaveManager.NO_SLOT);
    saveManager.GetSelectedSlotText(_localization).ShouldBe(NO_SLOT_KEY);
  }

  // Deleting a slot the player is not on leaves their selection where it was.
  [Test]
  public void KeepsTheSelectionWhenAnotherSlotIsDeleted() {
    var saveManager = new FakeSaveManager(selectedSlot: 1).WithFilledSlot(1).WithFilledSlot(2);

    saveManager.RemoveSaveSlot(2);

    saveManager.HasSelectedSlot().ShouldBeTrue();
    saveManager.GetSelectedSlotText(_localization).ShouldBe("2");
  }

  // The main menu and the select-slot screen print the same line, so it is built in
  // one place rather than assembled from the caption and the number twice over.
  [Test]
  public void BuildsTheCurrentSlotLine() {
    new FakeSaveManager(selectedSlot: 1)
        .GetCurrentSlotLine(_localization)
        .ShouldBe($"{CURRENT_SLOT_KEY}: 2");

    new FakeSaveManager(selectedSlot: ISaveManager.NO_SLOT)
        .GetCurrentSlotLine(_localization)
        .ShouldBe($"{CURRENT_SLOT_KEY}: {NO_SLOT_KEY}");
  }
}
