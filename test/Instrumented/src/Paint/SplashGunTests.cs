namespace Wfc.test.instrumented.Paint;

using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Event;
using Wfc.Entities.World.Paint;
using Wfc.Entities.World.Platforms;
using Wfc.Utils;
using Wfc.Utils.Colors;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;

// A gun bolted to the ceiling that follows the cube and fires paint at it. What it leaves is not
// the level's - it belongs to the run, so it goes when the run does and it goes on its own after a
// while. A room whose floor is slowly painted end to end stops being a puzzle about colour.
public class SplashGunTests(Node testScene) : TestClass(testScene) {
  private const float CEILING = 0f;
  private const float FLOOR_TOP = 700f;

  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";

  // Default, platform and fall zone: what the cube collides with in a level, minus the layers this
  // test has nothing on.
  private const uint PLAYER_MASK = 13;

  private FakeDependenciesProvider _services = default!;
  private FakeGameLevelProvider _level = default!;
  private SplashGun _gun = default!;

  [Setup]
  public async Task Setup() {
    _services = new FakeDependenciesProvider();
    TestScene.AddChild(_services);
    _level = new FakeGameLevelProvider();
    _services.AddChild(_level);
    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    player.CollisionMask = PLAYER_MASK;
    _level.AddChild(player);
    _level.PlayerNode = player;
    await PhysicsFrames.Frame(TestScene);
  }

  [Cleanup]
  public void Cleanup() => _services.QueueFree();

  // Out of reach is not only out of range: a cube behind the arc the gun is allowed to swing
  // through is one the barrel never arrives at, and firing anyway paints the wall it is pointed
  // into over and over for as long as the player stands there.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ItHoldsFireAtWhatItCannotReach() {
    _floor(-900f, 900f);
    var gun = _hang(0f);
    gun.Range = 400f;

    // Far past its range, and straight down where it is looking.
    _level.PlayerNode.GlobalPosition = new Vector2(0f, FLOOR_TOP - 40f + 2000f);
    await PhysicsFrames.Advance(TestScene, 150);
    _shots().Count.ShouldBe(0, "it fired at a cube far outside the range it was given");

