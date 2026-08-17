namespace Wfc.test.instrumented.Platforms;

using System;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Event;
using Wfc.Entities.World.Platforms;
using Wfc.Utils;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;

// A sliding platform is a floor that is somewhere else a second later, and everything about it that
// can go wrong goes wrong quietly: it runs the wrong way, or it comes back from a death at the
// other end of its run with the player's jump already committed to where it used to be.
public class SlidingPlatformTests(Node testScene) : TestClass(testScene) {
  private const float SPEED = 6.0f;
  private const float DISTANCE = 240.0f;
  private const float WAIT = 0.05f;

  // Long enough that half of it is many physics ticks, so "has not set off yet" is a real check.
  private const float DELAY = 0.4f;
  private const float START_X = 700.0f;
  private const float START_Y = 400.0f;

  // Well inside a pixel, which is the smallest thing a platform standing still is read against.
  private const float CLOSE = 0.5f;
  private const double TIMEOUT = 4.0;

  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";

  // Default, platform and fall zone: what the cube collides with in a level, minus the layers this
  // test has nothing on.
  private const uint PLAYER_MASK = 13;

  private SlidingPlatform _platform = default!;

  [Cleanup]
  public void Cleanup() => _platform.QueueFree();

  [Test]
  public async Task ItRunsOutToTheFarEndAndComesBack() {
    var platform = await _add();
    var start = platform.GlobalPosition;

    (await _waitFor(() => platform.GlobalPosition.X >= start.X + DISTANCE - CLOSE))
      .ShouldBeTrue("the platform never reached the far end of its run");
    platform.GlobalPosition.Y.ShouldBe(start.Y, CLOSE, "a horizontal run wandered off its axis");

    (await _waitFor(() => platform.GlobalPosition.X <= start.X + CLOSE))
      .ShouldBeTrue("the platform never came back");
  }

  [Test]
  public async Task AVerticalRunGoesTheWayItIsPointed() {
    var platform = await _add(p => p.Axis = PlatformSlide.SlideAxis.Vertical);
    var start = platform.GlobalPosition;

    (await _waitFor(() => platform.GlobalPosition.Y >= start.Y + DISTANCE - CLOSE))
      .ShouldBeTrue("a vertical run never reached the far end");
    platform.GlobalPosition.X.ShouldBe(start.X, CLOSE, "a vertical run wandered off its axis");
  }

  // A negative distance is how a platform is authored to run left or up, rather than by placing it
  // at the far end and reasoning backwards.
  [Test]
  public async Task ANegativeDistanceRunsTheOtherWay() {
    var platform = await _add(p => p.Distance = -DISTANCE);
    var start = platform.GlobalPosition;

    (await _waitFor(() => platform.GlobalPosition.X <= start.X - DISTANCE + CLOSE))
      .ShouldBeTrue("a negative distance never took the platform back down its own axis");
  }

  // The far end is a place a level wants a platform to be found at as readily as the near one - a
  // lift that is up when the player arrives and comes down for them. Where it is placed is where it
  // stands on the first frame either way, so the run is measured back out of that spot instead.
  [Test]
  public async Task APlatformPlacedOnTheFarEndOfItsRunStartsThereAndComesBackToIt() {
    var platform = await _add(p => p.StartAt = PlatformSlide.SlideOrigin.End);
    var start = platform.GlobalPosition;

    (await _waitFor(() => platform.GlobalPosition.X <= start.X - DISTANCE + CLOSE))
      .ShouldBeTrue("the platform never ran out to the other end of its run");

    (await _waitFor(() => platform.GlobalPosition.X >= start.X - CLOSE))
      .ShouldBeTrue("the platform never came back to where the level placed it");
  }

  [Test]
  public async Task ARespawnPutsAPlatformPlacedOnTheFarEndBackOnThatEnd() {
    var platform = await _add(p => p.StartAt = PlatformSlide.SlideOrigin.End);
    var start = platform.GlobalPosition;
    await _waitFor(() => platform.GlobalPosition.X < start.X - (DISTANCE / 2.0f));

    await _respawn(platform);

    platform.GlobalPosition.X.ShouldBe(start.X, CLOSE, "the platform came back to the wrong end of its run");
  }

