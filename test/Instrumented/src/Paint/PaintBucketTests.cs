namespace Wfc.test.instrumented.Paint;

using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Event;
using Wfc.Core.Serialization;
using Wfc.Entities.World.Paint;
using Wfc.Entities.World.Platforms;
using Wfc.Utils;
using Wfc.Utils.Colors;
using Wfc.Utils.Layers;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;

// A bucket is a puzzle piece the player moves by walking into it, and everything that then
// happens to it - where it turns over, where it lands, what colour that stretch of floor is
// afterwards - is what the puzzle is made of. None of it says anything on screen when it goes
// wrong: the bucket simply sits where it was, or paints a surface nobody has to cross.
public class PaintBucketTests(Node testScene) : TestClass(testScene) {
  private const float LEDGE_TOP = 400f;
  private const float LOWER_TOP = 800f;
  private const double TIMEOUT = 6.0;

  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";

  // Default, platform and fall zone: what the cube collides with in a level, minus the layers
  // this test has nothing on.
  private const uint PLAYER_MASK = 13;

  private FakeDependenciesProvider _services = default!;
  private FakeGameLevelProvider _level = default!;
  private PaintBucket _bucket = default!;

  [Setup]
  public async Task Setup() {
    _services = new FakeDependenciesProvider();
    TestScene.AddChild(_services);
    _level = new FakeGameLevelProvider();
    _services.AddChild(_level);
    await PhysicsFrames.Frame(TestScene);
  }

  [Cleanup]
  public void Cleanup() {
    _services.Input.ReleaseAll();
    _services.QueueFree();
  }

  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task WalkingIntoItShovesItAlong() {
    _ledge(-320f, 960f);
    var bucket = _standBucket(0f);
    var player = _playerAt(-200f, LEDGE_TOP - 120f);
    var startedAt = bucket.GlobalPosition.X;

    _services.Input.Press(Wfc.Core.Input.IInputManager.Action.MoveRight);
    await PhysicsFrames.Advance(TestScene, 90);

    bucket.GlobalPosition.X.ShouldBeGreaterThan(startedAt + 60f, "the cube walked into the bucket and nothing moved");
    bucket.IsUpright.ShouldBeTrue("the bucket turned over on flat ground");
    player.IsDying().ShouldBeFalse("pushing the bucket killed the cube");
  }

  // The shove has to be one continuous movement. The cube is held to the bucket's own pace for as
  // long as it lasts, and the bucket is moved before the cube each tick - without both, the cube
  // walks into the bucket, loses its speed to the collision, falls behind far enough to stop
  // touching it, and the pair judder along in fits and starts.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task TheShoveTravelsAtOneSpeedRatherThanInFitsAndStarts() {
    _ledge(-320f, 1600f);
    var bucket = _standBucket(0f);
    var player = _playerAt(-200f, LEDGE_TOP - 120f);

    _services.Input.Press(Wfc.Core.Input.IInputManager.Action.MoveRight);
    await PhysicsFrames.Advance(TestScene, 40);

    var slowest = float.MaxValue;
    var fastest = 0f;
    var last = bucket.GlobalPosition.X;
    for (var frame = 0; frame < 60; frame++) {
      await PhysicsFrames.Frame(TestScene);
      var travelled = bucket.GlobalPosition.X - last;
      last = bucket.GlobalPosition.X;
      slowest = Mathf.Min(slowest, travelled);
      fastest = Mathf.Max(fastest, travelled);
    }

    slowest.ShouldBeGreaterThan(fastest * 0.9f, "the bucket stalled partway through a shove");

