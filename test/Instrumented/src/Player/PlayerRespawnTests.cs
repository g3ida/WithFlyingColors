namespace Wfc.test.instrumented.Player;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.Entities.World;
using Wfc.test;
using Wfc.test.instrumented.Helpers.Fakes;
using EventHandler = Wfc.Core.Event.EventHandler;

// A respawn has to hand the cube back exactly as the level starts it: standing still, on the
// checkpoint, answering to the keys that are actually held.
public class PlayerRespawnTests(Node testScene) : TestClass(testScene) {
  private const float FLOOR_HALF_HEIGHT = 50f;
  private const float FLOOR_HALF_WIDTH = 1200f;
  private const float FLOOR_CENTER_Y = 400f;
  private const double DEATH_TIMEOUT = 6.0;
  private const int FRAMES_TO_WALK = 40;
  private const int FRAMES_TO_SETTLE = 60;
  private const float STILL = 2.0f;

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
  public async Task ACubeThatDiedWalkingRespawnsStandingStill() {
    var player = await _addPlayerOnGround();
    var checkpoint = player.GlobalPosition;
    EventHandler.Instance.EmitCheckpointReached(checkpoint, "purple");

    _provider.Input.Press(IInputManager.Action.MoveRight);
    await _frames(FRAMES_TO_WALK);
    player.GlobalPosition.X.ShouldBeGreaterThan(checkpoint.X, "the cube never started walking");

    EventHandler.Instance.EmitPlayerDying(player.GlobalPosition, EntityType.Platform);
    await _frames(2);
    player.IsDying().ShouldBeTrue("the cube never entered a dying state");

    _provider.Input.ReleaseAll();
    await _respawn();

    var restingPlace = player.GlobalPosition.X;
    await _frames(FRAMES_TO_SETTLE);
    player.GlobalPosition.X.ShouldBe(restingPlace, STILL, "the cube walked off on its own after respawning");
  }

  [Test]
  public async Task ACubeThatFellOutOfTheWorldWalkingRespawnsStandingStill() {
    var player = await _addPlayerOnGround();
    var checkpoint = player.GlobalPosition;
    EventHandler.Instance.EmitCheckpointReached(checkpoint, "purple");

    _provider.Input.Press(IInputManager.Action.MoveLeft);
    await _frames(FRAMES_TO_WALK);
    player.GlobalPosition.X.ShouldBeLessThan(checkpoint.X, "the cube never started walking");

    EventHandler.Instance.EmitPlayerDying(player.GlobalPosition, EntityType.FallZone);
    await _frames(2);
    player.IsDying().ShouldBeTrue("the cube never entered a dying state");

    _provider.Input.ReleaseAll();
    await _respawn();

    var restingPlace = player.GlobalPosition.X;
    await _frames(FRAMES_TO_SETTLE);
    player.GlobalPosition.X.ShouldBe(restingPlace, STILL, "the cube walked off on its own after respawning");
  }

  // The key held at the moment of death is the one the player is still holding when the cube
  // comes back, so the respawn has to leave it walking rather than freeze it.
  [Test]
  public async Task ACubeRespawnedUnderAHeldKeyWalksAgain() {
    var player = await _addPlayerOnGround();
    EventHandler.Instance.EmitCheckpointReached(player.GlobalPosition, "purple");

    _provider.Input.Press(IInputManager.Action.MoveRight);
    await _frames(FRAMES_TO_WALK);

    EventHandler.Instance.EmitPlayerDying(player.GlobalPosition, EntityType.Platform);
    await _frames(2);
    await _respawn();

    var respawnedAt = player.GlobalPosition.X;
    await _frames(FRAMES_TO_SETTLE);
    player.GlobalPosition.X.ShouldBeGreaterThan(respawnedAt + STILL, "a held key stopped moving the cube");
  }

  // The cube tips off a ledge, rotates itself over and dies out of the world. Nothing is ever
  // pressed here, so anything that moves after the respawn moved on its own.
  [Test]
  public async Task ACubeThatDiedSlippingRespawnsStandingStill() {
    var scene = GD.Load<PackedScene>("res://test/src/Wfc/Entities/World/Player/State/Fixture/PlayerOnRightEdge.tscn")
      .Instantiate<Node>();
    TestScene.AddChild(scene);
    await _frames(2);
    var player = scene.GetNode<Wfc.Entities.World.Player.Player>("Player");
    var checkpoint = new Vector2(200f, 96f);
    EventHandler.Instance.EmitCheckpointReached(checkpoint, "purple");

    var died = await TestScene.GetTree()
      .ExpectSignal(EventHandler.Instance.Events, Events.SignalName.PlayerDied, DEATH_TIMEOUT * 2);
    died.ShouldBeTrue("the cube never slipped off the edge and died");
    EventHandler.Instance.EmitCheckpointLoaded();
    await _frames(20);

    var restingPlace = player.GlobalPosition.X;
    await _frames(FRAMES_TO_SETTLE);
    player.GlobalPosition.X.ShouldBe(restingPlace, STILL, "the cube walked off on its own after respawning");
    player.Rotation.ShouldBe(0f, 0.05f, "the cube came back mid-rotation");

    scene.QueueFree();
  }

