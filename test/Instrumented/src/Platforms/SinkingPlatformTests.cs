namespace Wfc.test.instrumented.Platforms;

using System;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World;
using Wfc.Entities.World.Platforms;
using Wfc.Utils;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using EventHandler = Wfc.Core.Event.EventHandler;

// A sinking platform is a floor that answers being stood on, so nothing about it can be read from
// the platform alone: every one of these puts the cube on it and watches what the pair of them do.
// The way it fails is the way it would be shipped - a step that holds fast, or one that sinks and
// leaves the player standing on the air above it.
public class SinkingPlatformTests(Node testScene) : TestClass(testScene) {
  private const float DEPTH = 96.0f;
  private const float SINK_SPEED = 3.0f;
  private const float RISE_SPEED = 6.0f;
  private const float DELAY = 0.4f;
  private const float START_X = 700.0f;
  private const float START_Y = 400.0f;

  // Well inside a pixel, which is the smallest thing a platform standing still is read against.
  private const float CLOSE = 0.5f;

  // A cube being carried gives and takes a tick of the platform's own travel, which is what the
  // solver settles it back out of on the tick after.
  private const float RIDE_SLACK = 8.0f;
  private const double TIMEOUT = 4.0;

  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";

  // Default, platform and fall zone: what the cube collides with in a level, minus the layers this
  // test has nothing on.
  private const uint PLAYER_MASK = 13;

  private SinkingPlatform _platform = default!;
  private Node? _services;

  [Cleanup]
  public void Cleanup() {
    _platform.QueueFree();
    _services?.QueueFree();
    _services = null;
  }

  // The whole point of the platform, and the one thing that cannot be read off its own position: the
  // cube has to be carried down rather than left standing where the surface used to be.
  [Test]
  public async Task ItSinksUnderTheCubeAndTakesItDown() {
    var (platform, player) = await _addRidden();
    // How far the cube is standing above the platform, rather than where either of them is: the
    // platform is already giving by the time the cube has finished landing on it, so what says it is
    // being carried is that the two of them keep the same gap the whole way down.
    var carried = player.GlobalPosition.Y - platform.GlobalPosition.Y;
    var from = platform.GlobalPosition.Y;

    (await _waitFor(() => platform.GlobalPosition.Y >= START_Y + DEPTH - CLOSE))
      .ShouldBeTrue("the platform held fast under the cube");

    platform.GlobalPosition.Y.ShouldBeGreaterThan(from + (DEPTH / 2.0f), "there was no ride left to read");
    player.IsDying().ShouldBeFalse("riding the platform down killed the cube");
    (player.GlobalPosition.Y - platform.GlobalPosition.Y).ShouldBe(
      carried,
      RIDE_SLACK,
      "the platform sank out from under the cube instead of taking it down"
    );
  }

  // What the depth is for: a step gives way, it does not drop the player into the level below.
  [Test]
  public async Task ItGivesNoFurtherThanItsDepth() {
    var (platform, _) = await _addRidden();

    (await _waitFor(() => platform.GlobalPosition.Y >= START_Y + DEPTH - CLOSE))
      .ShouldBeTrue("the platform never gave all the way");
    await PhysicsFrames.Advance(TestScene, 30);

    platform.GlobalPosition.Y.ShouldBe(START_Y + DEPTH, CLOSE, "the platform kept sinking past its depth");
  }

  [Test]
  public async Task ItComesBackUpOnceItIsLeftAlone() {
    var (platform, player) = await _addRidden();
    await _waitFor(() => platform.GlobalPosition.Y > START_Y + (DEPTH / 2.0f));

    await _leave(player);

    (await _waitFor(() => platform.GlobalPosition.Y <= START_Y + CLOSE))
      .ShouldBeTrue("the platform stayed down after the cube left it");
    await PhysicsFrames.Advance(TestScene, 10);
    platform.GlobalPosition.Y.ShouldBe(START_Y, CLOSE, "the platform came back past where the level put it");
  }

  // What makes a run of these a stair that has to be taken forwards: a step the player has just left
  // is not there to be stepped back onto.
  [Test]
  public async Task ARiseDelayHoldsItDownAfterTheCubeLeaves() {
    var (platform, player) = await _addRidden(p => p.RiseDelay = DELAY);
    await _waitFor(() => platform.GlobalPosition.Y >= START_Y + DEPTH - CLOSE);

    await _leave(player);
    await PhysicsFrames.Advance(TestScene, (int)(DELAY / 2.0f * Engine.PhysicsTicksPerSecond));

    platform.GlobalPosition.Y.ShouldBe(START_Y + DEPTH, CLOSE, "the platform came back before its delay was up");

    (await _waitFor(() => platform.GlobalPosition.Y <= START_Y + CLOSE))
      .ShouldBeTrue("the platform never came back once its delay was up");
  }

