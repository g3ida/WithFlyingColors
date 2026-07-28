namespace Wfc.test.instrumented.BrickBreaker;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.BrickBreaker;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;
using Wfc.Utils.Colors;
using Wfc.Utils.Layers;

// A paddle dashing at a wall with the ball in the gap between the two. The ball is the only thing in
// the arena that neither of them can push through, so the gap cannot be closed: the cube has to stop
// a ball's width short of the wall. Squeezing the ball out of the way instead put it inside the cube,
// where it is neither visible nor reachable, and left the cube resting against the wall.
public class BallPaddleCrushTests(Node testScene) : TestClass(testScene) {
  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";
  private const string RIGHT_FACE_COLOR = ColorUtils.YELLOW;
  private const string WALL_GROUP = "wall";
  private const float DASH_SPEED = 20 * Constants.WORLD_TO_SCREEN;
  private static readonly Vector2 WALL_SIZE = new Vector2(200.0f, 2000.0f);
  private static readonly Vector2 WALL_POSITION = new Vector2(600.0f, 0.0f);
  private const float RUN_UP = 100.0f;
  private const int FRAMES = 10;

  private FakeDependenciesProvider _provider = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await _physicsFrame();
  }

  [Cleanup]
  public void Cleanup() => _provider.QueueFree();

  // The ball wears the color of the face that strikes it, so nothing here dies and the dash runs to
  // the end: a cube that is killed by the contact stops being a cube pressing the ball into a wall.
  [Test]
  public async Task ADashIntoAWallLeavesTheBallInTheGapItHoldsOpen() {
    var wall = _addWall();
    var player = await _addPlayer();
    var half = player.GetCollisionHalfExtents();
    var wallFace = wall.GlobalPosition.X - (WALL_SIZE.X * 0.5f);

    player.GlobalPosition = new Vector2(wallFace - RUN_UP - half.X, WALL_POSITION.Y);
    var ball = _addBall(new Vector2(wallFace - RUN_UP * 0.5f, player.GlobalPosition.Y));
    var radius = _radiusOf(ball);

    var deepest = float.MaxValue;
    var closest = float.MaxValue;

    for (var frame = 0; frame < FRAMES; frame++) {
      player.Velocity = Vector2.Right * DASH_SPEED;
      player.MoveAndSlide();
      await _physicsFrame();

      // Once the ball has slid out past a corner it is free to leave, and where the cube goes after
      // that is no longer what this test is about.
      if (Mathf.Abs(ball.GlobalPosition.Y - player.GlobalPosition.Y) > half.Y) {
        break;
      }
      deepest = Mathf.Min(deepest, ball.GlobalPosition.X - player.GlobalPosition.X);
      closest = Mathf.Min(closest, wallFace - (player.GlobalPosition.X + half.X));
    }

    closest.ShouldBeLessThan(RUN_UP, "the cube should have closed on the ball");
    deepest.ShouldBeGreaterThan(half.X, "the ball should never have been inside the cube");
    closest.ShouldBeGreaterThan(radius, "the ball should have held the cube off the wall");

    player.QueueFree();
    ball.QueueFree();
    wall.QueueFree();
    await _physicsFrame();
  }

  private StaticBody2D _addWall() {
    var wall = new StaticBody2D {
      CollisionLayer = PhysicsLayers.Default.Mask,
      CollisionMask = 0,
      Position = WALL_POSITION,
    };
    wall.AddToGroup(WALL_GROUP);
    wall.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = WALL_SIZE } });
    _provider.AddChild(wall);
    return wall;
  }

  // The player scene with its own physics turned off: the dash is driven from the test so the cube
  // closes at a known speed, and none of this depends on the player's state machine.
  private async Task<Wfc.Entities.World.Player.Player> _addPlayer() {
    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    _provider.AddChild(player);
    await _physicsFrame();
    player.SetPhysicsProcess(false);
    return player;
  }

  // Placed and aimed before any physics frame runs: a ball spawns with a random direction, and the
  // one thing this test needs of it is that it is sitting in the gap when the cube arrives.
  private BouncingBall _addBall(Vector2 position) {
    var ball = SceneHelpers.InstantiateNode<BouncingBall>();
    _provider.AddChild(ball);
    ball.GlobalPosition = position;
    ball.SetColor(RIGHT_FACE_COLOR);
    ball.SetBallVelocity(Vector2.Right);
    return ball;
  }

  private static float _radiusOf(BouncingBall ball) =>
    ((CircleShape2D)ball.GetNode<CollisionShape2D>("CollisionShape2D").Shape).Radius * ball.Scale.X;

  private async Task _physicsFrame() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
  }
}
