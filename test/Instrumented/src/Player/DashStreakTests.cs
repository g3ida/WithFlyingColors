namespace Wfc.test.instrumented.Player;

using System.Collections.Generic;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Input;
using Wfc.Entities.World.Player;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;

// The speed lines are drawn from where the cube passed to where the cube is. Laid out over the
// ground a dash was expected to cover instead, they carry on through whatever stopped it.
public class DashStreakTests(Node testScene) : TestClass(testScene) {
  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";
  private const float FLOOR_HALF_HEIGHT = 50f;
  private const float FLOOR_HALF_WIDTH = 1200f;
  private const float FLOOR_CENTER_Y = 400f;

  // Near enough that a dash cannot possibly spend itself before reaching it.
  private const float WALL_GAP = 120f;
  private const float WALL_HALF_WIDTH = 40f;
  private const int MID_DASH = 8;
  private const float A_HAIR = 2.0f;

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
  public async Task NoSpeedLineOutrunsTheCubeItPoursOffOf() {
    var player = await _addPlayerOnGround();
    _addWall(player.GlobalPosition + new Vector2(WALL_GAP, 0f));

    _provider.Input.Press(IInputManager.Action.MoveRight);
    _provider.Input.Press(IInputManager.Action.Dash);
    await _physicsFrame();
    _provider.Input.Release(IInputManager.Action.Dash);
    for (var frame = 0; frame < MID_DASH; frame++) {
      await _physicsFrame();
    }

    // Only the lines the player can see: one whose tail was pinned past the wall is one the cube
    // never reached, and it stays invisible where it was born.
    var drawn = _streaks().FindAll(streak => streak.Modulate.A > 0f);
    drawn.ShouldNotBeEmpty("the dash never drew a speed line to check");
    foreach (var streak in drawn) {
      _headOf(streak).X.ShouldBeLessThanOrEqualTo(
        player.GlobalPosition.X + A_HAIR,
        "a speed line ran on past the cube after the wall stopped it"
      );
    }
  }

  // The head of a line, in the world: its origin is its tail, and it reaches forward from there by
  // whatever it has been stretched to.
  private static Vector2 _headOf(DashStreak streak) =>
    streak.GlobalPosition + (Vector2.Right.Rotated(streak.GlobalRotation) * streak.Texture.GetSize().X * streak.Scale.X);

  private List<DashStreak> _streaks() {
    var found = new List<DashStreak>();
    foreach (var child in _provider.GetChildren()) {
      if (child is DashStreak streak) {
        found.Add(streak);
      }
    }
    return found;
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
