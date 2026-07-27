namespace Wfc.Entities.Tetris.Tetrominos;

using Godot;
using Wfc.Utils;

public abstract partial class Tetromino : Node2D {
  private const int DIRECTIONS = 4;

  // A static table per piece, not a fresh one per read. This used to be an expression-bodied
  // property building a nested Godot Array on every single access, and it is read once per
  // block per rotation test - which the placement search performs a few hundred times per
  // spawned piece, inside a physics frame.
  protected abstract Vector2[][] RotationMap { get; }
  private int rotateIndex = 0;
  private Block?[,]? grid = null;

  public void IncRotateIndex() => rotateIndex = (rotateIndex + 1) % DIRECTIONS;
  public void DecRotateIndex() => rotateIndex = (rotateIndex - 1 + DIRECTIONS) % DIRECTIONS;
  public void MoveRotateIndexBy(int dir) => rotateIndex = (rotateIndex + dir + DIRECTIONS) % DIRECTIONS;

  public void SetGrid(Block?[,] _grid) {
    grid = _grid;
    foreach (Node ch in GetChildren()) {
      if (ch is Block block) {
        block.Grid = _grid;
      }
    }
  }

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

  private void RotateDir(int dir) {
    int oldIdx = rotateIndex;
    MoveRotateIndexBy(dir);
    int i = 0;
    foreach (Node ch in GetChildren()) {
      if (ch is Block block) {
        Vector2 pos = RotationMap[rotateIndex][i];
        Vector2 oldPos = RotationMap[oldIdx][i];
        Vector2 dDist = pos - oldPos;
        block.MoveBy((int)dDist.X, (int)dDist.Y);
        i++;
      }
    }
    SetShape();
  }

  private bool RotateDirSafe(int dir) {
    if (CanRotateDir(dir)) {
      RotateDir(dir);
      return true;
    }
    return false;
  }

  public bool CanMoveDown() => CanMoveBy(0, 1);
  public bool CanMoveLeft() => CanMoveBy(-1, 0);
  public bool CanMoveRight() => CanMoveBy(1, 0);

  private bool CanRotateDir(int dir) {
    int oldIdx = rotateIndex;
    MoveRotateIndexBy(dir);
    int i = 0;
    foreach (Node ch in GetChildren()) {
      if (ch is Block block) {
        Vector2 pos = RotationMap[rotateIndex][i];
        Vector2 oldPos = RotationMap[oldIdx][i];
        Vector2 dDist = pos - oldPos;
        if (!block.CanMoveBy((int)dDist.X, (int)dDist.Y)) {
          MoveRotateIndexBy(-dir);
          return false;
        }
        i++;
      }
    }
    MoveRotateIndexBy(-dir);
    return true;
  }

  public bool CanMoveBy(int i, int j) {
    foreach (Node ch in GetChildren()) {
      if (ch is Block block && !block.CanMoveBy(i, j)) {
        return false;
      }
    }
    return true;
  }

  public void MoveBy(int i, int j) {
    foreach (Node ch in GetChildren()) {
      if (ch is Block block) {
        block.MoveBy(i, j);
      }
    }
    Position += new Vector2(i * Constants.TETRIS_BLOCK_SIZE, j * Constants.TETRIS_BLOCK_SIZE);
  }

  private bool MoveBySafe(int i, int j) {
    if (CanMoveBy(i, j)) {
      MoveBy(i, j);
      return true;
    }
    return false;
  }

  public void AddToGrid(bool permissiveMode = true) {
    foreach (Node ch in GetChildren()) {
      if (ch is Block block) {
        block.AddToGrid(permissiveMode);
      }
    }
  }

  public void RemoveFromGrid() {
    foreach (Node ch in GetChildren()) {
      if (ch is Block block) {
        block.RemoveFromGrid();
      }
    }
  }

  // Drops this piece straight onto a grid cell in a given rotation, without moving through
  // the intermediate states. It exists so the placement search can walk one instance over
  // every candidate rather than instantiating a ~45-node scene per (rotation, column) pair.
  public void PlaceAt(int originI, int originJ, int rotationIndex) {
    rotateIndex = ((rotationIndex % DIRECTIONS) + DIRECTIONS) % DIRECTIONS;
    var offsets = RotationMap[rotateIndex];
    int i = 0;
    foreach (Node ch in GetChildren()) {
      if (ch is Block block) {
        block.MoveTo(originI + (int)offsets[i].X, originJ + (int)offsets[i].Y);
        i++;
      }
    }
  }

  public bool IsInValidPosition() {
    foreach (Node ch in GetChildren()) {
      if (ch is Block block && !block.IsInValidPosition()) {
        return false;
      }
    }
    return true;
  }

  // Once a piece locks, the grid owns its blocks and the shell has no behavior left. Hand
  // them over so the caller can free it: an unfreed shell stayed a child of the pool for the
  // rest of the level, one per piece ever played, long after its blocks were cleared.
  public void ReleaseBlocksTo(Node2D newParent) {
    foreach (Node ch in GetChildren()) {
      if (ch is Block block) {
        // The owner is only meaningful for scene serialization, and this one is about to stop
        // being an ancestor.
        block.Owner = null;
        block.Reparent(newParent, keepGlobalTransform: true);
      }
    }
  }

  public void SetShape() {
    int i = 0;
    foreach (Node ch in GetChildren()) {
      if (ch is Block block) {
        block.Position = RotationMap[rotateIndex][i] * Constants.TETRIS_BLOCK_SIZE;
        i++;
      }
    }
  }
}
