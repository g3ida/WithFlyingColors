namespace Wfc.Entities.World.BreakerBricks;

using System;
using System.Collections.Generic;
using Godot;
using Wfc.Utils.Colors;

// The shape a wall of brick-breaker bricks is, read off the tilemap it was painted on: paint a tile
// to lay a brick, and the tile it was painted with is the colour that brick wears.
//
// The brick is the unit all the way down, which is what a toy-brick platform's grid is not. A shot
// takes out one brick, so every brick keeps its own box on the body and its own box in its colour's
// area rather than being merged into the fewest rectangles the painted shape can be covered by.
//
// Bricks are ordered by where they were painted rather than by whatever order the tilemap hands its
// cells back in, so a brick's place in the list is a name a save file can use.
public sealed class BreakerBrickGrid {
  #region Constants
  public const int EMPTY = -1;

  // The four colour groups, then the neutral, indexed the way every other tilemap in this project
  // indexes colours - see ColorUtils.FromTileSourceId.
  public const int NEUTRAL_SLOT = ColorUtils.TILE_SOURCE_ID_COUNT;
  public const int SLOT_COUNT = NEUTRAL_SLOT + 1;

  // How many cells long a brick is. BreakerBricks.tres is laid out from exactly this, so the two
  // cannot drift apart without a test noticing.
  public const int BRICK_CELLS = 3;
  #endregion Constants

  public readonly record struct Brick(Vector2I Cell, int Slot);

  #region Fields
  private readonly Brick[] _bricks;
  // Row-major over the bounding box, so a cell is EMPTY wherever the wall has a gap in it. Only the
  // surface check reads it: what is built is built off the bricks themselves.
  private readonly int[] _slots;
  #endregion Fields

  private BreakerBrickGrid(Vector2I origin, int columns, int rows, float cellSize, Brick[] bricks, int[] slots) {
    Origin = origin;
    Columns = columns;
    Rows = rows;
    CellSize = cellSize;
    _bricks = bricks;
    _slots = slots;
  }

  // The cell the bounding box starts at, in the tilemap's own coordinates. A wall painted away from
  // the origin stays where it was painted rather than being pulled back to the node.
  public Vector2I Origin { get; }
  public int Columns { get; }
  public int Rows { get; }
  public float CellSize { get; }

  public IReadOnlyList<Brick> Bricks => _bricks;

  public bool IsEmpty => _bricks.Length == 0;

  // The box the wall fills, in the tilemap's own pixels.
  public Rect2 Bounds => new Rect2(
    new Vector2(Origin.X, Origin.Y) * CellSize,
    new Vector2(Columns, Rows) * CellSize
  );

  public Rect2 BoxOf(Brick brick) => new Rect2(
    new Vector2(brick.Cell.X, brick.Cell.Y) * CellSize,
    new Vector2(BRICK_CELLS, 1) * CellSize
  );

  public int SlotAt(int column, int row) =>
    column < 0 || row < 0 || column >= Columns || row >= Rows ? EMPTY : _slots[(row * Columns) + column];

  // Which brick covers a point given in the tilemap's own pixels, or EMPTY where the wall has a gap.
  // `standing` is asked about each brick, so a wall that has lost some of them is read as the shape
  // it is now rather than the shape it was painted as.
  public int IndexAt(Vector2 point, Func<int, bool> standing) {
    for (var index = 0; index < _bricks.Length; index++) {
      if (standing(index) && BoxOf(_bricks[index]).HasPoint(point)) {
        return index;
      }
    }
    return EMPTY;
  }

  // The brick nearest a point, for a shot that stopped just short of what it was fired at. Anything
  // further away than the given reach is not a hit at all.
  public int NearestTo(Vector2 point, float reach, Func<int, bool> standing) {
    var nearest = EMPTY;
    var best = reach * reach;
    for (var index = 0; index < _bricks.Length; index++) {
      if (!standing(index)) {
        continue;
      }
      var box = BoxOf(_bricks[index]);
      var distance = point.DistanceSquaredTo(point.Clamp(box.Position, box.End));
      if (distance <= best) {
        best = distance;
        nearest = index;
      }
    }
    return nearest;
  }

