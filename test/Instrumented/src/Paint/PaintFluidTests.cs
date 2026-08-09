namespace Wfc.test.instrumented.Paint;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Serialization;
using Wfc.Entities.World.Paint;
using Wfc.Entities.World.Platforms;
using Wfc.Entities.World.Player;
using System.Collections.Generic;
using System.Linq;
using Wfc.Utils;
using Wfc.Utils.Colors;
using Wfc.Utils.Layers;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using EventHandler = Wfc.Core.Event.EventHandler;

// The paint is simulated rather than drawn, so nothing here pins what it looks like. What these
// pin is the shape of the chase: that the paint stays on the floor it was poured onto, that it
// runs down the room rather than pooling where it landed, that it stops short of the safe end,
// that it can be outrun, and that a reload puts the room back the way the player first met it.
public class PaintFluidTests(Node testScene) : TestClass(testScene) {
  private const float STEP_WIDTH = 480f;
  private const float STEP_DROP = 96f;
  private const float TOP = 200f;
  private const int STEPS = 5;

  private PaintFluid _fluid = default!;
  private Node2D _ground = default!;
  private FakeGameLevelProvider _level = default!;

  // A staircase down to the right, the shape the paint is authored against in the level.
  [Setup]
  public async Task Setup() {
    _level = new FakeGameLevelProvider();
    TestScene.AddChild(_level);
    _ground = new Node2D();
    TestScene.AddChild(_ground);
    for (var i = 0; i < STEPS; i++) {
      var step = SceneHelpers.InstantiateNode<FlatPlatform>();
      step.SnapToGrid = false;
      step.Group = FlatPlatform.NEUTRAL;
      step.Size = new Vector2(STEP_WIDTH, 600f);
      step.Position = new Vector2((i * STEP_WIDTH) + (STEP_WIDTH / 2f), TOP + (i * STEP_DROP) + 300f);
      _ground.AddChild(step);
    }

    _fluid = SceneHelpers.InstantiateNode<PaintFluid>();
    _fluid.Position = Vector2.Zero;
    _fluid.Width = STEP_WIDTH * STEPS;
    _fluid.Depth = 900f;
    _fluid.Group = ColorUtils.PURPLE;
    _fluid.SpoutOffset = new Vector2(120f, TOP);
    _fluid.SpoutWidth = 120f;
    _fluid.CameraZoom = 0f;
    _level.AddChild(_fluid);
    await PhysicsFrames.Frame(TestScene);
  }

  [Cleanup]
  public void Cleanup() {
    _fluid.QueueFree();
    _ground.QueueFree();
    _level.QueueFree();
  }

  // Nothing about the staircase is authored on the paint, so if the floor is not read off the
  // level every particle falls straight through the room and out of the bottom of it.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ItStaysOnTheFloorItWasPouredOnto() {
    _fluid.Pour();
    await PhysicsFrames.Advance(TestScene, 120);

