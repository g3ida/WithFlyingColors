namespace Wfc.test.instrumented.Platforms;

using System;
using System.Collections.Generic;
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

// A falling tetromino is a floor that is one row lower a moment later, and the pause between those
// rows is the whole mechanic: it is what the player reads, waits for and steps onto. A piece that
// slid down continuously would look nearly the same and play like a wall.
public class TetrominoPlatformTests(Node testScene) : TestClass(testScene) {
  private const float CELL = 96.0f;
  private const float STEP = 0.25f;
  private const float START_X = 700.0f;
  private const float START_Y = 100.0f;

  // Well inside a pixel, which is the smallest thing a piece standing on a row is read against.
  private const float CLOSE = 0.5f;
  private const double TIMEOUT = 4.0;

  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";

  // Default, platform and fall zone: what the cube collides with in a level, minus the layers this
  // test has nothing on.
  private const uint PLAYER_MASK = 13;

  private TetrominoPlatform _piece = default!;

  [Cleanup]
  public void Cleanup() {
    if (GodotObject.IsInstanceValid(_piece)) {
      _piece.QueueFree();
    }
  }

  // Read off a cell rather than off the piece: a cell is the body the player stands on, and it is
  // the one that has to have moved. The piece's own node stays where it was dropped.
  [Test]
  public async Task ItComesDownOneCellAtATime() {
    var piece = await _add();
    var start = _cellY(piece);

    (await _waitFor(() => _cellY(piece) >= start + CELL - CLOSE))
      .ShouldBeTrue("the piece never came down a row");
    (await _waitFor(() => _cellY(piece) >= start + (2.0f * CELL) - CLOSE))
      .ShouldBeTrue("the piece came down one row and stopped");
  }

  // The pause is not a side effect of the descent being slow: the piece is asked to stand still on
  // the row it has reached, and a run of ticks where it does not move at all is the proof. Without
  // it there is nothing to time a jump against.
  [Test]
  public async Task ItStandsStillOnEveryRowItReaches() {
    var piece = await _add();

    // Sampled over more than a row's period, so a hold has to fall inside the window wherever the
    // piece happened to be when sampling started.
    var samples = new List<float>();
    var ticks = (int)(STEP * 2.0f * Engine.PhysicsTicksPerSecond);
    for (var tick = 0; tick < ticks; tick++) {
      samples.Add(_cellY(piece));
      await PhysicsFrames.Frame(TestScene);
    }

    var held = samples.Zip(samples.Skip(1), (before, after) => after - before)
      .Count(moved => Mathf.IsZeroApprox(moved));

    held.ShouldBeGreaterThan(0, "the piece never stood still, so it slid down instead of stepping");
    samples.Last().ShouldBeGreaterThan(samples.First(), "the piece stood still for good instead of stepping");
  }

  // A piece that fell forever would be a body per spawn for the rest of the level, and every one of
  // them still being ticked somewhere under the fallzone.
  [Test]
  public async Task ItFreesItselfOnceItHasFallenItsDistance() {
    var piece = await _add(p => p.FallDistance = 3.0f * CELL);

    (await PhysicsFrames.WaitFor(TestScene, () => !GodotObject.IsInstanceValid(piece), TIMEOUT))
      .ShouldBeTrue("the piece kept falling past the depth it was given");
  }

  [Test]
  public void EveryPieceIsFourCellsAndWearsOneColour() {
    foreach (var kind in TetrominoShape.KINDS) {
      for (var rotation = 0; rotation < TetrominoShape.ROTATION_COUNT; rotation++) {
        var cells = TetrominoShape.CellsOf(kind, rotation);

        cells.Length.ShouldBe(TetrominoShape.CELL_COUNT, $"{kind} at rotation {rotation} is not a tetromino");
        cells.Distinct().Count().ShouldBe(cells.Length, $"{kind} at rotation {rotation} stacks two cells on one square");
      }

      ColorUtils.COLOR_GROUPS.ShouldContain(
        TetrominoShape.ColorGroupOf(kind),
        $"{kind} wears a colour the cube has no face for"
      );
    }
  }

