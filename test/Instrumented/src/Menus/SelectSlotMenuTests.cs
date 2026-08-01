namespace Wfc.test.instrumented.Menus;

using System;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Persistence;
using Wfc.Entities.Ui;
using Wfc.Entities.Ui.Dialogs;
using Wfc.Entities.Ui.Slots;
using Wfc.Screens;
using Wfc.Screens.MenuManager;
using Wfc.test.Helpers.Fakes;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// The slot picker's two modes, exercised through the real screen: a single press on
// a slot performs the mode's action, empty slots are out of reach in Load mode, and
// starting a new game over an existing save always asks first.
public class SelectSlotMenuTests(Node testScene) : TestClass(testScene) {
  private const double ENTER_TIMEOUT_SECONDS = 6.0;

  private FakeDependenciesProvider _provider = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await _idle();
  }

  [Cleanup]
  public void Cleanup() => _provider.QueueFree();

  // The dead end this screen used to have: an empty slot in Load mode answered a
  // press with nothing and swallowed controller focus with it. Now it is simply
  // not selectable.
  [Test]
  public async Task LoadModeDisablesEmptySlots() {
    _provider.Save = new FakeSaveManager(selectedSlot: ISaveManager.NO_SLOT)
      .WithFilledSlot(0, progress: 40)
      .WithFilledSlot(1, progress: 10);

    var screen = await _openSlotPicker(SlotPickerMode.Load);

    var panels = _panels(screen);
    panels[0].IsDisabled.ShouldBeFalse();
    panels[1].IsDisabled.ShouldBeFalse();
    panels[2].IsDisabled.ShouldBeTrue("an empty slot cannot answer a load");
  }

  [Test]
  public async Task NewGameModeOffersEverySlot() {
    _provider.Save = new FakeSaveManager(selectedSlot: ISaveManager.NO_SLOT)
      .WithFilledSlot(0, progress: 40);

    var screen = await _openSlotPicker(SlotPickerMode.NewGame);

    _panels(screen).ShouldAllBe(panel => !panel.IsDisabled);
  }

  [Test]
  public async Task LoadModeSelectLoadsTheSlotAndEntersTheGame() {
    _provider.Save = new FakeSaveManager(selectedSlot: ISaveManager.NO_SLOT)
      .WithFilledSlot(0, progress: 40)
      .WithFilledSlot(1, progress: 10);

    var screen = await _openSlotPicker(SlotPickerMode.Load);
    _pressSlot(screen, 1);

    (await _waitUntil(() => _provider.MenuManager.GetCurrentMenu() == GameMenus.GAME))
      .ShouldBeTrue("selecting a filled slot in load mode should enter the game");
    _provider.Save.SelectedSlot.ShouldBe(1);
  }

  [Test]
  public async Task NewGameOnAnEmptySlotStartsImmediately() {
    _provider.Save = new FakeSaveManager(selectedSlot: ISaveManager.NO_SLOT);

    var screen = await _openSlotPicker(SlotPickerMode.NewGame);
    _pressSlot(screen, 2);

    (await _waitUntil(() => _provider.MenuManager.GetCurrentMenu() == GameMenus.GAME))
      .ShouldBeTrue("a new game in an empty slot should start without asking");
    _provider.Save.SelectedSlot.ShouldBe(2);
    _provider.Save.SaveGameCallCount.ShouldBe(1);
  }

  // A filled slot is one confirm away from losing a save, so the press must ask
  // rather than act.
  [Test]
  public async Task NewGameOnAFilledSlotAsksBeforeWiping() {
    _provider.Save = new FakeSaveManager(selectedSlot: ISaveManager.NO_SLOT)
      .WithFilledSlot(0, progress: 40);

    var screen = await _openSlotPicker(SlotPickerMode.NewGame);
    _pressSlot(screen, 0);
    await _idle();

    _provider.ModalStack.IsAnyOpen.ShouldBeTrue("overwriting a save should ask first");
    _provider.Save.RemoveSaveSlotCallCount.ShouldBe(0);
    _provider.MenuManager.GetCurrentMenu().ShouldBe(GameMenus.SELECT_SLOT);
  }

  // Confirming the overwrite wipes exactly the slot that was pressed and starts
  // the new game there.
  [Test]
  public async Task ConfirmingTheOverwriteWipesTheSlotAndStarts() {
    _provider.Save = new FakeSaveManager(selectedSlot: ISaveManager.NO_SLOT)
      .WithFilledSlot(0, progress: 40);

    var screen = await _openSlotPicker(SlotPickerMode.NewGame);
    _pressSlot(screen, 0);
    await _idle();

    var dialog = screen.FindDescendants<ConfirmDialog>().Single();
    dialog.EmitSignal(ConfirmDialog.SignalName.Confirmed);

    (await _waitUntil(() => _provider.MenuManager.GetCurrentMenu() == GameMenus.GAME))
      .ShouldBeTrue("a confirmed overwrite should start the new game");
    _provider.Save.RemoveSaveSlotCallCount.ShouldBe(1);
    _provider.Save.SelectedSlot.ShouldBe(0);
    _provider.Save.SaveGameCallCount.ShouldBe(1);
  }

  // While the dialog is up, focus must not wander back to the slot cards behind it:
  // it opens on a dialog button and every arrow leads to a dialog button.
  [Test]
  public async Task TheOverwriteDialogKeepsFocusOnItsOwnButtons() {
    _provider.Save = new FakeSaveManager(selectedSlot: ISaveManager.NO_SLOT)
      .WithFilledSlot(0, progress: 40);

    var screen = await _openSlotPicker(SlotPickerMode.NewGame);
    _pressSlot(screen, 0);
    await _idle();

    var dialog = screen.FindDescendants<ConfirmDialog>().Single();
    var focused = screen.GetViewport().GuiGetFocusOwner();
    focused.ShouldNotBeNull("the dialog should have grabbed focus");
    dialog.IsAncestorOf(focused).ShouldBeTrue("focus should sit on a dialog button");
    foreach (var neighborPath in new[] {
        focused.FocusNeighborLeft, focused.FocusNeighborRight,
        focused.FocusNeighborTop, focused.FocusNeighborBottom,
        focused.FocusNext, focused.FocusPrevious }) {
      neighborPath.IsEmpty.ShouldBeFalse("every focus exit must be pinned down");
      dialog.IsAncestorOf(focused.GetNode<Control>(neighborPath))
        .ShouldBeTrue("arrows must not leave the dialog");
    }
  }

  // Focus opens on the slot Continue would resume - the most recently played one - so
  // the card the player almost certainly wants is the one a press already acts on.
  [Test]
  public async Task TheMostRecentlyPlayedSlotOpensFocused() {
    _provider.Save = new FakeSaveManager(selectedSlot: ISaveManager.NO_SLOT)
      .WithFilledSlot(0, progress: 80, timestamp: 100UL)
      .WithFilledSlot(1, progress: 10, timestamp: 200UL);

    var screen = await _openSlotPicker(SlotPickerMode.Load);

    var panels = _panels(screen);
    panels[1].GetHasFocus().ShouldBeTrue("focus should open on the last played slot");
    panels[0].GetHasFocus().ShouldBeFalse();
  }

  // The two modes share one title; the instruction line is what tells the player
  // which question the screen is asking.
  [Test]
  public async Task BothModesShareTheTitleAndWearTheirOwnInstruction() {
    _provider.Save = new FakeSaveManager(selectedSlot: ISaveManager.NO_SLOT)
      .WithFilledSlot(0, progress: 40)
      .WithFilledSlot(1, progress: 10);

    var loadScreen = await _openSlotPicker(SlotPickerMode.Load);
    _titleOf(loadScreen).ShouldBe("SELECT SLOT");
    _instructionOf(loadScreen).ShouldContain("LOAD");

    var newGameScreen = await _openSlotPicker(SlotPickerMode.NewGame);
    _titleOf(newGameScreen).ShouldBe("SELECT SLOT");
    _instructionOf(newGameScreen).ShouldContain("NEW GAME");
  }

  // The entrance the screen owes its parts: every slot card slides in on its own
  // transition, staggered like the title's words - and the title keeps its own.
  // A mode-specific title rebuild used to replace the labels after their
  // transitions were parsed, and rebuilt labels slide in no more.
  [Test]
  public async Task TheTitleAndEverySlotCarryAnEntranceTransition() {
    _provider.Save = new FakeSaveManager(selectedSlot: ISaveManager.NO_SLOT);

    var screen = await _openSlotPicker(SlotPickerMode.NewGame);

    var titleLabels = screen.FindDescendants<TitleLabel>().ToList();
    titleLabels.ShouldNotBeEmpty();
    titleLabels.ShouldAllBe(label => label.FindDescendants<UITransition>().Any());
    _panels(screen).ShouldAllBe(panel => panel.FindDescendants<UITransition>().Any());
  }

  // One word per label, in the order they were built.
  private static string _titleOf(GameMenu screen) =>
    string.Join(" ", screen.FindDescendants<TitleLabel>().Select(label => label.content)).ToUpperInvariant();

  private static string _instructionOf(GameMenu screen) =>
    screen.GetNode<Label>("InstructionLabel").Text.ToUpperInvariant();

  private static System.Collections.Generic.List<SaveSlotPanel> _panels(GameMenu screen) =>
    screen.FindDescendants<SaveSlotPanel>().ToList();

  private static void _pressSlot(GameMenu screen, int id) =>
    _panels(screen)[id].EmitSignal(SaveSlotPanel.SignalName.Pressed);

  private async Task<GameMenu> _openSlotPicker(SlotPickerMode mode) {
    // Seeded like the real boot: the main menu first, so back has somewhere to go.
    _provider.MenuManager.GoToMenu(GameMenus.MAIN_MENU);
    await _idle();
    var mainMenu = _currentScreen();
    if (mainMenu != null) {
      await _waitUntil(() => !mainMenu.IsInTransitionState());
    }

    _provider.MenuManager.SetSlotPickerMode(mode);
    _provider.MenuManager.GoToMenu(GameMenus.SELECT_SLOT);
    await _idle();
    var screen = _currentScreen();
    screen.ShouldNotBeNull("the slot picker produced no screen");
    (await _waitUntil(() => !screen.IsInTransitionState()))
      .ShouldBeTrue("the slot picker never finished entering");
    return screen;
  }

  private GameMenu? _currentScreen() =>
    _provider.FindDescendants<GameMenu>().FirstOrDefault(screen => !screen.IsQueuedForDeletion());

  private async Task<bool> _waitUntil(Func<bool> condition) {
    var tree = TestScene.GetTree();
    var elapsed = 0.0;
    while (elapsed < ENTER_TIMEOUT_SECONDS) {
      if (condition()) {
        return true;
      }
      await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
      elapsed += 1.0 / 60.0;
    }
    return condition();
  }

  private async Task _idle() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
  }
}
