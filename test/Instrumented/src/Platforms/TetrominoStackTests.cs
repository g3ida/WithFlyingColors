namespace Wfc.test.instrumented.Platforms;

using System;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Platforms;
using Wfc.Utils;
using Wfc.Utils.Colors;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;

// Tetrominos that have settled: ground the level author laid out, rather than pieces still on their
// way to the fallzone. What has to hold is that the grid the author wrote is the grid that gets
// built, and that the surface it leaves can be crossed - a stretch of it at one height that changes
// colour partway along kills whoever walks it, and no amount of playing well gets around that.
public class TetrominoStackTests(Node testScene) : TestClass(testScene) {
  private const float CELL = 72.0f;
  private const float START_X = 700.0f;
  private const float START_Y = 100.0f;
  private const double TIMEOUT = 4.0;

  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";
  private const string TETRIS_LEVEL_SCENE = "res://src/Wfc/Screens/Levels/LevelList/TetrisLevel.tscn";

  // Default, platform and fall zone: what the cube collides with in a level, minus the layers this
  // test has nothing on.
  private const uint PLAYER_MASK = 13;

  private TetrominoStack _stack = default!;
  private FakeDependenciesProvider? _services;

  [Cleanup]
  public void Cleanup() {
    if (GodotObject.IsInstanceValid(_stack)) {
      _stack.QueueFree();
    }
    _services?.QueueFree();
    _services = null;
  }

