namespace Wfc.test.instrumented.Paint;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Paint;
using Wfc.Entities.World.Platforms;
using Wfc.Entities.World.Player;
using Wfc.Utils;
using Wfc.Utils.Colors;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using EventHandler = Wfc.Core.Event.EventHandler;

// The flood is the one hazard in the game that has to be outrun rather than solved, so what these
// pin is the shape of the chase: that the paint finds the floor it was poured onto, that it runs
// downhill rather than sitting where it landed, that it stops short of the safe end, and that a
// reload puts the room back the way the player first walked into it.
public class PaintFloodTests(Node testScene) : TestClass(testScene) {
  private const float STEP_WIDTH = 480f;
  private const float STEP_DROP = 96f;
  private const float TOP = 200f;

  private PaintFlood _flood = default!;
  private Node2D _ground = default!;
  private FakeGameLevelProvider _level = default!;

  // A staircase down to the right, the shape the flood is authored against in the level.
  [Setup]
  public async Task Setup() {
    _level = new FakeGameLevelProvider();
    TestScene.AddChild(_level);
    _ground = new Node2D();
    TestScene.AddChild(_ground);
    for (var i = 0; i < 5; i++) {
      var step = SceneHelpers.InstantiateNode<FlatPlatform>();
      step.SnapToGrid = false;
      step.Group = FlatPlatform.NEUTRAL;
      step.Size = new Vector2(STEP_WIDTH, 600f);
      step.Position = new Vector2((i * STEP_WIDTH) + (STEP_WIDTH / 2f), TOP + (i * STEP_DROP) + 300f);
      _ground.AddChild(step);
    }

    _flood = SceneHelpers.InstantiateNode<PaintFlood>();
    _flood.Position = new Vector2(0f, 0f);
    _flood.Width = STEP_WIDTH * 5f;
    _flood.Depth = 900f;
    _flood.Group = ColorUtils.PURPLE;
    _flood.SpoutOffset = new Vector2(120f, TOP);
    _flood.SpoutWidth = 120f;
    _flood.CameraZoom = 0f;
    _level.AddChild(_flood);
    await PhysicsFrames.Frame(TestScene);
  }

  [Cleanup]
  public void Cleanup() {
    _flood.QueueFree();
    _ground.QueueFree();
    _level.QueueFree();
  }

  // Nothing about the staircase is authored on the flood, so if the floor is not read off the
  // level the paint pours into a room with no bottom and every column drains.
  [Test]
  public async Task ItFindsTheFloorItWasPouredOnto() {
    _flood.Pour();
    await PhysicsFrames.Advance(TestScene, 84);

    _flood.WettedWidth.ShouldBeGreaterThan(0f, "the paint found nothing to lie on");
    _flood.DeepestDepth.ShouldBeGreaterThan(0f);
  }

  // The point of the set piece: paint poured at the top arrives at the bottom under its own
  // weight. A flood that pools where it lands is a puddle, and there is nothing to run from.
  [Test]
  public async Task ItRunsDownhillAwayFromWhereItWasPoured() {
    _flood.Pour();
    await PhysicsFrames.Advance(TestScene, 60);
    var early = _flood.FrontX;

    await PhysicsFrames.Advance(TestScene, 165);

    _flood.FrontX.ShouldBeGreaterThan(early + STEP_WIDTH, "the paint stayed where it was poured");
  }

  // The paint may not cross the end of what the level gave it, whatever it has left to spend:
  // the stretch past the last column is the safe zone, and a flood that runs into it has no end.
  [Test]
  public async Task ItStopsAtTheEndOfTheRunItWasGiven() {
    _flood.PourRate = 4000f;
    _flood.PourDuration = 6f;
    _flood.Pour();
    await PhysicsFrames.Advance(TestScene, 360);

    _flood.FrontX.ShouldBeLessThanOrEqualTo(_flood.Width, "the paint ran past the room");
  }

  // A retry starts at the top of the room with a full bucket, the same way the player first met
  // it. Paint left lying about would drown them where they respawn.
  [Test]
  public async Task AReloadPutsTheRoomBack() {
    _flood.Pour();
    await PhysicsFrames.Advance(TestScene, 84);
    _flood.DeepestDepth.ShouldBeGreaterThan(0f);

    EventHandler.Instance.EmitCheckpointLoaded();
    await PhysicsFrames.Frame(TestScene);

    _flood.DeepestDepth.ShouldBe(0f, "the paint the player died to is still lying there");
    _flood.IsRunning.ShouldBeFalse("the bucket is still pouring into a room nobody has entered");
  }

  // The columns only ever hand paint to one another, so what the flood holds may change by the
  // pour and by the drain at the ends and by nothing else. A leak here reads in the room as a
  // chase that quietly runs out of steam halfway down.
  [Test]
  public async Task ThePaintIsOnlyEverMovedAround() {
    _flood.PourRate = 600f;
    _flood.PourDuration = 1f;
    _flood.Pour();
    await PhysicsFrames.Advance(TestScene, 90);

    var held = _flood.TotalPaint;
    held.ShouldBe(600f, 1f, "the pour did not put in what it was asked for");

    await PhysicsFrames.Advance(TestScene, 60);
    _flood.TotalPaint.ShouldBeLessThanOrEqualTo(held + 0.5f, "the flood is making paint");
  }

  // What the chase is actually made of: the front has to be slower than the cube and fast enough
  // that standing still loses.
  [Test]
  public async Task ItsFrontTravelsAtAChaseableSpeed() {
    _flood.Pour();
    await PhysicsFrames.Advance(TestScene, 72);

    var from = _flood.FrontX;
    await PhysicsFrames.Advance(TestScene, 60);
    var speed = _flood.FrontX - from;

    GD.Print($"[PaintFlood] front speed {speed:F0} px/s  front {_flood.FrontX:F0}  " +
      $"deepest {_flood.DeepestDepth:F1}  wetted {_flood.WettedWidth:F0}");
    // Against the cube's own top speed rather than a number of its own: the room is only fair
    // while the player can pull away from it by running, and a margin is what leaves them room to
    // turn a corner, land a jump, or set off a step late.
    speed.ShouldBeGreaterThan(120f, "the flood is too slow to be a chase");
    speed.ShouldBeLessThan(Player.SPEED * 0.8f, "the flood outruns the cube at a dead run");
  }
}
