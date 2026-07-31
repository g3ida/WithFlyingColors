namespace Wfc.test.instrumented.Ui;

using System;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Input;
using Wfc.Core.Localization;
using Wfc.Entities.Ui;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// The presentation around a level start. The orchestrator swaps levels on Covered and
// ends the intro cutscene on TitleFinished, so those signals firing - in that order,
// exactly once per run - is the whole contract.
public class LevelTitleCardTests(Node testScene) : TestClass(testScene) {
  // Cover lift + title fades + hold, with slack for a slow CI machine.
  private const double SEQUENCE_TIMEOUT_SECONDS = 6.0;
  // Long enough for the fade-out, far shorter than the remaining hold: reaching
  // TitleFinished within this window proves the hold was actually skipped.
  private const double SKIP_TIMEOUT_SECONDS = 0.9;
  // Comfortably past the title's fade-in, so the press lands during the hold.
  private const double REACH_HOLD_SECONDS = 1.0;

  private FakeDependenciesProvider _provider = default!;
  private LevelTitleCard _card = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    _card = SceneHelpers.InstantiateNode<LevelTitleCard>();
    _provider.AddChild(_card);
    await _idle();
  }

  [Cleanup]
  public void Cleanup() => _provider.QueueFree();

  [Test]
  public async Task CoversForTheSwapThenRunsTheTitleToTheEnd() {
    var covered = 0;
    var finished = 0;
    _card.Covered += () => covered++;
    _card.TitleFinished += () => finished++;

    _card.CoverForSwap();
    // A second cover request while one is up must not re-run the fade.
    _card.CoverForSwap();

    (await _waitUntil(() => covered == 1, SEQUENCE_TIMEOUT_SECONDS)).ShouldBeTrue("the cover never became opaque");
    finished.ShouldBe(0);

    _card.PresentTitle(TranslationKey.game_level_title_fourColors);

    (await _waitUntil(() => finished == 1, SEQUENCE_TIMEOUT_SECONDS)).ShouldBeTrue("the title never finished");
    covered.ShouldBe(1);
  }

  [Test]
  public async Task AConfirmPressSkipsTheHold() {
    var finished = false;
    _card.TitleFinished += () => finished = true;

    _card.PresentTitle(TranslationKey.game_level_title_fourColors);
    await _wallWait(REACH_HOLD_SECONDS);

    _provider.Input.Press(IInputManager.Action.UIConfirm);
    _card._Input(new InputEventKey());
    _provider.Input.Release(IInputManager.Action.UIConfirm);

    (await _waitUntil(() => finished, SKIP_TIMEOUT_SECONDS)).ShouldBeTrue("the press did not cut the hold short");
  }

  // Wall-clock rather than frame-counting: the card's hold runs on a Timer and its
  // fades on tweens, both of which advance with real time, while a headless run can
  // push frames much faster than the display ever would.
  private async Task<bool> _waitUntil(Func<bool> condition, double timeoutSeconds) {
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

  private async Task _wallWait(double seconds) {
    var tree = TestScene.GetTree();
    var deadline = Time.GetTicksMsec() + (ulong)(seconds * 1000);
    while (Time.GetTicksMsec() < deadline) {
      await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
  }

  private async Task _idle() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
  }
}
