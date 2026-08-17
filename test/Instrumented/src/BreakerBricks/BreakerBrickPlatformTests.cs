namespace Wfc.test.instrumented.BreakerBricks;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Event;
using Wfc.Entities.World.BreakerBricks;
using Wfc.Entities.World.Enemies;
using Wfc.Utils;
using Wfc.Utils.Colors;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;

// A wall painted as a tilemap and built into a body, a set of colour areas and a row of bricks. What
// has to hold is that the shape the author painted is the shape that gets built, that each colour
// answers for exactly its own bricks, and that a shot takes out the one brick it landed on and no
// others - a wall that loses more than it was shot for is a level that opens itself.
public class BreakerBrickPlatformTests(Node testScene) : TestClass(testScene) {
  private const float CELL = 36.0f;
  private const float BRICK = CELL * BreakerBrickGrid.BRICK_CELLS;
  private const float START_X = 640.0f;
  private const float START_Y = 320.0f;
  private const double TIMEOUT = 4.0;

  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";
  private const string LEVEL_SCENE = "res://src/Wfc/Screens/Levels/LevelList/BrickBreakerLevel.tscn";

  // Default, platform and fall zone: what the cube collides with in a level, minus the layers this
  // test has nothing on.
  private const uint PLAYER_MASK = 13;

  // The palette columns of Assets/Tilesets/BreakerBricks.tres, in the order ColorUtils declares the
  // groups in, then the neutral. One letter is one brick.
  private static readonly Dictionary<char, int> Palette = new() {
    ['B'] = 0, ['P'] = 1, ['Y'] = 2, ['U'] = 3, ['W'] = 4,
  };

  private BreakerBrickPlatform _platform = default!;
  private FakeDependenciesProvider? _services;

  [Cleanup]
  public void Cleanup() {
    if (GodotObject.IsInstanceValid(_platform)) {
      _platform.QueueFree();
    }
    _services?.QueueFree();
    _services = null;
  }

  // The map is a picture of the wall, so where a letter is written is where its brick goes, and the
  // node sits on the cell the tilemap counts from.
  [Test]
  public async Task ItLaysABoxForEveryPaintedBrick() {
    var platform = await _add("WWW", "WWW");

    var boxes = _boxesOf(platform);
    boxes.Count.ShouldBe(6, "a brick is not a box of its own, so a shot has nothing to take out");
    boxes[0].ShouldBe(new Rect2(START_X, START_Y, BRICK, CELL));
    boxes[3].ShouldBe(new Rect2(START_X, START_Y + CELL, BRICK, CELL));
  }

  [Test]
  public async Task ItLeavesTheHolesInTheShapeUnfilled() {
    var platform = await _add("W.W");

    var boxes = _boxesOf(platform);
    boxes.Count.ShouldBe(2, "the gap painted into the wall was built solid");
    boxes[1].Position.X.ShouldBe(START_X + (2.0f * BRICK));
  }

  // The colour is the tile it was painted with: a brick answering to anything else is a surface that
  // kills the face the player picked by looking at it.
  [Test]
  public async Task EachColourAnswersForItsOwnBricks() {
    var platform = await _add("BB", "PP");

    var areas = _colorAreasOf(platform);
    areas.Count.ShouldBe(2, "the two colours painted onto the wall were not judged apart");

    var blue = areas.Single(area => area.IsInGroup(ColorUtils.BLUE));
    blue.IsInGroup(ColorUtils.PINK).ShouldBeFalse("a blue brick answers to pink");
    _rectOf(blue).ShouldBe(new Rect2(START_X, START_Y, 2.0f * BRICK, CELL));
  }

  // The neutral brick is the ground's own white: it answers to every face rather than to none, which
  // is what a wall the level puts in the way is built out of.
  [Test]
  public async Task ANeutralBrickAnswersToEveryFace() {
    var platform = await _add("WW");

    var area = _colorAreasOf(platform).Single();
    foreach (var group in ColorUtils.COLOR_GROUPS) {
      area.IsInGroup(group).ShouldBeTrue($"a neutral brick does not answer to {group}");
    }
  }

