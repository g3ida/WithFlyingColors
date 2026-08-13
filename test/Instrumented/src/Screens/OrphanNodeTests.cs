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
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// A node that leaves the tree without being freed stays allocated for the rest of the run.
// Nothing about the game looks wrong when that happens - the level plays, the menu draws -
// so the only way it is ever noticed is a count that never comes back down.
public class OrphanNodeTests(Node testScene) : TestClass(testScene) {
  private const double LOAD_TIMEOUT_SECONDS = 20.0;

  private FakeDependenciesProvider _provider = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider {
      Save = new FakeSaveManager(selectedSlot: 0).WithFilledSlot(0, progress: 0).WithHubArrivalSeen(0)
    };
    TestScene.AddChild(_provider);
    await _idle();
  }

  [Cleanup]
  public void Cleanup() {
    TestScene.GetTree().Paused = false;
    _provider.QueueFree();
  }

  // The player used to build itself two animation helpers that were Nodes but were never
  // parented, so every level ever loaded left two behind - two per Continue, for the run.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task LeavingAndReenteringTheGameFreesEverythingItBuilt() {
    // The first visit is the one that pays for whatever is built once and kept. What the
    // count does over the visits after it is the question.
    await _enterTheGame();
    await _returnToTheMainMenu();
    var orphansAfterTheFirstVisit = _orphanCount();

    await _enterTheGame();
    await _returnToTheMainMenu();
    await _enterTheGame();
    await _returnToTheMainMenu();

    _orphanCount().ShouldBe(
      orphansAfterTheFirstVisit,
      "leaving the game screen left nodes behind that nothing will ever free");
  }

  private async Task _enterTheGame() {
    _provider.MenuManager.GoToMenu(GameMenus.GAME);
    (await _waitUntil(() => _orchestrator() is { } orchestrator && _levelOf(orchestrator) != null))
      .ShouldBeTrue("the game screen never loaded a level");
  }

  // Freeing is deferred, so the count only means anything once the screen is really gone.
  private async Task _returnToTheMainMenu() {
    _provider.MenuManager.GoToMenu(GameMenus.MAIN_MENU);
    (await _waitUntil(() => _orchestrator() == null))
      .ShouldBeTrue("the game screen was never freed");
  }

  private static int _orphanCount() =>
    (int)Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount);

  private SceneOrchester? _orchestrator() =>
    _provider.FindDescendants<SceneOrchester>().FirstOrDefault();

  private static GameLevel? _levelOf(SceneOrchester orchestrator) =>
    orchestrator.GetChildren().OfType<GameLevel>().FirstOrDefault();

  private async Task<bool> _waitUntil(Func<bool> condition, double timeoutSeconds = LOAD_TIMEOUT_SECONDS) {
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
