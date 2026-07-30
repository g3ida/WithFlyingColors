namespace Wfc.test.instrumented.Tetris;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.Tetris.Tetrominos;
using Wfc.Utils;
using Wfc.test.instrumented.Helpers.Fakes;

// The user-facing contract of a landed piece: it is a platform. Walking into it with the
// matching face stops the cube against its side; walking into it with any other face kills.
// The piece here is landed exactly the way TetrisPool lands one - PlaceAt, AddToGrid,
// ReleaseBlocksTo, shell freed - so whatever those steps do to the color areas is on trial.
public class TetrominoPlayerContactTests(Node testScene) : TestClass(testScene) {
  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";

  private const float GROUND_TOP = 760f;
  private const float PIECE_X = 1200f;
  // The O piece spans one block on each side of its origin.
  private const float PIECE_HALF_WIDTH = Constants.TETRIS_BLOCK_SIZE;
  private const float PIECE_HEIGHT = 2 * Constants.TETRIS_BLOCK_SIZE;
  private const float PLAYER_HALF_WIDTH = 95.4f / 2f;
  private const float APPROACH_DISTANCE = 300f;

  private const int FRAMES_TO_LAND = 40;
  private const double WALK_TIMEOUT = 4.0;
  // A body stopped by a wall rests within a physics margin of it, not at exact contact.
  private const float REST_TOLERANCE = 4f;

  private FakeDependenciesProvider _provider = default!;
  private Node2D _root = default!;
  private Block?[,] _grid = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    _root = new Node2D();
    _provider.AddChild(_root);
    _grid = new Block?[Constants.TETRIS_POOL_WIDTH, Constants.TETRIS_POOL_HEIGHT];
    await _physicsFrame();
  }

  [Cleanup]
  public void Cleanup() {
    _provider.Input.ReleaseAll();
    _provider.QueueFree();
  }

  // Right face and O piece are both yellow: contact is a wall, not a death.
  [Test]
  public async Task WalkingIntoALandedPieceWithTheMatchingFaceStopsAgainstIt() {
    await _landOPiece();
    var player = await _addPlayerOnGround(PIECE_X - PIECE_HALF_WIDTH - APPROACH_DISTANCE);

    _provider.Input.Press(Wfc.Core.Input.IInputManager.Action.MoveRight);
    var stopped = await _waitFor(() =>
      player.IsDying() ||
      player.GlobalPosition.X >= PIECE_X - PIECE_HALF_WIDTH - PLAYER_HALF_WIDTH - REST_TOLERANCE);

    player.IsDying().ShouldBeFalse("touching the piece with its own color killed the cube");
    stopped.ShouldBeTrue("the cube never reached the side of the piece");
    await _frames(FRAMES_TO_LAND);
    player.IsDying().ShouldBeFalse("resting against the piece with its own color killed the cube");
    player.GlobalPosition.X.ShouldBeLessThan(
      PIECE_X - PIECE_HALF_WIDTH - PLAYER_HALF_WIDTH + REST_TOLERANCE,
      "the cube walked through the piece instead of being stopped by it");
  }

  // Left face is pink, the piece is yellow: same approach from the other side is fatal.
  [Test]
  public async Task WalkingIntoALandedPieceWithTheWrongFaceKills() {
    await _landOPiece();
    var player = await _addPlayerOnGround(PIECE_X + PIECE_HALF_WIDTH + APPROACH_DISTANCE);

    _provider.Input.Press(Wfc.Core.Input.IInputManager.Action.MoveLeft);
    var died = await _waitFor(player.IsDying);

    died.ShouldBeTrue("touching the piece with the wrong color left the cube alive");
  }

  // The other way a block ever descends: rows dropping after a line clear. A dropping block
  // that reaches a cube pinned on the floor has to squash it, exactly like a falling piece.
  // The block wears the top face's own color, so no color reading can explain the death away.
  [Test]
  public async Task ALineDropOntoThePinnedCubeSquashesIt() {
    var player = await _addPlayerOnGround(PIECE_X);
    var block = SceneHelpers.InstantiateNode<Block>();
    block.ColorGroup = "blue";
    var playerTop = GROUND_TOP - (2f * PLAYER_HALF_WIDTH);
    block.Position = new Vector2(
      PIECE_X - (Constants.TETRIS_BLOCK_SIZE / 2f),
      playerTop - Constants.TETRIS_BLOCK_SIZE - 8f);
    _root.AddChild(block);
    await _frames(2);

    block.QueueDrop(Constants.TETRIS_BLOCK_SIZE);
    var died = await _waitFor(player.IsDying);

    died.ShouldBeTrue("the dropping block came down on the cube and left it alive");
    (player.PlayerState is Wfc.Entities.World.Player.PlayerSquashedState).ShouldBeTrue(
      "the cube pinned under the dropping block died some way other than the crush");
  }

  // The same sequence TetrisPool runs: spawn placed on the grid, then lock, hand the blocks
  // to the level and free the shell.
  private async Task _landOPiece() {
    var shape = SceneHelpers.LoadScene<O_Block>().Instantiate<Tetromino>();
    shape.SetGrid(_grid);
    shape.PlaceAt(5, 10, 0);
    shape.SetShape();
    _root.AddChild(shape);
    shape.Position = new Vector2(PIECE_X, GROUND_TOP - PIECE_HEIGHT);
    await _physicsFrame();

    shape.AddToGrid();
    shape.ReleaseBlocksTo(_root);
    shape.QueueFree();
    await _physicsFrame();
  }

  private async Task<Wfc.Entities.World.Player.Player> _addPlayerOnGround(float x) {
    var ground = new StaticBody2D { Position = new Vector2(PIECE_X, GROUND_TOP + 100f) };
    ground.AddChild(new CollisionShape2D {
      Shape = new RectangleShape2D { Size = new Vector2(2400f, 200f) }
    });
    _provider.AddChild(ground);

    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    player.Position = new Vector2(x, GROUND_TOP - 200f);
    _provider.AddChild(player);

    await _frames(FRAMES_TO_LAND);
    player.IsOnFloor().ShouldBeTrue("the cube never landed on the test floor");
    return player;
  }

  private async Task<bool> _waitFor(System.Func<bool> until) {
    var deadline = WALK_TIMEOUT * Engine.PhysicsTicksPerSecond;
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