  // What staggers platforms that cross: without it a row of them set off together and read as one
  // moving wall rather than as a pattern to be timed.
  [Test]
  public async Task AStartDelayHoldsThePlatformBackBeforeItsFirstRun() {
    var platform = await _add(p => p.StartDelay = DELAY);
    var start = platform.GlobalPosition;

    // Past the wait a platform with no delay has, and halfway into the delay on top of it.
    await PhysicsFrames.Advance(TestScene, (int)((WAIT + (DELAY / 2.0f)) * Engine.PhysicsTicksPerSecond));
    platform.GlobalPosition.X.ShouldBe(start.X, CLOSE, "the platform set off before its delay was up");

    (await _waitFor(() => platform.GlobalPosition.X > start.X + CLOSE))
      .ShouldBeTrue("the platform never set off once its delay was up");
  }

  // The delay is what holds a set of crossing platforms apart, so a death has to hand it back with
  // them: one that came back without it would run in step with the platform it was staggered
  // against for the rest of the level.
  [Test]
  public async Task ARespawnBeforeAnyCheckpointOwesTheStartDelayAgain() {
    var platform = await _add(p => p.StartDelay = DELAY);
    var start = platform.GlobalPosition;
    await _waitFor(() => platform.GlobalPosition.X > start.X + (DISTANCE / 2.0f));

    GameEvents.Instance.OnCheckpointLoaded();
    await PhysicsFrames.Advance(TestScene, (int)((WAIT + (DELAY / 2.0f)) * Engine.PhysicsTicksPerSecond));

    platform.GlobalPosition.X.ShouldBe(start.X, CLOSE, "the respawn dropped the delay and set the platform off early");
  }

  // The bug this guards: nothing had recorded where a platform belonged until the player reached a
  // checkpoint, so the first death in a level moved every sliding platform in it to the origin -
  // out of the level, taking the floor the player was about to land on with it.
  [Test]
  public async Task ARespawnBeforeAnyCheckpointPutsThePlatformBackWhereTheLevelPutIt() {
    var platform = await _add();
    var start = platform.GlobalPosition;
    await _waitFor(() => platform.GlobalPosition.X > start.X + (DISTANCE / 2.0f));

    await _respawn(platform);

    platform.GlobalPosition.X.ShouldBe(start.X, CLOSE, "the platform came back somewhere the level never put it");
    platform.GlobalPosition.Y.ShouldBe(start.Y, CLOSE);
  }

  // What the player is retrying is the jump they died on, so the platform has to be back where it
  // was standing when they took the checkpoint - not at the top of its cycle.
  [Test]
  public async Task ARespawnHandsBackThePlatformAsTheCheckpointFoundIt() {
    var platform = await _add();
    var start = platform.GlobalPosition;
    await _waitFor(() => platform.GlobalPosition.X > start.X + (DISTANCE / 2.0f));

    var atCheckpoint = platform.GlobalPosition;
    GameEvents.Instance.OnCheckpointReached(atCheckpoint, "blue");
    await _waitFor(() => platform.GlobalPosition.X >= start.X + DISTANCE - CLOSE);

    await _respawn(platform);

    platform.GlobalPosition.X.ShouldBe(atCheckpoint.X, CLOSE, "the platform resumed from somewhere else in its cycle");
  }

