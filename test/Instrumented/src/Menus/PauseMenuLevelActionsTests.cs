namespace Wfc.test.instrumented.Menus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.Ui;
using Wfc.Screens;
using Wfc.Screens.Levels;
using Wfc.Screens.MenuManager;
using Wfc.test.Helpers.Fakes;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// The pause menu offers only what the level it sits in can carry out. The hub is
// where levels are picked: there is nothing to restart there and nowhere to walk
// back out to, so those entries are left off it entirely. The tutorial is played
// before the hub has been reached, so it leaves out the way back to it.
public class PauseMenuLevelActionsTests(Node testScene) : TestClass(testScene) {
  private const double TIMEOUT_SECONDS = 10.0;

  private FakeDependenciesProvider _provider = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await _idle();
  }

  [Cleanup]
  public void Cleanup() {
    TestScene.GetTree().Paused = false;
    _provider.QueueFree();
  }

  [Test]
  public async Task ALevelOffersEveryEntry() {
    var pauseMenu = await _pauseMenuIn(
      LevelDispatcher.LEVELS.First(level => level.Id != LevelId.Tutorial).Id);

    _shownButtons(pauseMenu).ShouldBe([
      "ResumeButton",
      "RestartCheckpointButton",
      "RestartLevelButton",
      "ReturnToHubButton",
      "SettingsButton",
      "BackButton",
    ]);
  }

  [Test]
  public async Task TheTutorialLeavesOutTheReturnToHubEntry() {
    var pauseMenu = await _pauseMenuIn(LevelId.Tutorial);

    _shownButtons(pauseMenu).ShouldBe([
      "ResumeButton",
      "RestartCheckpointButton",
      "RestartLevelButton",
      "SettingsButton",
      "BackButton",
    ]);
  }

  [Test]
  public async Task TheHubLeavesOutTheLevelOnlyEntries() {
    var pauseMenu = await _pauseMenuIn(LevelId.Hub);

    _shownButtons(pauseMenu).ShouldBe(["ResumeButton", "SettingsButton", "BackButton"]);
  }

  // Boots the game screen on the given level and pauses it, the way losing the
  // window does.
  private async Task<PauseMenu> _pauseMenuIn(LevelId levelId) {
    _provider.Save = new FakeSaveManager(selectedSlot: 0).WithFilledSlot(0, progress: 0);
    _provider.MenuManager.SetCurrentLevel(levelId);
    _provider.MenuManager.GoToMenu(GameMenus.GAME);
    await _idle();

    var orchestrator = _provider.FindDescendants<SceneOrchester>().FirstOrDefault();
    orchestrator.ShouldNotBeNull();
    (await _waitUntil(() => _levelOf(orchestrator!)?.LevelId == levelId))
      .ShouldBeTrue($"the game screen never loaded {levelId}");

    var pauseMenu = _levelOf(orchestrator!)!.PauseMenuNode;
    _provider.PropagateNotification((int)Node.NotificationWMWindowFocusOut);
    (await _waitUntil(() => _shownButtons(pauseMenu).Count > 0))
      .ShouldBeTrue("the pause menu never came up");
    return pauseMenu;
  }

  private static List<string> _shownButtons(PauseMenu pauseMenu) =>
    [.. pauseMenu.FindDescendants<PauseMenuBtn>()
        .Where(button => button.Visible)
        .Select(button => button.Name.ToString())];

  private static GameLevel? _levelOf(SceneOrchester orchestrator) =>
    orchestrator.GetChildren().OfType<GameLevel>().FirstOrDefault();

  private async Task<bool> _waitUntil(Func<bool> condition, double timeoutSeconds = TIMEOUT_SECONDS) {
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
