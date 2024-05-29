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
        int minI = 3;
        int minJ = 3;
        int maxI = -3;
        int maxJ = -3;
        foreach (Node ch in piece.GetChildren())
        {
            Block block = (Block)ch;
            minI = Math.Min(block.I, minI);
            minJ = Math.Min(block.J, minJ);
            maxI = Math.Max(block.I, maxI);
            maxJ = Math.Max(block.J, maxJ);
        }
        return new Vector2(minI, minJ) + new Vector2(maxI - minI + 1, maxJ - minJ + 1) * 0.5f * Constants.TETRIS_BLOCK_SIZE;
    }
}
