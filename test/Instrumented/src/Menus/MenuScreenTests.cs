namespace Wfc.test.instrumented.Menus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.Core.Persistence;
using Wfc.Entities.Ui.Menubox;
using Wfc.Entities.Ui.Slots;
using Wfc.Screens;
using Wfc.Screens.Levels;
using Wfc.Screens.MenuManager;
using Wfc.test.Helpers.Fakes;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// Builds each menu screen for real, under a provider that stands in for RootNode, and
// checks it gets all the way through its entrance.
//
// Godot logs an exception thrown out of _Ready and carries on, so a screen can be half
// built without anything failing. These assert what the screen ends up looking like
// rather than the absence of a log line.
public class MenuScreenTests(Node testScene) : TestClass(testScene) {
  private static readonly GameMenus[] MENU_SCREENS = [
    GameMenus.MAIN_MENU,
    GameMenus.SETTINGS_MENU,
    GameMenus.CREDITS_MENU,
    GameMenus.SELECT_SLOT,
    GameMenus.LEVEL_SELECT_MENU,
    GameMenus.LEVEL_CLEAR_MENU,
  ];

  // Longest entrance is the main menu's title, whose last word waits three delays and
  // then slides for another. Generous so a slow CI machine doesn't turn it red.
  private const double ENTER_TIMEOUT_SECONDS = 6.0;

  // Where the settings face sits in the box's own order, which runs the way the box
  // turns rather than the way the faces are listed in the scene.
  private const int SETTINGS_BUTTON_INDEX = 1;

