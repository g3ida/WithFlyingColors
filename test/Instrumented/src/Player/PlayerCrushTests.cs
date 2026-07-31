namespace Wfc.test.instrumented.Player;

using System;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Event;
using Wfc.Entities.World.Player;
using Wfc.test;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using EventHandler = Wfc.Core.Event.EventHandler;

// A sliding platform driving the cube into the floor, which is the one death in the game the
// engine will not report on its own: neither body gives way, so what actually happens without
// this is that the cube is shoved down through the level and comes out the other side alive.
//
// The other half of it is the ride. The brick breaker and the tetris pool both put the player on
// one of these platforms and lift them into the arena, and that ride overlaps the platform by
// whatever it moved this frame - so the same code that has to kill has to leave every one of
// those alone.
public class PlayerCrushTests(Node testScene) : TestClass(testScene) {
  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";
  private const string FLOOR_SCENE = "res://src/Wfc/Entities/World/Tetris/SlidingFloor/SlidingFloor.tscn";
  private const string SLIDER_SCENE = "res://src/Wfc/Entities/World/Platforms/SlidingPlatform/SlidingPlatform.tscn";

  private const float GROUND_TOP = 760f;
  private const float LANE_X = 700f;
  private const float FLOOR_HALF_HEIGHT = 36f;

  // Room enough above the cube for the platform to be well clear of it at rest, and travel enough
  // to bring its underside down onto the ground.
  private const float PLATFORM_CLEARANCE = 260f;

  private const int FRAMES_TO_LAND = 40;
  private const int FRAMES_TO_SETTLE = 60;
  private const double CRUSH_TIMEOUT = 4.0;

  // A crush reported the moment the two meet costs the cube one frame of being shoved along, and a
  // frame of that is a few pixels. Anything approaching a tenth of the cube means the report is
  // late and the corpse ends up buried.
  private const float SHOVED = 8f;

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
  public async Task APlatformComingDownOnAPinnedCubeSquashesIt() {
    var player = await _addPlayerOnGround();
    _addDescendingPlatform(travel: PLATFORM_CLEARANCE - FLOOR_HALF_HEIGHT);

    var squashed = await _waitFor(() => player.PlayerState is PlayerSquashedState);

    squashed.ShouldBeTrue("the platform drove the cube into the floor and nothing killed it");
  }

  // The cube must not be able to survive by being pushed somewhere it could never have walked.
  [Test]
  public async Task ACrushedCubeIsNotShovedThroughTheFloor() {
    var player = await _addPlayerOnGround();
    var restingY = player.GlobalPosition.Y;
    _addDescendingPlatform(travel: PLATFORM_CLEARANCE - FLOOR_HALF_HEIGHT);

    await _waitFor(() => player.PlayerState is PlayerSquashedState);
    await _frames(FRAMES_TO_SETTLE);

    var sunk = player.GlobalPosition.Y - restingY;
    sunk.ShouldBeLessThan(
      SHOVED,
      "the cube was carried into the floor before anything noticed it was being crushed"
    );
  }

  // The lift, both ways. Nothing above the cube and nothing below the platform, so whatever the
  // overlap looks like on any given frame there is no crush here.
  [Test]
  public async Task ACubeRidingAPlatformIsCarriedRatherThanCrushed() {
    var floor = _addFloorBody(GROUND_TOP - FLOOR_HALF_HEIGHT);
    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    player.CollisionMask = 13;
    player.Position = new Vector2(LANE_X, GROUND_TOP - 200f);
    _provider.AddChild(player);
    await _frames(FRAMES_TO_LAND);
    player.IsOnFloor().ShouldBeTrue("the cube never landed on the platform");

    _driveFloor(floor, travel: -PLATFORM_CLEARANCE);
    await _frames(FRAMES_TO_SETTLE * 2);

    player.IsDying().ShouldBeFalse("riding the lift killed the cube");
    player.GlobalPosition.Y.ShouldBeLessThan(
      GROUND_TOP - FLOOR_HALF_HEIGHT,
      "the cube was left behind instead of being carried"
    );
  }