  // The player retries the run they died on, which means the stair standing as they first found it.
  [Test]
  public async Task ARespawnPutsItBackAtItsRest() {
    var (platform, player) = await _addRidden();
    await _waitFor(() => platform.GlobalPosition.Y > START_Y + (DEPTH / 2.0f));

    await _leave(player);
    EventHandler.Instance.EmitCheckpointLoaded();
    await PhysicsFrames.Advance(TestScene, 2);

    platform.GlobalPosition.Y.ShouldBe(START_Y, CLOSE, "the respawn left the platform partway through its give");
  }

  // A cube dying on a step spends its death on the spot, and a step that went on giving under it
  // would take the ground out from under the death it is being shown.
  [Test]
  public async Task ADyingCubeIsNoLongerWeightOnIt() {
    var (platform, player) = await _addRidden();
    await _waitFor(() => platform.GlobalPosition.Y > START_Y + (DEPTH / 4.0f));

    EventHandler.Instance.EmitPlayerDying(platform, player.GlobalPosition, EntityType.Lazer);
    (await _waitFor(player.IsDying)).ShouldBeTrue("the cube never took the death it was dealt");
    var sunkTo = platform.GlobalPosition.Y;
    await PhysicsFrames.Advance(TestScene, 5);

    platform.GlobalPosition.Y.ShouldBeLessThanOrEqualTo(sunkTo, "the platform went on sinking under a dying cube");
  }

  // A platform nobody is standing on costs a physics tick for every platform in the level, forever.
  [Test]
  public async Task APlatformWithNothingOnItStopsBeingTickedAtAll() {
    var platform = await _add();
    await PhysicsFrames.Advance(TestScene, 3);

    platform.GlobalPosition.Y.ShouldBe(START_Y, CLOSE, "the platform gave way with nothing standing on it");
    platform.IsPhysicsProcessing().ShouldBeFalse("a platform at rest is still being asked to move every tick");
  }

  // The strip that notices the player is not what holds them up: it stands proud of the surface so
  // that a platform sinking faster than the cube falls does not lose sight of it, and inside the
  // platform's sides so that brushing past one on the way down does not sink it.
  [Test]
  public async Task TheRideStripStandsProudOfTheSurfaceAndInsideItsSides() {
    var platform = await _add(p => p.Size = new Vector2(192.0f, 64.0f));
    var strip = platform.GetNode<CollisionShape2D>("RideArea/RideAreaShape");
    var size = ((RectangleShape2D)strip.Shape).Size;
    var surface = -platform.Size.Y / 2.0f;

    size.X.ShouldBeLessThan(platform.Size.X, "the strip reaches the platform's own sides");
    (strip.Position.Y - (size.Y / 2.0f)).ShouldBeLessThan(surface, "the strip does not stand above the surface");
    (strip.Position.Y + (size.Y / 2.0f)).ShouldBeGreaterThan(surface, "the strip does not reach a body resting on the surface");
  }

  [Test]
  public async Task TheTrackIsDrawnFromTheRestDownToTheBottomOfTheGive() {
    var platform = await _add(p => p.Track = PlatformSlide.TrackDisplay.Always);
    var track = platform.GetNode<SlideTrack>("Track");

    track.Visible.ShouldBeTrue("a platform asked to show its give drew nothing");
    track.From.ShouldBe(new Vector2(START_X, START_Y));
    track.To.ShouldBe(new Vector2(START_X, START_Y + DEPTH));
  }

  [Test]
  public async Task TheTrackIsAnAuthoringGizmoUnlessTheLevelAsksForIt() {
    var platform = await _add();

    platform.GetNode<SlideTrack>("Track").Visible.ShouldBeFalse(
      "the authoring gizmo was left on screen in the level"
    );
  }

  // The cog is the only thing that says a surface will not hold, so it has to turn with the give and
  // unwind as the platform comes back.
  [Test]
  public async Task TheGearTurnsWithTheGiveAndUnwindsOnTheWayBack() {
    var (platform, player) = await _addRidden();
    var gear = platform.GetNode<SlidingPlatformGear>("Gear");

    await _waitFor(() => platform.GlobalPosition.Y >= START_Y + DEPTH - CLOSE);
    var turned = gear.Rotation;
    turned.ShouldBeGreaterThan(0.0f, "the cog stood still while the platform gave way");

    await _leave(player);
    await _waitFor(() => platform.GlobalPosition.Y < START_Y + (DEPTH / 2.0f));

    gear.Rotation.ShouldBeLessThan(turned, "the cog kept turning the same way as the platform came back");
  }

