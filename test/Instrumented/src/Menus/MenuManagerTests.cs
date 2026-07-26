namespace Wfc.test.instrumented.Menus;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Screens.Levels;
using Wfc.Screens.MenuManager;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// The navigation core: which screen is current, which one back goes to, and the level
// the game screen is about to load. None of it was covered.
//
// These run against real scenes rather than a stub, so a screen that fails to build or
// to resolve its dependencies fails here too.
public class MenuManagerTests(Node testScene) : TestClass(testScene) {
  private FakeDependenciesProvider _provider = default!;
  private IMenuManager _menuManager = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await _idle();
    _menuManager = _provider.MenuManager;
  }

  [Cleanup]
  public void Cleanup() {
    _provider.QueueFree();
  }

  // Nothing has been shown yet, so there is nowhere to go back to. The real game seeds
  // the history by navigating to the main menu as it boots.
  [Test]
  public void StartsWithAnEmptyHistory() {
    _menuManager.PeekBack().ShouldBeNull();
    _menuManager.GoToMenu(GameMenus.MAIN_MENU).ShouldBeTrue();
    _menuManager.GetCurrentMenu().ShouldBe(GameMenus.MAIN_MENU);
    _menuManager.PeekBack().ShouldBeNull();
  }

  [Test]
  public void EveryScreenResolvesToAScenePath() {
    foreach (GameMenus menu in System.Enum.GetValues<GameMenus>()) {
      _menuManager.GetMenuScenePath(menu).ShouldNotBeNull($"{menu} has no scene path");
    }
  }

  [Test]
  public async Task GoingToAScreenMakesItCurrentAndRemembersTheLast() {
    await _goTo(GameMenus.MAIN_MENU);

    await _goTo(GameMenus.STATS_MENU);

    _menuManager.GetCurrentMenu().ShouldBe(GameMenus.STATS_MENU);
    _menuManager.PeekBack().ShouldBe(GameMenus.MAIN_MENU);
    _menuManager.GetLastVisitedMenu().ShouldBe(GameMenus.MAIN_MENU);
  }

  [Test]
  public async Task OnlyOneScreenIsAliveAtATime() {
    await _goTo(GameMenus.MAIN_MENU);
    await _goTo(GameMenus.STATS_MENU);
    await _goTo(GameMenus.SETTINGS_MENU);

    // QueueFree is deferred, so the outgoing screen is only gone a frame later.
    await _idle();
    _provider.FindDescendants<Wfc.Screens.GameMenu>().ShouldHaveSingleItem();
  }

  // Back walks the whole history, one screen at a time, rather than bouncing between
  // the last two. A single "previous" slot could only ever remember one hop.
  [Test]
  public async Task BackUnwindsTheHistoryOneScreenAtATime() {
    await _goTo(GameMenus.MAIN_MENU);
    await _goTo(GameMenus.SELECT_SLOT);
    await _goTo(GameMenus.SETTINGS_MENU);

    _menuManager.PeekBack().ShouldBe(GameMenus.SELECT_SLOT);
    await _goTo(GameMenus.SELECT_SLOT);

    _menuManager.PeekBack().ShouldBe(GameMenus.MAIN_MENU);
    await _goTo(GameMenus.MAIN_MENU);

    _menuManager.GetCurrentMenu().ShouldBe(GameMenus.MAIN_MENU);
    _menuManager.PeekBack().ShouldBeNull();
  }

  // Returning to a screen already in the history unwinds to it rather than stacking a
  // second copy. Leaving the pause menu for the main menu drops the game screen in
  // between, and moving back and forth can't grow the history without bound.
  [Test]
  public async Task ReturningToAVisitedScreenUnwindsToIt() {
    await _goTo(GameMenus.MAIN_MENU);
    await _goTo(GameMenus.GAME);
    await _goTo(GameMenus.LEVEL_SELECT_MENU);

    await _goTo(GameMenus.MAIN_MENU);

    _menuManager.GetCurrentMenu().ShouldBe(GameMenus.MAIN_MENU);
    _menuManager.PeekBack().ShouldBeNull("the game and level select should have been dropped");
  }

  [Test]
  public async Task MovingBetweenTwoScreensDoesNotGrowTheHistory() {
    await _goTo(GameMenus.MAIN_MENU);

    for (var i = 0; i < 4; i++) {
      await _goTo(GameMenus.STATS_MENU);
      await _goTo(GameMenus.MAIN_MENU);
    }

    _menuManager.GetCurrentMenu().ShouldBe(GameMenus.MAIN_MENU);
    _menuManager.PeekBack().ShouldBeNull();
  }

  [Test]
  public async Task AskingForTheScreenAlreadyShownChangesNothing() {
    await _goTo(GameMenus.MAIN_MENU);
    await _goTo(GameMenus.STATS_MENU);

    _menuManager.GoToMenu(GameMenus.STATS_MENU).ShouldBeFalse();

    _menuManager.GetCurrentMenu().ShouldBe(GameMenus.STATS_MENU);
    // A refused navigation must not make back point at the screen already showing.
    _menuManager.PeekBack().ShouldBe(GameMenus.MAIN_MENU);
    _menuManager.GetLastVisitedMenu().ShouldBe(GameMenus.MAIN_MENU);
  }

  [Test]
  public void TheLevelToLoadStartsUnset() {
    _menuManager.GetCurrentLevelId().ShouldBeNull();
  }

  // Asking where a screen lives is a question, not a navigation. It used to clear the
  // queued level as a side effect, so reading a path changed what the game would load.
  [Test]
  public void AskingForAScenePathLeavesTheQueuedLevelAlone() {
    _menuManager.SetCurrentLevel(LevelId.Level1);

    _menuManager.GetMenuScenePath(GameMenus.MAIN_MENU);

    _menuManager.GetCurrentLevelId().ShouldBe(LevelId.Level1);
  }

  [Test]
  public async Task GoingToTheGameScreenKeepsTheQueuedLevel() {
    await _goTo(GameMenus.MAIN_MENU);
    _menuManager.SetCurrentLevel(LevelId.Level1);

    await _goTo(GameMenus.GAME);

    _menuManager.GetCurrentLevelId().ShouldBe(LevelId.Level1);
  }

  [Test]
  public async Task GoingToAMenuForgetsTheQueuedLevel() {
    await _goTo(GameMenus.MAIN_MENU);
    _menuManager.SetCurrentLevel(LevelId.Level1);

    await _goTo(GameMenus.STATS_MENU);

    _menuManager.GetCurrentLevelId().ShouldBeNull();
  }

  private async Task _goTo(GameMenus menu) {
    _menuManager.GoToMenu(menu);
    await _idle();
  }

  private async Task _idle() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
  }
}
