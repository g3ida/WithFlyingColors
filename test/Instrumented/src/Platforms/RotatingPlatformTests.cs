namespace Wfc.test.instrumented.Platforms;

using System;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Platforms;
using Wfc.Utils;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using EventHandler = Wfc.Core.Event.EventHandler;

// A rotating platform is a floor that is a wall a second later. What it has to get right is which
// way round it is standing - on the first frame, on the frame the player respawns, and on the frame
// the arrow tells them which way it is about to go.
public class RotatingPlatformTests(Node testScene) : TestClass(testScene) {
  // Half a turn a second, so a full circle fits inside the timeout with room to spare.
  private const float SPEED = 180.0f;
  private const float SWEEP = 90.0f;
  private const float WAIT = 0.05f;

  // Long enough that half of it is many physics ticks, so "has not set off yet" is a real check.
  private const float DELAY = 0.4f;
  private const float START_X = 700.0f;
  private const float START_Y = 400.0f;

  // A tick of the fastest turn these tests run, which is what a platform parking on the tick it
  // reaches its end can overshoot by.
  private const float CLOSE = 4.0f;
  private const double TIMEOUT = 4.0;

  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";

  // Default, platform and fall zone: what the cube collides with in a level, minus the layers this
  // test has nothing on.
  private const uint PLAYER_MASK = 13;

  private RotatingPlatform _platform = default!;

  [Cleanup]
  public void Cleanup() => _platform.QueueFree();

  [Test]
  public async Task ItTurnsTheWayItIsPointed() {
    var platform = await _add();

    (await _waitFor(() => _degrees(platform) >= 45.0f))
      .ShouldBeTrue("the platform never turned");
    (await _waitFor(() => _degrees(platform) >= 135.0f))
      .ShouldBeTrue("the platform stopped partway through a turn it was never told to stop in");
  }

  [Test]
  public async Task ANegativeSpeedTurnsTheOtherWay() {
    var platform = await _add(p => p.Speed = -SPEED);

    (await _waitFor(() => _degrees(platform) <= -45.0f))
      .ShouldBeTrue("a negative speed turned the platform the way a positive one does");
  }

  [Test]
  public async Task ASweepGoesOutAndComesBack() {
    var platform = await _sweeping();

    (await _waitFor(() => _degrees(platform) >= SWEEP - CLOSE))
      .ShouldBeTrue("the platform never reached the far end of its sweep");
    (await _waitFor(() => _degrees(platform) <= CLOSE))
      .ShouldBeTrue("the platform never came back");
  }

  // The far end is a place a level wants a platform to be found at as readily as the near one - a
  // bar standing across a doorway that swings out of the way and back. How it is left in the editor
  // is how it stands on the first frame either way, so the sweep is measured back out of that
  // angle.
  [Test]
  public async Task APlatformLeftAtTheEndOfItsSweepStartsThereAndComesBackToIt() {
    var platform = await _sweeping(p => p.StartAt = PlatformSpin.SpinOrigin.End);

    (await _waitFor(() => _degrees(platform) <= -SWEEP + CLOSE))
      .ShouldBeTrue("the platform never turned out to the other end of its sweep");
    (await _waitFor(() => _degrees(platform) >= -CLOSE))
      .ShouldBeTrue("the platform never came back to the angle the level left it at");
  }

  // What staggers platforms that turn against each other: without it a row of them sets off
  // together and reads as one turning wall rather than as a pattern to be timed.
  [Test]
  public async Task AStartDelayHoldsThePlatformBackBeforeItsFirstTurn() {
    var platform = await _add(p => p.StartDelay = DELAY);

    await PhysicsFrames.Advance(TestScene, (int)(DELAY / 2.0f * Engine.PhysicsTicksPerSecond));
    _degrees(platform).ShouldBe(0.0f, CLOSE, "the platform set off before its delay was up");

    (await _waitFor(() => _degrees(platform) >= 45.0f))
      .ShouldBeTrue("the platform never set off once its delay was up");
  }

  // Nothing records how a platform was left standing until the player reaches a checkpoint, so a
  // death before the first one has to hand back the angle the level authored - not zero, and not
  // wherever the platform had got to.
  [Test]
  public async Task ARespawnBeforeAnyCheckpointPutsThePlatformBackTheWayTheLevelLeftIt() {
    var platform = await _sweeping();
    await _waitFor(() => _degrees(platform) > SWEEP / 2.0f);

    await _respawn(platform);

    _degrees(platform).ShouldBe(0.0f, CLOSE, "the platform came back at an angle the level never left it at");
  }