  public static BreakerBrickGrid Nothing(float cellSize) =>
    new BreakerBrickGrid(Vector2I.Zero, 0, 0, cellSize, [], []);

  public static BreakerBrickGrid Read(TileMapLayer layer) {
    var tileSet = layer.TileSet;
    var cellSize = tileSet?.TileSize.Y ?? 0;
    if (cellSize <= 0) {
      return Nothing(1.0f);
    }

    var cells = layer.GetUsedCells();
    if (cells.Count == 0) {
      return Nothing(cellSize);
    }

    var bricks = new List<Brick>(cells.Count);
    var min = cells[0];
    var max = cells[0];
    foreach (var cell in cells) {
      // The colour is which brick along the palette this is. A tile from some other tileset names a
      // colour we do not know, and a lethal surface nobody meant to author is worse than a neutral
      // one.
      var slot = layer.GetCellAtlasCoords(cell).X / BRICK_CELLS;
      bricks.Add(new Brick(cell, slot is >= 0 and < SLOT_COUNT ? slot : NEUTRAL_SLOT));
      min = new Vector2I(Mathf.Min(min.X, cell.X), Mathf.Min(min.Y, cell.Y));
      max = new Vector2I(Mathf.Max(max.X, cell.X + BRICK_CELLS - 1), Mathf.Max(max.Y, cell.Y));
    }
    bricks.Sort((left, right) =>
      left.Cell.Y != right.Cell.Y ? left.Cell.Y - right.Cell.Y : left.Cell.X - right.Cell.X);

    var columns = max.X - min.X + 1;
    var rows = max.Y - min.Y + 1;
    var slots = new int[columns * rows];
    Array.Fill(slots, EMPTY);
    foreach (var brick in bricks) {
      for (var along = 0; along < BRICK_CELLS; along++) {
        slots[((brick.Cell.Y - min.Y) * columns) + brick.Cell.X + along - min.X] = brick.Slot;
      }
    }

    return new BreakerBrickGrid(min, columns, rows, cellSize, [.. bricks], slots);
  }

  // The colour group a slot answers to, or null for the neutral one - which answers to all of them
  // rather than to none, the same way the ground does.
  public static string? GroupOf(int slot) => ColorUtils.FromTileSourceId(slot);

  // Where in the palette a colour's brick sits. The palette is one row of bricks, each as many
  // cells long as a brick is.
  public static Vector2I AtlasCoordsOf(int slot) => new Vector2I(slot * BRICK_CELLS, 0);

  // The stretches of surface the cube walks: cells with nothing laid on top of them, taken in runs
  // of neighbours at the same height. Two of them meeting is a step, which is jumped rather than
  // walked - so a run is exactly as far as the cube gets without a chance to turn a new face down.
  public List<string> SurfaceWarnings() {
    var warnings = new List<string>();
    for (var row = 0; row < Rows; row++) {
      var start = -1;
      for (var column = 0; column <= Columns; column++) {
        if (_isExposed(column, row)) {
          start = start < 0 ? column : start;
          continue;
        }
        if (start >= 0) {
          var warning = _runWarning(start, column - 1, row);
          if (warning is not null) {
            warnings.Add(warning);
          }
          start = -1;
        }
      }
    }
    return warnings;
  }

  private bool _isExposed(int column, int row) =>
    SlotAt(column, row) != EMPTY && SlotAt(column, row - 1) == EMPTY;

  // The cube dies on touching a colour its downward face does not accept, and walking is touching,
  // so a stretch of surface that changes colour partway along kills whoever walks it whichever face
  // they have turned down. The neutral is left out of it: it answers to every face, so it is safe to
  // walk from any colour onto and back off again.
  private string? _runWarning(int start, int end, int row) {
    var group = EMPTY;
    for (var column = start; column <= end; column++) {
      var slot = SlotAt(column, row);
      if (slot == NEUTRAL_SLOT) {
        continue;
      }
      if (group == EMPTY) {
        group = slot;
        continue;
      }
      if (slot != group) {
        return $"The surface on row {Origin.Y + row} changes colour around column {Origin.X + column} without changing height, so whoever walks along it is killed partway across whichever face they have turned down. Step the colours up or down instead of butting them together.";
      }
    }
    return null;
  }
}
