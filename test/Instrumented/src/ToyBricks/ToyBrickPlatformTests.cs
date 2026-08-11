namespace Wfc.test.instrumented.ToyBricks;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Platforms;
using Wfc.Entities.World.ToyBricks;
using Wfc.Utils;
using Wfc.Utils.Colors;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;

// A platform painted as a tilemap and built into a body, a set of colour areas and a wall of
// bricks. What has to hold is that the shape the author painted is the shape that gets built, that
// each colour answers for exactly its own bricks, and that the surface it leaves can be crossed -
// a stretch of it at one height that changes colour partway along kills whoever walks it, and no
// amount of playing well gets around that.
public class ToyBrickPlatformTests(Node testScene) : TestClass(testScene) {
  private const float CELL = 32.0f;
  private const float START_X = 640.0f;
  private const float START_Y = 320.0f;
  private const double TIMEOUT = 4.0;

  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";
  private const string BRICK_LEVEL_SCENE = "res://src/Wfc/Screens/Levels/LevelList/BrickLevel.tscn";

  // Default, platform and fall zone: what the cube collides with in a level, minus the layers this
  // test has nothing on.
  private const uint PLAYER_MASK = 13;

  // The palette columns of Assets/Tilesets/ToyBricks.tres, in the order ColorUtils declares the
  // groups in, then the neutral. The palette's rows are the piece kinds, which only the drawing
  // reads - a map is painted with whichever row the test is about.
  private static readonly Dictionary<char, int> Palette = new() {
    ['B'] = 0, ['P'] = 1, ['Y'] = 2, ['U'] = 3, ['W'] = 4,
  };

  private ToyBrickPlatform _platform = default!;
  private FakeDependenciesProvider? _services;

  [Cleanup]
  public void Cleanup() {
    if (GodotObject.IsInstanceValid(_platform)) {
      _platform.QueueFree();
    }
    _services?.QueueFree();
    _services = null;
  }

  // The map is a picture of the platform, so where a letter is written is where its brick goes, and
  // the node sits on the cell the tilemap counts from.
  [Test]
  public async Task ItCoversThePaintedCellsWithBoxes() {
    var platform = await _add("BBBB", "BBBB");

    var boxes = _boxesOf(platform);
    boxes.Count.ShouldBe(1, "a rectangle of bricks was not covered by a single box");
    boxes[0].Size.ShouldBe(new Vector2(4.0f * CELL, 2.0f * CELL));
    boxes[0].Position.ShouldBe(new Vector2(START_X, START_Y));
  }

  // A hole in the shape is a hole in the body: a platform painted as an arch is walked under.
  [Test]
  public async Task ItLeavesTheHolesInTheShapeUnfilled() {
    var platform = await _add("BBB", "B.B");

    var covered = _boxesOf(platform).Sum(box => box.Size.X * box.Size.Y / (CELL * CELL));
    covered.ShouldBe(5.0f, "the hole painted into the platform was built solid");
  }

  // The colour is the tile it was painted with: a brick answering to anything else is a surface
  // that kills the face the player picked by looking at it.
  [Test]
  public async Task EachColourAnswersForItsOwnBricks() {
    var platform = await _add("BB", "PP");

    var areas = _colorAreasOf(platform);
    areas.Count.ShouldBe(2, "the two colours painted onto the platform were not judged apart");

    var blue = areas.Single(area => area.IsInGroup(ColorUtils.BLUE));
    var pink = areas.Single(area => area.IsInGroup(ColorUtils.PINK));
    blue.IsInGroup(ColorUtils.PINK).ShouldBeFalse("a blue brick answers to pink");
    _rectOf(blue).ShouldBe(new Rect2(START_X, START_Y, 2.0f * CELL, CELL));
    _rectOf(pink).ShouldBe(new Rect2(START_X, START_Y + CELL, 2.0f * CELL, CELL));
  }

  // The neutral brick is the ground's own white: it answers to every face rather than to none, so a
  // stretch of it is what a level puts between two puzzles.
  [Test]
  public async Task ANeutralBrickAnswersToEveryFace() {
    var platform = await _add("WW");

    var area = _colorAreasOf(platform).Single();
    foreach (var group in ColorUtils.COLOR_GROUPS) {
      area.IsInGroup(group).ShouldBeTrue($"a neutral brick does not answer to {group}");
    }
  }

  // Painting over the tilemap while the game runs lays the platform again rather than leaving the
  // old body standing under the new bricks.
  [Test]
  public async Task RepaintingTheBricksBuildsThePlatformAgain() {
    var platform = await _add("BBBB");
    _paint(platform, "PP", "PP");
    platform.Rebuild();
    await PhysicsFrames.Frame(TestScene);

    var boxes = _boxesOf(platform);
    boxes.Count.ShouldBe(1, "the old body was left standing under the new bricks");
    boxes[0].Size.ShouldBe(new Vector2(2.0f * CELL, 2.0f * CELL));
    _colorAreasOf(platform).Single().IsInGroup(ColorUtils.PINK).ShouldBeTrue();
  }

