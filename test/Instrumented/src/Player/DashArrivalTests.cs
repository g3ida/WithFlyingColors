namespace Wfc.test.instrumented.Player;

using System;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Input;
using Wfc.Entities.World.Player;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;

// How a dash ends is not the same event as how it started. One that ran into something has had
// its speed taken off it and says so; one that reached open air hands what is left of it back to
// the run.
public class DashArrivalTests(Node testScene) : TestClass(testScene) {
  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";
  private const float FLOOR_HALF_HEIGHT = 50f;
  private const float FLOOR_HALF_WIDTH = 1200f;
  private const float FLOOR_CENTER_Y = 400f;

  // Near enough that a dash cannot possibly spend itself before reaching it.
  private const float WALL_GAP = 120f;
  private const float WALL_HALF_WIDTH = 40f;

  // A dash holds the state for its full duration, and the hitstop it opens with stretches that
  // over more frames than the duration alone would take.
  private const int WHOLE_DASH = 40;

  private FakeDependenciesProvider _provider = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await _physicsFrame();
  }

  [Cleanup]
  public void Cleanup() {
    _provider.Input.ReleaseAll();
    _provider.QueueFree();
  }

  [Test]
  public async Task ADashStoppedByAWallBurstsWhereItWasStopped() {
    var player = await _addPlayerOnGround();
    _addWall(player.GlobalPosition + new Vector2(WALL_GAP, 0f));

    var burst = await _dashRight(player, until: () => player.DashImpactParticlesNode.Emitting);

    burst.ShouldBeTrue("the wall took the whole dash and nothing marked the arrival");
  }

  [Test]
  public async Task ADashThatReachesOpenAirCoastsOutOfIt() {
    var player = await _addPlayerOnGround();

    await _dashRight(player, until: () => !player.IsDashing());

    player.IsDashing().ShouldBeFalse("the dash never ended");
    player.DashImpactParticlesNode.Emitting.ShouldBeFalse("open air was reported as an impact");
    player.Velocity.X.ShouldBeGreaterThan(
      Wfc.Entities.World.Player.Player.SPEED,
      "the dash arrived at a dead stop instead of coasting out of itself"
    );
  }

  // Dashes right and waits for `until`, or for the dash to have had every frame it could use. The
  // direction is held throughout: the dash does not read one until its permissiveness window is
  // up, and letting go any earlier leaves it with nothing to commit to and nowhere to arrive.
  private async Task<bool> _dashRight(Wfc.Entities.World.Player.Player player, Func<bool> until) {
    _provider.Input.Press(IInputManager.Action.MoveRight);
    _provider.Input.Press(IInputManager.Action.Dash);
    await _physicsFrame();
    _provider.Input.Release(IInputManager.Action.Dash);

    for (var frame = 0; frame < WHOLE_DASH; frame++) {
      await _physicsFrame();
      if (until()) {
        return true;
      }
    }
    return false;
  }

  private void _addWall(Vector2 position) {
    var wall = new StaticBody2D();
    wall.AddChild(new CollisionShape2D {
      Shape = new RectangleShape2D { Size = new Vector2(WALL_HALF_WIDTH * 2f, FLOOR_HALF_HEIGHT * 4f) }
    });
    _provider.AddChild(wall);
    wall.GlobalPosition = position;
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
