namespace Wfc.test.instrumented.Tetris;

using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Input;
using Wfc.Entities.Tetris;
using Wfc.Entities.Tetris.Tetrominos;
using Wfc.Entities.World.Player;
using Wfc.Utils;
using Wfc.test.instrumented.Helpers.Fakes;

// The same contract as TetrominoPlayerContactTests, but through the real pool: the piece is
// picked and placed by the AI, falls the whole way and locks on its own. The cube then walks
// into its side wearing the matching color, which must be a wall - and wearing any other,
// which must kill. The cube is rotated until the approaching face matches (or not), so the
// tests hold for whatever piece the bag deals first.
public class TetrisPoolPlayerContactTests(Node testScene) : TestClass(testScene) {
  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";

  private static readonly Vector2 POOL_POS = new(1000f, 1000f);
  // The pool scene is scaled; its grid geometry in world space is scaled with it.
  private const float POOL_SCALE = 1.5f;
  // The bottom row of the pool ends just above the pool origin; the cube stands there.
  private static readonly float FLOOR_TOP = POOL_POS.Y - 3f;
  private const float PLAYER_HALF_WIDTH = 95.4f / 2f;
  // World x of the pool's leftmost column, from the spawn marker and spawn column.
  private static readonly float POOL_LEFT = POOL_POS.X + POOL_SCALE * (-1f - 72f * 5f);

  private const float APPROACH_GAP = 200f;
  private const int FRAMES_TO_LAND = 40;
  private const int FRAMES_PER_ROTATION = 20;
  private const double FIRST_LOCK_TIMEOUT = 12.0;
  private const double WALK_TIMEOUT = 4.0;
  private const float REST_TOLERANCE = 4f;