  // The rule the whole colour palette exists under. The cube's downward face is wider than a cell,
  // so it is always touching two of them: it cannot be stood at the seam between two colours, and
  // it cannot be turned over without touching what it is standing on. A surface that changes colour
  // without changing height is therefore lethal to walk and there is no way to play it well.
  [Test]
  public async Task ItWarnsWhenASurfaceChangesColourWithoutAStep() {
    var platform = await _add("BBPP");

    platform._GetConfigurationWarnings().ShouldContain(
      warning => warning.Contains("colour"),
      "a stretch of surface that changes colour underfoot was accepted"
    );
  }

  // The same two colours, stepped instead of butted together, is the shape the crossing is made of:
  // the step is jumped, and the jump is where the cube turns a new face down.
  [Test]
  public async Task AColourChangeAcrossAStepIsNotWarnedAbout() {
    var platform = await _add("..PP", "BBPP");

    platform._GetConfigurationWarnings().ShouldBeEmpty("a colour change the player jumps was warned about");
  }

  // Buried bricks are not surface, so what colour they are is nobody's business but the author's -
  // which is what lets a platform be as many colours as it has bricks.
  [Test]
  public async Task ColoursUnderTheSurfaceAreNotWarnedAbout() {
    var platform = await _add("BBBB", "PPYY");

    platform._GetConfigurationWarnings().ShouldBeEmpty("bricks with something laid on top of them were read as surface");
  }

  // The neutral is safe to walk from any colour onto, so it is not a colour change.
  [Test]
  public async Task NeutralBricksAlongASurfaceAreNotWarnedAbout() {
    var platform = await _add("BBWWBB");

    platform._GetConfigurationWarnings().ShouldBeEmpty("a neutral stretch of surface was read as a colour change");
  }

  [Test]
  public async Task ItWarnsWhenNoBricksArePainted() {
    var platform = await _add("....");

    platform._GetConfigurationWarnings().ShouldContain(
      warning => warning.Contains("nothing"),
      "a platform with no bricks in it was accepted"
    );
  }

  // The one that guards the level rather than the node. Every warning above is about a crossing
  // that cannot be made, so the platforms that ship have to raise none of them.
  [Test]
  public void ThePlatformsTheLevelIsBuiltFromRaiseNoWarning() {
    // Instantiated without being added to a tree: the painted cells are read straight off the
    // scene, and standing a whole level up to look at ten tilemaps would drag in everything it
    // depends on.
    var level = GD.Load<PackedScene>(BRICK_LEVEL_SCENE).Instantiate();
    var platforms = _platformsUnder(level).ToList();

    platforms.ShouldNotBeEmpty("the level has no brick platforms in it, so this checks nothing");
    foreach (var platform in platforms) {
      platform._GetConfigurationWarnings().ShouldBeEmpty($"{platform.Name} is a platform the level cannot be crossed on");
    }

    level.QueueFree();
  }

  // A wall of bricks is a floor like any other floor of its colour.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ThePlayerLandsOnItAndStaysThere() {
    _services = new FakeDependenciesProvider();
    TestScene.AddChild(_services);
    var level = new FakeGameLevelProvider();
    _services.AddChild(level);
    await PhysicsFrames.Frame(TestScene);

    _platform = SceneHelpers.InstantiateNode<ToyBrickPlatform>();
    _platform.Position = new Vector2(START_X, START_Y);
    level.AddChild(_platform);
    _paint(_platform, "WWWWWWWW", "WWWWWWWW");
    _platform.Rebuild();

    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    player.CollisionMask = PLAYER_MASK;
    player.Position = new Vector2(START_X + (4.0f * CELL), START_Y - 200.0f);
    level.AddChild(player);

    (await PhysicsFrames.WaitFor(TestScene, player.IsOnFloor, TIMEOUT))
      .ShouldBeTrue("the cube fell through the bricks");

    var restingAt = player.GlobalPosition.Y;
    await PhysicsFrames.Advance(TestScene, 60);