  // The map is a picture of the stack, so where a letter is written is where its cell goes. The
  // node sits on the top-left corner of the grid, which is what lets an author place a stack by
  // lining that corner up with the ground it meets.
  [Test]
  public async Task ItLaysOneCellPerLetterWhereTheLetterIsWritten() {
    var stack = await _add("..I.\nJJII");

    var cells = stack.GetChildren().OfType<TetrominoCell>().ToList();
    cells.Count.ShouldBe(5, "the stack is not one cell per letter, holes left out");

    var expected = new[] {
      new Vector2(2, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(2, 1), new Vector2(3, 1),
    };
    foreach (var column in expected) {
      cells.ShouldContain(
        cell => cell.Position == ((column + (Vector2.One * 0.5f)) * CELL),
        $"no cell was laid on column {column.X} of row {column.Y}"
      );
    }
  }

  // A dot is a hole and so is a space, which is what lets a map be indented to line its rows up.
  [Test]
  public async Task DotsAndSpacesAreBothHoles() {
    // Counted before the next stack goes up: laying one takes the last one down.
    var dotted = (await _add("O.O")).GetChildren().OfType<TetrominoCell>().Count();
    var spaced = (await _add("O O")).GetChildren().OfType<TetrominoCell>().Count();

    dotted.ShouldBe(2, "a dot was laid as a cell");
    spaced.ShouldBe(dotted, "a space was laid as a cell");
  }

  // The letter is the colour: a cell wearing anything else is a surface that kills the face the
  // player picked by looking at it.
  [Test]
  public async Task EachCellWearsTheColourOfItsLetter() {
    foreach (var kind in TetrominoShape.KINDS) {
      var stack = await _add(kind.ToString());
      var cell = stack.GetChildren().OfType<TetrominoCell>().Single();

      cell.Group.ShouldBe(TetrominoShape.ColorGroupOf(kind));
      cell.GetNode<Area2D>("Area2D").IsInGroup(TetrominoShape.ColorGroupOf(kind))
        .ShouldBeTrue($"an {kind} cell does not answer to its own colour");
      cell.Size.ShouldBe(CELL);
    }
  }

  // Lower case reads the same as upper: a map is typed by hand, and a stack that silently loses a
  // row to a held shift key is a hole in a level nobody put there.
  [Test]
  public async Task LowerCaseNamesTheSamePieceAsUpper() {
    var stack = await _add("ssZZ");

    stack.GetChildren().OfType<TetrominoCell>().Count().ShouldBe(4);
    foreach (var cell in stack.GetChildren().OfType<TetrominoCell>()) {
      cell.Group.ShouldBe(ColorUtils.BLUE);
    }
  }

  [Test]
  public async Task RewritingTheMapLaysTheStackAgain() {
    var stack = await _add("IIII");
    stack.Map = "OO\nOO";
    await PhysicsFrames.Frame(TestScene);

    var cells = stack.GetChildren().OfType<TetrominoCell>().Where(GodotObject.IsInstanceValid).ToList();
    cells.Count.ShouldBe(4, "the old stack was left standing under the new one");
    cells.ShouldAllBe(cell => cell.Group == TetrominoShape.ColorGroupOf(TetrominoShape.Kind.O));
  }

  // The whole difference between a stack and the rain. A settled piece that drifted would take the
  // ground out from under a jump the player had already committed to.
  [Test]
  public async Task ItStaysWhereItWasPut() {
    var stack = await _add("IIII");
    var placed = stack.GetChildren().OfType<TetrominoCell>().Select(cell => cell.GlobalPosition).ToList();

    await PhysicsFrames.Advance(TestScene, 60);

    var now = stack.GetChildren().OfType<TetrominoCell>().Select(cell => cell.GlobalPosition).ToList();
    now.ShouldBe(placed, "the stack moved, so it is weather rather than ground");
  }

  // The rule the whole map format exists to keep. The cube's downward face is wider than a cell, so
  // it is always touching two of them: it cannot be stood at the seam between two colours, and it
  // cannot be turned over without touching what it is standing on. A surface that changes colour
  // without changing height is therefore lethal to walk and there is no way to play it well.
  [Test]
  public async Task ItWarnsWhenASurfaceChangesColourWithoutAStep() {
    var stack = await _add("IIJJ");

    stack._GetConfigurationWarnings().ShouldContain(
      warning => warning.Contains("colour"),
      "a stretch of surface that changes colour underfoot was accepted"
    );
  }

  // The same two colours, stepped instead of butted together, is the shape the crossing is made of:
  // the step is jumped, and the jump is where the cube turns a new face down.
  [Test]
  public async Task AColourChangeAcrossAStepIsNotWarnedAbout() {
    var stack = await _add("..JJ\nIIJJ");

    stack._GetConfigurationWarnings().ShouldBeEmpty("a colour change the player jumps was warned about");
  }

  // Buried cells are not surface, so what colour they are is nobody's business but the author's -
  // which is what lets a stack be built out of whole pieces and still read as one heap.
  [Test]
  public async Task ColoursUnderTheSurfaceAreNotWarnedAbout() {
    var stack = await _add("IIII\nJJOO");

    stack._GetConfigurationWarnings().ShouldBeEmpty("cells with something laid on top of them were read as surface");
  }

  [Test]
  public async Task ItWarnsAboutCharactersThatNameNoPiece() {
    var stack = await _add("IQI");

    stack._GetConfigurationWarnings().ShouldContain(
      warning => warning.Contains('Q'),
      "a letter naming no piece was left as a silent hole"
    );
  }

  [Test]
  public async Task ItWarnsWhenTheMapHasNoCells() {
    var stack = await _add("....\n....");

    stack._GetConfigurationWarnings().ShouldContain(
      warning => warning.Contains("empty"),
      "a stack with nothing in it was accepted"
    );
  }

  // The one that guards the level rather than the node. Every warning above is about a crossing
  // that cannot be made, so the maps that ship have to raise none of them.
  [Test]
  public void TheStacksTheLevelIsBuiltFromRaiseNoWarning() {
    // Instantiated without being added to a tree: the exported map is read straight off the scene,
    // and standing a whole level up to look at four strings would drag in everything it depends on.
    var level = GD.Load<PackedScene>(TETRIS_LEVEL_SCENE).Instantiate();
    var stacks = _stacksUnder(level).ToList();

    stacks.ShouldNotBeEmpty("the level has no tetromino stacks in it, so this checks nothing");
    foreach (var stack in stacks) {
      stack._GetConfigurationWarnings().ShouldBeEmpty($"{stack.Name} is a stack the level cannot be crossed on");
    }

    level.QueueFree();
  }

  // A settled piece is a floor like any other floor of its colour.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ThePlayerLandsOnItAndStaysThere() {
    _services = new FakeDependenciesProvider();
    TestScene.AddChild(_services);
    var level = new FakeGameLevelProvider();
    _services.AddChild(level);
    await PhysicsFrames.Frame(TestScene);

    _stack = SceneHelpers.InstantiateNode<TetrominoStack>();
    _stack.CellSize = CELL;
    _stack.Map = "IIII";
    _stack.Position = new Vector2(START_X, START_Y);
    level.AddChild(_stack);

    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    player.CollisionMask = PLAYER_MASK;
    player.Position = new Vector2(START_X + (2.0f * CELL), START_Y - 160.0f);
    level.AddChild(player);
    await PhysicsFrames.Frame(TestScene);

    // Whichever face the cube happens to be showing is not what this is about, and a colour it may
    // not land on kills it instead of holding it up.
    foreach (var cell in _stack.GetChildren().OfType<TetrominoCell>()) {
      cell.Group = _colorFacingDownOf(player);
    }

    (await PhysicsFrames.WaitFor(TestScene, player.IsOnFloor, TIMEOUT))
      .ShouldBeTrue("the cube fell through the stack");

    var restingAt = player.GlobalPosition.Y;
    await PhysicsFrames.Advance(TestScene, 60);

    player.IsDying().ShouldBeFalse("standing on the stack killed the cube");
    player.GlobalPosition.Y.ShouldBe(restingAt, 1.0f, "the cube sank into the stack it was standing on");
  }

  private static System.Collections.Generic.IEnumerable<TetrominoStack> _stacksUnder(Node node) {
    foreach (var child in node.GetChildren()) {
      if (child is TetrominoStack stack) {
        yield return stack;
      }
      foreach (var nested in _stacksUnder(child)) {
        yield return nested;
      }
    }
  }

  private static string _colorFacingDownOf(Wfc.Entities.World.Player.Player player) {
    foreach (var group in ColorUtils.COLOR_GROUPS) {
      if (player.WearsColorToward(group, Vector2.Down)) {
        return group;
      }
    }
    return ColorUtils.BLUE;
  }

  private async Task<TetrominoStack> _add(string map) {
    Cleanup();
    _stack = SceneHelpers.InstantiateNode<TetrominoStack>();
    _stack.CellSize = CELL;
    _stack.Map = map;
    _stack.Position = new Vector2(START_X, START_Y);

    TestScene.AddChild(_stack);
    await PhysicsFrames.Frame(TestScene);
    return _stack;
  }
}
