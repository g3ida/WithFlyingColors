namespace Wfc.test.instrumented.Persistence;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Persistence;

// Which slot the game reopens on. It is remembered in a file of its own beside the slots, so
// every move of the selection has to reach disk - and a selection that survives the slot it
// points at is a run the game offers to continue and then has nothing to continue from.
//
// These drive the real SaveManager rather than the fake, so they are the only tests that touch
// slot files at all. SavePaths.Root is redirected for the whole suite in Main.RunTests, and
// again here, so nothing below can reach a player's own saves.
public class SaveManagerSlotSelectionTests(Node testScene) : TestClass(testScene) {
  private const string SCRATCH_ROOT = "user://test-slot-selection";
  private const int SLOT = 1;

  private string _rootBeforeTest = default!;

  [Setup]
  public void Setup() {
    _rootBeforeTest = SavePaths.Root;
    SavePaths.Root = SCRATCH_ROOT;
    _wipe();
  }

  [Cleanup]
  public void Cleanup() {
    _wipe();
    SavePaths.Root = _rootBeforeTest;
  }

  [Test]
  public void ASelectedSlotIsStillSelectedOnTheNextLaunchTest() {
    var manager = _managerWithSavedSlot();

    _relaunched().GetSelectedSlotIndex().ShouldBe(SLOT);
    manager.IsSLotFilled(SLOT).ShouldBeTrue();
  }

  [Test]
  public void DeletingTheSelectedSlotClearsTheSelectionTest() {
    var manager = _managerWithSavedSlot();

    manager.RemoveSaveSlot(SLOT);

    manager.HasSelectedSlot().ShouldBeFalse();
    manager.GetSelectedSlotIndex().ShouldBe(ISaveManager.NO_SLOT);
  }

  // The regression this file exists for: the cleared selection never reached disk, so the next
  // launch read the deleted slot back out and opened as though that run were still in play.
  [Test]
  public void DeletingTheSelectedSlotIsRememberedOnTheNextLaunchTest() {
    var manager = _managerWithSavedSlot();

    manager.RemoveSaveSlot(SLOT);

    var relaunched = _relaunched();
    relaunched.HasSelectedSlot().ShouldBeFalse();
    relaunched.GetSelectedSlotIndex().ShouldBe(ISaveManager.NO_SLOT);
  }

  // Deleting a slot nobody had selected says nothing about the selection.
  [Test]
  public void DeletingAnotherSlotLeavesTheSelectionAloneTest() {
    var manager = _managerWithSavedSlot();

    manager.RemoveSaveSlot(SLOT + 1);

    manager.GetSelectedSlotIndex().ShouldBe(SLOT);
    _relaunched().GetSelectedSlotIndex().ShouldBe(SLOT);
  }

  // However the files went - deleted from a menu, or taken out from under the game - a slot
  // with nothing in it is nothing to have selected. The slot data is removed by hand here and
  // the file naming the selection left behind, which is the state the game finds them in.
  //
  // Every slot's metadata goes rather than one by index: a slot's directory is numbered from
  // one while the manager indexes its slots from zero, so naming the directory here would be
  // encoding that off-by-one into the test.
  [Test]
  public void ASelectionPointingAtAnEmptySlotIsNoSelectionTest() {
    _managerWithSavedSlot();
    _removeEverySlotsMetaData();

    _relaunched().HasSelectedSlot().ShouldBeFalse();
  }

  private SaveManager _managerWithSavedSlot() {
    var manager = new SaveManager();
    manager.Init();
    manager.SelectSlot(SLOT);
    manager.SaveGame(TestScene.GetTree(), SLOT);
    return manager;
  }

  // What the next launch sees: a manager built from nothing but what is on disk.
  private static SaveManager _relaunched() {
    var manager = new SaveManager();
    manager.Init();
    return manager;
  }

  private static void _removeEverySlotsMetaData() {
    for (var directory = 0; directory <= 3; directory++) {
      DirAccess.RemoveAbsolute($"{SavePaths.SlotDirectory(directory)}/save_slot_meta.save");
    }
  }

  private static void _wipe() {
    for (var slot = 0; slot <= 3; slot++) {
      var directory = SavePaths.SlotDirectory(slot);
      DirAccess.RemoveAbsolute($"{directory}/save_slot.save");
      DirAccess.RemoveAbsolute($"{directory}/save_slot_meta.save");
      DirAccess.RemoveAbsolute(directory);
    }
    DirAccess.RemoveAbsolute(SavePaths.SlotsInfo);
    DirAccess.RemoveAbsolute(SavePaths.Root);
  }
}
