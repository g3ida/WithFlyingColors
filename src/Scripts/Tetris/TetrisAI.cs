using System;
using System.Collections.Generic;
using Godot;
using Wfc.Utils;

public partial class TetrisAI : Node {
  private const float HEIGHT_WEIGHT = 0.510066f;
  private const float LINES_WEIGHT = 0.760666f;
  private const float HOLES_WEIGHT = 0.35663f;
  private const float BUMPINESS_WEIGHT = 0.184483f;

  // grid[col][line]
  public override void _Ready() { }

  private List<List<Block>> CloneGrid(List<List<Block>> grid) {
    var clone = new List<List<Block>>();
    for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++) {
      clone.Add(new List<Block>());
      for (int j = 0; j < Constants.TETRIS_POOL_HEIGHT; j++) {
        clone[i].Add(grid[i][j]);
      }
    }
    return clone;
  }

  private int ColumnHeight(List<List<Block>> grid, int c) {
    for (int i = 0; i < Constants.TETRIS_POOL_HEIGHT; i++) {
      if (grid[c][i] != null) {
        return Constants.TETRIS_POOL_HEIGHT - i;
      }
    }
    return 0;
  }

  private int AggregateHeight(List<List<Block>> grid) {
    int total = 0;
    for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++) {
      total += ColumnHeight(grid, i);
    }
    return total;
  }

  private bool IsLine(List<List<Block>> grid, int line) {
    for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++) {
      if (grid[i][line] == null) {
        return false;
      }
    }
    return true;
  }

  private int NumLines(List<List<Block>> grid) {
    int count = 0;
    for (int i = 0; i < Constants.TETRIS_POOL_HEIGHT; i++) {
      if (IsLine(grid, i)) {
        count++;
      }
    }
    return count;
  }

  private int NumHoles(List<List<Block>> grid) {
    int count = 0;
    for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++) {
      bool block = false;
      for (int j = 0; j < Constants.TETRIS_POOL_HEIGHT; j++) {
        if (grid[i][j] != null) {
          block = true;
        }
        else if (grid[i][j] == null && block) {
          count++;
        }
      }
    }
    return count;
  }

  private int Bumpiness(List<List<Block>> grid) {
    int total = 0;
    for (int i = 0; i < Constants.TETRIS_POOL_WIDTH - 1; i++) {
      total += Math.Abs(ColumnHeight(grid, i) - ColumnHeight(grid, i + 1));
    }
    return total;
  }

  private float CalculateGridScore(List<List<Block>> grid) {
    var cloned = CloneGrid(grid);
    RemoveFullLines(cloned);

    float heightScore = -HEIGHT_WEIGHT * AggregateHeight(cloned);
    float linesScore = LINES_WEIGHT * NumLines(grid);
    float holesScore = -HOLES_WEIGHT * NumHoles(cloned);
    float bumpinessScore = -BUMPINESS_WEIGHT * Bumpiness(cloned);

    return heightScore + linesScore + holesScore + bumpinessScore;
  }

  public Dictionary<string, float> Best(List<List<Block>> grid, PackedScene tetromino) {
    int bestRotation = 0;
    int bestPosition = Constants.TETRIS_SPAWN_J;
    float bestScore = float.NegativeInfinity;

    for (int i = 0; i < 4; i++) {
      for (int c = 0; c < Constants.TETRIS_POOL_WIDTH; c++) {
        var rotatedPiece = tetromino.Instantiate<Tetromino>();
        rotatedPiece._Ready(); //FIXME: why ? just move the logic to constructor
        rotatedPiece.SetGrid(grid);
        rotatedPiece.MoveBy(0, Constants.TETRIS_SPAWN_J);

        for (int j = 0; j < i; j++) {
          rotatedPiece.RotateLeft();
        }

        if (rotatedPiece.CanMoveBy(c, 0)) {
          rotatedPiece.MoveBy(c, 0);
          while (rotatedPiece.MoveDownSafe())
            ;
          rotatedPiece.AddToGrid(false);

          float score = CalculateGridScore(grid);
          if (score > bestScore) {
            bestScore = score;
            bestPosition = c;
            bestRotation = i;
          }
          rotatedPiece.RemoveFromGrid();
          rotatedPiece.QueueFree();
        }
      }
    }
    return new Dictionary<string, float>
    {
            { "position", bestPosition },
            { "rotation", bestRotation },
            { "score", bestScore }
        };
  }

  private void RemoveFullLines(List<List<Block>> grid) {
    var lines = DetectLines(grid);
    foreach (int line in lines) {
      RemoveLineCells(grid, line);
    }
  }

  private void RemoveLineCells(List<List<Block>> grid, int line) {
    for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++) {
      grid[i][line] = null;
    }
    MoveDownLinesAbove(grid, line);
  }

  private void MoveDownLinesAbove(List<List<Block>> grid, int line) {
    for (int j = line - 1; j >= 0; j--) {
      for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++) {
        grid[i][j + 1] = grid[i][j];
        grid[i][j] = null;
      }
    }
  }

  private List<int> DetectLines(List<List<Block>> grid) {
    var linesToRemove = new List<int>();
    for (int j = 0; j < Constants.TETRIS_POOL_HEIGHT; j++) {
      bool completeLine = true;
      for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++) {
        if (grid[i][j] == null) {
          completeLine = false;
          break;
        }
      }
      if (completeLine) {
        linesToRemove.Add(j);
      }
    }
    return linesToRemove;
  }
}