  // What the player is retrying is the jump they died on, so the platform has to be standing the way
  // it stood when they took the checkpoint - not at the top of its cycle.
  [Test]
  public async Task ARespawnHandsBackThePlatformAsTheCheckpointFoundIt() {
    var platform = await _sweeping();
    await _waitFor(() => _degrees(platform) > SWEEP / 2.0f);

    var atCheckpoint = _degrees(platform);
    EventHandler.Instance.EmitCheckpointReached(platform.GlobalPosition, "blue");
    await _waitFor(() => _degrees(platform) >= SWEEP - CLOSE);

    await _respawn(platform);

    _degrees(platform).ShouldBe(atCheckpoint, CLOSE, "the platform resumed from somewhere else in its cycle");
  }

  // A platform that is going nowhere costs a physics tick for every platform in the level, forever.
  [Test]
  public async Task AParkedPlatformStopsBeingTickedAtAll() {
    var platform = await _add(p => p.StartsStopped = true);
    await PhysicsFrames.Advance(TestScene, 3);

    platform.IsPhysicsProcessing().ShouldBeFalse("a parked platform is still being asked to turn every tick");

    platform.ResumeSpinner();
    platform.IsPhysicsProcessing().ShouldBeTrue("resuming left the platform parked");
  }

  // A door that swings open once. The sweep has four phases to stop in, so which one is asked for.
  [Test]
  public async Task AOneShotSweepStopsAtTheEndOfTheLegItIsGiven() {
    var platform = await _sweeping(p => {
      p.OneShot = true;
      p.OneShotPhase = PlatformSpin.SpinPhase.TurningForth;
    });

    (await _waitFor(() => _degrees(platform) >= SWEEP - CLOSE))
      .ShouldBeTrue("the one-shot platform never made its turn");
    await PhysicsFrames.Advance(TestScene, 40);

    _degrees(platform).ShouldBe(SWEEP, CLOSE, "a one-shot platform turned back the way it came");
  }

  // A platform going one way passes through only two phases, so one shot of it is a single leg.
  [Test]
  public async Task AOneShotOneWayPlatformMakesASingleLeg() {
    var platform = await _add(p => p.OneShot = true);

    (await _waitFor(() => !platform.IsPhysicsProcessing()))
      .ShouldBeTrue("the one-shot platform never stopped turning");

    _degrees(platform).ShouldBe(SWEEP, CLOSE, "a single leg left the platform standing somewhere else");
  }

  // What the wait is for: a surface that never holds an angle is a surface the player cannot be
  // asked to step onto. The bug this guards: the wait was read as belonging to the ends of a sweep,
  // so a platform going round turned without ever pausing.
  [Test]
  public async Task ItStandsStillAtTheEndOfEveryLeg() {
    var platform = await _add(p => p.WaitTime = 0.5f);

    (await _waitFor(() => _degrees(platform) >= SWEEP - CLOSE))
      .ShouldBeTrue("the platform never finished its first leg");
    var atRest = _degrees(platform);
    await PhysicsFrames.Advance(TestScene, (int)(0.25f * Engine.PhysicsTicksPerSecond));

    _degrees(platform).ShouldBe(atRest, CLOSE, "the platform ran the legs of its turn together");

    (await _waitFor(() => _degrees(platform) >= SWEEP + 45.0f))
      .ShouldBeTrue("the platform never set off on its next leg");
  }

  // A platform given no wait is a platform turning under its own power, and the legs it is counted
  // in are not the player's business: an arrival that costs it a tick is a stutter once a leg.
  [Test]
  public async Task NoWaitLeavesTheTurnUnbroken() {
    var platform = await _add(p => p.WaitTime = 0.0f);

    var ticks = Engine.PhysicsTicksPerSecond;
    await PhysicsFrames.Advance(TestScene, ticks);

    // A second of it, which at this speed is two whole legs and the arrivals between them.
    Mathf.RadToDeg(Mathf.AngleDifference(Mathf.DegToRad(SPEED), platform.GlobalRotation))
      .ShouldBe(0.0f, 2.0f * CLOSE, "the platform lost time at the end of a leg");
  }

