namespace Wfc.Entities.Tetris;

using Godot;
using Wfc.Entities.Tetris.Tetrominos;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class NextPiece : Node2D {
  #region Constants
  // The preview is dealt a piece at a time rather than sliding a queue along, so the arrival is
  // what sells it: the piece drops in a shade too big and settles.
  private const float ARRIVAL_SCALE = 0.55f;
  private const float ARRIVAL_DURATION = 0.32f;
  private const float FADE_IN_SHARE = 0.6f;

  private const float DRIFT = 5.0f;
  private const float DRIFT_DURATION = 1.7f;
  #endregion Constants

  #region Nodes
  [NodePath("PieceAnchor")]
  private Node2D _anchorNode = default!;
  #endregion Nodes

  private Tetromino? _nextPieceNode = null;
  private Tween? _arrival;
  private float _restY;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _restY = _anchorNode.Position.Y;
    _startDrifting();
  }

  // A slow float, so a board with nothing falling on it is not completely still.
  private void _startDrifting() {
    var drift = CreateTween().SetLoops();
    drift.TweenProperty(_anchorNode, "position:y", _restY - DRIFT, DRIFT_DURATION)
      .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    drift.TweenProperty(_anchorNode, "position:y", _restY + DRIFT, DRIFT_DURATION)
      .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
  }

  public void SetNextPiece(PackedScene piece) {
    // QueueFree detaches as well, so the RemoveChild this used to do on its own only orphaned
    // the outgoing preview - and the one reference to it was overwritten on the next line, so
    // a ~45-node subtree was stranded for every piece the pool ever spawned.
    _nextPieceNode?.QueueFree();
    _nextPieceNode = piece.Instantiate<Tetromino>();
    // Hung off the anchor with its own centre on the anchor's origin, so the arrival below
    // scales it about the middle of the piece rather than about whichever corner it starts in.
    _nextPieceNode.Position = -_centerOffset(_nextPieceNode);
    _nextPieceNode.Modulate = Colors.White with { A = 0.0f };
    _anchorNode.AddChild(_nextPieceNode);
    _nextPieceNode.Owner = this;
    _nextPieceNode.ResetPhysicsInterpolation();

    _arrival?.Kill();
    _anchorNode.Scale = Vector2.One * ARRIVAL_SCALE;
    _arrival = CreateTween().SetParallel();
    _arrival.TweenProperty(_anchorNode, "scale", Vector2.One, ARRIVAL_DURATION)
      .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    _arrival.TweenProperty(_nextPieceNode, "modulate:a", 1.0f, ARRIVAL_DURATION * FADE_IN_SHARE);
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