  #region Breaking
  // The whole point of a wall made of these. One shot, one brick: what it landed on goes and what it
  // did not stays, because a wall that comes apart faster than it is shot at opens itself.
  [Test]
  public async Task AShotTakesOutTheBrickItLandedOn() {
    var platform = await _add("WWW");

    platform.OnShot(_middleOf(platform, 1));
    await PhysicsFrames.Frame(TestScene);

    platform.IsBroken(1).ShouldBeTrue("the brick the shot landed on is still standing");
    platform.IsBroken(0).ShouldBeFalse("a brick nowhere near the shot was taken out with it");
    platform.IsBroken(2).ShouldBeFalse("a brick nowhere near the shot was taken out with it");
    _standingBoxesOf(platform).Count.ShouldBe(2, "a broken brick is still something to stand on");
  }

  // A projectile stops against the face of the wall rather than inside it, so the brick it broke is
  // never quite the brick it is standing on.
  [Test]
  public async Task AShotThatStoppedShortOfTheWallBreaksTheBrickBehindIt() {
    var platform = await _add("WWW");

    platform.OnShot(_middleOf(platform, 1) - new Vector2(0.0f, CELL * 0.8f));
    await PhysicsFrames.Frame(TestScene);

    platform.IsBroken(1).ShouldBeTrue("a shot that stopped against the wall broke nothing");
  }

  // A shot that flew in through a hole hit nothing: the brick that was there is already gone, and
  // the ones beside it are not what it was aimed at.
  [Test]
  public async Task AShotIntoAHoleBreaksNothingMore() {
    var platform = await _add("WWW");
    platform.OnShot(_middleOf(platform, 1));
    await PhysicsFrames.Frame(TestScene);

    platform.OnShot(_middleOf(platform, 1));
    await PhysicsFrames.Frame(TestScene);

    _standingBoxesOf(platform).Count.ShouldBe(2, "a shot through a hole took a brick beside it");
  }

  // A hole is not permanent. A wall that kept them would only ever get thinner - the canon shooting
  // at the player goes on chewing it whether they are winning or losing - and a bridge shot away for
  // good is a level that cannot be finished from its own checkpoint.
  [Test]
  public async Task EveryBrickIsLaidAgainOnRespawn() {
    var platform = await _add("WWWW");
    platform.OnShot(_middleOf(platform, 0));
    GameEvents.Instance.OnCheckpointReached(Vector2.Zero, ColorUtils.BLUE);
    platform.OnShot(_middleOf(platform, 2));
    await PhysicsFrames.Frame(TestScene);

    GameEvents.Instance.OnCheckpointLoaded();
    await PhysicsFrames.Frame(TestScene);

    platform.IsBroken(0).ShouldBeFalse("a brick broken before the last checkpoint stayed broken");
    platform.IsBroken(2).ShouldBeFalse("a brick broken after the last checkpoint stayed broken");
    _standingBoxesOf(platform).Count.ShouldBe(4, "a mended brick is not something to stand on");
  }

  // A shot that arrives slowly, or at a shallow angle, settles onto the wall instead of driving into
  // it. The colour area around a bullet is barely wider than the bullet itself, so an arrival like
  // that can be stopped by a surface without ever overlapping it enough to be reported - and the
  // shot ends up sitting on top of a wall it never broke.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task AShotThatSettlesOntoTheWallStillBreaksIt() {
    var platform = await _add("WWW");

    var bullet = SceneHelpers.InstantiateNode<Bullet>();
    bullet.GlobalPosition = _middleOf(platform, 1) - new Vector2(0.0f, 80.0f);
    TestScene.AddChild(bullet);
    bullet.SetColorGroup(ColorUtils.BLUE);
    // Barely moving, so it drifts down onto the bricks rather than driving into them.
    bullet.Shoot(Vector2.Down * 0.02f);

    (await PhysicsFrames.WaitFor(TestScene, () => platform.IsBroken(1), TIMEOUT))
      .ShouldBeTrue("a shot came to rest on the wall without breaking anything");

