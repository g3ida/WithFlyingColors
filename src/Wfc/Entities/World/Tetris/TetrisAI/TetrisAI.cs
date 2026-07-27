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

  // Scores every rotation in every column and reports the best placement. Nothing here is
  // added to the tree - the piece is a scratch object used to ask the grid questions.
  //
  // One instance for the whole search, walked from candidate to candidate. It used to build a
  // fresh ~45-node subtree per candidate, 40 of them synchronously inside the physics frame
  // that spawns a piece, each one running four BlockSprite color setters (three skin lookups
  // and three node path resolutions apiece). Worse, only the candidates that fit were ever
  // freed, and rejections are guaranteed - an I piece rejects 3 of 10 columns on an empty
  // board - so the rest leaked for the rest of the level.
  public Dictionary<string, float> Best(Block?[,] grid, PackedScene tetromino) {
    // A column, not a row: seeded with the spawn row this would send an unplaceable piece to
    // column 2 and overwrite whatever the grid holds there.
    int bestPosition = Constants.TETRIS_SPAWN_I;
    int bestRotation = 0;
    float bestScore = float.NegativeInfinity;

    var piece = tetromino.Instantiate<Tetromino>();
    try {
      piece.SetGrid(grid);

      for (int rotation = 0; rotation < 4; rotation++) {
        for (int c = 0; c < Constants.TETRIS_POOL_WIDTH; c++) {
          piece.PlaceAt(c, Constants.TETRIS_SPAWN_J, rotation);
          if (!piece.IsInValidPosition()) {
            continue;
          }

          while (piece.MoveDownSafe())
            ;
          piece.AddToGrid(false);

          float score = CalculateGridScore(grid);
          if (score > bestScore) {
            bestScore = score;
            bestPosition = c;
            bestRotation = rotation;
          }
          piece.RemoveFromGrid();
        }
      }
    }
    finally {
      piece.QueueFree();
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
