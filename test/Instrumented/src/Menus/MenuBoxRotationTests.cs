namespace Wfc.test.instrumented.Menus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Input;
using Wfc.Entities.Ui.Menubox;
using Wfc.Screens;
using Wfc.Screens.MenuManager;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// The box turns a quarter at a time and has to stop on the face it was aimed at. It used
// to be turned for as many physics ticks as its clock was still running, which is not the
// same count as the turn's duration when the press arrives from inside the tick: the box
// swept a tick's worth past the face it was going to and was pulled back onto it.
public class MenuBoxRotationTests(Node testScene) : TestClass(testScene) {
  private const int TICKS_TO_TRACE = 14;
  private const double SETTLE_TIMEOUT_SECONDS = 6.0;
  private const float QUARTER_TURN_EPSILON = 0.001f;
  private const int FACES = 4;

  private FakeDependenciesProvider _provider = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await _idle();
  }

  [Cleanup]
  public void Cleanup() => _provider.QueueFree();

  // The press the player actually makes: polled by Menubox._PhysicsProcess, so the turn
  // is started midway through the same tick that then turns the box.
  [Test]
  public async Task TurningFromSettingsStopsOnPlayWithoutSweepingPastIt() {
    var (menubox, body) = await _openMainMenuFrom(GameMenus.SETTINGS_MENU);
    var start = body.Rotation;

    var angles = await _traceTurn(IInputManager.Action.UIRight, body);

    // Play is a quarter turn towards the box's resting face; nothing on the way there
    // may go beyond it.
    var target = start + MathUtils.PI2;
    foreach (var angle in angles) {
      angle.ShouldBeLessThanOrEqualTo(target + QUARTER_TURN_EPSILON, "the box swept past Play");
    }
    angles[^1].ShouldBe(target, QUARTER_TURN_EPSILON);
    menubox.ActiveIndex.ShouldBe(0, "the box did not end up on Play");
  }

  [Test]
  public async Task TurningTheOtherWayAlsoStopsOnItsFace() {
    var (menubox, body) = await _openMainMenuFrom(GameMenus.SETTINGS_MENU);
    var start = body.Rotation;

    var angles = await _traceTurn(IInputManager.Action.UILeft, body);

    var target = start - MathUtils.PI2;
    foreach (var angle in angles) {
      angle.ShouldBeGreaterThanOrEqualTo(target - QUARTER_TURN_EPSILON, "the box swept past Credits");
    }
    angles[^1].ShouldBe(target, QUARTER_TURN_EPSILON);
    menubox.ActiveIndex.ShouldBe(2, "the box did not end up on Credits");
  }

  // Holding the direction down turns the box face by face, and every one of those faces
  // has to be a quarter turn - a turn that overshot left the next one measuring from a
  // crooked angle. Compared as a difference, since a turn that completes a circle folds
  // the angle back down rather than letting it climb.
  [Test]
  public async Task EveryFaceInAChainOfTurnsIsAQuarterTurn() {
    var (_, body) = await _openMainMenuFrom(GameMenus.CREDITS_MENU);
    var previous = body.Rotation;

    for (var turn = 0; turn < FACES; turn++) {
      var angles = await _traceTurn(IInputManager.Action.UIRight, body);
      Mathf.AngleDifference(previous, angles[^1]).ShouldBe(MathUtils.PI2, QUARTER_TURN_EPSILON);
      previous = angles[^1];
    }
  }

  // Spinning the box in one direction used to walk its angle up a quarter turn at a time
  // with nothing to bring it back down.
  [Test]
  public async Task SpinningTheBoxRightRoundLeavesItsAngleWhereItStarted() {
    var (_, body) = await _openMainMenuFrom(GameMenus.MAIN_MENU);
    var start = body.Rotation;

    for (var turn = 0; turn < FACES; turn++) {
      await _traceTurn(IInputManager.Action.UIRight, body);
    }

    body.Rotation.ShouldBe(start, QUARTER_TURN_EPSILON);
  }

  // Presses the action for a single tick, the way the input manager reports one, then
  // reads the box's angle on each tick of the turn that follows.
  private async Task<List<float>> _traceTurn(IInputManager.Action action, Node2D body) {
    var tree = TestScene.GetTree();
    var angles = new List<float>();

    await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    _provider.Input.Press(action);
    for (var i = 0; i < TICKS_TO_TRACE; i++) {
      await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
      _provider.Input.ReleaseAll();
      angles.Add(body.Rotation);
    }
    return angles;
  }

  private async Task<(Menubox Menubox, Node2D Body)> _openMainMenuFrom(GameMenus previous) {
    await _open(previous);
    var screen = await _open(GameMenus.MAIN_MENU, from: previous);
    var menubox = screen.FindDescendants<Menubox>().FirstOrDefault();
    menubox.ShouldNotBeNull();
    return (menubox!, menubox!.GetNode<CharacterBody2D>("MenuBox"));
  }

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

  private GameMenu? _currentScreen() =>
    _provider.FindDescendants<GameMenu>().FirstOrDefault(screen => !screen.IsQueuedForDeletion());

  // Wall clock rather than a frame count: the transitions these wait on are driven by
  // tweens, which run faster than the frame counter under a headless run.
  private async Task<bool> _waitUntil(Func<bool> condition) {
    var tree = TestScene.GetTree();
    var deadline = Time.GetTicksMsec() + (ulong)(SETTLE_TIMEOUT_SECONDS * 1000);
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