    _fluid.ParticleCount.ShouldBeGreaterThan(0, "the paint fell straight through the level");
    _fluid.LowestY.ShouldBeLessThan(TOP + (STEPS * STEP_DROP) + 60f, "the paint is below the last step");
  }

  // The point of the set piece: paint poured at the top arrives at the bottom under its own
  // weight. Paint that pools where it lands is a puddle, and there is nothing to run from.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ItRunsDownhillAwayFromWhereItWasPoured() {
    _fluid.Pour();
    await PhysicsFrames.Advance(TestScene, 60);
    var early = _fluid.FrontX;

    await PhysicsFrames.Advance(TestScene, 180);

    _fluid.FrontX.ShouldBeGreaterThan(early + STEP_WIDTH, "the paint stayed where it was poured");
  }

  // The paint may not cross the end of what the level gave it: the stretch past the last of it is
  // the safe zone, and a flood that runs into that has no end.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ItStopsAtTheEndOfTheRunItWasGiven() {
    _fluid.PourRate = 900f;
    _fluid.PourDuration = 5f;
    _fluid.Pour();
    await PhysicsFrames.Advance(TestScene, 420);

    _fluid.FrontX.ShouldBeLessThanOrEqualTo(_fluid.Width, "the paint ran past the room");
  }

  // The coat the flood leaves is a painted surface, not a picture of one. A player who walks back
  // over dried paint in the wrong colour has to die to it exactly as they would on a platform
  // somebody inked, which the faces decide by asking what colour the surface is in.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task WhatItDriesOnIsAsLethalAsPaintSomebodyPutThere() {
    _fluid.Pour();
    await PhysicsFrames.Advance(TestScene, 240);

    var coats = _coatsOf(_fluid);
    coats.Count.ShouldBeGreaterThan(0, "the paint it left behind is only a picture");

    foreach (var coat in coats) {
      coat.IsInGroup(_fluid.Group).ShouldBeTrue("the coat is not the colour the paint was");
    }

    // Lying along the ground it dried on rather than floating over it: the staircase drops away to
    // the right, so a coat laid at one height would hang in the air further down.
    var last = coats[^1];
    var step = Mathf.FloorToInt(last.Position.X / STEP_WIDTH);
    last.Position.Y.ShouldBeInRange(
      TOP + (step * STEP_DROP), TOP + (step * STEP_DROP) + 60f, "the coat is not on the step");
  }

  // A retry starts at the top of the room with a full bucket, the same way the player first met
  // it. Paint left lying about would drown them where they respawn.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task AReloadPutsTheRoomBack() {
    _fluid.Pour();
    await PhysicsFrames.Advance(TestScene, 120);
    _fluid.ParticleCount.ShouldBeGreaterThan(0);

    EventHandler.Instance.EmitCheckpointLoaded();
    await PhysicsFrames.Frame(TestScene);

    _fluid.ParticleCount.ShouldBe(0, "the paint the player died to is still lying there");
    _fluid.IsRunning.ShouldBeFalse("the bucket is still pouring into a room nobody has entered");
    _coatsOf(_fluid).Count.ShouldBe(0, "the coat the player died to is still lethal");
  }

  // Unless the player got past it first. A checkpoint reached once the bucket has gone over says
  // the room was crossed, and a retry from there must not stand the bucket back up in a dry room:
  // the player would be made to outrun the flood a second time to reach the point the checkpoint
  // already granted them.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ACheckpointPastTheFloodDoesNotMakeThemOutrunItTwice() {
    _fluid.Pour();
    await PhysicsFrames.Advance(TestScene, 200);

    EventHandler.Instance.EmitCheckpointReached(Vector2.Zero, ColorUtils.PURPLE);
    var front = _fluid.FrontX;
    var coats = _coatsOf(_fluid).Count;
    front.ShouldBeGreaterThan(0f, "the flood had not started, so there was nothing to remember");
    coats.ShouldBeGreaterThan(0, "nothing had dried yet, so there was nothing to remember");

    await PhysicsFrames.Advance(TestScene, 120);
    EventHandler.Instance.EmitCheckpointLoaded();

    // Read before letting it run on, so this is what the reload put back rather than what the
    // flood had already done with it by the next tick.
    _fluid.IsRunning.ShouldBeTrue("the bucket is standing full again in a room it has already flooded");
    _fluid.FrontX.ShouldBe(front, 0.5f, "the flood came back to somewhere it had not reached");
    _coatsOf(_fluid).Count.ShouldBe(coats, "the paint it had already dried was washed off");

    // And it carries on from there rather than starting the pour again.
    await PhysicsFrames.Advance(TestScene, 60);
    _fluid.FrontX.ShouldBeGreaterThanOrEqualTo(front, "the flood went backwards after the reload");
  }

  // And the same again across the game being closed. Nothing that was in memory survives that, so
  // a room that only remembered its own state would greet a returning player with a full bucket
  // and a dry floor, and make them run the room a second time.
  //
  // The coat is not written down, only which ground wears it: how a column dries follows from
  // where it is and the seed, so it can be worked out again rather than stored.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task WhatItDriedOntoSurvivesTheGameBeingClosed() {
    _fluid.Pour();
    await PhysicsFrames.Advance(TestScene, 240);
    EventHandler.Instance.EmitCheckpointReached(Vector2.Zero, ColorUtils.PURPLE);
    var coats = _coatsOf(_fluid).Count;
    coats.ShouldBeGreaterThan(0);

    var serializer = new SimpleJsonSerializer();
    var written = serializer.Serialize(_fluid.Save(serializer));
    written.Length.ShouldBeLessThan(4000, "the room writes out more of itself than it needs to");

    // A room that has never heard of any of it, which is all a reopened game has.
    var reopened = SceneHelpers.InstantiateNode<PaintFluid>();
    reopened.Position = Vector2.Zero;
    reopened.Width = _fluid.Width;
    reopened.Depth = _fluid.Depth;
    reopened.Group = ColorUtils.PURPLE;
    reopened.SpoutOffset = _fluid.SpoutOffset;
    reopened.CameraZoom = 0f;
    _level.AddChild(reopened);
    await PhysicsFrames.Frame(TestScene);

    reopened.Load(serializer, serializer.Deserialize<string>(written)!);
    await PhysicsFrames.Advance(TestScene, 3);

    _coatsOf(reopened).Count.ShouldBe(coats, "the paint the flood had dried did not come back");

    // And the bucket stays empty. A room that came back dry-but-armed would flood itself again the
    // moment the returning player stepped into the trigger they had already been past.
    reopened.Pour();
    await PhysicsFrames.Advance(TestScene, 60);
    reopened.ParticleCount.ShouldBe(0, "the emptied bucket poured itself a second time");
    reopened.QueueFree();
  }

  // Whatever the flood has dried onto and left able to kill. Read off the node rather than exposed
  // by it, because nothing in the game asks the paint for this - the faces find it by touching it.
  private static List<Area2D> _coatsOf(PaintFluid fluid) =>
    fluid.GetChildren().OfType<Area2D>()
      .Where(area => area.Monitorable && (area.CollisionLayer & PhysicsLayers.Platform.Mask) != 0)
      .ToList();

  // What the chase is actually made of. Measured against the cube's own top speed rather than a
  // number of its own: the room is only fair while the player can pull away from it by running,
  // and the margin is what leaves them room to land a jump or set off a step late.
  //
  // Timed over the whole run rather than sampled across a moment of it. The front does not advance
  // evenly - it gathers behind each step until it has the depth to spill, then surges onto the next
  // one - so a short window lands on either the surge or the wait and reports whichever it caught.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ItsFrontTravelsAtAChaseableSpeed() {
    var finish = _fluid.Width * 0.9f;
    _fluid.Pour();

    var ticks = 0;
    while (ticks < 900 && _fluid.FrontX < finish) {
      await PhysicsFrames.Frame(TestScene);
      ticks++;
    }

    _fluid.FrontX.ShouldBeGreaterThanOrEqualTo(finish, "the paint never came down the room at all");

    // How long the cube needs for the same stretch at a dead run, which is what the flood has to
    // lose to. Read off its own speed so the room stays fair if the cube is ever made quicker.
    var cube = finish / Player.SPEED * 60f;
    GD.Print($"[PaintFluid] crossed {finish:F0}px in {ticks} ticks, cube needs {cube:F0}  " +
      $"margin x{ticks / cube:F2}  particles {_fluid.ParticleCount}");
    ticks.ShouldBeGreaterThan((int)(cube * 1.15f), "the paint outruns the cube at a dead run");
    ticks.ShouldBeLessThan((int)(cube * 4f), "the paint is too slow to be a chase");
  }
}