  // The arrow rides round with the surface it is drawn on, which a curved arrow can do without ever
  // saying the wrong thing: it is mirroring that turns one round, not turning it.
  [Test]
  public async Task TheArrowRidesRoundWithTheSurface() {
    var platform = await _add();
    var arrow = platform.GetNode<RotatingPlatformArrow>("Arrow");

    await _waitFor(() => _degrees(platform) >= 45.0f);

    Mathf.RadToDeg(arrow.GlobalRotation).ShouldBe(
      _degrees(platform),
      CLOSE,
      "the arrow was left behind by the platform it is drawn on"
    );
  }

  // What the arrow is for: the player reads which way the surface is about to go while it is still
  // standing at the end of its last leg, rather than finding out once it has set off.
  [Test]
  public async Task TheArrowTurnsRoundBeforeTheSweepDoes() {
    var platform = await _sweeping();
    var arrow = platform.GetNode<RotatingPlatformArrow>("Arrow");
    arrow.FlipH.ShouldBeFalse("the arrow started off pointing against the way the platform turns");

    (await _waitFor(() => arrow.FlipH))
      .ShouldBeTrue("the arrow was still pointing out while the platform came back");
    _degrees(platform).ShouldBeGreaterThan(SWEEP / 2.0f, "the arrow turned round partway out");
  }

  [Test]
  public async Task ANegativeSpeedPointsTheArrowTheOtherWay() {
    var platform = await _add(p => p.Speed = -SPEED);

    platform.GetNode<RotatingPlatformArrow>("Arrow").FlipH.ShouldBeTrue(
      "the arrow pointed the way a clock goes on a platform that turns the other way"
    );
  }

  // The arrow has to be readable on the thin bars as well as the deep blocks - which means sized off
  // the platform, and never drawn bigger than it was drawn.
  [Test]
  public async Task TheArrowSitsInsideTheSurfaceWhateverShapeItIs() {
    foreach (var size in new[] { new Vector2(256f, 32f), new Vector2(192f, 16f), new Vector2(384f, 160f) }) {
      var platform = await _add(p => p.Size = size);
      var arrow = platform.GetNode<RotatingPlatformArrow>("Arrow");

      arrow.Scale.X.ShouldBeLessThanOrEqualTo(1f, $"the arrow was blown up past its own art on a {size} platform");
      var drawn = arrow.Texture.GetHeight() * arrow.Scale.Y;
      drawn.ShouldBeLessThanOrEqualTo(
        Mathf.Min(platform.Size.X, platform.Size.Y),
        $"the arrow hangs off the edge of a {size} platform"
      );

      Cleanup();
    }
  }

  // The rumble belongs to the turn, not to the platform: a level with a row of these standing
  // through their waits would otherwise hum continuously from the moment it loaded.
  [Test]
  public async Task ItRumblesOnlyWhileItIsActuallyTurning() {
    var platform = await _sweeping(p => p.WaitTime = 0.5f);
    var sound = platform.GetNode<AudioStreamPlayer2D>("Spin");
    sound.Playing.ShouldBeFalse("the platform was already sounding off while it stood waiting");

    (await _waitFor(() => _degrees(platform) > SWEEP / 2.0f)).ShouldBeTrue();
    sound.Playing.ShouldBeTrue("the platform turned its whole leg in silence");

    (await _waitFor(() => !sound.Playing))
      .ShouldBeTrue("the platform kept rumbling after it had stopped turning");
    _degrees(platform).ShouldBe(SWEEP, CLOSE, "the rumble stopped partway through the turn");
  }

  [Test]
  public async Task ASilencedPlatformNeverSoundsOff() {
    var platform = await _add(p => p.PlaySound = false);

    await _waitFor(() => _degrees(platform) >= 45.0f);

    platform.GetNode<AudioStreamPlayer2D>("Spin").Playing.ShouldBeFalse();
  }

  [Test]
  public async Task TurningTheArrowOffLeavesThePlainSurface() {
    var platform = await _add(p => p.ShowArrow = false);

    platform.GetNode<RotatingPlatformArrow>("Arrow").Visible.ShouldBeFalse();
  }