  private FakeDependenciesProvider _provider = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await _idle();
  }

  [Cleanup]
  public void Cleanup() => _provider.QueueFree();

  // The broad one: every screen builds, resolves and settles.
  [Test]
  public async Task EveryScreenFinishesEntering() {
    foreach (var menu in MENU_SCREENS) {
      _provider.MenuManager.GoToMenu(menu);
      await _idle();

      var screen = _currentScreen();
      screen.ShouldNotBeNull($"{menu} produced no screen");
      (await _waitUntil(() => !screen.IsInTransitionState()))
        .ShouldBeTrue($"{menu} never finished entering");
    }
  }

  // The select-slot screen reads save data to fill its panels in. It used to do that
  // from _Ready, before AutoInject had resolved anything, so the read threw and the
  // rest of the method - metadata, focus, centering - never ran.
  [Test]
  public async Task SelectSlotScreenFillsItsPanelsFromSaveData() {
    _provider.Save = new FakeSaveManager(selectedSlot: 1)
      .WithFilledSlot(1, progress: 60)
      .WithClearedLevel(1, LevelId.Tutorial);

    var screen = await _open(GameMenus.SELECT_SLOT);

    var panels = screen.FindDescendants<SaveSlotPanel>().ToList();
    panels.Count.ShouldBe(FakeSaveManager.NUM_SLOTS);
    // A filled slot describes its whole-game completion - the one cleared level against
    // however many the chain offers - while an empty one says so.
    var completion = 100 / LevelDispatcher.LEVELS.Count;
    panels[1].Description.ShouldContain(completion.ToString());
    panels[0].Description.ShouldBe("<EMPTY>");
  }

  // The crash from the slot review: delete the slot you are on, come back, and the
  // container indexed its panels with -1.
  [Test]
  public async Task SelectSlotScreenOpensWithNoSlotSelected() {
    _provider.Save = new FakeSaveManager(selectedSlot: ISaveManager.NO_SLOT).WithFilledSlot(0);

    var screen = await _open(GameMenus.SELECT_SLOT);

    screen.FindDescendants<SaveSlotPanel>().Count().ShouldBe(FakeSaveManager.NUM_SLOTS);
  }

  // Cancel is the screen's to answer, and answering it means going back one hop.
  [Test]
  public async Task CancelTakesASubScreenBackWhereItCameFrom() {
    var screen = await _open(GameMenus.CREDITS_MENU);

    _press(screen, IInputManager.Action.UICancel);

    (await _waitUntil(() => _provider.MenuManager.GetCurrentMenu() == GameMenus.MAIN_MENU))
      .ShouldBeTrue("cancel did not go back to the main menu");
  }

  // The main menu is the root of the tree, so cancel there must not walk off it. Back
  // from here would otherwise land on the game screen, which is what previous still
  // points at on a fresh run.
  [Test]
  public async Task CancelOnTheMainMenuStaysPut() {
    var screen = await _open(GameMenus.MAIN_MENU, from: GameMenus.CREDITS_MENU);

    _press(screen, IInputManager.Action.UICancel);
    await _idle();
    await _idle();

    _provider.MenuManager.GetCurrentMenu().ShouldBe(GameMenus.MAIN_MENU);
  }

  // The box is built inside the navigation that brings it back, so it used to ask
  // where the player had come from one transition too early: the answer was always the
  // main menu itself, the box always came back facing Play, and the next press then
  // turned it away from the button the player was aiming at.
  [Test]
  public async Task TheBoxComesBackFacingTheScreenThePlayerLeftFor() {
    await _open(GameMenus.SETTINGS_MENU);

    var screen = await _open(GameMenus.MAIN_MENU, from: GameMenus.SETTINGS_MENU);

    var menubox = screen.FindDescendants<Menubox>().FirstOrDefault();
    menubox.ShouldNotBeNull();
    menubox!.ActiveIndex.ShouldBe(SETTINGS_BUTTON_INDEX, "the box came back facing something else");
  }

  // The sub-menu only ever opens with at least one occupied slot. Load Game needs a
  // second save to be worth a row - with a single one it could only repeat Continue.
  [Test]
  public async Task PlaySubMenuHidesLoadGameWithASingleSave() {
    _provider.Save = new FakeSaveManager(selectedSlot: 0).WithFilledSlot(0, progress: 40);

    var items = await _buildPlaySubMenu();

    items.Count.ShouldBe(2);
  }

  [Test]
  public async Task PlaySubMenuOffersLoadGameWithASecondSave() {
    _provider.Save = new FakeSaveManager(selectedSlot: 0)
      .WithFilledSlot(0, progress: 40)
      .WithFilledSlot(2, progress: 10);

    var items = await _buildPlaySubMenu();

    items.Count.ShouldBe(3);
  }

  // A fresh install has nothing to continue or load, so Play skips the sub-menu
  // and the slot picker entirely: it creates a save in the first slot and starts.
  [Test]
  public async Task PlayWithEverySlotEmptyStartsANewGameDirectly() {
    _provider.Save = new FakeSaveManager(selectedSlot: ISaveManager.NO_SLOT);

    await _open(GameMenus.MAIN_MENU, from: GameMenus.CREDITS_MENU);
    GameEvents.Instance.OnMenuActionPressed(MenuAction.Play);

    (await _waitUntil(() => _provider.MenuManager.GetCurrentMenu() == GameMenus.GAME))
      .ShouldBeTrue("play with no saves should enter the game directly");
    _provider.Save.SelectedSlot.ShouldBe(0);
    _provider.Save.SaveGameCallCount.ShouldBe(1);
  }

  private async Task<List<SubMenuItem>> _buildPlaySubMenu() {
    var subMenu = new PlaySubMenu();
    _provider.AddChild(subMenu);
    await _idle();
    await _idle();
    return subMenu.FindDescendants<SubMenuItem>().ToList();
  }

  // Opens a screen and waits for it to settle, seeding the history first so back has
  // somewhere to go, the way the real provider does as the game boots.
  //
  // The seeding happens here rather than in Setup so a test can put its own save data
  // in place before the first screen resolves anything.
  private async Task<GameMenu> _open(GameMenus menu, GameMenus from = GameMenus.MAIN_MENU) {
    if (menu != from) {
      _provider.MenuManager.GoToMenu(from);
      await _idle();
      var first = _currentScreen();
      if (first != null) {
        await _waitUntil(() => !first.IsInTransitionState());
      }
    }

    _provider.MenuManager.GoToMenu(menu);
    await _idle();
    var screen = _currentScreen();
    screen.ShouldNotBeNull($"{menu} produced no screen");
    (await _waitUntil(() => !screen.IsInTransitionState())).ShouldBeTrue($"{menu} never finished entering");
    return screen;
  }

  // The menus read the action rather than the event, so the event only has to exist.
  private void _press(GameMenu screen, IInputManager.Action action) {
    _provider.Input.Press(action);
    screen._Input(new InputEventAction());
    _provider.Input.ReleaseAll();
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
