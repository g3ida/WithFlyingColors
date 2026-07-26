namespace Wfc.Entities.Tetris.Tetrominos.Test;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.Tetris.Tetrominos;
using Wfc.Utils.Colors;

// The seam between two blocks of different colors is the one place a face can touch two
// colors at once. The edge area joins both groups so that seam accepts either; the port
// inverted the test, so edge areas were built between blocks that already agreed and the
// dangerous seams got nothing.
public class BlockEdgeTests(Node testScene) : TestClass(testScene) {
  [Test]
  public void NeedsAnEdgeBetweenTwoDifferentColors() =>
    Block.NeedsEdgeBetween(ColorUtils.BLUE, ColorUtils.PINK).ShouldBeTrue();

  [Test]
  public void NeedsNoEdgeBetweenTwoBlocksOfTheSameColor() =>
    Block.NeedsEdgeBetween(ColorUtils.BLUE, ColorUtils.BLUE).ShouldBeFalse();

  [Test]
  public void NeedsAnEdgeForEveryDistinctPairOfColors() {
    foreach (var left in ColorUtils.COLOR_GROUPS) {
      foreach (var right in ColorUtils.COLOR_GROUPS) {
        Block.NeedsEdgeBetween(left, right).ShouldBe(left != right, $"{left} next to {right}");
      }
    }
  }

  // Nothing to straddle when there is no neighbor.
  [Test]
  public void NeedsNoEdgeWithoutANeighbor() =>
    Block.NeedsEdgeBetween(ColorUtils.BLUE, null).ShouldBeFalse();
}
