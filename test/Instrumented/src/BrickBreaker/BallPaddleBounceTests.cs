namespace Wfc.test.instrumented.BrickBreaker;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.BrickBreaker;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// This paddle can jump, and a jump is faster than the ball. Bouncing off it as though it were a
// static mirror could only ever send the ball away at the ball's own speed, so the cube climbed into
// it and carried it for as long as the jump lasted. The ball has to leave the face faster than the
// face advances, whatever the cube is doing and wherever along the face it was struck - the far end
// of the face being the case that broke, since that is where the aim deflects hardest and so takes
// the most speed away from the direction that matters. What the paddle lends runs out, so a cube
// that never stops climbing does catch the ball up again and throw it clear a second time: it is
// the clearance the ball keeps, not a gap that grows forever, that says it was not carried.
public class BallPaddleBounceTests(Node testScene) : TestClass(testScene) {
  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";
  private const float JUMP_SPEED = 1200.0f;
  private const float START_HEIGHT_IN_HALF_CUBES = 2.0f;
  private const float NEAR_THE_END_OF_THE_FACE = 0.75f;
  private const int FRAMES = 30;

  private FakeDependenciesProvider _provider = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await _physicsFrame();
  }

  [Cleanup]
  public void Cleanup() => _provider.QueueFree();

  [Test]
  public async Task AJumpingPlayerThrowsTheBallClearOfItsFace() => await _jumpIntoBall(0.0f);

  [Test]
  public async Task AJumpingPlayerThrowsTheBallClearWhenItLandsNearTheEndOfTheFace() =>
    await _jumpIntoBall(NEAR_THE_END_OF_THE_FACE);

  private async Task _jumpIntoBall(float acrossFace) {
    var player = await _addPlayer();
    var half = player.GetCollisionHalfExtents();

    var ball = SceneHelpers.InstantiateNode<BouncingBall>();
    _provider.AddChild(ball);
    ball.GlobalPosition = player.GlobalPosition
      + new Vector2(half.X * acrossFace, -half.Y * START_HEIGHT_IN_HALF_CUBES);
    await _physicsFrame();
    ball.SetBallVelocity(Vector2.Down);

    var speedBefore = ball.BallVelocity.Length();
    var radius = _radiusOf(ball);
    var fastest = 0.0f;
    var clearest = float.MaxValue;

    // The cube climbs at a constant jump speed, never slowing: a ball that only gets free because
    // the paddle ran out of jump is still stuck.
    for (var frame = 0; frame < FRAMES; frame++) {
      player.Velocity = Vector2.Up * JUMP_SPEED;
      player.MoveAndSlide();
      await _physicsFrame();
      fastest = Mathf.Max(fastest, ball.BallVelocity.Length());
      clearest = Mathf.Min(clearest, _clearanceOf(ball, player.GlobalPosition, half));
    }

    fastest.ShouldBeGreaterThan(speedBefore, "the paddle should have struck the ball and lent it speed");
    fastest.ShouldBeGreaterThan(JUMP_SPEED, "the ball should leave the face faster than the face climbs");
    clearest.ShouldBeGreaterThan(radius, "the ball should have stayed clear of the face rather than riding it up");
  }

  // How far the ball's center sits outside the cube's box, along whichever axis it left by, so that
  // a ball thrown off the end of the face is measured the same as one thrown straight up.
  private static float _clearanceOf(BouncingBall ball, Vector2 center, Vector2 half) {
    var outside = (ball.GlobalPosition - center).Abs() - half;
    return Mathf.Max(outside.X, outside.Y);
  }

  private static float _radiusOf(BouncingBall ball) =>
    ((CircleShape2D)ball.GetNode<CollisionShape2D>("CollisionShape2D").Shape).Radius * ball.Scale.X;

  // The player scene, with its own physics turned off: the jump is driven from the test so the cube
  // climbs at a known speed, and none of this depends on the player's state machine.
  private async Task<Wfc.Entities.World.Player.Player> _addPlayer() {
    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    _provider.AddChild(player);
    await _physicsFrame();
    player.SetPhysicsProcess(false);
    return player;
  }

  private Task _physicsFrame() => PhysicsFrames.Frame(TestScene);
}
