namespace Wfc.test.instrumented.BrickBreaker;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Utils.Layers;

// The ball-and-paddle layer contract, which no `.tscn` can explain in place.
//
// The two do not collide, in either direction. A ball that blocks its paddle grinds against it, and a
// paddle that blocks its ball traps it: a cube climbing into the ball buries it deeper than its own
// radius within a frame, and a body that overlaps another cannot be swept out of it, so the ball
// stops moving under its own power and travels with the cube instead. Both the bounce and the
// wrong-color death are driven from the overlap the ball's own area reports.
public class BallPaddleLayersTests(Node testScene) : TestClass(testScene) {
  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";
  private const string BALL_SCENE = "res://src/Wfc/Entities/World/BrickBreaker/BouncingBall/BouncingBall.tscn";
  private const string SHIELD_SCENE = "res://src/Wfc/Entities/World/BrickBreaker/Powerups/ProtectionArea/ProtectionArea.tscn";
  private const string POWER_UP_SCENE = "res://src/Wfc/Entities/World/BrickBreaker/Powerups/PowerUp/PowerUp.tscn";
  private const string DEATH_ZONE_SCENE = "res://src/Wfc/Entities/World/BrickBreaker/DeathZone/DeathZone.tscn";

  [Test]
  public void TheBallDoesNotObstructThePlayer() {
    using var player = _load(PLAYER_SCENE);

    (player.CollisionMask & PhysicsLayers.BouncingBall.Mask)
      .ShouldBe(0u, "a ball that blocks the paddle grinds against it instead of bouncing off");
  }

  [Test]
  public void ThePlayerDoesNotObstructTheBall() {
    using var ball = _load(BALL_SCENE);

    (ball.CollisionMask & PhysicsLayers.Player.Mask)
      .ShouldBe(0u, "a paddle the ball can collide with swallows it and carries it");
  }

  [Test]
  public void TheBallsAreaSeesThePlayer() {
    using var ball = _load(BALL_SCENE);
    var area = ball.GetNode<Area2D>("Area2D");

    (area.CollisionMask & PhysicsLayers.Player.Mask)
      .ShouldBe(PhysicsLayers.Player.Mask, "both the bounce and the wrong-color death come from this overlap");
  }

  [Test]
  public void TheBallStillBouncesOffTheArena() {
    using var ball = _load(BALL_SCENE);
    var arena = PhysicsLayers.Default.Mask | PhysicsLayers.Platform.Mask | PhysicsLayers.Bricks.Mask;

    (ball.CollisionMask & arena).ShouldBe(arena, "walls and bricks are meant to stop the ball");
  }

  // The shield power-up is a barrier across the arena floor that buys the player one save. It sat on
  // the ball's own layer, which the ball does not collide with - so the ball flew straight through
  // while the shield's area saw it pass and deleted itself. It needs a layer of its own: on the ball's
  // layer the three balls of a triple-ball power-up would grind against each other, and on the bricks'
  // the player would stand on a barrier that spawns at cube height.
  [Test]
  public void TheShieldStopsTheBall() {
    using var ball = _load(BALL_SCENE);
    using var shield = _load<StaticBody2D>(SHIELD_SCENE);

    (shield.CollisionLayer & PhysicsLayers.Shield.Mask)
      .ShouldBe(PhysicsLayers.Shield.Mask, "the shield needs a layer nothing else shares");
    (ball.CollisionMask & shield.CollisionLayer)
      .ShouldBe(shield.CollisionLayer, "a shield the ball passes through saves nothing");
  }

  // And it is spent by the save: the area is what notices the ball and takes the shield away.
  [Test]
  public void TheShieldSeesTheBallThatSpendsIt() {
    using var ball = _load(BALL_SCENE);
    using var shield = _load<StaticBody2D>(SHIELD_SCENE);
    var area = shield.GetNode<Area2D>("Area2D");

    (area.CollisionMask & ball.CollisionLayer).ShouldBe(ball.CollisionLayer);
  }

  // An uncollected power-up falls past the paddle into the death zone at the bottom of the arena and
  // is meant to despawn there. Its area could not see the zone's layer, so it fell out of the world
  // and kept falling for the rest of the level.
  [Test]
  public void AnUncollectedPowerUpSeesTheDeathZone() {
    using var powerUp = _load<Node2D>(POWER_UP_SCENE);
    using var deathZone = _load<Area2D>(DEATH_ZONE_SCENE);
    var area = powerUp.GetNode<Area2D>("Area2D");

    (area.CollisionMask & deathZone.CollisionLayer)
      .ShouldBe(deathZone.CollisionLayer, "a power-up that cannot see the death zone never despawns");
  }

  // Never added to the tree: the masks are scene data, and staying out keeps _Ready, and the
  // dependencies it would want resolved, out of it.
  private static T _load<T>(string path) where T : Node => GD.Load<PackedScene>(path).Instantiate<T>();

  private static CharacterBody2D _load(string path) => _load<CharacterBody2D>(path);
}
