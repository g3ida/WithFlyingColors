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

// A cube that jumps off a moving floor leaves with the floor's own velocity under it. Keeping it
// is the whole of the effect: the run limit, the run damping and the jump cut own the motion the
// cube makes itself, and a push it was given is none of theirs - each of them read the two as one
// and spent the push on a rule it was never subject to.
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

  // The floor's own rise, in the units the cube's velocity is read in.
  private const float A_LIFT = PLATFORM_SPEED * Constants.WORLD_TO_SCREEN;

  // What a jump read a tick out of step would shift a reading by. The lift is worth many times
  // this, and half a lift - which is what the cut used to leave of it - many times it again, so
  // nothing here turns on the slack.
  private const float A_TICK_OUT_OF_STEP = 60.0f;

  // Under what the cut takes off a jump, so a reading short of the full arc is a cut and not slack.
  private const float A_CUT_TAKES_AT_LEAST = 400.0f;

  private const float A_JUMP_IS_UNDER_WAY = 100.0f;

  // Further than the cube could come down on the platform it left, and less than the drop to the
  // floor.
  private const float A_FALL = 400.0f;

  // What is left of a run the damping has had a while at, against a push that would still be whole.
  private const float A_CRAWL = 10.0f;

  private const int A_RUN_UP_TO_SPEED = 8;
  private const int A_CUT = 2;
  private const int A_STOP = 30;
  private const int AN_ARC = 20;

  // What the platform is worth over the stretch of arc that is measured, less the slack a jump
  // leaves either side of it.
  private const float A_PUSH = 150.0f;
  private const float A_TICK_OF_DRAG = 20.0f;
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
    await _standOnPlatform(PlatformSlide.SlideAxis.Horizontal, RUN, stopped: true);
    var onStillGround = await _jumpRight();

    await _standOnPlatform(PlatformSlide.SlideAxis.Horizontal, RUN, stopped: false);
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
    await _standOnPlatform(PlatformSlide.SlideAxis.Horizontal, RUN, stopped: true);
    var onStillGround = await _jumpRight();

    await _standOnPlatform(PlatformSlide.SlideAxis.Horizontal, -RUN, stopped: false);
    var againstThePlatform = await _jumpRight();

    againstThePlatform.ShouldBeGreaterThan(
      onStillGround - A_TICK_OF_DRAG,
      "jumping against the platform dragged the cube back"
    );
  }

  [Test]
  public async Task SteeringAgainstThePushGivesTheRunBack() {
    await _standOnPlatform(PlatformSlide.SlideAxis.Horizontal, RUN, stopped: false);
    await _jumpRight();
    _player.Velocity.X.ShouldBeGreaterThan(_player.SpeedLimit, "the cube left the platform without its push");

    _services.Input.Release(IInputManager.Action.MoveRight);
    _services.Input.Press(IInputManager.Action.MoveLeft);
    await PhysicsFrames.Advance(TestScene, 2);

    _player.Velocity.X.ShouldBeLessThanOrEqualTo(0.0f, "the cube could not steer out of the push");
  }

  // Landing hands the cube back to the floor it lands on: the push ends there, and the speed the
  // cube came down with is ordinary run speed the damping bleeds off. The platform is parked as
  // the cube leaves it so that what it comes down on is the still floor - a cube that lands back
  // on the platform and walks off it again is owed a second push, and proves nothing about the
  // first.
  [Test]
  public async Task ThePushEndsWhenTheCubeLands() {
    await _standOnPlatform(PlatformSlide.SlideAxis.Horizontal, RUN, stopped: false);
    await _jumpRight();
    _services.Input.ReleaseAll();
    _platform.StopSlider(true);

    (await PhysicsFrames.WaitFor(TestScene, _player.IsOnFloor, TIMEOUT)).ShouldBeTrue("the cube never came down");
    _player.GlobalPosition.Y.ShouldBeGreaterThan(
      START_Y + A_FALL,
      "the cube came down on the platform rather than on the still floor below it"
    );
    _player.CarriedVelocity.ShouldBe(Vector2.Zero, "the cube landed still carrying the platform's push");

    await PhysicsFrames.Advance(TestScene, A_STOP);
    Mathf.Abs(_player.Velocity.X).ShouldBeLessThan(A_CRAWL, "the speed the cube landed with never bled off");
  }

  [Test]
  public async Task ATappedJumpKeepsTheLiftFromARisingPlatform() {
    await _standOnPlatform(PlatformSlide.SlideAxis.Vertical, -RUN, stopped: true);
    var offStillGround = await _riseAfterTheCut(tap: true);

    await _standOnPlatform(PlatformSlide.SlideAxis.Vertical, -RUN, stopped: false);
    var offARisingPlatform = await _riseAfterTheCut(tap: true);

    (offStillGround - offARisingPlatform).ShouldBe(
      A_LIFT,
      A_TICK_OUT_OF_STEP,
      "the cut took the platform's lift along with the cube's own jump"
    );
  }

  [Test]
  public async Task ATappedJumpOffARisingPlatformIsStillCut() {
    await _standOnPlatform(PlatformSlide.SlideAxis.Vertical, -RUN, stopped: false);
    var held = await _riseAfterTheCut(tap: false);

    await _standOnPlatform(PlatformSlide.SlideAxis.Vertical, -RUN, stopped: false);
    var tapped = await _riseAfterTheCut(tap: true);

    tapped.ShouldBeGreaterThan(held + A_CUT_TAKES_AT_LEAST, "letting the jump go early stopped cutting it");
  }

  // Releasing after the jump has been applied rather than on the tick it goes on: the lift is
  // added by the same move that carries the cube off the floor, so a release any earlier is over
  // before there is a lift to take.
  private async Task<float> _riseAfterTheCut(bool tap) {
    _services.Input.Press(IInputManager.Action.Jump);
    (await PhysicsFrames.WaitFor(TestScene, () => _player.Velocity.Y < -A_JUMP_IS_UNDER_WAY, TIMEOUT))
      .ShouldBeTrue("the cube never jumped");
    if (tap) {
      _services.Input.Release(IInputManager.Action.Jump);
    }
    await PhysicsFrames.Advance(TestScene, A_CUT);
    return _player.Velocity.Y;
  }

  // Walks the way the platform runs, jumps, and answers with the ground covered over a fixed
  // stretch of the arc - measured from the tick the cube is first off the floor, so the ride
  // itself is not counted as jumping.
  private async Task<float> _jumpRight() {
    _services.Input.Press(IInputManager.Action.MoveRight);
    await PhysicsFrames.Advance(TestScene, A_RUN_UP_TO_SPEED);
    // Held for the whole of the arc that is measured, so what is compared is two full jumps: a
    // jump let go of inside the cut window is a shorter jump, and one of these runs measuring a
    // cut arc against an uncut one would read as a push that was never there.
    _services.Input.Press(IInputManager.Action.Jump);

    (await PhysicsFrames.WaitFor(TestScene, () => !_player.IsOnFloor(), TIMEOUT))
      .ShouldBeTrue("the cube never left the platform");
    var from = _player.GlobalPosition.X;
    await PhysicsFrames.Advance(TestScene, AN_ARC);
    _services.Input.Release(IInputManager.Action.Jump);
    return _player.GlobalPosition.X - from;
  }

  // A test that measures two jumps stands the whole scene up twice, and the cube of the first one
  // is still falling through the second.
  private async Task _standOnPlatform(PlatformSlide.SlideAxis axis, float distance, bool stopped) {
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
    _platform.Axis = axis;
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
