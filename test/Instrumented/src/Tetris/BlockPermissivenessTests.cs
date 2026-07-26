namespace Wfc.test.instrumented.Tetris;

using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.Tetris;
using Wfc.Entities.Tetris.Tetrominos;
using Wfc.Utils;
using Wfc.Utils.Colors;

// The end of the same story as BlockEdgeTests, through the real grid: landing a block
// beside one of another color has to leave an area that answers to both groups, or a
// player standing on the seam is killed by whichever color they are not wearing.
public class BlockPermissivenessTests(Node testScene) : TestClass(testScene) {
  private Node2D _root = default!;
  private Block?[,] _grid = default!;

  [Setup]
  public async Task Setup() {
    _root = new Node2D();
    TestScene.AddChild(_root);
    _grid = new Block?[Constants.TETRIS_POOL_WIDTH, Constants.TETRIS_POOL_HEIGHT];
    await _idle();
  }

  [Cleanup]
  public void Cleanup() => _root.QueueFree();

  [Test]
  public async Task BuildsAnEdgeAreaBetweenTwoDifferentColors() {
    var left = await _landBlock(ColorUtils.BLUE, 3, 5);
    var right = await _landBlock(ColorUtils.PINK, 4, 5);

    var edge = _edgeAreasOf(right).ShouldHaveSingleItem();
    edge.IsInGroup(ColorUtils.PINK).ShouldBeTrue("the seam should accept the block's own color");
    edge.IsInGroup(ColorUtils.BLUE).ShouldBeTrue("the seam should accept the neighbor's color");
    // Both sides of the seam are permissive, not just the one that landed last.
    _edgeAreasOf(left).ShouldHaveSingleItem();
  }

  [Test]
  public async Task BuildsNoEdgeAreaBetweenTwoBlocksOfTheSameColor() {
    var left = await _landBlock(ColorUtils.YELLOW, 3, 5);
    var right = await _landBlock(ColorUtils.YELLOW, 4, 5);

    _edgeAreasOf(left).ShouldBeEmpty();
    _edgeAreasOf(right).ShouldBeEmpty();
  }

  [Test]
  public async Task BuildsNoEdgeAreaForABlockWithNoNeighbor() {
    var alone = await _landBlock(ColorUtils.PURPLE, 3, 5);

    _edgeAreasOf(alone).ShouldBeEmpty();
  }

  private async Task<Block> _landBlock(string colorGroup, int i, int j) {
    var block = SceneHelpers.InstantiateNode<Block>();
    block.ColorGroup = colorGroup;
    block.I = i;
    block.J = j;
    block.Grid = _grid;
    _root.AddChild(block);
    await _idle();
    block.AddToGrid();
    await _idle();
    return block;
  }

  private static EdgeArea[] _edgeAreasOf(Block block) => block.GetChildren().OfType<EdgeArea>().ToArray();

  private async Task _idle() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
  }
}