  // Dying halfway through a quarter turn: the respawn snaps the cube back to the angle the
  // checkpoint asked for, and nothing may still be turning it afterwards.
  [Test]
  public async Task ACubeThatDiedRotatingRespawnsStandingStill() {
    var player = await _addPlayerOnGround();
    EventHandler.Instance.EmitCheckpointReached(player.GlobalPosition, "purple");

    _provider.Input.Press(IInputManager.Action.RotateRight);
    await _frames(2);
    _provider.Input.Release(IInputManager.Action.RotateRight);
    EventHandler.Instance.EmitPlayerDying(player.GlobalPosition, EntityType.Platform);
    await _frames(2);
    await _respawn();

    var restingPlace = player.GlobalPosition.X;
    await _frames(FRAMES_TO_SETTLE);
    player.GlobalPosition.X.ShouldBe(restingPlace, STILL, "the cube walked off on its own after respawning");
    player.Rotation.ShouldBe(0f, 0.05f, "the cube came back mid-rotation");
  }

  // A respawn can land on a cube that is not dying at all - a second death report, or the save
  // system reloading the slot. Whatever it interrupts, what comes back has to be still.
  [Test]
  public async Task ARespawnThatInterruptsASlipLeavesTheCubeStill() {
    var scene = GD.Load<PackedScene>("res://test/src/Wfc/Entities/World/Player/State/Fixture/PlayerOnRightEdge.tscn")
      .Instantiate<Node>();
    TestScene.AddChild(scene);
    await _frames(2);
    var player = scene.GetNode<Wfc.Entities.World.Player.Player>("Player");
    EventHandler.Instance.EmitCheckpointReached(new Vector2(200f, 96f), "purple");

    var slipped = await TestScene.GetTree()
      .ExpectSignal(EventHandler.Instance.Events, Events.SignalName.PlayerSlippering, DEATH_TIMEOUT);
    slipped.ShouldBeTrue("the cube never started slipping off the edge");
    await _frames(5);
    EventHandler.Instance.EmitCheckpointLoaded();
    await _frames(20);

    var restingPlace = player.GlobalPosition.X;
    await _frames(FRAMES_TO_SETTLE);
    player.GlobalPosition.X.ShouldBe(restingPlace, STILL, "the cube walked off on its own after respawning");
    player.Rotation.ShouldBe(0f, 0.05f, "the cube came back still turning");

    scene.QueueFree();
  }

  // One contact is one death. A hazard that keeps reporting - a beam still crossing the corpse -
  // must not buy a second explosion: every explosion reports a death of its own, and the respawn
  // that answers it would land on a cube that is already up and playing.
  [Test]
  public async Task AHazardStillTouchingTheCorpseDoesNotKillItTwice() {
    var player = await _addPlayerOnGround();
    EventHandler.Instance.EmitCheckpointReached(player.GlobalPosition, "purple");
    var deaths = 0;
    EventHandler.Instance.Events.PlayerDied += _count;

    EventHandler.Instance.EmitPlayerDying(player.GlobalPosition, EntityType.Lazer);
    await _frames(2);
    player.IsDying().ShouldBeTrue();
    for (var i = 0; i < 10; i++) {
      EventHandler.Instance.EmitPlayerDying(player.GlobalPosition, EntityType.Lazer);
      await _physicsFrame();
    }

    await TestScene.GetTree()
      .ExpectSignal(EventHandler.Instance.Events, Events.SignalName.PlayerDied, DEATH_TIMEOUT);
    EventHandler.Instance.EmitCheckpointLoaded();
    await _frames(FRAMES_TO_SETTLE * 3);
    EventHandler.Instance.Events.PlayerDied -= _count;

    deaths.ShouldBe(1, "one death reported itself more than once, so the respawn runs again later");

    void _count() => deaths++;
  }

  private async Task _respawn() {
    var died = await TestScene.GetTree()
      .ExpectSignal(EventHandler.Instance.Events, Events.SignalName.PlayerDied, DEATH_TIMEOUT);
    died.ShouldBeTrue("the death never completed");
    EventHandler.Instance.EmitCheckpointLoaded();
    await _frames(20);
  }

  private async Task<Wfc.Entities.World.Player.Player> _addPlayerOnGround() {
    var floor = new StaticBody2D();
    var floorShape = new CollisionShape2D {
      Shape = new RectangleShape2D { Size = new Vector2(FLOOR_HALF_WIDTH * 2f, FLOOR_HALF_HEIGHT * 2f) }
    };
    floor.AddChild(floorShape);
    floor.GlobalPosition = new Vector2(0f, FLOOR_CENTER_Y);
    _provider.AddChild(floor);

    var player = GD.Load<PackedScene>("res://src/Wfc/Entities/World/Player/Player/Player.tscn")
      .Instantiate<Wfc.Entities.World.Player.Player>();
    player.CollisionMask = 13;
    player.GlobalPosition = new Vector2(0f, FLOOR_CENTER_Y - FLOOR_HALF_HEIGHT - 60f);
    _provider.AddChild(player);

    await _frames(30);
    player.PlayerState.ShouldNotBeNull("the player state machine never started");
    player.IsOnFloor().ShouldBeTrue("the cube never landed on the test floor");
    return player;
  }

  private async Task _frames(int count) {
    for (var i = 0; i < count; i++) {
      await _physicsFrame();
    }
  }

  private async Task _physicsFrame() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
  }
}