  // The tetris pool and the brick breaker both lift the player into the arena and are then told to
  // stop at the end of that leg. A checkpoint taken while that stop is pending has to remember the
  // platform as already parked where the stop leaves it: a respawn that replayed the leg would
  // carry the player straight back out of the arena they died in.
  [Test]
  public async Task ACheckpointTakenWhileAStopIsPendingComesBackParkedWhereTheStopLeftIt() {
    var platform = await _add();
    var start = platform.GlobalPosition;
    await _waitFor(() => platform.GlobalPosition.X > start.X + (DISTANCE / 2.0f));

    platform.StopSlider(false);
    GameEvents.Instance.OnCheckpointReached(platform.GlobalPosition, "blue");
    await _waitFor(() => platform.GlobalPosition.X >= start.X + DISTANCE - CLOSE);

    GameEvents.Instance.OnCheckpointLoaded();
    await PhysicsFrames.Advance(TestScene, 30);

    platform.GlobalPosition.X.ShouldBe(
      start.X + DISTANCE,
      CLOSE,
      "the respawn set the platform running again and carried the player out of the arena"
    );
  }

  [Test]
  public async Task AOneShotPlatformStopsAtTheEndOfTheLegItIsGiven() {
    var platform = await _add(p => {
      p.OneShot = true;
      p.OneShotPhase = PlatformSlide.SlidePhase.SlidingForth;
    });
    var start = platform.GlobalPosition;

    (await _waitFor(() => platform.GlobalPosition.X >= start.X + DISTANCE - CLOSE))
      .ShouldBeTrue("the one-shot platform never made its run");
    await PhysicsFrames.Advance(TestScene, 40);

    platform.GlobalPosition.X.ShouldBe(start.X + DISTANCE, CLOSE, "a one-shot platform turned round and came back");
  }

  // A platform that is going nowhere costs a physics tick for every platform in the level, forever.
  [Test]
  public async Task AParkedPlatformStopsBeingTickedAtAll() {
    var platform = await _add(p => p.StartsStopped = true);
    await PhysicsFrames.Advance(TestScene, 3);

    platform.IsPhysicsProcessing().ShouldBeFalse("a parked platform is still being asked to move every tick");

    platform.ResumeSlider();
    platform.IsPhysicsProcessing().ShouldBeTrue("resuming left the platform parked");
  }

  // A centred box on a half-pixel puts its own edges between pixels, and the player walks into that
  // lip and stops dead against it. The far end of a run is walked off exactly like the near one.
  [Test]
  public async Task DistanceIsHeldToWholePixels() {
    var platform = await _add(p => p.Distance = 199.5f);

    platform.Distance.ShouldBe(200.0f);
  }

  [Test]
  public async Task TheTrackIsDrawnFromOneEndOfTheRunToTheOther() {
    var platform = await _add(p => p.Track = PlatformSlide.TrackDisplay.Always);
    var track = platform.GetNode<SlideTrack>("Track");

    track.Visible.ShouldBeTrue("a platform asked to show its track drew nothing");
    track.From.ShouldBe(new Vector2(START_X, START_Y));
    track.To.ShouldBe(new Vector2(START_X + DISTANCE, START_Y));
  }

  // What tells an author which way a platform they have just placed is going to go.
  [Test]
  public async Task TheTrackOfAPlatformPlacedOnTheFarEndIsDrawnBehindIt() {
    var platform = await _add(p => {
      p.StartAt = PlatformSlide.SlideOrigin.End;
      p.Track = PlatformSlide.TrackDisplay.Always;
    });
    var track = platform.GetNode<SlideTrack>("Track");

    track.From.ShouldBe(new Vector2(START_X - DISTANCE, START_Y));
    track.To.ShouldBe(new Vector2(START_X, START_Y));
  }

  // Drawn top level, or the track rides along with the platform it is supposed to be measuring.
  [Test]
  public async Task TheTrackStaysPutWhileThePlatformRunsAlongIt() {
    var platform = await _add(p => p.Track = PlatformSlide.TrackDisplay.Always);
    var track = platform.GetNode<SlideTrack>("Track");
    var start = platform.GlobalPosition;

    await _waitFor(() => platform.GlobalPosition.X > start.X + (DISTANCE / 2.0f));

    track.TopLevel.ShouldBeTrue();
    track.GlobalPosition.ShouldBe(Vector2.Zero, "the track was carried off by the platform running on it");
  }

