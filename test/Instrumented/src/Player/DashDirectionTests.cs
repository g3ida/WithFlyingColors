namespace Wfc.test.instrumented.Player;

using Chickensoft.Sync.Primitives;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using EventHandler = Wfc.Core.Event.EventHandler;

// Which way a dash goes is read off what is held when it commits: left and right as
// pressed, the run's momentum standing in when nothing is, and holding only down
// meaning straight down - momentum must not bend an aimed slam sideways.
public class DashDirectionTests(Node testScene) : TestClass(testScene) {
  private AutoChannel.Binding? _dashBinding;

  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";
  private const float FLOOR_HALF_HEIGHT = 50f;
  private const float FLOOR_HALF_WIDTH = 1200f;
  private const float FLOOR_CENTER_Y = 400f;

  // Long enough for the run to reach full speed.
  private const int RUN_UP_FRAMES = 20;
  // The permissiveness window is a handful of frames; this outlasts it comfortably.
  private const int COMMIT_FRAMES = 8;

  private FakeDependenciesProvider _provider = default!;
  private Vector2? _dashDirection;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    _dashDirection = null;
    _dashBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.PlayerDashed m) => _onPlayerDash(m.Direction));
    await _physicsFrame();
  }

  [Cleanup]
  public void Cleanup() {
    _dashBinding?.Dispose();
    _dashBinding = null;
    _provider.Input.ReleaseAll();
    _provider.QueueFree();
  }

  [Test]
  public async Task HoldingOnlyDownWhileMovingDashesStraightDown() {
    await _addPlayerOnGround();
    await _runRight();

    _provider.Input.Press(IInputManager.Action.Down);
    await _dash();

    _dashDirection.ShouldNotBeNull("the dash never committed");
    _dashDirection.Value.X.ShouldBe(0f, "leftover run speed bent an aimed slam into a diagonal");
    _dashDirection.Value.Y.ShouldBe(1f);
  }

  [Test]
  public async Task HoldingNothingWhileMovingDashesAlongTheRun() {
    await _addPlayerOnGround();
    await _runRight();

    await _dash();

    _dashDirection.ShouldNotBeNull("the dash never committed");
    _dashDirection.Value.X.ShouldBe(1f, "a neutral dash lost the run's direction");
    _dashDirection.Value.Y.ShouldBe(0f);
  }

  [Test]
  public async Task HoldingDownAndRightDashesDiagonally() {
    await _addPlayerOnGround();

    _provider.Input.Press(IInputManager.Action.MoveRight);
    _provider.Input.Press(IInputManager.Action.Down);
    await _dash();

    _dashDirection.ShouldNotBeNull("the dash never committed");
    _dashDirection.Value.X.ShouldBe(1f, "holding a side with down should still aim the diagonal");
    _dashDirection.Value.Y.ShouldBe(1f);
  }

  private void _onPlayerDash(Vector2 direction) => _dashDirection = direction;

  // Runs right long enough to be at full speed, then lets go so only momentum is
  // left pointing that way.
  private async Task _runRight() {
    _provider.Input.Press(IInputManager.Action.MoveRight);
    for (var frame = 0; frame < RUN_UP_FRAMES; frame++) {
      await _physicsFrame();
    }
    _provider.Input.Release(IInputManager.Action.MoveRight);
  }

  // Taps dash and waits for the commit. Whatever direction the test holds stays
  // held: the dash does not read one until its permissiveness window is up.
  private async Task _dash() {
    _provider.Input.Press(IInputManager.Action.Dash);
    await _physicsFrame();
    _provider.Input.Release(IInputManager.Action.Dash);

    for (var frame = 0; frame < COMMIT_FRAMES; frame++) {
      await _physicsFrame();
      if (_dashDirection != null) {
        return;
      }
    }
  }

  private async Task<Wfc.Entities.World.Player.Player> _addPlayerOnGround() {
    var floor = new StaticBody2D();
    floor.AddChild(new CollisionShape2D {
      Shape = new RectangleShape2D { Size = new Vector2(FLOOR_HALF_WIDTH * 2f, FLOOR_HALF_HEIGHT * 2f) }
    });
    _provider.AddChild(floor);
    floor.GlobalPosition = new Vector2(0f, FLOOR_CENTER_Y);

    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    player.CollisionMask = 13;
    player.GlobalPosition = new Vector2(0f, FLOOR_CENTER_Y - FLOOR_HALF_HEIGHT - 60f);
    _provider.AddChild(player);

    for (var frame = 0; frame < 30; frame++) {
      await _physicsFrame();
    }
    player.IsOnFloor().ShouldBeTrue("the cube never landed on the test floor");
    return player;
  }

  private Task _physicsFrame() => PhysicsFrames.Frame(TestScene);
}