  private FakeDependenciesProvider _provider = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);

    var ground = new StaticBody2D { Position = new Vector2(POOL_POS.X, FLOOR_TOP + 100f) };
    ground.AddChild(new CollisionShape2D {
      Shape = new RectangleShape2D { Size = new Vector2(2600f, 200f) }
    });
    _provider.AddChild(ground);
    await _physicsFrame();
  }

  [Cleanup]
  public void Cleanup() {
    _provider.Input.ReleaseAll();
    _provider.QueueFree();
  }

  [Test]
  public async Task WalkingIntoAPoolLandedPieceWithTheMatchingFaceStopsAgainstIt() {
    var pool = _startPool();
    var player = await _admitAndParkPlayer(pool);
    var blocks = await _waitForFirstLockedPiece(pool);

    var (start, dir, edgeX) = _approachFor(pool, blocks);
    var color = blocks[0].ColorGroup;
    await _rotateUntil(player, () => _faceToward(player, dir).IsInGroup(color));

    player.Position = start;
    await _frames(FRAMES_TO_LAND);
    _provider.Input.Press(dir.X > 0 ? IInputManager.Action.MoveRight : IInputManager.Action.MoveLeft);

    var restX = edgeX - dir.X * PLAYER_HALF_WIDTH;
    var stopped = await _waitFor(() =>
      player.IsDying() ||
      (restX - player.GlobalPosition.X) * dir.X <= REST_TOLERANCE);

    player.IsDying().ShouldBeFalse("touching the piece with its own color killed the cube");
    stopped.ShouldBeTrue("the cube never reached the side of the piece");
    await _frames(FRAMES_TO_LAND);
    player.IsDying().ShouldBeFalse("resting against the piece with its own color killed the cube");
    ((player.GlobalPosition.X - restX) * dir.X).ShouldBeLessThan(
      REST_TOLERANCE,
      "the cube walked through the piece instead of being stopped by it");
  }

  [Test]
  public async Task WalkingIntoAPoolLandedPieceWithTheWrongFaceKills() {
    var pool = _startPool();
    var player = await _admitAndParkPlayer(pool);
    var blocks = await _waitForFirstLockedPiece(pool);

    var (start, dir, _) = _approachFor(pool, blocks);
    var color = blocks[0].ColorGroup;
    await _rotateUntil(player, () => !_faceToward(player, dir).IsInGroup(color));

    player.Position = start;
    await _frames(FRAMES_TO_LAND);
    _provider.Input.Press(dir.X > 0 ? IInputManager.Action.MoveRight : IInputManager.Action.MoveLeft);

    var died = await _waitFor(player.IsDying);

    died.ShouldBeTrue("touching the piece with the wrong color left the cube alive");
  }

  // A piece the cube cannot get out from under must squash it - the platform crush, not a
  // color reading taken off whichever face the descending areas sweep through second. The
  // top face is dressed in the piece's own color first, so the only honest outcome left to
  // the old code would be "nothing happens", and the death that does come is the crush.
  [Test]
  public async Task APieceComingDownOnThePinnedCubeSquashesIt() {
    var pool = _startPool();
    var player = await _admitAndParkPlayer(pool);

    var piece = pool.GetChildren().OfType<Tetromino>().First();
    var pieceBlocks = piece.GetChildren().OfType<Block>().ToArray();
    var cell = Constants.TETRIS_BLOCK_SIZE * POOL_SCALE;
    // A column from the piece's bottom row: an S or Z's outermost column holds only a top-row
    // block, which locks into the row above the cube's head without ever reaching it.
    var bottomRow = pieceBlocks.Max(b => b.J);
    var columnX = pieceBlocks.First(b => b.J == bottomRow).GlobalPosition.X + (cell * 0.5f);
    await _rotateUntil(player, () => _faceToward(player, Vector2.Up).IsInGroup(pieceBlocks[0].ColorGroup));

    player.Position = new Vector2(columnX, FLOOR_TOP - PLAYER_HALF_WIDTH);
    var died = await _waitFor(player.IsDying, FIRST_LOCK_TIMEOUT);

    died.ShouldBeTrue("the piece came down on the cube and left it alive");
    (player.PlayerState is PlayerSquashedState).ShouldBeTrue(
      "the cube pinned under the piece died some way other than the crush");
  }

  // Meeting the falling piece from below in mid-air, wearing its color on top, with nothing
  // but air under the cube. The piece descends by transform, so without help it would bury
  // the cube and read a kill off a side face; with room to yield the cube must be carried
  // down ahead of it instead, alive.
  [Test]
  public async Task MeetingTheFallingPieceFromBelowInMidAirCarriesTheCubeDown() {
    var pool = _startPool();
    var player = await _admitAndParkPlayer(pool);

    var piece = pool.GetChildren().OfType<Tetromino>().First();
    var pieceBlocks = piece.GetChildren().OfType<Block>().ToArray();
    var cell = Constants.TETRIS_BLOCK_SIZE * POOL_SCALE;
    var bottomRow = pieceBlocks.Max(b => b.J);
    var bottomBlock = pieceBlocks.First(b => b.J == bottomRow);
    await _rotateUntil(player, () => _faceToward(player, Vector2.Up).IsInGroup(bottomBlock.ColorGroup));

    // Leap up at the piece's underside from well within jumping distance, high above the floor.
    player.Position = new Vector2(
      bottomBlock.GlobalPosition.X + (cell * 0.5f),
      bottomBlock.GlobalPosition.Y + cell + 150f + PLAYER_HALF_WIDTH);
    player.Velocity = new Vector2(0f, -900f);

    var bonked = false;
    for (var frame = 0; frame < 50; frame++) {
      await _physicsFrame();
      var gap = (player.GlobalPosition.Y - PLAYER_HALF_WIDTH) - (bottomBlock.GlobalPosition.Y + cell);
      player.IsDying().ShouldBeFalse("the piece caught the cube in mid-air and killed it despite the open air below");
      bonked |= gap < 10f;
    }
    bonked.ShouldBeTrue("the cube never reached the piece's underside, so the contact was not exercised");
  }

  private TetrisPool _startPool() {
    var pool = SceneHelpers.LoadScene<TetrisPool>().Instantiate<TetrisPool>();
    // The localizer reaches for the viewport camera, which a test scene does not have.
    pool.GetNode("CameraLocalizer").Free();
    pool.Position = POOL_POS;
    _provider.AddChild(pool);
    return pool;
  }

  // The pool waits for the cube to step off the lift before it deals a piece. Overlap the
  // trigger until the first piece is dealt, then park the cube clear of every column so the
  // piece cannot land on it.
  private async Task<Player> _admitAndParkPlayer(TetrisPool pool) {
    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Player>();
    player.Position = new Vector2(POOL_POS.X + 29.5f, FLOOR_TOP - PLAYER_HALF_WIDTH);
    _provider.AddChild(player);
    var dealt = await _waitFor(() => pool.GetChildren().OfType<Tetromino>().Any());
    dealt.ShouldBeTrue("stepping on the entry trigger never started the pool");

    player.Position = new Vector2(POOL_LEFT - 500f, FLOOR_TOP - PLAYER_HALF_WIDTH);
    await _physicsFrame();
    return player;
  }

  // Locking hands the blocks to the pool itself, so the first direct Block child marks it.
  // The pool is then frozen and the piece it has already dealt next is discarded: the trial
  // is the cube against the landed piece, not against whatever falls across its path.
  private async Task<Block[]> _waitForFirstLockedPiece(TetrisPool pool) {
    var locked = await _waitFor(
      () => pool.GetChildren().OfType<Block>().Any(),
      FIRST_LOCK_TIMEOUT);
    locked.ShouldBeTrue("no piece ever locked into the pool");
    pool.SetPhysicsProcess(false);
    foreach (var next in pool.GetChildren().OfType<Tetromino>()) {
      next.Free();
    }
    await _physicsFrame();
    return [.. pool.GetChildren().OfType<Block>()];
  }

  // Walk in from whichever side has room to stand between the piece and the pool wall.
  // Cells are wider in world space than the block constant: the pool is scaled.
  private static (Vector2 start, Vector2 dir, float edgeX) _approachFor(TetrisPool pool, Block[] blocks) {
    var cell = Constants.TETRIS_BLOCK_SIZE * pool.Scale.X;
    var leftEdge = blocks.Min(b => b.GlobalPosition.X);
    var rightEdge = blocks.Max(b => b.GlobalPosition.X) + cell;
    var fromLeft = leftEdge - POOL_LEFT >= APPROACH_GAP + PLAYER_HALF_WIDTH * 2f;
    return fromLeft
      ? (new Vector2(leftEdge - APPROACH_GAP, FLOOR_TOP - PLAYER_HALF_WIDTH), Vector2.Right, leftEdge)
      : (new Vector2(rightEdge + APPROACH_GAP, FLOOR_TOP - PLAYER_HALF_WIDTH), Vector2.Left, rightEdge);
  }

  private async Task _rotateUntil(Player player, System.Func<bool> facing) {
    for (var turn = 0; turn < 4 && !facing(); turn++) {
      _provider.Input.Press(IInputManager.Action.RotateLeft);
      await _physicsFrame();
      _provider.Input.Release(IInputManager.Action.RotateLeft);
      await _frames(FRAMES_PER_ROTATION);
    }
    facing().ShouldBeTrue("no quarter turn presented the wanted face");
  }

  private static BoxFace _faceToward(Player player, Vector2 dir) =>
    player.GetChildren().OfType<BoxFace>()
      .OrderByDescending(f => (f.GlobalPosition - player.GlobalPosition).Normalized().Dot(dir))
      .First();

  private async Task<bool> _waitFor(System.Func<bool> until, double timeout = WALK_TIMEOUT) {
    var deadline = timeout * Engine.PhysicsTicksPerSecond;
    for (var frame = 0; frame < deadline; frame++) {
      if (until()) {
        return true;
      }
      await _physicsFrame();
    }
    return false;
  }

  private async Task _frames(int count) {
    for (var frame = 0; frame < count; frame++) {
      await _physicsFrame();
    }
  }

  private async Task _physicsFrame() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
  }
}
