namespace Wfc.test.instrumented.Screens;

using System;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Screens;
using Wfc.Screens.Levels;
using Wfc.Screens.MenuManager;
using Wfc.test.Helpers.Fakes;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;
using EventHandler = Wfc.Core.Event.EventHandler;

// The auto-advance itself, end to end: a real level cleared under a real orchestrator
// must swap to the next chain level behind the title card and hand play back, and the
// last level must still reach the cleared screen instead.
public class SceneOrchesterTests(Node testScene) : TestClass(testScene) {
  // Cover fade + hold + reveal fade, with slack for a slow CI machine.
  private const double SEQUENCE_TIMEOUT_SECONDS = 10.0;

  private FakeDependenciesProvider _provider = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await _idle();
  }

  [Cleanup]
  public void Cleanup() {
    // The interstitial pauses the tree; a failing test must not leave it that way
    // for whatever runs next.
    TestScene.GetTree().Paused = false;
    _provider.QueueFree();
  }

  [Test]
  public async Task ClearingAChainLevelSwapsToTheNextBehindTheCard() {
    _provider.Save = new FakeSaveManager(selectedSlot: 0);
    _provider.MenuManager.GoToMenu(GameMenus.GAME);
    await _idle();

    var orchestrator = _orchestrator();
    orchestrator.ShouldNotBeNull();
    (await _waitUntil(() => _currentLevelIdOf(orchestrator!) == LevelId.Tutorial))
      .ShouldBeTrue("the game screen never loaded the first level");

    EventHandler.Instance.EmitLevelCleared();

    (await _waitUntil(() => _currentLevelIdOf(orchestrator!) == LevelId.FourColors))
      .ShouldBeTrue("the cleared level was never swapped for the next one");
    _provider.Save.RecordLevelClearedCallCount.ShouldBe(1);
    (await _waitUntil(() => !TestScene.GetTree().Paused))
      .ShouldBeTrue("play was never handed back after the cover");
    // The intro cutscene owns the player until the title has faded out; only then is
    // the input lock released.
    (await _waitUntil(() => _currentPlayerInputEnabled(orchestrator!)))
      .ShouldBeTrue("the intro cutscene never returned control to the player");
  }

  private static bool _currentPlayerInputEnabled(SceneOrchester orchestrator) {
    var level = orchestrator.GetChildren().OfType<GameLevel>().FirstOrDefault();
    return level?.PlayerNode != null && !level.PlayerNode.HandleInputIsDisabled;
  }

  [Test]
  public async Task ClearingTheLastLevelReachesTheClearedScreen() {
    _provider.Save = new FakeSaveManager(selectedSlot: 0);
    _provider.MenuManager.SetCurrentLevel(LevelId.Level1);
    _provider.MenuManager.GoToMenu(GameMenus.GAME);
    await _idle();

    var orchestrator = _orchestrator();
    orchestrator.ShouldNotBeNull();
    (await _waitUntil(() => _currentLevelIdOf(orchestrator!) == LevelId.Level1))
      .ShouldBeTrue("the game screen never loaded the last level");

    EventHandler.Instance.EmitLevelCleared();

    (await _waitUntil(() => _provider.MenuManager.GetCurrentMenu() == GameMenus.LEVEL_CLEAR_MENU))
      .ShouldBeTrue("the end of the chain never reached the cleared screen");
    _provider.Save.RecordLevelClearedCallCount.ShouldBe(1);
  }

  private SceneOrchester? _orchestrator() =>
    _provider.FindDescendants<SceneOrchester>().FirstOrDefault();

  private static LevelId? _currentLevelIdOf(SceneOrchester orchestrator) =>
    orchestrator.GetChildren().OfType<GameLevel>().FirstOrDefault()?.LevelId;

  // Wall-clock rather than frame-counting: the card's hold runs on a Timer and its
  // fades on tweens, both of which advance with real time.
  private async Task<bool> _waitUntil(Func<bool> condition, double timeoutSeconds = SEQUENCE_TIMEOUT_SECONDS) {
    var tree = TestScene.GetTree();
    var deadline = Time.GetTicksMsec() + (ulong)(timeoutSeconds * 1000);
    while (Time.GetTicksMsec() < deadline) {
      if (condition()) {
        return true;
      }
      await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
    return condition();
  }

  private async Task _idle() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
  }
}
