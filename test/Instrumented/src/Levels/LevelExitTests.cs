namespace Wfc.test.instrumented.Levels;

using Chickensoft.Sync.Primitives;
using System;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Event;
using Wfc.Entities.World.Exit;
using Wfc.Screens.Levels;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// The end of a level is a walk out of frame. What has to hold is that crossing the exit takes
// the player's input away and pins the camera where it stood, that the clear waits until they
// are past the edge rather than firing on contact, and that a death on the way puts the whole
// thing back - the level is not over because the player once touched its last few metres.
//
// The walk itself is driven in real time, so these tests put the player where the walk would
// have taken them instead of waiting it out: what is under test is what the exit decides, not
// how long the cube takes to cross a screen.
public class LevelExitTests(Node testScene) : TestClass(testScene) {
  private AutoChannel.Binding? _clearedBinding;

  // Clear of the edge by enough that no drag margin or rounding can leave the player inside
  // the view, and well short of the wall that closes the level off.
  private const float PAST_THE_EDGE = 300.0f;
  // Further beyond the exit than the arch it is drawn under is wide, and short of the ground any
  // level leaves past its exit: where a long last jump puts the player down.
  private const float PAST_THE_ARCH = 400.0f;

  private FakeDependenciesProvider _provider = default!;
  private GameLevel? _level;
  private bool _isCleared;

  [Setup]
  public async Task Setup() {
    _isCleared = false;
    _clearedBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.LevelCleared _) => _onLevelCleared());
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await _frames(1);
  }

  [Cleanup]
  public void Cleanup() {
    _clearedBinding?.Dispose();
    _clearedBinding = null;
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

    player.GlobalPosition = new Vector2(_edgeOfView() + PAST_THE_EDGE, player.GlobalPosition.Y);

    (await _waitUntil(() => _isCleared))
      .ShouldBeTrue("the level never cleared once the player was past the edge");

    // The pin is behind the camera that was chasing the player into it, so what is left to
    // move is the pin settling back into the frame it took. Travel the other way is the
    // camera going after the player, whatever the exit had told it.
    var clearedOn = _level!.CameraNode.GetScreenCenterPosition().X;
    await _frames(20);
    var heldAt = _level!.CameraNode.GetScreenCenterPosition().X;
    heldAt.ShouldBeLessThan(clearedOn + 1.0f,
      "the camera set off after the player instead of holding the frame they left");
    heldAt.ShouldBeLessThan(player.GlobalPosition.X - _halfView(),
      "the camera followed the player out of the level");
  }

  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ARespawnCallsTheWalkOffAndArmsTheExitAgain() {
    var player = (await _load(LevelId.FourColors)).PlayerNode;
    player.GlobalPosition = _exit().GlobalPosition;

    (await _waitUntil(() => player.HandleInputIsDisabled))
      .ShouldBeTrue("crossing the exit never took the player's input away");

    GameEvents.Instance.OnCheckpointLoaded();
    (await _waitUntil(() => !player.HandleInputIsDisabled))
      .ShouldBeTrue("the respawn left the player without their input");

    // Nothing may still be shoving the cube the respawn has just put back: the walk is the only
    // thing that pushes, so a player who has not been asked to move and does not move is a walk
    // that went back with them.
    var putBackAt = player.GlobalPosition.X;
    await _frames(30);
    player.GlobalPosition.X.ShouldBe(putBackAt, 1.0f,
      "the walk-out was still pushing the player after the respawn");
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

  // The exit is a line and not a doorway, so coming down on the far side of it counts as having
  // crossed it. The last drop in a level can carry the player clear over a strip no wider than
  // the arch, and what they land on is run-off: they walk into the end wall, nothing is left to
  // trigger, and there is no way back up to try the jump again.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task LandingPastTheExitCountsAsCrossingIt() {
    foreach (var levelId in LevelDispatcher.LEVELS.Select(info => info.Id)) {
      _isCleared = false;
      var player = (await _load(levelId)).PlayerNode;
      var exitPosition = _exit().GlobalPosition;
      player.GlobalPosition = new Vector2(exitPosition.X + PAST_THE_ARCH, exitPosition.Y);

      (await _waitUntil(() => player.HandleInputIsDisabled))
        .ShouldBeTrue($"{levelId}'s exit ignored a player who came down past it");
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

  private float _edgeOfView() => _level!.CameraNode.GetScreenCenterPosition().X + _halfView();

  private float _halfView() {
    var cameraNode = _level!.CameraNode;
    return cameraNode.GetViewportRect().Size.X * 0.5f / cameraNode.Zoom.X;
  }

  private void _onLevelCleared() => _isCleared = true;

  private async Task _frames(int count) => await PhysicsFrames.Advance(TestScene, count);

  private async Task<bool> _waitUntil(Func<bool> until) =>
    await PhysicsFrames.WaitFor(TestScene, until, 5.0);
}