  // What the whole node is for. The player keeps their footing only while the surface is within a
  // few degrees of level, so a platform that turns under them and leaves them behind looks exactly
  // like one that works, right until the jump they lined up is a pixel short.
  [Test]
  public async Task ItCarriesThePlayerStandingOnIt() {
    // The cube resolves the game's services, and the platform resolves the level it is in for the
    // landing splash - so both are stood up before either of them is.
    var services = new FakeDependenciesProvider();
    TestScene.AddChild(services);
    var level = new FakeGameLevelProvider();
    services.AddChild(level);
    await PhysicsFrames.Frame(TestScene);

    _platform = SceneHelpers.InstantiateNode<RotatingPlatform>();
    _platform.Position = new Vector2(START_X, START_Y);
    _platform.Mode = PlatformSpin.SpinMode.BackAndForth;
    // Turning the far side of the bar up rather than down, so what carries the cube cannot be
    // confused with it falling. Slow and shallow: past a few degrees the surface stops counting as
    // a floor at all, which is the platform working as designed rather than dropping anyone.
    _platform.Speed = -5.0f;
    _platform.Sweep = 5.0f;
    // Long enough that the platform is still standing level when the cube arrives on it.
    _platform.WaitTime = 0.8f;
    // Whichever face lands, lands: which colour the cube happens to be showing is not what this is
    // about, and a platform it may not land on kills it instead.
    _platform.Group = FlatPlatform.NEUTRAL;
    level.AddChild(_platform);

    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    player.CollisionMask = PLAYER_MASK;
    // Well off the pivot, where the turn is a real distance rather than a couple of pixels.
    player.Position = new Vector2(START_X + 80.0f, START_Y - 100.0f);
    level.AddChild(player);

    (await _waitFor(player.IsOnFloor)).ShouldBeTrue("the cube never landed on the platform");
    var ridingFrom = player.GlobalPosition.Y;
    (await _waitFor(() => _degrees(_platform) <= -_platform.Sweep + 1.0f))
      .ShouldBeTrue("the platform never finished its sweep");

    player.IsDying().ShouldBeFalse("riding the platform killed the cube");
    player.GlobalPosition.Y.ShouldBeLessThan(
      ridingFrom - 3.0f,
      "the platform turned out from under the cube instead of carrying it"
    );

    services.QueueFree();
  }

  // The landing splash is drawn against the camera, which the platform asks its level for. This is
  // the one thing a rotating platform inherits rather than declares - and an injected dependency
  // that a subclass cannot resolve throws from inside the frame that the player lands on it.
  [Test]
  public async Task ItResolvesTheLevelItIsPlacedIn() {
    var level = new FakeGameLevelProvider();
    TestScene.AddChild(level);
    await PhysicsFrames.Frame(TestScene);

    _platform = SceneHelpers.InstantiateNode<RotatingPlatform>();
    level.AddChild(_platform);
    await PhysicsFrames.Frame(TestScene);

    _platform.GameLevel.ShouldNotBeNull("a rotating platform cannot reach the level it is standing in");
    level.QueueFree();
  }

  private static float _degrees(RotatingPlatform platform) => Mathf.RadToDeg(platform.GlobalRotation);

  private Task<RotatingPlatform> _add() => _add(_ => { });

  // Everything the turn is measured from is read as the platform enters the tree, and an
  // AnimatableBody2D already in the tree takes its transform from the physics server rather than
  // from whoever placed it - so both the placement and the turn are set up before it is added.
  private async Task<RotatingPlatform> _add(Action<RotatingPlatform> configure) {
    _platform = SceneHelpers.InstantiateNode<RotatingPlatform>();
    _platform.Position = new Vector2(START_X, START_Y);
    _platform.Speed = SPEED;
    _platform.WaitTime = WAIT;
    configure(_platform);

    TestScene.AddChild(_platform);
    await PhysicsFrames.Frame(TestScene);
    return _platform;
  }

  private Task<RotatingPlatform> _sweeping() => _sweeping(_ => { });

  private Task<RotatingPlatform> _sweeping(Action<RotatingPlatform> configure) => _add(p => {
    p.Mode = PlatformSpin.SpinMode.BackAndForth;
    p.Sweep = SWEEP;
    configure(p);
  });

  // A body already in the tree takes its transform from the physics server, so a platform put back
  // the way it belongs is only read there a tick later - by which time it has resumed its cycle and
  // turned on. Parking it on the way out is what leaves the respawn itself to be read.
  private async Task _respawn(RotatingPlatform platform) {
    EventHandler.Instance.EmitCheckpointLoaded();
    platform.StopSpinner(true);
    await PhysicsFrames.Advance(TestScene, 2);
  }

  private Task<bool> _waitFor(Func<bool> until) => PhysicsFrames.WaitFor(TestScene, until, TIMEOUT);
}
