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
    if (_nextPieceNode != null) {
      RemoveChild(_nextPieceNode);
    }
    _nextPieceNode = piece.Instantiate<Tetromino>();
    AddChild(_nextPieceNode);
    _nextPieceNode.Owner = this;
    _nextPieceNode.Position -= _getPieceBounds(_nextPieceNode);
  }

  // Used to center piece in container
  private static Vector2 _getPieceBounds(Tetromino piece) {
    float minI = 3f;
    float minJ = 3f;
    float maxI = -3f;
    float maxJ = -3f;
    foreach (Node ch in piece.GetChildren()) {
      Block block = (Block)ch;
      minI = Mathf.Min(block.I, minI);
      minJ = Mathf.Min(block.J, minJ);
      maxI = Mathf.Max(block.I, maxI);
      maxJ = Mathf.Max(block.J, maxJ);
    }
    // FIXME: Why did I had to change this from +1f to -1f (C# migration) ?
    return new Vector2(minI, minJ) + new Vector2(maxI - minI - 1f, maxJ - minJ - 1f) * 0.5f * Constants.TETRIS_BLOCK_SIZE;
  }
}