    if (GodotObject.IsInstanceValid(bullet)) {
      bullet.QueueFree();
    }
  }

  // The shot the player photographed: it flew in through a hole an earlier shot had made and came to
  // rest at the bottom of it. What it landed on is the brick under the hole - the gap it came
  // through is not something a shot can hit twice.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task AShotThatLandsInsideAHoleBreaksTheBrickUnderIt() {
    var platform = await _add("WWW", "WWW");
    platform.Break(1);
    await PhysicsFrames.Frame(TestScene);

    var bullet = SceneHelpers.InstantiateNode<Bullet>();
    bullet.GlobalPosition = _middleOf(platform, 1) - new Vector2(0.0f, 100.0f);
    TestScene.AddChild(bullet);
    bullet.SetColorGroup(ColorUtils.BLUE);
    bullet.Shoot(Vector2.Down);

    (await PhysicsFrames.WaitFor(TestScene, () => platform.IsBroken(4), TIMEOUT))
      .ShouldBeTrue("a shot that dropped into a hole sat at the bottom of it without breaking anything");

    if (GodotObject.IsInstanceValid(bullet)) {
      bullet.QueueFree();
    }
  }

  // The shot the player photographed: fired from a canon above and to the side, it comes in almost
  // flat and lands on the deck rather than into the face of the wall.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task AShotThatSkimsTheDeckStillBreaksIt() {
    var platform = await _add("WWWWWW");

    var bullet = SceneHelpers.InstantiateNode<Bullet>();
    // Just clear of the surface, flying along it: what a canon standing off to one side sends down.
    bullet.GlobalPosition = _middleOf(platform, 0) - new Vector2(0.0f, 30.0f);
    TestScene.AddChild(bullet);
    bullet.SetColorGroup(ColorUtils.BLUE);
    bullet.Shoot(new Vector2(0.97f, 0.24f));

    (await PhysicsFrames.WaitFor(TestScene, () => Enumerable.Range(0, 6).Any(platform.IsBroken), TIMEOUT))
      .ShouldBeTrue("a shot that skimmed along the wall broke nothing");

    if (GodotObject.IsInstanceValid(bullet)) {
      bullet.QueueFree();
    }
  }

  // A shot fired before the player died is part of the run that ended with them: it must not land
  // on the wall that has just been laid again, or the player walks back to a hole nothing in this
  // life put there.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task AShotStillInTheAirWhenThePlayerDiesBreaksNothing() {
    var platform = await _add("WWW");

    var bullet = SceneHelpers.InstantiateNode<Bullet>();
    bullet.GlobalPosition = _middleOf(platform, 0) - new Vector2(180.0f, 0.0f);
    TestScene.AddChild(bullet);
    bullet.SetColorGroup(ColorUtils.BLUE);
    bullet.Shoot(Vector2.Right);

    // Before it has had time to reach the wall, which is the whole of the case.
    GameEvents.Instance.OnCheckpointLoaded();
    await PhysicsFrames.Advance(TestScene, 60);

    _standingBoxesOf(platform).Count.ShouldBe(3, "a shot fired before the player died went on breaking the level");
    GodotObject.IsInstanceValid(bullet).ShouldBeFalse("the shot outlived the run it was fired in");
  }

  // The wall and the canon, end to end: nothing about a bullet knows what a brick is, and nothing
  // about a brick knows what a bullet is - the shot says where it landed and the wall answers.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ABulletFiredAtTheWallBreaksTheBrickItHits() {
    var platform = await _add("WWW");

    var bullet = SceneHelpers.InstantiateNode<Bullet>();
    // Fired at the end of the row, because that is the face of the wall a shot from this side
    // reaches: a bullet stops against the first brick it meets rather than the one it was aimed at.
    bullet.GlobalPosition = _middleOf(platform, 0) - new Vector2(180.0f, 0.0f);
    TestScene.AddChild(bullet);
    bullet.SetColorGroup(ColorUtils.BLUE);
    bullet.Shoot(Vector2.Right);

    (await PhysicsFrames.WaitFor(TestScene, () => platform.IsBroken(0), TIMEOUT))
      .ShouldBeTrue("a bullet fired into the wall left every brick standing");
    platform.IsBroken(1).ShouldBeFalse("one bullet took out more of the wall than it hit");

    if (GodotObject.IsInstanceValid(bullet)) {
      bullet.QueueFree();
    }
  }
  #endregion Breaking

  #region Authoring
  // The cube's downward face is wider than a brick, so a stretch of surface that changes colour
  // without changing height is lethal to walk and there is no way to play it well.
  [Test]
  public async Task ItWarnsWhenASurfaceChangesColourWithoutAStep() {
    var platform = await _add("BP");

    platform._GetConfigurationWarnings().ShouldContain(
      warning => warning.Contains("colour"),
      "a stretch of surface that changes colour underfoot was accepted"
    );
  }

  [Test]
  public async Task NeutralBricksAlongASurfaceAreNotWarnedAbout() {
    var platform = await _add("BWB");

    platform._GetConfigurationWarnings().ShouldBeEmpty("a neutral stretch of surface was read as a colour change");
  }

  [Test]
  public async Task ItWarnsWhenNoBricksArePainted() {
    var platform = await _add("..");

    platform._GetConfigurationWarnings().ShouldContain(
      warning => warning.Contains("nothing"),
      "a platform with no bricks in it was accepted"
    );
  }

  // The palette's layout is written down twice - once in the tileset and once in the code that reads
  // it - and a colour picked out of the wrong column is a surface that kills the face the player
  // chose by looking at it.
  [Test]
  public void EveryPaletteTileIsWhereTheCodeExpectsIt() {
    var tileSet = GD.Load<TileSet>("res://Assets/Tilesets/BreakerBricks.tres");
    tileSet.TileSize.Y.ShouldBe((int)CELL, "the palette's cells are not the size the code lays bricks out on");
    var source = (TileSetAtlasSource)tileSet.GetSource(0);

    for (var slot = 0; slot < BreakerBrickGrid.SLOT_COUNT; slot++) {
      var coords = BreakerBrickGrid.AtlasCoordsOf(slot);
      source.HasTile(coords).ShouldBeTrue($"the palette has no brick in colour {slot} at {coords}");
      source.GetTileSizeInAtlas(coords).ShouldBe(
        new Vector2I(BreakerBrickGrid.BRICK_CELLS, 1), $"the brick in colour {slot} is not as long as a brick"
      );
    }
  }

  // The one that guards the level rather than the node. Every warning above is about a crossing that
  // cannot be made, so the walls that ship have to raise none of them.
  [Test]
  public void TheWallsTheLevelIsBuiltFromRaiseNoWarning() {
    // Instantiated without being added to a tree: the painted cells are read straight off the scene,
    // and standing a whole level up to look at three tilemaps would drag in everything it depends on.
    var level = GD.Load<PackedScene>(LEVEL_SCENE).Instantiate();
    var platforms = _platformsUnder(level).ToList();

    platforms.ShouldNotBeEmpty("the level has no brick walls in it, so this checks nothing");
    foreach (var platform in platforms) {
      platform._GetConfigurationWarnings().ShouldBeEmpty($"{platform.Name} is a wall the level cannot be crossed on");
    }

    level.QueueFree();
  }
  #endregion Authoring

  // A wall of bricks is a floor like any other floor of its colour.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ThePlayerLandsOnItAndStaysThere() {
    _services = new FakeDependenciesProvider();
    TestScene.AddChild(_services);
    var level = new FakeGameLevelProvider();
    _services.AddChild(level);
    await PhysicsFrames.Frame(TestScene);

    _platform = SceneHelpers.InstantiateNode<BreakerBrickPlatform>();
    _platform.Position = new Vector2(START_X, START_Y);
    level.AddChild(_platform);
    _paint(_platform, "WWWW", "WWWW");
    _platform.Rebuild();

    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    player.CollisionMask = PLAYER_MASK;
    player.Position = new Vector2(START_X + (2.0f * BRICK), START_Y - 200.0f);
    level.AddChild(player);

    (await PhysicsFrames.WaitFor(TestScene, player.IsOnFloor, TIMEOUT))
      .ShouldBeTrue("the cube fell through the bricks");

    var restingAt = player.GlobalPosition.Y;
    await PhysicsFrames.Advance(TestScene, 60);

    player.IsDying().ShouldBeFalse("standing on neutral bricks killed the cube");
    player.GlobalPosition.Y.ShouldBe(restingAt, 1.0f, "the cube sank into the bricks it was standing on");
  }

  // The crush check reads one box for the whole platform, so a wall that has been repainted has to
  // be measured again along with everything else the painted bricks decide.
  [Test]
  public async Task TheCrushBoxOfARunningWallCoversWhatWasPainted() {
    Cleanup();
    var slider = SceneHelpers.InstantiateNode<SlidingBreakerBrickPlatform>();
    slider.Position = new Vector2(START_X, START_Y);
    slider.PlaySound = false;
    slider.Track = Wfc.Entities.World.Platforms.PlatformSlide.TrackDisplay.Never;
    _platform = slider;
    TestScene.AddChild(slider);
    _paint(slider, "WWW", "WWW");
    slider.Rebuild();
    await PhysicsFrames.Frame(TestScene);

    var bounds = slider.GetNode<CollisionShape2D>("Bounds");
    ((RectangleShape2D)bounds.Shape).Size.ShouldBe(new Vector2(3.0f * BRICK, 2.0f * CELL));
    bounds.Position.ShouldBe(new Vector2(1.5f * BRICK, CELL));
  }

  #region Helpers
  private static IEnumerable<BreakerBrickPlatform> _platformsUnder(Node node) {
    foreach (var child in node.GetChildren()) {
      if (child is BreakerBrickPlatform platform) {
        yield return platform;
      }
      foreach (var nested in _platformsUnder(child)) {
        yield return nested;
      }
    }
  }

  // The middle of a brick, in world terms, which is where a shot that hit it landed.
  private static Vector2 _middleOf(BreakerBrickPlatform platform, int index) =>
    platform.ToGlobal(_shapesOf(platform)[index].Position);

  // The body's own shapes, in the order the bricks were laid. The crush box a running wall carries
  // is not one of them: it covers the whole platform rather than any brick of it.
  private static List<CollisionShape2D> _shapesOf(BreakerBrickPlatform platform) => [
    .. platform.GetChildren().OfType<CollisionShape2D>().Where(shape => shape.Name != "Bounds")
  ];

  // The boxes in the platform's own space, which is the space the bricks were painted in.
  private static List<Rect2> _boxesOf(BreakerBrickPlatform platform) =>
    [.. _shapesOf(platform).Select(shape => _rectOf(shape, platform.Position))];

  private static List<Rect2> _standingBoxesOf(BreakerBrickPlatform platform) =>
    [.. _shapesOf(platform).Where(shape => !shape.Disabled).Select(shape => _rectOf(shape, platform.Position))];

  private static List<Area2D> _colorAreasOf(BreakerBrickPlatform platform) =>
    [.. platform.GetChildren().OfType<Area2D>()];

  private static Rect2 _rectOf(Area2D area) {
    var platform = (Node2D)area.GetParent();
    var boxes = area.GetChildren().OfType<CollisionShape2D>().Select(shape => _rectOf(shape, platform.Position));
    return boxes.Aggregate((merged, box) => merged.Merge(box));
  }

  private static Rect2 _rectOf(CollisionShape2D shape, Vector2 origin) {
    var size = ((RectangleShape2D)shape.Shape).Size;
    return new Rect2(origin + shape.Position - (size / 2.0f), size);
  }

  // One letter is one brick, and a brick covers as many cells as the palette's tiles are wide.
  private static void _paint(BreakerBrickPlatform platform, params string[] rows) {
    var layer = platform.GetNode<TileMapLayer>("Bricks");
    layer.Clear();
    for (var row = 0; row < rows.Length; row++) {
      for (var brick = 0; brick < rows[row].Length; brick++) {
        if (!Palette.TryGetValue(rows[row][brick], out var slot)) {
          continue;
        }
        layer.SetCell(
          new Vector2I(brick * BreakerBrickGrid.BRICK_CELLS, row), 0, BreakerBrickGrid.AtlasCoordsOf(slot)
        );
      }
    }
  }

  private async Task<BreakerBrickPlatform> _add(params string[] rows) {
    Cleanup();
    _platform = SceneHelpers.InstantiateNode<BreakerBrickPlatform>();
    _platform.Position = new Vector2(START_X, START_Y);

    TestScene.AddChild(_platform);
    _paint(_platform, rows);
    _platform.Rebuild();
    await PhysicsFrames.Frame(TestScene);
    return _platform;
  }
  #endregion Helpers
}