  // Every cell is a platform in its own right, and a cell that never joined the piece's colour group
  // kills whichever face lands on it.
  [Test]
  public async Task ItsCellsAreLaidOutOnItsShapeAndAllShareItsColour() {
    var piece = await _add(p => {
      p.Kind = TetrominoShape.Kind.L;
      p.RotationIndex = 1;
    });

    var cells = piece.GetChildren().OfType<TetrominoCell>().ToList();
    cells.Count.ShouldBe(TetrominoShape.CELL_COUNT);

    var expected = TetrominoShape.CellsOf(TetrominoShape.Kind.L, 1)
      .Select(cell => new Vector2(cell.X, cell.Y) * CELL)
      .ToList();
    // The whole piece has come down by however far it has come down; what the layout is read
    // against is the shape it holds while it does, so the common drop is taken back out first.
    var drop = cells.Min(cell => cell.Position.Y) - expected.Min(cell => cell.Y);

    foreach (var cell in cells) {
      expected.ShouldContain(
        cell.Position - new Vector2(0.0f, drop),
        "a cell was laid out somewhere its shape does not reach"
      );
      cell.Group.ShouldBe(TetrominoShape.ColorGroupOf(TetrominoShape.Kind.L));
      cell.Size.ShouldBe(CELL);
    }
  }

  // What the whole node is for. Each cell is rooted in an AnimatableBody2D exactly so the player is
  // taken down with the piece; a piece that descended out from under them would look identical right
  // up to the moment they are standing on air over the fallzone.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ItCarriesThePlayerStandingOnIt() {
    // The cube resolves the game's services, and every cell resolves the level it is in for the
    // landing splash - so both are stood up before either of them is.
    var services = new FakeDependenciesProvider();
    TestScene.AddChild(services);
    var level = new FakeGameLevelProvider();
    services.AddChild(level);
    await PhysicsFrames.Frame(TestScene);

    _piece = SceneHelpers.InstantiateNode<TetrominoPlatform>();
    _piece.Position = new Vector2(START_X, START_Y);
    _piece.CellSize = CELL;
    _piece.StepInterval = STEP;
    _piece.FallDistance = 100.0f * CELL;
    // A flat four, so there is a wide surface under the cube however it comes down.
    _piece.Kind = TetrominoShape.Kind.I;
    level.AddChild(_piece);

    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    player.CollisionMask = PLAYER_MASK;
    // Dropped onto the middle of the bar, high enough to have settled before the piece has stepped
    // far enough to matter.
    player.Position = new Vector2(START_X, START_Y - 160.0f);
    level.AddChild(player);
    await PhysicsFrames.Frame(TestScene);

    // Whichever face the cube happens to be showing is not what this is about, and a piece it may
    // not land on kills it instead of carrying it - so the piece is painted the colour it is
    // already wearing downwards. A tetromino has no neutral colour to fall back on.
    var safeColor = _colorFacingDownOf(player);
    foreach (var cell in _piece.GetChildren().OfType<TetrominoCell>()) {
      cell.Group = safeColor;
    }

    (await _waitFor(player.IsOnFloor)).ShouldBeTrue("the cube never landed on the piece");
    var ridingFrom = player.GlobalPosition.Y;
    var landedOn = _cellY(_piece);

    (await _waitFor(() => _cellY(_piece) >= landedOn + (3.0f * CELL) - CLOSE))
      .ShouldBeTrue("the piece stopped descending with the cube on it");

    player.IsDying().ShouldBeFalse("riding the piece down killed the cube");
    player.GlobalPosition.Y.ShouldBeGreaterThan(
      ridingFrom + CELL,
      "the piece descended out from under the cube instead of taking it down"
    );