  // The press ends the way every death ends: the cube blows apart. The explosion is what carries
  // the death report, so a squash that never reaches it is a squash the level waits on forever.
  [Test]
  public async Task ACrushEndsInTheCubeExploding() {
    var player = await _addPlayerOnGround();
    _addDescendingPlatform(travel: PLATFORM_CLEARANCE - FLOOR_HALF_HEIGHT);

    await _waitFor(() => player.PlayerState is PlayerSquashedState);
    var exploded = await _waitFor(
      () => player.GetChildren().Any(child => child is Wfc.Entities.World.Explosion.Explosion)
    );

    exploded.ShouldBeTrue("the squash never handed the cube over to the explosion");
  }

  // The squash is written straight onto the sprite's scale and position, and neither is part of
  // what a checkpoint restores - so the state has to put them back itself or the cube comes back
  // to play as a pancake.
  [Test]
  public async Task ARespawnHandsBackASquareCube() {
    var player = await _addPlayerOnGround();
    _addDescendingPlatform(travel: PLATFORM_CLEARANCE - FLOOR_HALF_HEIGHT);

    await _waitFor(() => player.PlayerState is PlayerSquashedState);
    await _frames(4);
    player.AnimatedSpriteNode.Scale.Y.ShouldBeLessThan(1f, "the cube was never flattened");

    EventHandler.Instance.EmitCheckpointLoaded();
    await _frames(4);

    player.AnimatedSpriteNode.Scale.ShouldBe(Vector2.One);
    player.AnimatedSpriteNode.Position.ShouldBe(Vector2.Zero);
    player.AnimatedSpriteNode.Visible.ShouldBeTrue("the cube came back invisible");
    player.IsDying().ShouldBeFalse("the cube came back still dying");
  }

  private async Task<Wfc.Entities.World.Player.Player> _addPlayerOnGround() {
    var ground = new StaticBody2D { Position = new Vector2(LANE_X, GROUND_TOP + 100f) };
    ground.AddChild(new CollisionShape2D {
      Shape = new RectangleShape2D { Size = new Vector2(2400f, 200f) }
    });
    _provider.AddChild(ground);

    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    player.CollisionMask = 13;
    player.Position = new Vector2(LANE_X, GROUND_TOP - 200f);
    _provider.AddChild(player);

    await _frames(FRAMES_TO_LAND);
    player.IsOnFloor().ShouldBeTrue("the cube never landed on the test floor");
    return player;
  }

  private AnimatableBody2D _addDescendingPlatform(float travel) {
    var floor = _addFloorBody(GROUND_TOP - PLATFORM_CLEARANCE);
    _driveFloor(floor, travel);
    return floor;
  }

  // Positioned before it enters the tree: an AnimatableBody2D syncs its transform from the physics
  // server, so moving one that is already in the tree does not take.
  private AnimatableBody2D _addFloorBody(float centreY) {
    var floor = GD.Load<PackedScene>(FLOOR_SCENE).Instantiate<AnimatableBody2D>();
    floor.Position = new Vector2(LANE_X, centreY);
    _provider.AddChild(floor);
    return floor;
  }

  // A one-shot slider, so the platform arrives and stays rather than turning round and lifting the
  // paint back out of frame mid-assertion.
  private static void _driveFloor(AnimatableBody2D floor, float travel) {
    var slider = GD.Load<PackedScene>(SLIDER_SCENE).Instantiate<Node2D>();
    slider.Set("wait_time", 0.1f);
    slider.Set("one_shot", true);
    slider.Set("one_shot_state", (int)Wfc.Entities.World.Platforms.SlidingPlatform.State.SlidingForth);
    slider.AddChild(new Marker2D { Position = new Vector2(0f, travel) });
    floor.AddChild(slider);
  }

  private Task<bool> _waitFor(Func<bool> until) =>
    PhysicsFrames.WaitFor(TestScene, until, CRUSH_TIMEOUT);

  private Task _frames(int count) => PhysicsFrames.Advance(TestScene, count);

  private Task _physicsFrame() => PhysicsFrames.Frame(TestScene);
}
