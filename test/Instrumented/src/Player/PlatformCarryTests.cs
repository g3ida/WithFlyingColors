namespace Wfc.test.instrumented.Player;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Input;
using Wfc.Entities.World.Platforms;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;
using PlayerNode = Wfc.Entities.World.Player.Player;

// A cube that jumps off a moving floor leaves with the floor's own speed under it. Keeping it is
// the whole of the effect: the run limit and the run damping own the speed the cube makes itself,
// and a push it was given is neither - a clamp that reads the two as one speed erases the push on
// the tick after the jump, which is the tick the player is still holding the direction on.
public class PlatformCarryTests(Node testScene) : TestClass(testScene) {
  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";

  // Default, platform and fall zone, minus the layers this test has nothing on.
  private const uint PLAYER_MASK = 13;

  // Well over the cube's own run, so a jump that keeps the push cannot be read as one that
  // happened to be fast.
  private const float PLATFORM_SPEED = 6.0f;
  private const float RUN = 900.0f;
  private const float START_X = 700.0f;
  private const float START_Y = 400.0f;
  private const float FLOOR_Y = 1400.0f;
  private const float FLOOR_HALF_WIDTH = 6000.0f;
  private const float FLOOR_HALF_HEIGHT = 50.0f;

  // Long enough that the cube has landed on the platform before it sets off with it.
  private const float WAIT = 0.8f;
  private const float UNDER_WAY = 40.0f;

  private const int A_RUN_UP_TO_SPEED = 8;
  private const int AN_ARC = 20;
  private const int A_STOP = 30;

  // What the platform is worth over the stretch of arc that is measured, less the slack a jump
  // leaves either side of it.
  private const float A_PUSH = 150.0f;
  private const float A_TICK_OF_DRAG = 20.0f;

  // What is left of a run the damping has had a while at, against a push that would still be
  // whole.
  private const float A_CRAWL = 10.0f;
  private const double TIMEOUT = 4.0;

  private FakeDependenciesProvider _services = default!;
  private SlidingPlatform _platform = default!;
  private PlayerNode _player = default!;

  [Cleanup]
  public void Cleanup() {
    _services.Input.ReleaseAll();
    _services.QueueFree();
  }

  [Test]
  public async Task AJumpWithThePlatformCoversMoreGround() {
    await _standOnPlatform(RUN, stopped: true);
    var onStillGround = await _jumpRight();

    await _standOnPlatform(RUN, stopped: false);
    var withThePlatform = await _jumpRight();

    withThePlatform.ShouldBeGreaterThan(
      onStillGround + A_PUSH,
      "jumping the way the platform runs covered no more ground than jumping off still ground"
    );
  }

  // The other half of the same rule, and the half that existing levels are built on: a push is
  // taken only when it helps, the way an upward one is and a downward one is not.
  [Test]
  public async Task AJumpAgainstThePlatformIsTheJumpItAlwaysWas() {
    await _standOnPlatform(RUN, stopped: true);
    var onStillGround = await _jumpRight();

    await _standOnPlatform(-RUN, stopped: false);
    var againstThePlatform = await _jumpRight();

    againstThePlatform.ShouldBeGreaterThan(
      onStillGround - A_TICK_OF_DRAG,
      "jumping against the platform dragged the cube back"
    );
  }

  [Test]
  public async Task SteeringAgainstThePushGivesTheRunBack() {
    await _standOnPlatform(RUN, stopped: false);
    await _jumpRight();
    _player.Velocity.X.ShouldBeGreaterThan(_player.SpeedLimit, "the cube left the platform without its push");

    _services.Input.Release(IInputManager.Action.MoveRight);
    _services.Input.Press(IInputManager.Action.MoveLeft);
    await PhysicsFrames.Advance(TestScene, 2);

    _player.Velocity.X.ShouldBeLessThanOrEqualTo(0.0f, "the cube could not steer out of the push");
  }

  [Test]
  public async Task ThePushEndsWhenTheCubeLands() {
    await _standOnPlatform(RUN, stopped: false);
    await _jumpRight();
    _services.Input.ReleaseAll();

    (await PhysicsFrames.WaitFor(TestScene, _player.IsOnFloor, TIMEOUT)).ShouldBeTrue("the cube never came down");
    await PhysicsFrames.Advance(TestScene, A_STOP);

    Mathf.Abs(_player.Velocity.X).ShouldBeLessThan(A_CRAWL, "the cube kept the platform's push after landing");
  }

  // Walks the way the platform runs, jumps, and answers with the ground covered over a fixed
  // stretch of the arc - measured from the tick the cube is first off the floor, so the ride
  // itself is not counted as jumping.
  private async Task<float> _jumpRight() {
    _services.Input.Press(IInputManager.Action.MoveRight);
    await PhysicsFrames.Advance(TestScene, A_RUN_UP_TO_SPEED);
    _services.Input.Press(IInputManager.Action.Jump);
    await PhysicsFrames.Frame(TestScene);
    _services.Input.Release(IInputManager.Action.Jump);

    (await PhysicsFrames.WaitFor(TestScene, () => !_player.IsOnFloor(), TIMEOUT))
      .ShouldBeTrue("the cube never left the platform");
    var from = _player.GlobalPosition.X;
    await PhysicsFrames.Advance(TestScene, AN_ARC);
    return _player.GlobalPosition.X - from;
  }

  // A test that measures two jumps stands the whole scene up twice, and the cube of the first one
  // is still falling through the second.
  private async Task _standOnPlatform(float distance, bool stopped) {
    if (_services is not null && GodotObject.IsInstanceValid(_services)) {
      _services.Input.ReleaseAll();
      _services.QueueFree();
      await PhysicsFrames.Frame(TestScene);
    }

    _services = new FakeDependenciesProvider();
    TestScene.AddChild(_services);
    var level = new FakeGameLevelProvider();
    _services.AddChild(level);
    await PhysicsFrames.Frame(TestScene);

    var floor = new StaticBody2D();
    floor.AddChild(new CollisionShape2D {
      Shape = new RectangleShape2D { Size = new Vector2(FLOOR_HALF_WIDTH * 2.0f, FLOOR_HALF_HEIGHT * 2.0f) }
    });
    level.AddChild(floor);
    floor.GlobalPosition = new Vector2(START_X, FLOOR_Y);

    _platform = SceneHelpers.InstantiateNode<SlidingPlatform>();
    _platform.Position = new Vector2(START_X, START_Y);
    _platform.Speed = PLATFORM_SPEED;
    _platform.Distance = distance;
    _platform.WaitTime = WAIT;
    _platform.StartsStopped = stopped;
    // Whichever face lands, lands: a platform the cube may not land on kills it instead.
    _platform.Group = FlatPlatform.NEUTRAL;
    level.AddChild(_platform);
    var start = _platform.GlobalPosition;

    _player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<PlayerNode>();
    _player.CollisionMask = PLAYER_MASK;
    _player.Position = new Vector2(START_X, START_Y - 100.0f);
    level.AddChild(_player);

    (await PhysicsFrames.WaitFor(TestScene, _player.IsOnFloor, TIMEOUT))
      .ShouldBeTrue("the cube never landed on the platform");
    if (stopped) {
      return;
    }
    (await PhysicsFrames.WaitFor(TestScene, () => (_platform.GlobalPosition - start).Length() > UNDER_WAY, TIMEOUT))
      .ShouldBeTrue("the platform never set off");
  }
}