  // The rumble belongs to the movement: a platform sat at the bottom of its give under the player's
  // feet is as still as one nobody has touched.
  [Test]
  public async Task ItRumblesOnlyWhileItIsActuallyGiving() {
    var (platform, _) = await _addRidden();
    var sound = platform.GetNode<AudioStreamPlayer2D>("Sink");

    (await _waitFor(() => sound.Playing)).ShouldBeTrue("the platform gave way in silence");

    (await _waitFor(() => !sound.Playing)).ShouldBeTrue("the platform kept rumbling once it had nowhere left to give");
    platform.GlobalPosition.Y.ShouldBe(START_Y + DEPTH, CLOSE, "the rumble stopped partway through the give");
  }

  [Test]
  public async Task ASilencedPlatformNeverSoundsOff() {
    var (platform, _) = await _addRidden(p => p.PlaySound = false);
    await _waitFor(() => platform.GlobalPosition.Y > START_Y + (DEPTH / 2.0f));

    platform.GetNode<AudioStreamPlayer2D>("Sink").Playing.ShouldBeFalse();
  }

  [Test]
  public async Task TurningTheGearOffLeavesThePlainSurface() {
    var platform = await _add(p => p.ShowGear = false);

    platform.GetNode<SlidingPlatformGear>("Gear").Visible.ShouldBeFalse();
  }

  // Held to whole pixels, or the bottom of the give puts the platform's own edges between pixels and
  // the player walks into that lip and stops dead against it.
  [Test]
  public async Task DepthIsHeldToWholePixels() {
    var platform = await _add(p => p.Depth = 95.5f);

    platform.Depth.ShouldBe(96.0f);
  }

  // The landing splash is drawn against the camera, which the platform asks its level for. An
  // injected dependency a subclass cannot resolve throws from inside the frame the cube lands on it.
  [Test]
  public async Task ItResolvesTheLevelItIsPlacedIn() {
    var level = new FakeGameLevelProvider();
    TestScene.AddChild(level);
    _services = level;
    await PhysicsFrames.Frame(TestScene);

    _platform = SceneHelpers.InstantiateNode<SinkingPlatform>();
    level.AddChild(_platform);
    await PhysicsFrames.Frame(TestScene);

    _platform.GameLevel.ShouldNotBeNull("a sinking platform cannot reach the level it is standing in");
  }

  private Task<SinkingPlatform> _add() => _add(_ => { });

  // Everything the give is measured from is read as the platform enters the tree, and an
  // AnimatableBody2D already in the tree takes its transform from the physics server rather than from
  // whoever moved it - so both the placement and the give are set up before it is added.
  private async Task<SinkingPlatform> _add(Action<SinkingPlatform> configure) {
    _platform = _make(configure);
    TestScene.AddChild(_platform);
    await PhysicsFrames.Frame(TestScene);
    return _platform;
  }

  private Task<(SinkingPlatform, Wfc.Entities.World.Player.Player)> _addRidden() => _addRidden(_ => { });

  // The cube resolves the game's services and the platform resolves the level it is in for the
  // landing splash, so both are stood up before either of them is.
  private async Task<(SinkingPlatform, Wfc.Entities.World.Player.Player)> _addRidden(Action<SinkingPlatform> configure) {
    var services = new FakeDependenciesProvider();
    TestScene.AddChild(services);
    _services = services;
    var level = new FakeGameLevelProvider();
    services.AddChild(level);
    await PhysicsFrames.Frame(TestScene);

    _platform = _make(configure);
    // Whichever face lands, lands: which colour the cube happens to be showing is not what any of
    // this is about, and a platform it may not land on kills it instead.
    _platform.Group = FlatPlatform.NEUTRAL;
    level.AddChild(_platform);

    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    player.CollisionMask = PLAYER_MASK;
    player.Position = new Vector2(START_X, START_Y - 100.0f);
    level.AddChild(player);

    (await _waitFor(player.IsOnFloor)).ShouldBeTrue("the cube never landed on the platform");
    return (_platform, player);
  }

  private static SinkingPlatform _make(Action<SinkingPlatform> configure) {
    var platform = SceneHelpers.InstantiateNode<SinkingPlatform>();
    platform.Position = new Vector2(START_X, START_Y);
    platform.Depth = DEPTH;
    platform.SinkSpeed = SINK_SPEED;
    platform.RiseSpeed = RISE_SPEED;
    configure(platform);
    return platform;
  }

  // Carried off rather than freed: what the platform has to notice is a surface nobody is standing on
  // any more, which is the same thing whether the cube walked off it or died somewhere else.
  private async Task _leave(Wfc.Entities.World.Player.Player player) {
    player.GlobalPosition = new Vector2(START_X + 2000.0f, START_Y - 2000.0f);
    await PhysicsFrames.Advance(TestScene, 2);
  }

  private Task<bool> _waitFor(Func<bool> until) => PhysicsFrames.WaitFor(TestScene, until, TIMEOUT);
}