  [Test]
  public async Task TheTrackIsAnAuthoringGizmoUnlessTheLevelAsksForIt() {
    var platform = await _add();

    platform.GetNode<SlideTrack>("Track").Visible.ShouldBeFalse(
      "the authoring gizmo was left on screen in the level"
    );
  }

  // The cog is the only thing that says a surface is going somewhere, so it has to be readable on
  // the thin ledges as well as the deep blocks - which means sized off the platform, and never
  // drawn bigger than it was drawn.
  [Test]
  public async Task TheGearSitsInsideTheSurfaceWhateverShapeItIs() {
    foreach (var size in new[] { new Vector2(256f, 32f), new Vector2(192f, 16f), new Vector2(384f, 160f) }) {
      var platform = await _add(p => p.Size = size);
      var gear = platform.GetNode<SlidingPlatformGear>("Gear");

      gear.Scale.X.ShouldBeLessThanOrEqualTo(1f, $"the cog was blown up past its own art on a {size} platform");
      var drawn = gear.Texture.GetHeight() * gear.Scale.Y;
      drawn.ShouldBeLessThanOrEqualTo(
        Mathf.Min(platform.Size.X, platform.Size.Y),
        $"the cog hangs off the edge of a {size} platform"
      );

      Cleanup();
    }
  }

  // The cog is what sells the platform as driven, and it only reads that way if it turns with the
  // travel and unwinds on the way back. The bug this guards: the turn was measured by reading the
  // body's position straight back after moving it, and a body that syncs to physics reports that
  // move a tick later - so the cog was handed a travel of zero on every tick of the run and sat
  // there while the platform slid out from under it.
  [Test]
  public async Task TheGearTurnsWithTheTravelAndUnwindsOnTheWayBack() {
    var platform = await _add();
    var gear = platform.GetNode<SlidingPlatformGear>("Gear");
    var start = platform.GlobalPosition;
    gear.Rotation.ShouldBe(0f);

    await _waitFor(() => platform.GlobalPosition.X >= start.X + DISTANCE - CLOSE);
    var turned = gear.Rotation;
    turned.ShouldBeGreaterThan(0f, "the cog stood still while the platform ran out");

    await _waitFor(() => platform.GlobalPosition.X < start.X + (DISTANCE / 2.0f));

    gear.Rotation.ShouldBeLessThan(turned, "the cog kept turning the same way on the return leg");
  }

  // The rumble belongs to the travel, not to the platform: a level with a row of these standing
  // through their waits would otherwise hum continuously from the moment it loaded.
  [Test]
  public async Task ItRumblesOnlyWhileItIsActuallyTravelling() {
    var platform = await _add();
    var sound = platform.GetNode<AudioStreamPlayer2D>("Slide");
    var start = platform.GlobalPosition;
    sound.Playing.ShouldBeFalse("the platform was already sounding off while it stood waiting");

    await _waitFor(() => platform.GlobalPosition.X > start.X + (DISTANCE / 2.0f));
    sound.Playing.ShouldBeTrue("the platform ran its whole leg in silence");

    (await _waitFor(() => !sound.Playing))
      .ShouldBeTrue("the platform kept rumbling after it had stopped moving");
    platform.GlobalPosition.X.ShouldBe(start.X + DISTANCE, CLOSE, "the rumble stopped somewhere mid-run");
  }

  [Test]
  public async Task ASilencedPlatformNeverSoundsOff() {
    var platform = await _add(p => p.PlaySound = false);
    var start = platform.GlobalPosition;

    await _waitFor(() => platform.GlobalPosition.X > start.X + (DISTANCE / 2.0f));

    platform.GetNode<AudioStreamPlayer2D>("Slide").Playing.ShouldBeFalse();
  }

  [Test]
  public async Task TurningTheGearOffLeavesThePlainSurface() {
    var platform = await _add(p => p.ShowGear = false);

    platform.GetNode<SlidingPlatformGear>("Gear").Visible.ShouldBeFalse();
  }

