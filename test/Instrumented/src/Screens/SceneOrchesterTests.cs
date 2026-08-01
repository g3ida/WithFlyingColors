namespace Wfc.test.instrumented.Screens;

using System;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Door;
using Wfc.Screens;
using Wfc.Screens.Levels;
using Wfc.Screens.MenuManager;
using Wfc.test.Helpers.Fakes;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;
using Wfc.Utils.Colors;
using EventHandler = Wfc.Core.Event.EventHandler;

// The in-game level flow, end to end: a real level cleared under a real orchestrator
// must walk back out to the hub behind the title card and hand play back, a door
// entered in the hub must swap to the level behind it, and the last level must still
// reach the cleared screen instead.
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
  public async Task ClearingAChainLevelWalksBackOutToTheHub() {
    _provider.Save = new FakeSaveManager(selectedSlot: 0);
    _provider.MenuManager.GoToMenu(GameMenus.GAME);
    await _idle();

    var orchestrator = _orchestrator();
    orchestrator.ShouldNotBeNull();
    (await _waitUntil(() => _currentLevelIdOf(orchestrator!) == LevelId.Tutorial))
      .ShouldBeTrue("the game screen never loaded the first level");

    EventHandler.Instance.EmitLevelCleared();

    (await _waitUntil(() => _currentLevelIdOf(orchestrator!) == LevelId.Hub))
      .ShouldBeTrue("the cleared level never walked back out to the hub");
    _provider.Save.RecordLevelClearedCallCount.ShouldBe(1);
    (await _waitUntil(() => !TestScene.GetTree().Paused))
      .ShouldBeTrue("play was never handed back after the cover");
    // The intro cutscene owns the player until the title has faded out; only then is
    // the input lock released.
    (await _waitUntil(() => _currentPlayerInputEnabled(orchestrator!)))
      .ShouldBeTrue("the intro cutscene never returned control to the player");
  }

  [Test]
  public async Task EnteringADoorSwapsToTheLevelBehindIt() {
    _provider.Save = new FakeSaveManager(selectedSlot: 0);
    _provider.MenuManager.SetCurrentLevel(LevelId.Hub);
    _provider.MenuManager.GoToMenu(GameMenus.GAME);
    await _idle();

    var orchestrator = _orchestrator();
    orchestrator.ShouldNotBeNull();
    (await _waitUntil(() => _currentLevelIdOf(orchestrator!) == LevelId.Hub))
      .ShouldBeTrue("the game screen never loaded the hub");

    EventHandler.Instance.EmitDoorEntered((int)LevelId.FourColors);

    (await _waitUntil(() => _currentLevelIdOf(orchestrator!) == LevelId.FourColors))
      .ShouldBeTrue("the door never swapped to the level behind it");
    (await _waitUntil(() => !TestScene.GetTree().Paused))
      .ShouldBeTrue("play was never handed back after the cover");
    (await _waitUntil(() => _currentPlayerInputEnabled(orchestrator!)))
      .ShouldBeTrue("the intro cutscene never returned control to the player");
  }

  // A restart rides the same cover-and-swap a door does, minus the write: the run so
  // far is exactly what the player is giving up on, and banking it would push the
  // resume point back to the start of a level they had got further into.
  [Test]
  public async Task RestartingRebuildsTheLevelAndBanksNothing() {
    _provider.Save = new FakeSaveManager(selectedSlot: 0).WithFilledSlot(0, progress: 0);
    _provider.MenuManager.GoToMenu(GameMenus.GAME);
    await _idle();

    var orchestrator = _orchestrator();
    orchestrator.ShouldNotBeNull();
    (await _waitUntil(() => _currentLevelIdOf(orchestrator!) == LevelId.Tutorial))
      .ShouldBeTrue("the game screen never loaded the first level");
    var before = _currentLevelInstanceOf(orchestrator!);
    var writesBefore = _provider.Save.RecordProgressCallCount;

    EventHandler.Instance.EmitLevelRestartRequested();

    (await _waitUntil(() => _currentLevelInstanceOf(orchestrator!) is { } now && now != before))
      .ShouldBeTrue("the level was never rebuilt");
    _currentLevelIdOf(orchestrator!).ShouldBe(LevelId.Tutorial, "the restart landed on another level");
    _provider.Save.RecordProgressCallCount
      .ShouldBe(writesBefore, "a restart wrote its own doorstep progress into the slot");
    (await _waitUntil(() => !TestScene.GetTree().Paused))
      .ShouldBeTrue("play was never handed back after the cover");
  }

  private static ulong? _currentLevelInstanceOf(SceneOrchester orchestrator) =>
    orchestrator.GetChildren().OfType<GameLevel>().FirstOrDefault()?.GetInstanceId();

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

  // The hub is built before the clear that sent the player back to it is banked, so a
  // door that only read the slot on the way in showed the state from before the level
  // was finished: the next door still chained, the pentagon still gray, until the game
  // was quit and loaded again.
  [Test]
  public async Task TheHubDoorsShowTheClearThatJustSentThePlayerBackToThem() {
    _provider.Save = new FakeSaveManager(selectedSlot: 0).WithFilledSlot(0, progress: 0);
    _provider.MenuManager.GoToMenu(GameMenus.GAME);
    await _idle();

    var orchestrator = _orchestrator();
    orchestrator.ShouldNotBeNull();
    (await _waitUntil(() => _currentLevelIdOf(orchestrator!) == LevelId.Tutorial))
      .ShouldBeTrue("the game screen never loaded the first level");

    EventHandler.Instance.EmitLevelCleared();

    (await _waitUntil(() => _currentLevelIdOf(orchestrator!) == LevelId.Hub))
      .ShouldBeTrue("the cleared level never walked back out to the hub");
    var door = _doorFor(orchestrator!, LevelId.FourColors);
    door.ShouldNotBeNull("the hub has no door for the level the tutorial unlocks");
    door!.IsLocked.ShouldBeFalse("the door the clear unlocked is still chained");
  }

  // Same contract from the other side: anything written into the slot while the hub is
  // standing has to reach the doors, since that is the only thing that ever shows it.
  [Test]
  public async Task ABankedGemLightsItsDoorWhileTheHubIsStanding() {
    _provider.Save = new FakeSaveManager(selectedSlot: 0).WithFilledSlot(0, progress: 0);
    _provider.MenuManager.SetCurrentLevel(LevelId.Hub);
    _provider.MenuManager.GoToMenu(GameMenus.GAME);
    await _idle();

    var orchestrator = _orchestrator();
    orchestrator.ShouldNotBeNull();
    (await _waitUntil(() => _currentLevelIdOf(orchestrator!) == LevelId.Hub))
      .ShouldBeTrue("the game screen never loaded the hub");

    var door = _doorFor(orchestrator!, LevelId.Tutorial);
    door.ShouldNotBeNull();
    _archGemOf(door!, ColorUtils.BLUE)?.IsCollected
      .ShouldBe(false, "the door started out claiming a gem that was never collected");

    _provider.Save.RecordProgress(TestScene.GetTree(), LevelId.Tutorial, 10, [ColorUtils.BLUE]);

    _archGemOf(door!, ColorUtils.BLUE)?.IsCollected
      .ShouldBe(true, "the gem was banked but its door never lit up");
  }

  private static Door? _doorFor(SceneOrchester orchestrator, LevelId targetLevel) =>
    orchestrator.FindDescendants<Door>().FirstOrDefault(door => door.TargetLevel == targetLevel);

  private static DoorArchGem? _archGemOf(Door door, string colorGroup) =>
    door.FindDescendants<DoorArchGem>().FirstOrDefault(gem => gem.ColorGroup == colorGroup);

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