    // Touching, not trailing. The cube is a tick of the shove behind the bucket and cannot be any
    // closer - the bucket moves first, and the cube is tested against where it was at the top of
    // the tick - so anything beyond that tick is a gap the player can see.
    var tick = 190f / Engine.PhysicsTicksPerSecond;
    var bucketHalfWidth = ((RectangleShape2D)bucket.GetNode<CollisionShape2D>("CollisionShape2D").Shape).Size.X / 2f;
    (bucket.GlobalPosition.X - player.GlobalPosition.X - player.GetCollisionHalfExtents().X - bucketHalfWidth)
      .ShouldBeLessThan(tick + 0.5f, "the cube is shoving the bucket from a distance");
  }

  // A dash is not a shove. The cube arrives at ten times walking pace, and a bucket that answered
  // to it by setting off at the pace of a walk would read as weighing nothing the cube can spend.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task DashingIntoItSendsItSkidding() {
    _ledge(-320f, 2400f);
    var bucket = _standBucket(0f);
    _playerAt(-200f, LEDGE_TOP - 120f);
    var startedAt = bucket.GlobalPosition.X;

    _services.Input.Press(Wfc.Core.Input.IInputManager.Action.MoveRight);
    await PhysicsFrames.Advance(TestScene, 20);
    _services.Input.Press(Wfc.Core.Input.IInputManager.Action.Dash);
    await PhysicsFrames.Advance(TestScene, 8);
    _services.Input.ReleaseAll();

    // Long after the cube has stopped: what the kick is worth is the slide it leaves behind.
    await PhysicsFrames.Advance(TestScene, 90);

    (bucket.GlobalPosition.X - startedAt).ShouldBeGreaterThan(400f, "a dash moved the bucket no further than a walk would");
    bucket.IsUpright.ShouldBeTrue("the bucket turned over on flat ground");
  }

  // The same kick at an edge. A bucket carrying a dash does not teeter on the corner and drop down
  // the wall - it leaves the ledge and comes down well out from it.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ABucketKickedOffALedgeIsThrownClearOfIt() {
    _ledge(-320f, 320f);
    _lowerRun(-320f, 2400f);
    var bucket = _standBucket(0f);
    _playerAt(-200f, LEDGE_TOP - 120f);

    _services.Input.Press(Wfc.Core.Input.IInputManager.Action.MoveRight);
    await PhysicsFrames.Advance(TestScene, 20);
    _services.Input.Press(Wfc.Core.Input.IInputManager.Action.Dash);
    await PhysicsFrames.Advance(TestScene, 8);
    _services.Input.ReleaseAll();

    (await PhysicsFrames.WaitFor(TestScene, () => bucket.IsSpilled, TIMEOUT))
      .ShouldBeTrue("the kicked bucket never came down");
    bucket.GlobalPosition.X.ShouldBeGreaterThan(500f, "the kicked bucket dropped down the wall it was kicked over");
  }

  // A bucket nobody is touching is level furniture. One that drifts would wander off the ledge
  // it was authored on and paint a surface the level never meant to colour.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ItStaysWhereItWasPutWhileNothingPushesIt() {
    _ledge(-320f, 960f);
    var bucket = _standBucket(0f);
    var placedAt = bucket.GlobalPosition;

    await PhysicsFrames.Advance(TestScene, 60);

    bucket.GlobalPosition.X.ShouldBe(placedAt.X, 0.5f, "the bucket drifted along the surface");
    bucket.GlobalPosition.Y.ShouldBe(placedAt.Y, 1.0f, "the bucket sank into the surface it was standing on");
  }

  // The whole mechanic in one run: shoved past the edge it is standing on, the bucket turns over
  // rather than sliding off level, comes down on what is below, and leaves that surface painted.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ShovedPastTheEdgeItTurnsOverAndPaintsWhatItLandsOn() {
    _ledge(-320f, 320f);
    var lower = _lowerRun(-320f, 1600f);
    var bucket = _standBucket(160f);
    _playerAt(0f, LEDGE_TOP - 120f);

    _services.Input.Press(Wfc.Core.Input.IInputManager.Action.MoveRight);
    (await PhysicsFrames.WaitFor(TestScene, () => bucket.IsSpilled, TIMEOUT))
      .ShouldBeTrue("the bucket never went over the edge it was shoved at");

    // Which quarter turn it comes down on is the drop's business - the taller the fall the further
    // it goes over - but a bucket that emptied itself is never still standing on its base.
    Mathf.Abs(Mathf.Wrap(bucket.Rotation, -Mathf.Pi, Mathf.Pi))
      .ShouldBeGreaterThan(Mathf.Pi / 4f, "the bucket emptied itself while standing upright");

    var splat = bucket.Splat;
    splat.ShouldNotBeNull("the bucket emptied itself and left no paint");
    splat!.Group.ShouldBe(bucket.Group);
    splat.GlobalPosition.Y.ShouldBe(LOWER_TOP, 4f, "the paint did not land on the surface the bucket broke over");
    splat.GetParent().ShouldBe(lower, "the paint is not carried by the surface it landed on");
  }

  // The paint is a surface of the bucket's colour and no other. A splat that answered to every
  // face would be scenery, and one that answered to the wrong face kills whoever crosses it on
  // the face the paint told them to be on.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ThePaintAnswersToTheBucketsColourAlone() {
    _ledge(-320f, 320f);
    _lowerRun(-320f, 1600f);
    var bucket = _standBucket(160f, ColorUtils.PINK);
    _playerAt(0f, LEDGE_TOP - 120f);

    _services.Input.Press(Wfc.Core.Input.IInputManager.Action.MoveRight);
    (await PhysicsFrames.WaitFor(TestScene, () => bucket.IsSpilled, TIMEOUT)).ShouldBeTrue();

    var area = bucket.Splat!.GetNode<Area2D>("Area2D");
    area.IsInGroup(ColorUtils.PINK).ShouldBeTrue("the pink face has nothing to cross the paint on");
    foreach (var group in ColorUtils.COLOR_GROUPS) {
      if (group != ColorUtils.PINK) {
        area.IsInGroup(group).ShouldBeFalse($"pink paint also answers to {group}");
      }
    }
  }

  // The open tin is a surface of its own colour: what is in it is paint, and standing in it on the
  // wrong face is the same mistake as standing on a platform of that colour. Emptying it takes
  // that away with the paint - what is left is a tin to climb on.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ThePaintInTheTinIsASurfaceUntilItIsEmptied() {
    _ledge(-320f, 320f);
    _lowerRun(-320f, 1600f);
    var bucket = _standBucket(160f, ColorUtils.YELLOW);
    _playerAt(0f, LEDGE_TOP - 120f);

    var paint = bucket.GetNode<Area2D>("PaintArea");
    paint.IsInGroup(ColorUtils.YELLOW).ShouldBeTrue("a full tin of yellow paint answers to no face");
    paint.Monitorable.ShouldBeTrue("the paint in the tin cannot be landed in");

    _services.Input.Press(Wfc.Core.Input.IInputManager.Action.MoveRight);
    (await PhysicsFrames.WaitFor(TestScene, () => bucket.IsSpilled, TIMEOUT)).ShouldBeTrue();

    paint.Monitorable.ShouldBeFalse("an empty tin still kills whoever climbs on it");
  }

  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task LandingInTheTinOnTheWrongFaceKillsTheCube() {
    var player = await _dropOntoTheTin(matching: false);

    (await PhysicsFrames.WaitFor(TestScene, player.IsDying, TIMEOUT))
      .ShouldBeTrue("the cube stood in a tin of paint it may not touch and lived");
  }

  // The other half of the same rule, and the one the level is built on: the tin is a step for
  // whoever comes down on it wearing its colour.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task LandingInTheTinOnItsOwnFaceIsSafe() {
    var player = await _dropOntoTheTin(matching: true);

    await PhysicsFrames.Advance(TestScene, 60);

    player.IsDying().ShouldBeFalse("the cube came down on its own colour and was killed by it");
    player.GlobalPosition.Y.ShouldBeLessThan(LEDGE_TOP - 60f, "the cube fell through the tin instead of standing on it");
  }

  // A bucket is part of the puzzle rather than part of the level, so dying takes the paint back
  // with it. Left standing, a reload would find the floor already painted and the bucket already
  // empty, and the puzzle could only be played once.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task AReloadStandsItBackUpAndTakesItsPaintWithIt() {
    _ledge(-320f, 320f);
    _lowerRun(-320f, 1600f);
    var bucket = _standBucket(160f);
    _playerAt(0f, LEDGE_TOP - 120f);
    var placedAt = bucket.GlobalPosition;

    _services.Input.Press(Wfc.Core.Input.IInputManager.Action.MoveRight);
    (await PhysicsFrames.WaitFor(TestScene, () => bucket.IsSpilled, TIMEOUT)).ShouldBeTrue();
    var splat = bucket.Splat!;

    _services.Input.ReleaseAll();
    GameEvents.Instance.OnCheckpointLoaded();
    await PhysicsFrames.Advance(TestScene, 4);

    bucket.IsSpilled.ShouldBeFalse("the reloaded bucket is still the empty one that fell");
    bucket.GlobalPosition.X.ShouldBe(placedAt.X, 0.5f, "the bucket was not put back where it was authored");
    bucket.GlobalPosition.Y.ShouldBe(placedAt.Y, 0.5f, "the bucket was not put back where it was authored");
    bucket.Rotation.ShouldBe(0f, 0.01f, "the bucket was left lying on its side");
    GodotObject.IsInstanceValid(splat).ShouldBeFalse("the paint it spilled outlived the reload");
  }

  // Unless the player got past it first. A checkpoint reached after the bucket has gone over says
  // the puzzle was solved, and solving it again is not what a retry is for - so the paint stays on
  // the floor and the tin stays lying where it emptied itself.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ACheckpointPastItLeavesTheTinDownAndThePaintWhereItFell() {
    _ledge(-320f, 320f);
    _lowerRun(-320f, 1600f);
    var bucket = _standBucket(160f);
    _playerAt(0f, LEDGE_TOP - 120f);

    _services.Input.Press(Wfc.Core.Input.IInputManager.Action.MoveRight);
    (await PhysicsFrames.WaitFor(TestScene, () => bucket.IsSpilled, TIMEOUT)).ShouldBeTrue();
    _services.Input.ReleaseAll();
    await PhysicsFrames.Advance(TestScene, 30);

    var splat = bucket.Splat!;
    var restedAt = bucket.GlobalPosition;
    var lay = bucket.Rotation;
    GameEvents.Instance.OnCheckpointReached(Vector2.Zero, ColorUtils.PURPLE);

    GameEvents.Instance.OnCheckpointLoaded();
    await PhysicsFrames.Advance(TestScene, 4);

    bucket.IsSpilled.ShouldBeTrue("the bucket filled itself back up past a checkpoint that said it had not");
    GodotObject.IsInstanceValid(splat).ShouldBeTrue("the paint it had already put down was taken away");
    bucket.GlobalPosition.X.ShouldBe(restedAt.X, 0.5f, "the emptied bucket was carried back up the level");
    bucket.GlobalPosition.Y.ShouldBe(restedAt.Y, 0.5f, "the emptied bucket was carried back up the level");
    bucket.Rotation.ShouldBe(lay, 0.01f, "the bucket stood itself back up");
  }

  // Paint that lands on something that moves goes with it, and so must the tin it came out of.
  // Left behind, an emptied bucket sits in mid-air over the gap the platform used to be in.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task AnEmptiedTinRidesTheSurfaceItCameDownOn() {
    _ledge(-320f, 320f);
    var carriage = _movingRun(-320f, 1600f);
    var bucket = _standBucket(160f);
    _playerAt(0f, LEDGE_TOP - 120f);

    _services.Input.Press(Wfc.Core.Input.IInputManager.Action.MoveRight);
    (await PhysicsFrames.WaitFor(TestScene, () => bucket.IsSpilled, TIMEOUT)).ShouldBeTrue();
    _services.Input.ReleaseAll();
    await PhysicsFrames.Advance(TestScene, 20);

    var rodeFrom = bucket.GlobalPosition.X;
    var carried = carriage.Position.X;
    carriage.Position += new Vector2(240f, 0f);
    await PhysicsFrames.Advance(TestScene, 4);

    bucket.GlobalPosition.X.ShouldBe(rodeFrom + 240f, 1f, "the tin stayed behind when the floor moved");
    bucket.Splat!.GlobalPosition.X.ShouldBe(
      bucket.GlobalPosition.X, 40f, "the tin and its paint came apart");

    // And what it is measured against went with it, so a reload can still place it.
    carriage.Position = new Vector2(carried, carriage.Position.Y);
    await PhysicsFrames.Advance(TestScene, 4);
    bucket.GlobalPosition.X.ShouldBe(rodeFrom, 1f, "the tin did not come back with the floor");
  }

  // Which is what makes the save work on one: the position it is remembered at is the one it had
  // on the platform, so putting the platform back puts the bucket back with it.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ACheckpointRemembersWhereOnAMovingFloorItCameToRest() {
    _ledge(-320f, 320f);
    var carriage = _movingRun(-320f, 1600f);
    var bucket = _standBucket(160f);
    _playerAt(0f, LEDGE_TOP - 120f);

    _services.Input.Press(Wfc.Core.Input.IInputManager.Action.MoveRight);
    (await PhysicsFrames.WaitFor(TestScene, () => bucket.IsSpilled, TIMEOUT)).ShouldBeTrue();
    _services.Input.ReleaseAll();
    await PhysicsFrames.Advance(TestScene, 20);

    var startedAt = carriage.Position;
    var rode = bucket.GlobalPosition.X - carriage.Position.X;
    GameEvents.Instance.OnCheckpointReached(Vector2.Zero, ColorUtils.PURPLE);

    // The platform wanders off and the player dies out there. A reload puts the platform back to
    // where it starts, and the bucket has to come back to the same place on it.
    carriage.Position += new Vector2(400f, 0f);
    await PhysicsFrames.Advance(TestScene, 4);
    GameEvents.Instance.OnCheckpointLoaded();
    carriage.Position = startedAt;
    await PhysicsFrames.Advance(TestScene, 4);

    bucket.IsSpilled.ShouldBeTrue("the reload filled the tin back up past a checkpoint");
    (bucket.GlobalPosition.X - carriage.Position.X)
      .ShouldBe(rode, 1f, "the tin came back somewhere else on the platform");
  }

  // Paint cannot hold itself up past the end of what it is lying on. Emptied over a shelf narrower
  // than the splash, the coat is cut to the shelf rather than making the platform look wider than
  // it is - which reads as floor the cube can stand on and drops it through.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task PaintIsCutToTheSurfaceItLandsOnRatherThanOverhangingIt() {
    // Dropped straight onto the shelf rather than shoved at it, so the paint lands in the middle
    // of it and what this measures is the cut rather than where the bucket happened to come down.
    var shelf = _platform(420f, 580f, LOWER_TOP);
    var bucket = _standBucket(500f);

    (await PhysicsFrames.WaitFor(TestScene, () => bucket.IsSpilled, TIMEOUT)).ShouldBeTrue();

    var coat = bucket.Splat!;
    coat.Width.ShouldBeLessThanOrEqualTo(shelf.Size.X + 1f, "the coat is wider than the shelf under it");
    (coat.GlobalPosition.X - (coat.Width / 2f))
      .ShouldBeGreaterThanOrEqualTo(shelf.Position.X - (shelf.Size.X / 2f) - 1f, "the coat hangs off the left end");
    (coat.GlobalPosition.X + (coat.Width / 2f))
      .ShouldBeLessThanOrEqualTo(shelf.Position.X + (shelf.Size.X / 2f) + 1f, "the coat hangs off the right end");
  }

  // And what went past the ends has to go somewhere. Poured over a shelf with a floor beneath it,
  // the rest falls and coats the floor, the way paint tipped over the end of a plank does.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task WhatRunsOffTheEndsLandsOnWhateverIsBelow() {
    var shelf = _platform(420f, 580f, LOWER_TOP);
    var floor = _platform(-320f, 1600f, LOWER_TOP + 400f);
    var bucket = _standBucket(500f);

    (await PhysicsFrames.WaitFor(TestScene, () => bucket.IsSpilled, TIMEOUT)).ShouldBeTrue();
    await PhysicsFrames.Advance(TestScene, 4);

    var below = floor.GetChildren().OfType<PaintSplat>().ToList();
    below.Count.ShouldBe(2, "the paint that ran off the shelf did not come down on the floor under it");
    foreach (var run in below) {
      run.GlobalPosition.Y.ShouldBe(LOWER_TOP + 400f, 2f, "the spill did not land on the floor");
      // Past the shelf, which is the only reason it fell at all.
      var clearOfShelf =
        run.GlobalPosition.X + (run.Width / 2f) <= shelf.Position.X - (shelf.Size.X / 2f) + 1f
        || run.GlobalPosition.X - (run.Width / 2f) >= shelf.Position.X + (shelf.Size.X / 2f) - 1f;
      clearOfShelf.ShouldBeTrue("the spill came down under the shelf it was supposed to have missed");
    }
  }

  // Closing the game and opening it again is not a death: nothing that was in memory survives it,
  // so a bucket that only remembered the nodes it had spilled would come back full with the floor
  // clean, and the puzzle would be there to be solved a second time.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task WhatItSpilledSurvivesTheGameBeingClosed() {
    _ledge(-320f, 320f);
    var lower = _lowerRun(-320f, 1600f);
    var bucket = _standBucket(160f);
    _playerAt(0f, LEDGE_TOP - 120f);

    _services.Input.Press(Wfc.Core.Input.IInputManager.Action.MoveRight);
    (await PhysicsFrames.WaitFor(TestScene, () => bucket.IsSpilled, TIMEOUT)).ShouldBeTrue();
    _services.Input.ReleaseAll();
    await PhysicsFrames.Advance(TestScene, 30);
    GameEvents.Instance.OnCheckpointReached(Vector2.Zero, ColorUtils.PURPLE);

    var restedAt = bucket.GlobalPosition;
    var painted = lower.GetChildren().OfType<PaintSplat>().Select(s => s.GlobalPosition.X).ToList();
    painted.Count.ShouldBeGreaterThan(0);

    // Written out, then the level torn down and built again, and the entry matched to the new
    // bucket the way the slot matches it: by the name it is filed under. Loading back into the
    // same node would prove only that the writing round-trips, not that a reopened game finds it.
    var serializer = new SimpleJsonSerializer();
    var written = bucket.Save(serializer);
    var filedAs = bucket.GetSaveId();
    foreach (var splat in lower.GetChildren().OfType<PaintSplat>()) {
      splat.Free();
    }
    bucket.Free();

    var reopened = SceneHelpers.InstantiateNode<PaintBucket>();
    reopened.Group = ColorUtils.PURPLE;
    reopened.Position = new Vector2(160f, LEDGE_TOP);
    _level.AddChild(reopened);
    await PhysicsFrames.Frame(TestScene);

    reopened.GetSaveId().ShouldBe(filedAs, "the rebuilt bucket answers to a different name");
    reopened.Load(serializer, written);
    await PhysicsFrames.Advance(TestScene, 4);
    bucket = reopened;

    bucket.IsSpilled.ShouldBeTrue("the reopened game filled the tin back up");
    bucket.GlobalPosition.X.ShouldBe(restedAt.X, 1f, "the tin came back somewhere else");
    bucket.GlobalPosition.Y.ShouldBe(restedAt.Y, 1f, "the tin came back somewhere else");

    var back = lower.GetChildren().OfType<PaintSplat>().Select(s => s.GlobalPosition.X).ToList();
    back.Count.ShouldBe(painted.Count, "the paint it had spilled did not come back");
    for (var i = 0; i < back.Count; i++) {
      back[i].ShouldBe(painted[i], 1f, "the paint came back somewhere else along the floor");
    }
  }

  // What a saved entry is filed under has to be what the bucket answers to in a level that has
  // just been built. An emptied bucket moves in the tree - it goes to live under whatever caught
  // it, so that it rides - and a name taken from where it is now is a name nothing in the new
  // level has, so the entry is read back, matched against nothing, and silently dropped.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ItIsFiledUnderTheNameItWillHaveWhenTheLevelIsBuiltAgain() {
    _ledge(-320f, 320f);
    _lowerRun(-320f, 1600f);
    var bucket = _standBucket(160f);
    _playerAt(0f, LEDGE_TOP - 120f);
    var authored = bucket.GetSaveId();

    _services.Input.Press(Wfc.Core.Input.IInputManager.Action.MoveRight);
    (await PhysicsFrames.WaitFor(TestScene, () => bucket.IsSpilled, TIMEOUT)).ShouldBeTrue();
    _services.Input.ReleaseAll();
    await PhysicsFrames.Advance(TestScene, 30);

    bucket.GetSaveId().ShouldBe(authored, "the spilled bucket saves under a name it will not have again");
  }

  // A floor that moves. An AnimatableBody2D on the platform layer is a platform as far as the
  // bucket is concerned - it finds what it lands on by asking the physics server, not the level.
  private AnimatableBody2D _movingRun(float left, float right) {
    var carriage = new AnimatableBody2D {
      CollisionLayer = PhysicsLayers.Platform.Mask,
      Position = new Vector2((left + right) / 2f, LOWER_TOP + 128f),
      SyncToPhysics = true,
    };
    carriage.AddChild(new CollisionShape2D {
      Shape = new RectangleShape2D { Size = new Vector2(right - left, 256f) },
    });
    _level.AddChild(carriage);
    return carriage;
  }

  // Drops the cube into an open tin, painted either the colour it has facing down or one of the
  // three it has not.
  private async Task<Wfc.Entities.World.Player.Player> _dropOntoTheTin(bool matching) {
    _ledge(-320f, 320f);
    var bucket = _standBucket(0f);
    var player = _playerAt(0f, LEDGE_TOP - 360f);
    await PhysicsFrames.Advance(TestScene, 2);

    var facingDown = _colorFacingDownOf(player);
    bucket.Group = matching
      ? facingDown
      : System.Array.Find(ColorUtils.COLOR_GROUPS, group => group != facingDown)!;
    await PhysicsFrames.Frame(TestScene);
    return player;
  }

  private static string _colorFacingDownOf(Wfc.Entities.World.Player.Player player) {
    foreach (var group in ColorUtils.COLOR_GROUPS) {
      if (player.WearsColorToward(group, Vector2.Down)) {
        return group;
      }
    }
    return ColorUtils.BLUE;
  }

  private FlatPlatform _ledge(float left, float right) => _platform(left, right, LEDGE_TOP);

  private FlatPlatform _lowerRun(float left, float right) => _platform(left, right, LOWER_TOP);

  private FlatPlatform _platform(float left, float right, float top) {
    var platform = SceneHelpers.InstantiateNode<FlatPlatform>();
    platform.SnapToGrid = false;
    platform.Size = new Vector2(right - left, 256f);
    platform.Position = new Vector2((left + right) / 2f, top + 128f);
    platform.Group = FlatPlatform.NEUTRAL;
    _level.AddChild(platform);
    return platform;
  }

  private PaintBucket _standBucket(float x, string group = ColorUtils.PURPLE) {
    _bucket = SceneHelpers.InstantiateNode<PaintBucket>();
    _bucket.Group = group;
    _bucket.Position = new Vector2(x, LEDGE_TOP);
    _level.AddChild(_bucket);
    return _bucket;
  }

  private Wfc.Entities.World.Player.Player _playerAt(float x, float y) {
    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    player.CollisionMask = PLAYER_MASK;
    player.Position = new Vector2(x, y);
    _level.AddChild(player);
    _level.PlayerNode = player;
    return player;
  }
}
