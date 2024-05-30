using Godot;
using System;

[Tool]
public class NextPiece : Node
{
    [Export]
    private PackedScene next_piece;

    private Tetromino nextPieceNode = null;

    public void SetNextPiece(PackedScene piece)
    {
        if (nextPieceNode != null)
        {
            RemoveChild(nextPieceNode);
        }
        next_piece = piece;
        nextPieceNode = (Tetromino)next_piece.Instance();
        AddChild(nextPieceNode);
        nextPieceNode.Owner = this;
        nextPieceNode.Position -= GetPieceBounds(nextPieceNode);
    }

    public override void _Ready()
    {
        if (next_piece != null)
        {
            SetNextPiece(next_piece);
        }
    }

    // Used to center piece in container
    private Vector2 GetPieceBounds(Tetromino piece)
    {
        float minI = 3f;
        float minJ = 3f;
        float maxI = -3f;
        float maxJ = -3f;
        foreach (Node ch in piece.GetChildren())
        {
            Block block = (Block)ch;
            minI = Mathf.Min(block.I, minI);
            minJ = Mathf.Min(block.J, minJ);
            maxI = Mathf.Max(block.I, maxI);
            maxJ = Mathf.Max(block.J, maxJ);
        }
        // FIXME: Why did I had to chnage this from +1f to -1f (C# migration) ?
        return new Vector2(minI, minJ) + new Vector2(maxI- minI - 1f, maxJ - minJ - 1f) * 0.5f * Constants.TETRIS_BLOCK_SIZE;
    }
}
