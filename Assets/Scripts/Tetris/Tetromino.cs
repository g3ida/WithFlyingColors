using Godot;
using System;
using System.Collections.Generic;

public class Tetromino : Node2D
{
    private const int DIRECTIONS = 4;

    protected List<List<Vector2>> rotationMap;
    private int rotateIndex = 0;
    private List<List<Block>> grid;

    public void IncRotateIndex() => rotateIndex = (rotateIndex + 1) % DIRECTIONS;
    public void DecRotateIndex() => rotateIndex = (rotateIndex - 1 + DIRECTIONS) % DIRECTIONS;
    public void MoveRotateIndexBy(int dir) => rotateIndex = (rotateIndex + dir + DIRECTIONS) % DIRECTIONS;

    public void SetGrid(List<List<Block>> _grid)
    {
        grid = _grid;
        foreach (Node ch in GetChildren())
        {
            if (ch is Block block)
            {
                block.grid = _grid;
            }
        }
    }

    public override void _Ready() { }

    public void MoveDown() => MoveBy(0, 1);
    public bool MoveDownSafe() => MoveBySafe(0, 1);

    public void MoveLeft() => MoveBy(-1, 0);
    public bool MoveLeftSafe() => MoveBySafe(-1, 0);

    public void MoveRight() => MoveBy(1, 0);
    public bool MoveRightSafe() => MoveBySafe(1, 0);

    public void RotateLeft() => RotateDir(-1);
    public bool RotateLeftSafe() => RotateDirSafe(-1);

    public void RotateRight() => RotateDir(1);
    public bool RotateRightSafe() => RotateDirSafe(1);

    private void RotateDir(int dir)
    {
        int oldIdx = rotateIndex;
        MoveRotateIndexBy(dir);
        int i = 0;
        foreach (Node ch in GetChildren())
        {
            if (ch is Block block)
            {
                Vector2 pos = rotationMap[rotateIndex][i];
                Vector2 oldPos = rotationMap[oldIdx][i];
                Vector2 dDist = pos - oldPos;
                block.MoveBy((int)dDist.x, (int)dDist.y);
                i++;
            }
        }
        SetShape();
    }

    private bool RotateDirSafe(int dir)
    {
        if (CanRotateDir(dir))
        {
            RotateDir(dir);
            return true;
        }
        return false;
    }

    public bool CanMoveDown() => CanMoveBy(0, 1);
    public bool CanMoveLeft() => CanMoveBy(-1, 0);
    public bool CanMoveRight() => CanMoveBy(1, 0);

    private bool CanRotateDir(int dir)
    {
        int oldIdx = rotateIndex;
        MoveRotateIndexBy(dir);
        int i = 0;
        foreach (Node ch in GetChildren())
        {
            if (ch is Block block)
            {
                Vector2 pos = rotationMap[rotateIndex][i];
                Vector2 oldPos = rotationMap[oldIdx][i];
                Vector2 dDist = pos - oldPos;
                if (!block.CanMoveBy((int)dDist.x, (int)dDist.y))
                {
                    MoveRotateIndexBy(-dir);
                    return false;
                }
                i++;
            }
        }
        MoveRotateIndexBy(-dir);
        return true;
    }

    public bool CanMoveBy(int i, int j)
    {
        foreach (Node ch in GetChildren())
        {
            if (ch is Block block && !block.CanMoveBy(i, j))
            {
                return false;
            }
        }
        return true;
    }

    public void MoveBy(int i, int j)
    {
        foreach (Node ch in GetChildren())
        {
            if (ch is Block block)
            {
                block.MoveBy(i, j);
            }
        }
        Position += new Vector2(i * Constants.TETRIS_BLOCK_SIZE, j * Constants.TETRIS_BLOCK_SIZE);
    }

    private bool MoveBySafe(int i, int j)
    {
        if (CanMoveBy(i, j))
        {
            MoveBy(i, j);
            return true;
        }
        return false;
    }

    public void AddToGrid(bool permessiveMode = true)
    {
        foreach (Node ch in GetChildren())
        {
            if (ch is Block block)
            {
                block.AddToGrid(permessiveMode);
            }
        }
    }

    public void RemoveFromGrid()
    {
        foreach (Node ch in GetChildren())
        {
            if (ch is Block block)
            {
                block.RemoveFromGrid();
            }
        }
    }

    public void SetShape()
    {
        int i = 0;
        foreach (Node ch in GetChildren())
        {
            if (ch is Block block)
            {
                block.Position = rotationMap[rotateIndex][i] * Constants.TETRIS_BLOCK_SIZE;
                i++;
            }
        }
    }
}
