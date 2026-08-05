namespace Wfc.test.instrumented.Levels;

using System;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Exit;
using Wfc.Screens.Levels;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;
using EventHandler = Wfc.Core.Event.EventHandler;

// The end of a level is a walk out of frame. What has to hold is that crossing the exit takes
// the player's input away and pins the camera where it stood, that the clear waits until they
// are past the edge rather than firing on contact, and that a death on the way puts the whole
// thing back - the level is not over because the player once touched its last few metres.
//
// The walk itself is driven in real time, so these tests put the player where the walk would
// have taken them instead of waiting it out: what is under test is what the exit decides, not
// how long the cube takes to cross a screen.
public class LevelExitTests(Node testScene) : TestClass(testScene) {
  // Clear of the edge by enough that no drag margin or rounding can leave the player inside
  // the view, and well short of the wall that closes the level off.
  private const float PAST_THE_EDGE = 300.0f;

  private FakeDependenciesProvider _provider = default!;
  private GameLevel? _level;
  private bool _isCleared;

  [Setup]
  public async Task Setup() {
    _isCleared = false;
    EventHandler.Instance.Events.LevelCleared += _onLevelCleared;
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await _frames(1);
  }

  [Cleanup]
  public void Cleanup() {
    EventHandler.Instance.Events.LevelCleared -= _onLevelCleared;
    _provider.QueueFree();
  }

  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task CrossingTheExitTakesTheInputAndHoldsTheCamera() {
    var player = (await _load(LevelId.FourColors)).PlayerNode;
    player.GlobalPosition = _exit().GlobalPosition;

    (await _waitUntil(() => player.HandleInputIsDisabled))
      .ShouldBeTrue("crossing the exit never took the player's input away");
    _isCleared.ShouldBeFalse("the level cleared on contact instead of waiting for the walk");

    var heldAt = await _whereTheCameraComesToRest();
    player.GlobalPosition = new Vector2(_edgeOfView() + PAST_THE_EDGE, player.GlobalPosition.Y);

    (await _waitUntil(() => _isCleared))
      .ShouldBeTrue("the level never cleared once the player was past the edge");

    await _frames(20);
    _level!.CameraNode.GetScreenCenterPosition().X.ShouldBe(heldAt, 5.0f,
      "the camera followed the player out of the level");
  }

  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ARespawnCallsTheWalkOffAndArmsTheExitAgain() {
    var player = (await _load(LevelId.FourColors)).PlayerNode;
    player.GlobalPosition = _exit().GlobalPosition;

    (await _waitUntil(() => player.HandleInputIsDisabled))
      .ShouldBeTrue("crossing the exit never took the player's input away");

    EventHandler.Instance.EmitCheckpointLoaded();
    (await _waitUntil(() => !player.HandleInputIsDisabled))
      .ShouldBeTrue("the respawn left the player without their input");

    player.GlobalPosition = new Vector2(_edgeOfView() + PAST_THE_EDGE, player.GlobalPosition.Y);
    await _frames(30);
    _isCleared.ShouldBeFalse("the walk-out kept running after the respawn and cleared the level");

    player.GlobalPosition = _exit().GlobalPosition;
    (await _waitUntil(() => player.HandleInputIsDisabled))
      .ShouldBeTrue("the exit could not be crossed again after the respawn");
  }

  // Every level ends the same way, and each one places its own exit: an exit sitting where the
  // player cannot stand, or one whose ground runs out before the edge of the view, ends the
  // level nowhere. The walk is skipped the same way it is above - what is being asked of each
  // level is that its exit answers the player and that the clear arrives.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task EveryOfferedLevelCanBeWalkedOutOf() {
    foreach (var levelId in LevelDispatcher.LEVELS.Select(info => info.Id)) {
      _isCleared = false;
      var player = (await _load(levelId)).PlayerNode;
      player.GlobalPosition = _exit().GlobalPosition;

      (await _waitUntil(() => player.HandleInputIsDisabled))
        .ShouldBeTrue($"{levelId}'s exit never answered the player standing in it");

      player.GlobalPosition = new Vector2(_edgeOfView() + PAST_THE_EDGE, player.GlobalPosition.Y);
      (await _waitUntil(() => _isCleared)).ShouldBeTrue($"{levelId} never cleared");
    }
  }

  private async Task<GameLevel> _load(LevelId levelId) {
    if (_level != null && GodotObject.IsInstanceValid(_level)) {
      _provider.RemoveChild(_level);
      _level.QueueFree();
      await _frames(1);
    }
    _level = LevelDispatcher.InstantiateLevel(levelId)!;
    _provider.AddChild(_level);
    await _frames(2);
    return _level;
  }

  private LevelExit _exit() =>
    _level!.FindDescendants<LevelExit>().First();

  // The camera is travelling to wherever the player was put down when the exit pins it, so it
  // eases back to the pin and stops. That resting place, not the reading taken mid-travel, is
  // the framing the walk out has to leave alone.
  private async Task<float> _whereTheCameraComesToRest() {
    var previousX = float.MaxValue;
    var isStill = false;
    var settled = await PhysicsFrames.WaitFor(
      TestScene,
      () => {
        var currentX = _level!.CameraNode.GetScreenCenterPosition().X;
        isStill = Math.Abs(currentX - previousX) < 0.5f;
        previousX = currentX;
        return isStill;
      },
      5.0
    );
    settled.ShouldBeTrue("the camera never came to rest after the exit pinned it");
    return previousX;
  }

  private float _edgeOfView() {
    var cameraNode = _level!.CameraNode;
    return cameraNode.GetScreenCenterPosition().X
      + (cameraNode.GetViewportRect().Size.X * 0.5f / cameraNode.Zoom.X);
  }

  private void _onLevelCleared() => _isCleared = true;

  private async Task _frames(int count) => await PhysicsFrames.Advance(TestScene, count);

  private async Task<bool> _waitUntil(Func<bool> until) =>
    await PhysicsFrames.WaitFor(TestScene, until, 5.0);
}
