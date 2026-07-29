namespace Wfc.test.instrumented.BrickBreaker;

using System.Collections.Generic;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.BrickBreaker;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Core.Input;
using Wfc.Utils;
using Wfc.Utils.Colors;
using EventHandler = Wfc.Core.Event.EventHandler;

// A ball of the wrong color is fatal, and it is the ball's own area that says so. That makes the death
// sensitive to anything which changes what the ball collides with or where it comes to rest, and it
// has been broken twice by exactly that.
//
// Subscribed through the typed event rather than SignalsCounter, whose callable takes no arguments and
// so silently counts nothing for a signal that carries any.
public class BallHitsPlayerTests(Node testScene) : TestClass(testScene) {
  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";
  private const string TOP_FACE_COLOR = ColorUtils.BLUE;
  private const string A_DIFFERENT_COLOR = ColorUtils.PINK;
  private const string RIGHT_FACE_COLOR = ColorUtils.YELLOW;
  private const string LEFT_FACE_COLOR = ColorUtils.PINK;
  private const float REACHED_MID_DASH = 120.0f;
  private const int DASH_FRAMES = 14;
  private const float ARENA_CORNER_SCALE = 3.5f;
  private const float SCALED_UP = 1.3f;
  private const float NEAR_THE_EDGE = 8.0f;
  private const int RESTING_FRAMES = 6;

  private FakeDependenciesProvider _provider = default!;
  private int _deaths;

  [Setup]
  public async Task Setup() {
    _deaths = 0;
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    EventHandler.Instance.Events.PlayerDying += _onPlayerDying;
    await _physicsFrame();
  }

  [Cleanup]
  public void Cleanup() {
    EventHandler.Instance.Events.PlayerDying -= _onPlayerDying;
    _provider.QueueFree();
  }

  [Test]
  public async Task AWrongColorBallKillsThePlayer() {
    await _dropBallOnTopFace(A_DIFFERENT_COLOR);

    _deaths.ShouldBe(1, "the ball reported the same contact more than once");
  }

  [Test]
  public async Task AMatchingBallDoesNotKillThePlayer() {
    await _dropBallOnTopFace(TOP_FACE_COLOR);

    _deaths.ShouldBe(0);
  }

  // The arena widens the cube's corners so an edge catch is forgiven in either of the two colors the
  // corner joins. It asks for that with the player already standing in it, so the widening has to be
  // in force without a state change to carry it in. Scaled up, as it was when this was reported.
  [Test]
  public async Task AnEdgeCatchIsForgivenAsSoonAsTheArenaWidensTheCorners() {
    var player = await _addPlayer();
    player.Scale = new Vector2(SCALED_UP, SCALED_UP);
    player.CurrentDefaultCornerScaleFactor = ARENA_CORNER_SCALE;

    var half = player.GetCollisionHalfExtents();
    var ball = _addBall(
      RIGHT_FACE_COLOR,
      player.GlobalPosition + new Vector2(half.X - NEAR_THE_EDGE, -half.Y),
      Vector2.Down
    );
    _deaths = 0;

    await _physicsFrame();
    await _physicsFrame();

    _deaths.ShouldBe(0, "a ball caught on the top-right seam wears one of that corner's own colors");

    player.QueueFree();
    ball.QueueFree();
    await _physicsFrame();
  }

  // Dashing closes on the ball far faster than it can arrive on its own, so the contact lands deep and
  // several of the cube's shapes report it at once. Hitting a ball of the side's own color is the whole
  // point of the mechanic and must not be fatal.
  [Test]
  public async Task ADashIntoABallOfTheSideColorDoesNotKillThePlayer() {
    var killedAt = new List<float>();

    // Anywhere down the side, not just level with the middle of it: the height the ball happens to be
    // at is not something the player controls.
    foreach (var downTheSide in new[] { 0.0f, 0.25f, 0.5f, 0.7f, 0.85f, 1.0f }) {
      await _dashIntoBallOnTheRight(downTheSide, RIGHT_FACE_COLOR);
      if (_deaths > 0) {
        killedAt.Add(downTheSide);
      }
    }

    killedAt.ShouldBeEmpty($"a ball of the side's own color killed at {string.Join(", ", killedAt)} down the face");
  }

  // The other half of it: forgiving a contact that something accepted must not forgive one that
  // nothing did. Dashing right puts the ball among the right, bottom and bottom-right shapes, none of
  // which wears the left face's color.
  [Test]
  public async Task ADashIntoABallOfAnUntouchedColorStillKillsThePlayer() {
    await _dashIntoBallOnTheRight(0.0f, LEFT_FACE_COLOR);

    _deaths.ShouldBeGreaterThan(0);
  }

  // A real dash, driven through the input manager and the player's own state machine, rather than a
  // velocity written from the outside: the dash is what the report is about, so it is what has to run.
  private async Task _dashIntoBallOnTheRight(float downTheSide, string ballColor) {
    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    _provider.AddChild(player);
    await _physicsFrame();

    var half = player.GetCollisionHalfExtents();
    // Pinned, because a ball picks a random spawn direction and the contact geometry follows from it.
    var ball = _addBall(
      ballColor,
      player.GlobalPosition + new Vector2(REACHED_MID_DASH, half.Y * downTheSide),
      Vector2.Left
    );

    // Counted from here, not from the top of the test: freeing is deferred, so the cube and ball of
    // the run before this one are still in the arena for a frame, still colliding and still dying.
    _deaths = 0;

    _provider.Input.Press(IInputManager.Action.MoveRight);
    _provider.Input.Press(IInputManager.Action.Dash);
    for (var frame = 0; frame < DASH_FRAMES; frame++) {
      await _physicsFrame();
      _provider.Input.Release(IInputManager.Action.Dash);
    }
    _provider.Input.ReleaseAll();

    player.QueueFree();
    ball.QueueFree();
    await _physicsFrame();
  }

  private void _onPlayerDying(Node? area, Vector2 position, int entityType) => _deaths++;

  private async Task<Wfc.Entities.World.Player.Player> _addPlayer() {
    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    _provider.AddChild(player);
    await _physicsFrame();
    player.SetPhysicsProcess(false);
    return player;
  }

  // Placed and aimed before any physics frame runs. A ball left at the origin for a frame is a ball
  // inside the cube, which is a contact with no side to it and no color anyone could have read.
  private BouncingBall _addBall(string color, Vector2 position, Vector2 aim) {
    var ball = SceneHelpers.InstantiateNode<BouncingBall>();
    _provider.AddChild(ball);
    ball.GlobalPosition = position;
    ball.SetColor(color);
    ball.SetBallVelocity(aim);
    return ball;
  }

  private async Task _dropBallOnTopFace(string color) {
    var player = await _addPlayer();
    var ball = _addBall(color, player.GlobalPosition + Vector2.Up * player.GetCollisionHalfExtents().Y, Vector2.Down);
    _deaths = 0;

    // Long enough for the contact to be reported and acted on, and to go on being a contact
    // afterwards - a ball resting against a face it cannot take is one death, not one a frame.
    for (var frame = 0; frame < RESTING_FRAMES; frame++) {
      await _physicsFrame();
    }

    player.QueueFree();
    ball.QueueFree();
    await _physicsFrame();
  }

  private async Task _physicsFrame() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
  }
}