  // What the whole node is for. A platform that runs out from under the player rather than taking
  // them with it looks exactly like one that works, right until they are standing on air.
  [Test]
  public async Task ItCarriesThePlayerStandingOnIt() {
    // The cube resolves the game's services, and the platform resolves the level it is in for the
    // landing splash - so both are stood up before either of them is.
    var services = new FakeDependenciesProvider();
    TestScene.AddChild(services);
    var level = new FakeGameLevelProvider();
    services.AddChild(level);
    await PhysicsFrames.Frame(TestScene);

    _platform = SceneHelpers.InstantiateNode<SlidingPlatform>();
    _platform.Position = new Vector2(START_X, START_Y);
    _platform.Speed = SPEED;
    _platform.Distance = DISTANCE;
    // Long enough that the platform is still standing at its near end when the cube arrives on it.
    // At the usual wait it has crossed its whole run before the cube has finished falling, and
    // there is nothing left there to land on.
    _platform.WaitTime = 0.8f;
    // Whichever face lands, lands: which colour the cube happens to be showing is not what this is
    // about, and a platform it may not land on kills it instead.
    _platform.Group = FlatPlatform.NEUTRAL;
    level.AddChild(_platform);
    var start = _platform.GlobalPosition;

    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    player.CollisionMask = PLAYER_MASK;
    player.Position = new Vector2(start.X, start.Y - 100.0f);
    level.AddChild(player);

    (await _waitFor(player.IsOnFloor)).ShouldBeTrue("the cube never landed on the platform");
    var ridingFrom = player.GlobalPosition.X;
    await _waitFor(() => _platform.GlobalPosition.X > start.X + (DISTANCE / 2.0f));

    player.IsDying().ShouldBeFalse("riding the platform killed the cube");
    player.GlobalPosition.X.ShouldBeGreaterThan(
      ridingFrom + (DISTANCE / 4.0f),
      "the platform ran out from under the cube instead of carrying it"
    );

    services.QueueFree();
  }

  // The landing splash is drawn against the camera, which the platform asks its level for. This is
  // the one thing a sliding platform inherits rather than declares - and an injected dependency
  // that a subclass cannot resolve throws from inside the frame that the player lands on it.
  [Test]
  public async Task ItResolvesTheLevelItIsPlacedIn() {
    var level = new FakeGameLevelProvider();
    TestScene.AddChild(level);
    await PhysicsFrames.Frame(TestScene);

    _platform = SceneHelpers.InstantiateNode<SlidingPlatform>();
    level.AddChild(_platform);
    await PhysicsFrames.Frame(TestScene);

    _platform.GameLevel.ShouldNotBeNull("a sliding platform cannot reach the level it is standing in");
    level.QueueFree();
  }

  private Task<SlidingPlatform> _add() => _add(_ => { });

  // Everything the run is measured from is read as the platform enters the tree, and an
  // AnimatableBody2D already in the tree takes its transform from the physics server rather than
  // from whoever moved it - so both the placement and the run are set up before it is added.
  private async Task<SlidingPlatform> _add(Action<SlidingPlatform> configure) {
    _platform = SceneHelpers.InstantiateNode<SlidingPlatform>();
    _platform.Position = new Vector2(START_X, START_Y);
    _platform.Speed = SPEED;
    _platform.Distance = DISTANCE;
    _platform.WaitTime = WAIT;
    configure(_platform);

    TestScene.AddChild(_platform);
    await PhysicsFrames.Frame(TestScene);
    return _platform;
  }

  // A body already in the tree takes its transform from the physics server, so a platform put back
  // where it belongs is only read there a tick later - by which time it has resumed its run and
  // moved on. Parking it on the way out is what leaves the respawn itself to be read.
  private async Task _respawn(SlidingPlatform platform) {
    GameEvents.Instance.OnCheckpointLoaded();
    platform.StopSlider(true);
    await PhysicsFrames.Advance(TestScene, 2);
  }

  private Task<bool> _waitFor(Func<bool> until) => PhysicsFrames.WaitFor(TestScene, until, TIMEOUT);
}
