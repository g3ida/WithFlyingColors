namespace Wfc.test.instrumented.Player;

using System;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Player;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils.Colors;

// The arc says out loud which two colors are about to meet, so what it is drawn between matters as
// much as whether it is drawn at all. It is also deliberately not a proximity light: a cube parked
// beside a hazard has nothing to report, and only moving brings it back.
public class ColorArcTests(Node testScene) : TestClass(testScene) {
  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";

  // What the cube in the scene wears on the face the hazard is placed against, and one of the
  // three it does not.
  private const string RIGHT_FACE_COLOR = ColorUtils.YELLOW;
  private const string NOT_ON_THE_RIGHT_FACE = ColorUtils.PINK;

  private const float FLOOR_HALF_HEIGHT = 50f;
  private const float FLOOR_HALF_WIDTH = 1200f;
  private const float FLOOR_CENTER_Y = 400f;
  private const int A_LANDING = 30;

  // Comfortably inside the arc's reach and well clear of the contact it refuses to draw over, so
  // that retuning either bound does not quietly turn these tests into no-ops.
  private const float HAZARD_GAP = 25f;
  private const float HAZARD_HALF_SIZE = 30f;
  // Enough to count as moving without carrying the cube anywhere near the hazard.
  private const float A_NUDGE = 2f;

  private FakeDependenciesProvider _provider = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await PhysicsFrames.Frame(TestScene);
  }

  [Cleanup]
  public void Cleanup() => _provider.QueueFree();

  [Test]
  public async Task AColorTheNearFaceDoesNotWearArcsWhileTheCubeMoves() {
    var player = await _addPlayerOnGround();
    var arc = _arcOf(player);
    _addHazardBesideTheRightFace(player, NOT_ON_THE_RIGHT_FACE);

    var fired = await _nudgeUntil(player, () => arc.IsDischarging);

    fired.ShouldBeTrue("a color the near face cannot touch drew no arc");
    arc.AreaColorGroup.ShouldBe(NOT_ON_THE_RIGHT_FACE, "the arc was not drawn in the hazard's color");
  }

  [Test]
  public async Task TheKnobTurnsItOff() {
    var player = await _addPlayerOnGround();
    var arc = _arcOf(player);
    arc.Enabled = false;
    _addHazardBesideTheRightFace(player, NOT_ON_THE_RIGHT_FACE);

    var fired = await _nudgeUntil(player, () => arc.IsDischarging);

    fired.ShouldBeFalse("the arc fired with its knob turned off");
  }

  // The face already answers for this color, so there is no discharge waiting to happen - landing
  // on it is what the cube is for.
  [Test]
  public async Task AColorTheNearFaceWearsNeverArcs() {
    var player = await _addPlayerOnGround();
    var arc = _arcOf(player);
    _addHazardBesideTheRightFace(player, RIGHT_FACE_COLOR);

    var fired = await _nudgeUntil(player, () => arc.IsDischarging);

    fired.ShouldBeFalse("a color the cube is safe against arced anyway");
  }

  // A neutral surface is tagged with all four colors rather than none, so reading it as "the color
  // it is in" picks whichever is listed first and arcs the cube against a platform every one of
  // its faces can land on.
  [Test]
  public async Task ANeutralSurfaceEveryFaceIsSafeOnNeverArcs() {
    var player = await _addPlayerOnGround();
    var arc = _arcOf(player);
    _addHazardBesideTheRightFace(player, ColorUtils.COLOR_GROUPS);

    var fired = await _nudgeUntil(player, () => arc.IsDischarging);

    fired.ShouldBeFalse("a neutral platform arced as though it were one of the four colors");
  }

  [Test]
  public async Task NothingNearbyNeverArcs() {
    var player = await _addPlayerOnGround();
    var arc = _arcOf(player);

    var fired = await _nudgeUntil(player, () => arc.IsDischarging);

    fired.ShouldBeFalse("the cube arced against nothing at all");
  }

  // The burst is short by design and standing still cuts it off, which is the whole reason it is a
  // burst rather than a glow that sits there for as long as the two are close.
  [Test]
  public async Task AStandingCubeStopsArcingAndStaysDark() {
    var player = await _addPlayerOnGround();
    var arc = _arcOf(player);
    _addHazardBesideTheRightFace(player, NOT_ON_THE_RIGHT_FACE);

    (await _nudgeUntil(player, () => arc.IsDischarging))
      .ShouldBeTrue("the arc never fired, so there was nothing to see stop");

    // No nudges from here: the cube is where it was and stays there.
    var stopped = await PhysicsFrames.WaitFor(TestScene, () => !arc.IsDischarging, 1.0);
    stopped.ShouldBeTrue("a cube that came to rest went on arcing");

    var restarted = await PhysicsFrames.WaitFor(TestScene, () => arc.IsDischarging, 1.0);
    restarted.ShouldBeFalse("a cube standing still started arcing on its own");
  }

  private static ColorArc _arcOf(Wfc.Entities.World.Player.Player player) =>
    player.GetNode<ColorArc>("ColorArc");

  // Shuffles the cube on the spot so it reads as moving, and answers whether the condition ever
  // held while it did.
  private async Task<bool> _nudgeUntil(
    Wfc.Entities.World.Player.Player player, Func<bool> until
  ) {
    for (var frame = 0; frame < 90; frame++) {
      player.GlobalPosition += new Vector2((frame & 1) == 0 ? A_NUDGE : -A_NUDGE, 0f);
      await PhysicsFrames.Frame(TestScene);
      if (until()) {
        return true;
      }
    }
    return false;
  }

  private void _addHazardBesideTheRightFace(
    Wfc.Entities.World.Player.Player player, params string[] colorGroups
  ) {
    var hazard = new Area2D { CollisionLayer = 4, CollisionMask = 0 };
    hazard.AddChild(new CollisionShape2D {
      Shape = new RectangleShape2D {
        Size = new Vector2(HAZARD_HALF_SIZE * 2f, HAZARD_HALF_SIZE * 2f)
      }
    });
    foreach (var colorGroup in colorGroups) {
      hazard.AddToGroup(colorGroup);
    }
    _provider.AddChild(hazard);
    hazard.GlobalPosition = player.GlobalPosition
      + new Vector2(player.GetCollisionHalfExtents().X + HAZARD_GAP + HAZARD_HALF_SIZE, 0f);
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

    await PhysicsFrames.Advance(TestScene, A_LANDING);
    player.IsOnFloor().ShouldBeTrue("the cube never landed on the test floor");
    return player;
  }
}
