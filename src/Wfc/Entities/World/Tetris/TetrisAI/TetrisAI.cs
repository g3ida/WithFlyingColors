namespace Wfc.Entities.Tetris;

using System;
using System.Collections.Generic;
using Godot;
using Wfc.Entities.Tetris.Tetrominos;
using Wfc.Utils;

public partial class TetrisAI : Node {
  private const float HEIGHT_WEIGHT = 0.510066f;
  private const float LINES_WEIGHT = 0.760666f;
  private const float HOLES_WEIGHT = 0.35663f;
  private const float BUMPINESS_WEIGHT = 0.184483f;

  private static Block?[,] ShallowCloneGrid(Block?[,] grid) {
    int width = grid.GetLength(0);
    int height = grid.GetLength(1);
    var clone = new Block?[width, height];
    for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++) {
      for (int j = 0; j < Constants.TETRIS_POOL_HEIGHT; j++) {
        clone[i, j] = grid[i, j];
      }
    }
    return clone;
  }

  private static int ColumnHeight(Block?[,] grid, int c) {
    for (int i = 0; i < Constants.TETRIS_POOL_HEIGHT; i++) {
      if (grid[c, i] != null) {
        return Constants.TETRIS_POOL_HEIGHT - i;
      }
    }
    return 0;
  }

  private static int AggregateHeight(Block?[,] grid) {
    int total = 0;
    for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++) {
      total += ColumnHeight(grid, i);
    }
    return total;
  }

  private static bool IsLine(Block?[,] grid, int line) {
    for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++) {
      if (grid[i, line] == null) {
        return false;
      }
    }
    return true;
  }

  private static int NumLines(Block?[,] grid) {
    int count = 0;
    for (int i = 0; i < Constants.TETRIS_POOL_HEIGHT; i++) {
      if (IsLine(grid, i)) {
        count++;
      }
    }
    return count;
  }

  private static int NumHoles(Block?[,] grid) {
    int count = 0;
    for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++) {
      bool block = false;
      for (int j = 0; j < Constants.TETRIS_POOL_HEIGHT; j++) {
        if (grid[i, j] != null) {
          block = true;
        }
        else if (grid[i, j] == null && block) {
          count++;
        }
      }
    }
    return count;
  }

  private static int Bumpiness(Block?[,] grid) {
    int total = 0;
    for (int i = 0; i < Constants.TETRIS_POOL_WIDTH - 1; i++) {
      total += Math.Abs(ColumnHeight(grid, i) - ColumnHeight(grid, i + 1));
    }
    return total;
  }

  private static float CalculateGridScore(Block?[,] grid) {
    var cloned = ShallowCloneGrid(grid);
    RemoveFullLines(cloned);

    float heightScore = -HEIGHT_WEIGHT * AggregateHeight(cloned);
    float linesScore = LINES_WEIGHT * NumLines(grid);
    float holesScore = -HOLES_WEIGHT * NumHoles(cloned);
    float bumpinessScore = -BUMPINESS_WEIGHT * Bumpiness(cloned);

    return heightScore + linesScore + holesScore + bumpinessScore;
  }

  public Dictionary<string, float> Best(Block?[,] grid, PackedScene tetromino) {
    int bestRotation = 0;
    int bestPosition = Constants.TETRIS_SPAWN_J;
    float bestScore = float.NegativeInfinity;

    for (int i = 0; i < 4; i++) {
      for (int c = 0; c < Constants.TETRIS_POOL_WIDTH; c++) {
        // FIXME: The piece is never added to the tree.
        var rotatedPiece = tetromino.Instantiate<Tetromino>();
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

  private static void RemoveFullLines(Block?[,] grid) {
    var lines = DetectLines(grid);
    foreach (int line in lines) {
      RemoveLineCells(grid, line);
    }
  }

  private static void RemoveLineCells(Block?[,] grid, int line) {
    for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++) {
      grid[i, line] = null;
    }
    MoveDownLinesAbove(grid, line);
  }

  private static void MoveDownLinesAbove(Block?[,] grid, int line) {
    for (int j = line - 1; j >= 0; j--) {
      for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++) {
        grid[i, j + 1] = grid[i, j];
        grid[i, j] = null;
      }
    }
  }

  private static List<int> DetectLines(Block?[,] grid) {
    var linesToRemove = new List<int>();
    for (int j = 0; j < Constants.TETRIS_POOL_HEIGHT; j++) {
      bool completeLine = true;
      for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++) {
        if (grid[i, j] == null) {
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
