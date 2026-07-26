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

  [Test]
  public void StartsOnTheMainMenu() {
    _menuManager.GetCurrentMenu().ShouldBe(GameMenus.MAIN_MENU);
  }

  [Test]
  public void EveryScreenResolvesToAScenePath() {
    foreach (GameMenus menu in System.Enum.GetValues<GameMenus>()) {
      _menuManager.GetMenuScenePath(menu).ShouldNotBeNull($"{menu} has no scene path");
    }
  }

  [Test]
  public async Task GoingToAScreenMakesItCurrentAndRemembersTheLast() {
    await _goTo(GameMenus.STATS_MENU);

    _menuManager.GetCurrentMenu().ShouldBe(GameMenus.STATS_MENU);
    _menuManager.GetPreviousMenu().ShouldBe(GameMenus.MAIN_MENU);
  }

  [Test]
  public async Task OnlyOneScreenIsAliveAtATime() {
    await _goTo(GameMenus.STATS_MENU);
    await _goTo(GameMenus.SETTINGS_MENU);

    // QueueFree is deferred, so the outgoing screen is only gone a frame later.
    await _idle();
    _provider.FindDescendants<Wfc.Screens.GameMenu>().ShouldHaveSingleItem();
  }

  // Previous is a single slot rather than a stack, so it only ever remembers one hop.
  // Worth pinning down: it is the behavior every back button depends on.
  [Test]
  public async Task PreviousOnlyGoesBackOneHop() {
    await _goTo(GameMenus.STATS_MENU);
    await _goTo(GameMenus.SETTINGS_MENU);
    await _goTo(GameMenus.SELECT_SLOT);

    _menuManager.GetPreviousMenu().ShouldBe(GameMenus.SETTINGS_MENU);
  }

  [Test]
  public async Task AskingForTheScreenAlreadyShownChangesNothing() {
    await _goTo(GameMenus.STATS_MENU);

    await _goTo(GameMenus.STATS_MENU);

    _menuManager.GetCurrentMenu().ShouldBe(GameMenus.STATS_MENU);
    // Previous is untouched: a no-op navigation must not make back point at the
    // screen the player is already on.
    _menuManager.GetPreviousMenu().ShouldBe(GameMenus.MAIN_MENU);
  }

  [Test]
  public void TheLevelToLoadStartsUnset() {
    _menuManager.GetCurrentLevelId().ShouldBeNull();
  }

  [Test]
  public void SettingALevelIsRememberedForTheGameScreen() {
    _menuManager.SetCurrentLevel(LevelId.Level1);

    _menuManager.GetCurrentLevelId().ShouldBe(LevelId.Level1);
    // The game screen keeps it; asking for the path of any menu screen clears it, so
    // returning to a menu can't leave a stale level queued up.
    _menuManager.GetMenuScenePath(GameMenus.GAME);
    _menuManager.GetCurrentLevelId().ShouldBe(LevelId.Level1);
  }

  [Test]
  public void GoingToAMenuForgetsTheQueuedLevel() {
    _menuManager.SetCurrentLevel(LevelId.Level1);

    _menuManager.GetMenuScenePath(GameMenus.MAIN_MENU);

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