    player.IsDying().ShouldBeFalse("standing on neutral bricks killed the cube");
    player.GlobalPosition.Y.ShouldBe(restingAt, 1.0f, "the cube sank into the bricks it was standing on");
  }

  // The palette's rows say how the bricks are drawn - how long a piece is, and whether it is seen
  // from the side or from above. None of that is anything the cube can be played wrong against, so
  // the same shape painted with any of them is the same platform underneath.
  [Test]
  public async Task ThePieceAPaintedCellBelongsToChangesNothingItIsMadeOf() {
    var kinds = new[] {
      ToyBrickGrid.PieceKind.SideBond,
      ToyBrickGrid.PieceKind.Side1,
      ToyBrickGrid.PieceKind.Side4,
      ToyBrickGrid.PieceKind.Top1,
      ToyBrickGrid.PieceKind.Top4,
    };

    List<Rect2>? expectedBoxes = null;
    foreach (var kind in kinds) {
      Cleanup();
      _platform = SceneHelpers.InstantiateNode<ToyBrickPlatform>();
      _platform.Position = new Vector2(START_X, START_Y);
      TestScene.AddChild(_platform);
      _paintAs(_platform, kind, "BBBB", "BBBB");
      _platform.Rebuild();
      await PhysicsFrames.Frame(TestScene);

      var boxes = _boxesOf(_platform);
      expectedBoxes ??= boxes;
      boxes.ShouldBe(expectedBoxes, $"{kind} was built into a different body from the others");
      _colorAreasOf(_platform).Single().IsInGroup(ColorUtils.BLUE)
        .ShouldBeTrue($"{kind} does not answer for the colour it was painted");
      _platform._GetConfigurationWarnings().ShouldBeEmpty($"{kind} was warned about");
    }
  }

  // A piece longer than a stud is one tile covering several cells, so what the author lays down
  // once is a whole brick rather than a cell of one.
  [Test]
  public async Task ALongPieceIsLaidAsOneTileCoveringEveryStudOfIt() {
    Cleanup();
    _platform = SceneHelpers.InstantiateNode<ToyBrickPlatform>();
    _platform.Position = new Vector2(START_X, START_Y);
    TestScene.AddChild(_platform);
    var layer = _platform.GetNode<TileMapLayer>("Bricks");
    layer.Clear();
    layer.SetCell(Vector2I.Zero, 0, ToyBrickGrid.AtlasCoordsOf(0, ToyBrickGrid.PieceKind.Side4));
    _platform.Rebuild();
    await PhysicsFrames.Frame(TestScene);

    var boxes = _boxesOf(_platform);
    boxes.Count.ShouldBe(1);
    boxes[0].Size.ShouldBe(new Vector2(4.0f * CELL, CELL), "one four-stud brick did not cover four studs");
  }

  // The head is the one piece taller than a course, so it is also the one that proves a piece can
  // cover cells below the one it was painted into.
  [Test]
  public async Task AHeadCoversTwoStudsEachWay() {
    Cleanup();
    _platform = SceneHelpers.InstantiateNode<ToyBrickPlatform>();
    _platform.Position = new Vector2(START_X, START_Y);
    TestScene.AddChild(_platform);
    var layer = _platform.GetNode<TileMapLayer>("Bricks");
    layer.Clear();
    layer.SetCell(Vector2I.Zero, 0, ToyBrickGrid.AtlasCoordsOf(2, ToyBrickGrid.PieceKind.Head));
    _platform.Rebuild();
    await PhysicsFrames.Frame(TestScene);

    var boxes = _boxesOf(_platform);
    boxes.Count.ShouldBe(1);
    boxes[0].Size.ShouldBe(new Vector2(2.0f * CELL, 2.0f * CELL), "a head did not cover two studs each way");
    _colorAreasOf(_platform).Single().IsInGroup(ColorUtils.YELLOW)
      .ShouldBeTrue("a head does not answer for the colour it was painted");
  }

  // The palette's layout is written down twice - once in the tileset and once in the code that
  // reads it - and a colour picked out of the wrong column is a surface that kills the face the
  // player chose by looking at it.
  [Test]
  public void EveryPaletteTileIsWhereTheCodeExpectsIt() {
    var tileSet = GD.Load<TileSet>("res://Assets/Tilesets/ToyBricks.tres");
    var source = (TileSetAtlasSource)tileSet.GetSource(0);

    foreach (var kind in System.Enum.GetValues<ToyBrickGrid.PieceKind>()) {
      for (var slot = 0; slot < ToyBrickGrid.SLOT_COUNT; slot++) {
        var coords = ToyBrickGrid.AtlasCoordsOf(slot, kind);
        source.HasTile(coords).ShouldBeTrue($"the palette has no tile for {kind} in colour {slot} at {coords}");
        source.GetTileSizeInAtlas(coords).ShouldBe(
          ToyBrickGrid.SizeOf(kind), $"the {kind} tile does not cover as many cells as the piece does"
        );
      }
    }
  }

  // A tile from a row the palette does not have is a piece nobody authored, and it falls back to
  // the plain bond rather than to nothing at all.
  [Test]
  public async Task ATileFromNoPaletteRowIsReadAsAPlainBrick() {
    var platform = await _add("BB");
    var layer = platform.GetNode<TileMapLayer>("Bricks");
    layer.SetCell(new Vector2I(0, 0), 0, new Vector2I(0, 99));
    platform.Rebuild();
    await PhysicsFrames.Frame(TestScene);

    _boxesOf(platform).Count.ShouldBe(1, "a tile out of the palette's rows was left out of the body");
  }

  // The whole point of a platform that moves. A body the cube merely stands on top of leaves it
  // behind the moment it sets off, and the ride reads as the platform sliding out from under it.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ARunningPlatformCarriesThePlayerAlongWithIt() {
    _services = new FakeDependenciesProvider();
    TestScene.AddChild(_services);
    var level = new FakeGameLevelProvider();
    _services.AddChild(level);
    await PhysicsFrames.Frame(TestScene);

    var slider = SceneHelpers.InstantiateNode<SlidingToyBrickPlatform>();
    slider.Position = new Vector2(START_X, START_Y);
    // Set before the platform is stood up: the run is measured once, out of where the level placed
    // the platform and what it was authored with.
    slider.Axis = PlatformSlide.SlideAxis.Horizontal;
    slider.Distance = 320.0f;
    slider.Speed = 4.0f;
    slider.WaitTime = 0.0f;
    slider.PlaySound = false;
    slider.Track = PlatformSlide.TrackDisplay.Never;
    _platform = slider;
    level.AddChild(slider);
    _paint(slider, "WWWWWWWW", "WWWWWWWW");
    slider.Rebuild();

    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    player.CollisionMask = PLAYER_MASK;
    player.Position = new Vector2(START_X + (4.0f * CELL), START_Y - 200.0f);
    level.AddChild(player);

    (await PhysicsFrames.WaitFor(TestScene, player.IsOnFloor, TIMEOUT))
      .ShouldBeTrue("the cube fell through the moving bricks");

    // Followed frame by frame rather than compared across a stretch: the platform runs a there-and-
    // back, so a window that happens to straddle the turn shows a platform that has gone nowhere.
    var rode = player.GlobalPosition.X - slider.GlobalPosition.X;
    var drift = 0.0f;
    var swept = 0.0f;
    var from = slider.GlobalPosition.X;
    for (var frame = 0; frame < 90; frame++) {
      await PhysicsFrames.Frame(TestScene);
      drift = Mathf.Max(drift, Mathf.Abs((player.GlobalPosition.X - slider.GlobalPosition.X) - rode));
      swept = Mathf.Max(swept, Mathf.Abs(slider.GlobalPosition.X - from));
    }

    swept.ShouldBeGreaterThan(CELL, "the platform never set off, so this checks nothing");
    player.IsDying().ShouldBeFalse("riding the platform killed the cube");
    drift.ShouldBeLessThan(CELL / 2.0f, "the platform ran out from under the cube instead of carrying it");
  }

  private static IEnumerable<ToyBrickPlatform> _platformsUnder(Node node) {
    foreach (var child in node.GetChildren()) {
      if (child is ToyBrickPlatform platform) {
        yield return platform;
      }
      foreach (var nested in _platformsUnder(child)) {
        yield return nested;
      }
    }
  }

  // The body's boxes in the platform's own space, which is the space the cells were painted in.
  private static List<Rect2> _boxesOf(ToyBrickPlatform platform) => [
    .. platform.GetChildren()
      .OfType<CollisionShape2D>()
      .Select(shape => _rectOf(shape, platform.Position))
  ];

  private static List<Area2D> _colorAreasOf(ToyBrickPlatform platform) =>
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

  private static void _paint(ToyBrickPlatform platform, params string[] rows) =>
    _paintAs(platform, ToyBrickGrid.PieceKind.SideBond, rows);

  private static void _paintAs(ToyBrickPlatform platform, ToyBrickGrid.PieceKind kind, params string[] rows) {
    var layer = platform.GetNode<TileMapLayer>("Bricks");
    layer.Clear();
    var studs = ToyBrickGrid.SizeOf(kind).X;
    for (var row = 0; row < rows.Length; row++) {
      for (var column = 0; column < rows[row].Length;) {
        if (!Palette.TryGetValue(rows[row][column], out var slot)) {
          column++;
          continue;
        }
        // A piece is painted once and covers the studs after it, so the cells it took are stepped
        // over rather than painted again.
        layer.SetCell(new Vector2I(column, row), 0, ToyBrickGrid.AtlasCoordsOf(slot, kind));
        column += studs;
      }
    }
  }

  private async Task<ToyBrickPlatform> _add(params string[] rows) {
    Cleanup();
    _platform = SceneHelpers.InstantiateNode<ToyBrickPlatform>();
    _platform.Position = new Vector2(START_X, START_Y);

    TestScene.AddChild(_platform);
    _paint(_platform, rows);
    _platform.Rebuild();
    await PhysicsFrames.Frame(TestScene);
    return _platform;
  }
}
