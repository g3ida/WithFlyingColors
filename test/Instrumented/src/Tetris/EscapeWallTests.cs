namespace Wfc.test.instrumented.Tetris;

using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.Tetris;
using Wfc.Entities.Tetris.Tetrominos;
using Wfc.Entities.World.Checkpoints;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;
using Wfc.Utils.Colors;

// The wall the pool is won by: a column of bricks past the last one a piece can be placed in,
// which the player crosses once enough cleared lines have eaten it down to the stack they are
// standing on. It shares the grid with the playfield, so the two things worth pinning down are
// that it takes no part in completing a line and that it loses exactly one brick to each line
// that does complete.
public class EscapeWallTests(Node testScene) : TestClass(testScene) {
  private const int WALL_I = Constants.TETRIS_ESCAPE_WALL_I;
  private const int LAST_ROW = Constants.TETRIS_POOL_HEIGHT - 1;

  private FakeDependenciesProvider _provider = default!;
  private TetrisPool _pool = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    _pool = SceneHelpers.InstantiateNode<TetrisPool>();
    _provider.AddChild(_pool);
    await _idle();
  }

  [Cleanup]
  public void Cleanup() => _provider.QueueFree();

  [Test]
  public void TheWallStandsBesideEveryPlayfieldRow() {
    for (int j = 0; j < Constants.TETRIS_POOL_HEIGHT; j++) {
      _pool.Grid[WALL_I, j].ShouldNotBeNull($"row {j} should have a wall brick beside it");
    }
  }

  [Test]
  public async Task ARowThatIsShortOneCellDoesNotClearEvenThoughTheWallIsThere() {
    await _fillRow(LAST_ROW, except: 4);

    _pool.RemoveLines();
    await _blink();

    _pool.Grid[WALL_I, LAST_ROW].ShouldNotBeNull("the wall must not stand in for a missing cell");
    _pool.Grid[0, LAST_ROW].ShouldNotBeNull();
  }

  [Test]
  public async Task AClearedLineTakesTheWallBrickWithIt() {
    var wallTop = _pool.Grid[WALL_I, 0];
    await _fillRow(LAST_ROW);

    _pool.RemoveLines();
    await _blink();

    _pool.Grid[WALL_I, 0].ShouldBeNull("the wall should be one brick shorter");
    _pool.Grid[WALL_I, 1].ShouldBe(wallTop, "the bricks above should settle onto the gap");
    for (int j = 1; j < Constants.TETRIS_POOL_HEIGHT; j++) {
      _pool.Grid[WALL_I, j].ShouldNotBeNull($"row {j} should still be walled");
    }
  }

  // Two lines at once cost two bricks, which is what makes the wall wear down at the rate the
  // player is actually clearing rather than the rate they are locking pieces.
  [Test]
  public async Task TwoClearedLinesCostTwoBricks() {
    await _fillRow(LAST_ROW);
    await _fillRow(LAST_ROW - 1);

    _pool.RemoveLines();
    await _blink();

    _pool.Grid[WALL_I, 0].ShouldBeNull();
    _pool.Grid[WALL_I, 1].ShouldBeNull();
    _pool.Grid[WALL_I, 2].ShouldNotBeNull();
  }

  // A block landing against the wall meets a color it does not wear as often as not, and the
  // seam between them has to answer to both or standing on the join is fatal.
  [Test]
  public async Task ABlockLandingAgainstTheWallGetsAPermissiveSeam() {
    var wallBrick = _pool.Grid[WALL_I, LAST_ROW].ShouldNotBeNull();
    var neighborColor = wallBrick.ColorGroup == ColorUtils.BLUE ? ColorUtils.PINK : ColorUtils.BLUE;

    var landed = await _landBlock(neighborColor, Constants.TETRIS_POOL_WIDTH - 1, LAST_ROW);

    _edgeAreasOf(landed).ShouldHaveSingleItem();
    _edgeAreasOf(wallBrick).ShouldHaveSingleItem();
  }

  // Reaching the room is the end of the run, and what is left of the wall goes with it: the
  // player lands on the room's own floor, from which the standing part of the wall is a sheer
  // face they could not climb back over.
  [Test]
  public async Task ReachingTheRoomCrumblesWhatIsLeftOfTheWall() {
    var checkpoint = _pool.GetNode<CheckpointArea>("EscapeCheckpoint");

    checkpoint.EmitSignal(CheckpointArea.SignalName.checkpoint_hit);
    await _blink();

    for (int j = 0; j < Constants.TETRIS_POOL_HEIGHT; j++) {
      _pool.Grid[WALL_I, j].ShouldBeNull($"row {j} should no longer be walled");
    }
  }

  // An S or a Z dealt onto the empty floor settles into a step whose crook the cube can stand
  // in for the rest of the run, so the bag is not allowed to open with one. Run enough times
  // over to catch a shuffle slipping past the rule rather than a single lucky deal.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task TheRunNeverOpensWithAnSOrAZ() {
    var body = new Wfc.Entities.World.Player.Player();
    _pool.GetNode<Area2D>("TriggerEnterArea").EmitSignal(Area2D.SignalName.BodyEntered, body);
    body.QueueFree();

    for (var run = 0; run < 20; run++) {
      var opener = await _waitForPiece();
      opener.ShouldNotBeOfType<S_Block>($"run {run} opened with an S");
      opener.ShouldNotBeOfType<Z_Block>($"run {run} opened with a Z");
      _pool.reset();
      await _idle();
    }
  }

  private async Task<Tetromino> _waitForPiece() {
    for (var frame = 0; frame < 240; frame++) {
      await _frame();
      // A reset frees the piece it was holding, and a queued node is still a child until the
      // frame ends: without this the next run reads the outgoing piece as its opener.
      if (_pool.GetChildren().OfType<Tetromino>().FirstOrDefault(p => !p.IsQueuedForDeletion()) is { } piece) {
        return piece;
      }
    }
    throw new System.InvalidOperationException("the pool never dealt a piece");
  }

  private async Task _fillRow(int row, int except = -1) {
    for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++) {
      if (i != except) {
        await _landBlock(ColorUtils.BLUE, i, row);
      }
    }
  }

  private async Task<Block> _landBlock(string colorGroup, int i, int j) {
    var block = SceneHelpers.InstantiateNode<Block>();
    block.ColorGroup = colorGroup;
    block.I = i;
    block.J = j;
    block.Grid = _pool.Grid;
    _pool.AddChild(block);
    await _idle();
    block.AddToGrid();
    await _idle();
    return block;
  }

  private static EdgeArea[] _edgeAreasOf(Block block) => block.GetChildren().OfType<EdgeArea>().ToArray();

  // The blink the doomed cells play out before the rows above them are shifted down runs on a
  // Timer, so this waits on the clock rather than on a count of frames.
  private async Task _blink() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree.CreateTimer(Block.BLINK_ANIMATION_DURATION * 2.0f), SceneTreeTimer.SignalName.Timeout);
    await _idle();
  }

  private async Task _frame() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
  }

  private async Task _idle() {
    await _frame();
    await _frame();
  }
}