    // Near enough, but behind it: the arc cannot bring the barrel round that far.
    gun.Range = 4000f;
    gun.MinAngle = -95f;
    gun.MaxAngle = -85f;
    _level.PlayerNode.GlobalPosition = new Vector2(900f, CEILING + 40f);
    await PhysicsFrames.Advance(TestScene, 150);
    _shots().Count.ShouldBe(0, "it fired at a cube its barrel cannot be brought round to");
  }

  // And it does fire at what it can, or none of the above means anything.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ItFiresAtWhatItCanReach() {
    _floor(-900f, 900f);
    var gun = _hang(0f);
    _level.PlayerNode.GlobalPosition = new Vector2(0f, FLOOR_TOP - 40f);

    (await PhysicsFrames.WaitFor(TestScene, () => _shots().Count > 0, 6.0))
      .ShouldBeTrue("it never fired at a cube standing right under it");
  }

  // Paint the gun leaves belongs to the run. A death puts the room back, and a floor still wearing
  // the last attempt's paint is a floor the player is killed by for something they did not do.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task WhatItPaintsGoesWhenThePlayerDoes() {
    var floor = _floor(-900f, 900f);
    _fire(new Vector2(-400f, 200f), 6f);

    (await PhysicsFrames.WaitFor(TestScene, () => _splatsOn(floor).Count > 0, 4.0))
      .ShouldBeTrue("nothing was painted to begin with");

    GameEvents.Instance.OnCheckpointLoaded();
    await PhysicsFrames.Advance(TestScene, 4);

    _splatsOn(floor).Count.ShouldBe(0, "the last attempt's paint is still on the floor");
  }

  // It also goes on its own, and stops counting the moment it starts to. Paint the player can see
  // is on its way out but is still killed by reads as safe and is not.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ItsPaintDriesUpAndStopsBeingLethalAsItGoes() {
    var floor = _floor(-900f, 900f);
    _fire(new Vector2(-400f, 200f), 1f);

    (await PhysicsFrames.WaitFor(TestScene, () => _splatsOn(floor).Count > 0, 4.0)).ShouldBeTrue();
    var splat = _splatsOn(floor)[0];
    var lethal = splat.GetNode<Area2D>("Area2D");
    (await PhysicsFrames.WaitFor(TestScene, () => lethal.Monitorable, 4.0))
      .ShouldBeTrue("the paint never became a surface at all");

    // Once it starts to fade it must already have stopped counting.
    (await PhysicsFrames.WaitFor(TestScene, () => splat.Modulate.A < 0.99f, 6.0))
      .ShouldBeTrue("the paint never started to dry up");
    lethal.Monitorable.ShouldBeFalse("the paint is fading out and would still kill the cube");

    (await PhysicsFrames.WaitFor(TestScene, () => !GodotObject.IsInstanceValid(splat), 6.0))
      .ShouldBeTrue("the paint faded out but stayed on the level");
  }

  // The same rule the bucket answers to: paint does not hold itself up past the end of what it is
  // lying on. Fired at a shelf narrower than the splash, what lands is cut to the shelf and the
  // rest falls to the floor under it.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task PaintIsCutToWhatItLandsOnAndTheRestFalls() {
    var floor = _floor(-900f, 900f);
    var shelf = _platform(-80f, 80f, 520f);
    _fire(new Vector2(0f, 300f), 6f, width: 300f);

    (await PhysicsFrames.WaitFor(TestScene, () => _splatsOn(shelf).Count > 0, 4.0))
      .ShouldBeTrue("it never hit the shelf it was fired at");
    await PhysicsFrames.Advance(TestScene, 4);

    foreach (var coat in _splatsOn(shelf)) {
      coat.Width.ShouldBeLessThanOrEqualTo(shelf.Size.X + 1f, "the coat is wider than the shelf");
      (coat.GlobalPosition.X - (coat.Width / 2f))
        .ShouldBeGreaterThanOrEqualTo(shelf.Position.X - (shelf.Size.X / 2f) - 1f, "it hangs off the shelf");
      (coat.GlobalPosition.X + (coat.Width / 2f))
        .ShouldBeLessThanOrEqualTo(shelf.Position.X + (shelf.Size.X / 2f) + 1f, "it hangs off the shelf");
    }
    _splatsOn(floor).Count.ShouldBeGreaterThan(0, "what ran off the shelf never came down on the floor");
  }

  // Where the paint actually arrives, which is not the same question as where the joint is pointed.
  // The shot leaves the muzzle - most of the gun's length out from the joint, and swinging as it
  // turns - and it falls on the way there. Aiming the joint straight at the cube leaves the barrel's
  // line running past it by the whole of that offset, and the drop takes the rest.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ItsPaintArrivesWhereTheCubeIs() {
    _floor(-1200f, 1200f);
    _hang(700f);
    var standing = new Vector2(-300f, FLOOR_TOP - 40f);
    _level.PlayerNode.GlobalPosition = standing;

    // Let it come round onto the cube before judging where the paint goes.
    await PhysicsFrames.Advance(TestScene, 150);

    var nearest = float.MaxValue;
    for (var frame = 0; frame < 300; frame++) {
      await PhysicsFrames.Frame(TestScene);
      var half = _level.PlayerNode.GetCollisionHalfExtents();
      foreach (var shot in _shots()) {
        var gap = (shot.GlobalPosition - _level.PlayerNode.GlobalPosition).Abs() - half;
        nearest = Mathf.Min(nearest, Mathf.Max(Mathf.Max(gap.X, gap.Y), 0f));
      }
    }

    GD.Print($"[SplashGun] nearest a shot came to the cube: {nearest:F0}px");
    nearest.ShouldBeLessThan(24f, "the paint went wide of a cube standing still across the room");
  }

  // The tank is the cooldown. It holds a few shots, and when it is dry the gun has nothing to fire
  // until the ink has been drawn back up the cable - which is the part the player can see.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ItRunsDryAfterATankAndFillsBeforeFiringAgain() {
    _floor(-900f, 900f);
    var gun = _hang(0f);
    gun.ShotsPerTank = 2;
    gun.RefillTime = 2f;
    gun.FireInterval = 0.2f;
    _level.PlayerNode.GlobalPosition = new Vector2(0f, FLOOR_TOP - 40f);

    var fired = 0;
    var seen = new System.Collections.Generic.HashSet<ulong>();
    // Long enough for far more than a tankful at this interval, so a gun that ignored the tank
    // would be caught by the count rather than by the timing.
    for (var frame = 0; frame < 90; frame++) {
      await PhysicsFrames.Frame(TestScene);
      foreach (var shot in _shots()) {
        if (seen.Add(shot.GetInstanceId())) {
          fired++;
        }
      }
    }
    fired.ShouldBe(2, "it fired past what the tank holds without waiting for it to fill");

    // And once it has filled, it fires again.
    (await PhysicsFrames.WaitFor(TestScene, () => _shots().Any(s => seen.Add(s.GetInstanceId())), 6.0))
      .ShouldBeTrue("the tank filled but the gun never fired again");
  }

  // Paint that meets the cube has met something. Whether the face it struck takes the colour only
  // decides whether it kills - either way the shot has arrived, and one that carries on through and
  // lands on the floor beyond reads as the cube not being there.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ItsPaintDoesNotPassThroughTheCube() {
    _floor(-900f, 900f);
    _hang(0f);
    _level.PlayerNode.GlobalPosition = new Vector2(0f, FLOOR_TOP - 40f);

    var half = _level.PlayerNode.GetCollisionHalfExtents();
    for (var frame = 0; frame < 400; frame++) {
      await PhysicsFrames.Frame(TestScene);
      foreach (var shot in _shots()) {
        // Well inside the cube rather than merely touching it: a shot is allowed the tick it takes
        // to notice, but never the width of the cube.
        var gap = (shot.GlobalPosition - _level.PlayerNode.GlobalPosition).Abs() - (half * 0.5f);
        (gap.X < 0f && gap.Y < 0f)
          .ShouldBeFalse("a shot flew on into the middle of the cube instead of stopping at it");
      }
    }
  }

  // A death puts the gun back too. Caught mid-refill, a returning player would be handed a free run
  // past it that the attempt they died on never had.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ADeathFillsItsTankBackUp() {
    _floor(-900f, 900f);
    var gun = _hang(0f);
    gun.ShotsPerTank = 2;
    gun.RefillTime = 6f;
    gun.FireInterval = 0.2f;
    _level.PlayerNode.GlobalPosition = new Vector2(0f, FLOOR_TOP - 40f);

    // Empty it, and catch it partway through the long refill.
    var seen = new System.Collections.Generic.HashSet<ulong>();
    (await PhysicsFrames.WaitFor(TestScene, () => {
      foreach (var shot in _shots()) {
        seen.Add(shot.GetInstanceId());
      }
      return seen.Count >= 2;
    }, 6.0)).ShouldBeTrue("it never emptied its tank");
    await PhysicsFrames.Advance(TestScene, 60);
    _shots().Count(s => seen.Add(s.GetInstanceId())).ShouldBe(0, "it fired again before refilling");

    GameEvents.Instance.OnCheckpointLoaded();
    await PhysicsFrames.Frame(TestScene);

    // Full again, so it fires without waiting out the rest of a cooldown nobody is serving.
    (await PhysicsFrames.WaitFor(TestScene, () => _shots().Any(s => seen.Add(s.GetInstanceId())), 2.0))
      .ShouldBeTrue("the gun came back still serving the cooldown from the run before");
  }

  // One shot, dropped straight down from a point. What the paint does where it lands has nothing to
  // do with the gun's aim, and going through the gun for it means the cube it is aiming at catches
  // every shot before the floor ever sees one.
  private void _fire(Vector2 from, float life, float width = 180f) {
    var shot = SceneHelpers.InstantiateNode<SplashShot>();
    shot.Setup(ColorUtils.PURPLE, width, life);
    shot.Position = from;
    _level.AddChild(shot);
    shot.Fire(new Vector2(0f, 400f));
  }

  private SplashGun _hang(float x) {
    _gun = SceneHelpers.InstantiateNode<SplashGun>();
    _gun.Group = ColorUtils.PURPLE;
    _gun.Position = new Vector2(x, CEILING);
    _gun.FireInterval = 0.3f;
    _level.AddChild(_gun);
    return _gun;
  }

  private FlatPlatform _floor(float left, float right) => _platform(left, right, FLOOR_TOP);

  private FlatPlatform _platform(float left, float right, float top) {
    var platform = SceneHelpers.InstantiateNode<FlatPlatform>();
    platform.SnapToGrid = false;
    platform.Size = new Vector2(right - left, 256f);
    platform.Position = new Vector2((left + right) / 2f, top + 128f);
    platform.Group = FlatPlatform.NEUTRAL;
    _level.AddChild(platform);
    return platform;
  }

  private System.Collections.Generic.List<SplashShot> _shots() =>
    _level.GetChildren().OfType<SplashShot>().ToList();

  private static System.Collections.Generic.List<PaintSplat> _splatsOn(Node host) =>
    host.GetChildren().OfType<PaintSplat>().Where(GodotObject.IsInstanceValid).ToList();
}