    services.QueueFree();
  }

  // The dead jump. Leaving a moving platform hands the cube the platform's own velocity, and a
  // tetromino's is straight down - so a jump taken on the tick a piece steps down has most of its
  // height cancelled before it starts. Which is the worst possible timing to punish: a piece
  // stepping down is the beat the player is reading the whole crossing against.
  //
  // Godot has a setting for exactly this, and the cube now carries it. A jump off a piece has to
  // reach what the same jump reaches off still ground.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task AJumpTakenAsAPieceStepsDownGoesAsHighAsAnyOther() {
    var fromStill = await _jumpHeight(descending: false);
    Cleanup();
    var fromFalling = await _jumpHeight(descending: true);

    fromStill.ShouldBeGreaterThan(200.0f, "the cube never jumped, so this compares nothing");
    fromFalling.ShouldBeGreaterThan(
      fromStill * 0.9f,
      "a jump taken off a descending piece lost height to the piece's own fall"
    );
  }

  // How high the cube gets from one jump, taken either off a piece standing still on its row or off
  // one on the way down to the next.
  private async Task<float> _jumpHeight(bool descending) {
    var services = new FakeDependenciesProvider();
    TestScene.AddChild(services);
    var level = new FakeGameLevelProvider();
    services.AddChild(level);
    await PhysicsFrames.Frame(TestScene);

    _piece = SceneHelpers.InstantiateNode<TetrominoPlatform>();
    _piece.Position = new Vector2(START_X, START_Y);
    _piece.CellSize = CELL;
    // A piece that never steps is the control: the same surface with nothing moving under the cube.
    _piece.StepInterval = descending ? STEP : float.MaxValue;
    _piece.FallDistance = 100.0f * CELL;
    _piece.Kind = TetrominoShape.Kind.I;
    level.AddChild(_piece);

    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    player.CollisionMask = PLAYER_MASK;
    player.Position = new Vector2(START_X, START_Y - 160.0f);
    level.AddChild(player);
    await PhysicsFrames.Frame(TestScene);

    var safeColor = _colorFacingDownOf(player);
    foreach (var cell in _piece.GetChildren().OfType<TetrominoCell>()) {
      cell.Group = safeColor;
    }
    (await _waitFor(player.IsOnFloor)).ShouldBeTrue("the cube never landed on the piece");

    // Settled, not merely touching: the falling state spends the frame it lands on handing over to
    // the standing one, and a press on that frame is read by neither.
    (await _waitFor(() => player.PlayerState is Wfc.Entities.World.Player.PlayerStandingState))
      .ShouldBeTrue("the cube never settled on the piece");

    // And taken on a tick the piece is actually moving, which is the whole point of this case.
    if (descending) {
      (await _waitFor(_isMidStep)).ShouldBeTrue("the piece never stepped, so nothing was moving under the cube");
    }

    var from = player.GlobalPosition.Y;
    // Held past the window a short press is cut in, so both cases are the same full jump.
    services.Input.Press(Wfc.Core.Input.IInputManager.Action.Jump);
    await PhysicsFrames.Advance(TestScene, 12);
    services.Input.Release(Wfc.Core.Input.IInputManager.Action.Jump);

    var peak = from;
    for (var tick = 0; tick < (int)(1.5f * Engine.PhysicsTicksPerSecond); tick++) {
      await PhysicsFrames.Frame(TestScene);
      peak = Mathf.Min(peak, player.GlobalPosition.Y);
    }

    services.QueueFree();
    return from - peak;
  }

  // A cell that never joined its colour group is a surface the cube is killed by whichever face it
  // lands on, and one still in its old group kills whoever lands on what they can see.
  [Test]
  public async Task ItsCellsAnswerToTheirColourAndNoOther() {
    var piece = await _add(p => p.Kind = TetrominoShape.Kind.T);
    var group = TetrominoShape.ColorGroupOf(TetrominoShape.Kind.T);

    foreach (var cell in piece.GetChildren().OfType<TetrominoCell>()) {
      var area = cell.GetNode<Area2D>("Area2D");
      area.IsInGroup(group).ShouldBeTrue("a cell is not in the colour group it is painted");
      foreach (var other in ColorUtils.COLOR_GROUPS) {
        if (other != group) {
          area.IsInGroup(other).ShouldBeFalse($"a {group} cell also answers to {other}");
        }
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

  private Task<TetrominoPlatform> _add() => _add(_ => { });

  // A cell already in the tree takes its transform from the physics server rather than from whoever
  // moved it, so the piece is placed and shaped before it is added.
  private async Task<TetrominoPlatform> _add(Action<TetrominoPlatform> configure) {
    _piece = SceneHelpers.InstantiateNode<TetrominoPlatform>();
    _piece.Position = new Vector2(START_X, START_Y);
    _piece.CellSize = CELL;
    _piece.StepInterval = STEP;
    _piece.FallDistance = 100.0f * CELL;
    configure(_piece);

    TestScene.AddChild(_piece);
    await PhysicsFrames.Frame(TestScene);
    return _piece;
  }

  private Task<bool> _waitFor(Func<bool> until) => PhysicsFrames.WaitFor(TestScene, until, TIMEOUT);

  // Part way between two rows, which is when the surface under the cube is under power.
  private bool _isMidStep() =>
    _piece.Descended > 0.0f && !Mathf.IsZeroApprox(_piece.Descended % CELL);

  private static float _cellY(TetrominoPlatform piece) =>
    piece.GetChildren().OfType<TetrominoCell>().First().GlobalPosition.Y;
}
