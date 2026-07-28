namespace Wfc.Entities.Tetris;

using System;
using Godot;
using Wfc.Entities.Tetris.Tetrominos;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class NextPiece : Node {
  private Tetromino? _nextPieceNode = null;

  public void SetNextPiece(PackedScene piece) {
    // QueueFree detaches as well, so the RemoveChild this used to do on its own only orphaned
    // the outgoing preview - and the one reference to it was overwritten on the next line, so
    // a ~45-node subtree was stranded for every piece the pool ever spawned.
    _nextPieceNode?.QueueFree();
    _nextPieceNode = piece.Instantiate<Tetromino>();
    AddChild(_nextPieceNode);
    _nextPieceNode.Owner = this;
    _nextPieceNode.Position -= _centerOffset(_nextPieceNode);
  }

  // The offset that lands the piece's bounding box on the container's origin. A block's own
  // origin is its top-left corner, so the box runs a full cell past the last one. Both terms
  // have to be in pixels: scaling only half the sum leaves a cell count added to a pixel
  // count, which is a whole cell of error for any piece whose shape does not start at -1.
  private static Vector2 _centerOffset(Tetromino piece) {
    float minI = float.MaxValue;
    float minJ = float.MaxValue;
    float maxI = float.MinValue;
    float maxJ = float.MinValue;
    foreach (Node ch in piece.GetChildren()) {
      if (ch is not Block block) {
        continue;
      }
      minI = Mathf.Min(block.I, minI);
      minJ = Mathf.Min(block.J, minJ);
      maxI = Mathf.Max(block.I, maxI);
      maxJ = Mathf.Max(block.J, maxJ);
    }
    return new Vector2(minI + maxI + 1f, minJ + maxJ + 1f) * 0.5f * Constants.TETRIS_BLOCK_SIZE;
  }
}
