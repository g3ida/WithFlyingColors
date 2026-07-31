namespace Wfc.test.instrumented.BrickBreaker;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.BrickBreaker;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;
using Wfc.Utils.Layers;

// A cube that jumps into the ball lends it speed, and that loan is the whole of what a slam looks
// like. Spending it on the next surface the ball touches took it away at the first wall the ball
// reached - which, launched off a paddle, is usually the far end of the same flight: the ball
// bounced and stopped dead in the same frame. The loan has to run out on its own clock instead.
public class BallLentSpeedTests(Node testScene) : TestClass(testScene) {
  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";
  private const string WALL_GROUP = "wall";
  private const float JUMP_SPEED = 1200.0f;
  private const float START_HEIGHT_IN_HALF_CUBES = 2.0f;
  private static readonly Vector2 CEILING_SIZE = new Vector2(4000.0f, 200.0f);
  private const float CEILING_GAP = 250.0f;
  private const int JUMP_FRAMES = 6;
  private const int FLIGHT_FRAMES = 90;

  // The bounce is allowed to cost the ball a little, but not to hand back the loan.
  private const float CONTINUITY = 0.9f;

  // Long enough for the loan to have been halved away and the remainder floored off.
  private const int SETTLING_FRAMES = 240;
  private const float SETTLED_TOLERANCE = 0.01f;

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
  public async Task ABounceDoesNotTakeBackWhatThePaddleLent() {
    var arena = await _slamBallIntoCeiling();

    arena.SpeedBeforeBounce.ShouldBeGreaterThan(
      arena.NormalSpeed,
      "the paddle should have struck the ball and lent it speed"
    );
    arena.SpeedAfterBounce.ShouldBeGreaterThan(
      arena.SpeedBeforeBounce * CONTINUITY,
      "the wall should have turned the ball back without emptying it of the paddle's speed"
    );

    await arena.Clear();
  }

  [Test]
  public async Task WhatThePaddleLentRunsOutOnItsOwn() {
    var arena = await _slamBallIntoCeiling();

    for (var frame = 0; frame < SETTLING_FRAMES; frame++) {
      await _physicsFrame();
    }

    arena.Ball.BallVelocity.Length().ShouldBe(
      arena.NormalSpeed,
      SETTLED_TOLERANCE,
      "the ball should have settled back to its normal speed"
    );

    await arena.Clear();
  }

  // A cube jumping into a ball parked above it, with a ceiling close enough overhead that the ball
  // still has most of the loan when it gets there. The paddle is left behind after the strike so
  // that nothing but the wall is acting on the ball by the time it bounces.
  private async Task<Arena> _slamBallIntoCeiling() {
    var player = await _addPlayer();
    var half = player.GetCollisionHalfExtents();

    var ball = SceneHelpers.InstantiateNode<BouncingBall>();
    _provider.AddChild(ball);
    ball.GlobalPosition = player.GlobalPosition + new Vector2(0.0f, -half.Y * START_HEIGHT_IN_HALF_CUBES);
    await _physicsFrame();
    ball.SetBallVelocity(Vector2.Down);

    var ceiling = _addCeiling(ball.GlobalPosition.Y - CEILING_GAP - (CEILING_SIZE.Y * 0.5f));
    var normalSpeed = ball.BallVelocity.Length();

    for (var frame = 0; frame < JUMP_FRAMES; frame++) {
      player.Velocity = Vector2.Up * JUMP_SPEED;
      player.MoveAndSlide();
      await _physicsFrame();
    }
    player.Velocity = Vector2.Zero;

    var before = 0.0f;
    var after = 0.0f;
    for (var frame = 0; frame < FLIGHT_FRAMES; frame++) {
      var climbing = ball.BallVelocity;
      await _physicsFrame();

      // The frame the ceiling turned the ball around, read on either side of the contact.
      if (climbing.Y < 0.0f && ball.BallVelocity.Y > 0.0f) {
        before = climbing.Length();
        after = ball.BallVelocity.Length();
        break;
      }
    }

    after.ShouldBeGreaterThan(0.0f, "the ball should have reached the ceiling and bounced");
    return new Arena(this, player, ball, ceiling, normalSpeed, before, after);
  }

  private StaticBody2D _addCeiling(float height) {
    var ceiling = new StaticBody2D {
      CollisionLayer = PhysicsLayers.Default.Mask,
      CollisionMask = 0,
      Position = new Vector2(0.0f, height),
    };
    ceiling.AddToGroup(WALL_GROUP);
    ceiling.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = CEILING_SIZE } });
    _provider.AddChild(ceiling);
    return ceiling;
  }

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

  private sealed record Arena(
    BallLentSpeedTests Tests,
    Wfc.Entities.World.Player.Player Player,
    BouncingBall Ball,
    StaticBody2D Ceiling,
    float NormalSpeed,
    float SpeedBeforeBounce,
    float SpeedAfterBounce
  ) {
    public async Task Clear() {
      Player.QueueFree();
      Ball.QueueFree();
      Ceiling.QueueFree();
      await Tests._physicsFrame();
    }
  }
}
