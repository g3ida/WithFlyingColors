namespace Wfc.Entities.World.ToyBricks;

using System;
using System.Collections.Generic;
using Godot;
using Wfc.Utils.Colors;

// The shape a brick platform is, read off the tilemap it was painted on and turned into the three
// things a platform has to be: a body to stand on, an area per colour to be judged against, and a
// map for the shader to draw from.
//
// A cell is one stud, and the tile it was painted with says two things about it. The palette's row
// is the piece - which way up the brick is seen and how many studs long it is - and within a row
// the tiles are the colours in order: the four colour groups as ColorUtils declares them, then the
// neutral white the ground is drawn in.
//
// A piece longer than one stud is a tile that many cells wide, so the palette shows the brick you
// are about to lay and one click lays the whole of it. Godot paints such a tile into a single cell
// and draws it across the rest, so reading the shape back means counting the cells it covers.
//
// Only the drawing knows about pieces. What the cube stands on and what its faces are judged
// against is the painted cells, so a piece is never something a level can be played wrong against.
//
// Everything is measured in the tilemap's own pixels, so a platform is exactly where it was
// painted.
public sealed class ToyBrickGrid {
  #region Constants
  public const int EMPTY = -1;

  // The four colour groups, then the neutral. The colour slots are indexed the way the tilemaps in
  // this project already index colours - see ColorUtils.FromTileSourceId, which is where the
  // mapping is written down.
  public const int NEUTRAL_SLOT = ColorUtils.TILE_SOURCE_ID_COUNT;
  public const int SLOT_COUNT = NEUTRAL_SLOT + 1;

  // What a cell says about the piece it belongs to, in the order the palette's rows are laid out.
  // Serialized into levels as the tile's atlas row, so members are only ever appended.
  public enum PieceKind {
    // A brick two studs long, offset a stud every row. The platform lays the bond itself, which is
    // what a wall wants and what an author who has not thought about it should get.
    SideBond,
    Side1,
    Side2,
    Side4,
    // The same bricks seen from above: a plate lying on the table with its studs facing out, which
    // is how a shape too fiddly to read from the side gets built.
    Top1,
    Top2,
    Top4,
    // A round head, two studs each way, with a pair of eyes on it. The one piece that is a face
    // rather than a brick, and the only one taller than a single course.
    Head,
  }

  // Which end of its piece a cell is. A cell that is both is a piece one stud long. A piece with a
  // face rather than ends - a head - uses the same field for which corner of itself the cell is,
  // counted along the row and then down.
  [Flags]
  public enum PieceEnds {
    Middle = 0,
    Left = 1,
    Right = 2,
  }

  // How a piece is drawn, which is all the shader is told about the kind.
  public enum PieceView {
    Side,
    Top,
    Head,
  }
  #endregion Constants

  #region Fields
  // Row-major over the bounding box of what was painted, so a slot is EMPTY wherever the shape has
  // a hole or a notch in it.
  private readonly int[] _slots;
  private readonly PieceKind[] _kinds;
  private readonly PieceEnds[] _ends;
  #endregion Fields

  private ToyBrickGrid(
    Vector2I origin, int columns, int rows, float cellSize, int[] slots, PieceKind[] kinds, PieceEnds[] ends
  ) {
    Origin = origin;
    Columns = columns;
    Rows = rows;
    CellSize = cellSize;
    _slots = slots;
    _kinds = kinds;
    _ends = ends;
  }

  // The cell the bounding box starts at, in the tilemap's own coordinates. A platform painted away
  // from the origin stays where it was painted rather than being pulled back to the node.
  public Vector2I Origin { get; }
  public int Columns { get; }
  public int Rows { get; }
  public float CellSize { get; }

  public bool IsEmpty => Columns == 0 || Rows == 0;

  // The box the bricks fill, in the tilemap's own pixels.
  public Rect2 Bounds => new Rect2(
    new Vector2(Origin.X, Origin.Y) * CellSize,
    new Vector2(Columns, Rows) * CellSize
  );

  public int SlotAt(int column, int row) =>
    column < 0 || row < 0 || column >= Columns || row >= Rows ? EMPTY : _slots[(row * Columns) + column];

  public PieceKind KindAt(int column, int row) =>
    column < 0 || row < 0 || column >= Columns || row >= Rows
      ? PieceKind.SideBond
      : _kinds[(row * Columns) + column];

  // Which ends of its own piece a cell carries, which is all the shader needs to draw the piece
  // around it: where a piece stops, the next one starts.
  public PieceEnds EndsAt(int column, int row) =>
    column < 0 || row < 0 || column >= Columns || row >= Rows
      ? PieceEnds.Middle
      : _ends[(row * Columns) + column];

  public static ToyBrickGrid Nothing(float cellSize) =>
    new ToyBrickGrid(Vector2I.Zero, 0, 0, cellSize, [], [], []);

  public static ToyBrickGrid Read(TileMapLayer layer) {
    var tileSet = layer.TileSet;
    var cellSize = tileSet?.TileSize.X ?? 0;
    if (cellSize <= 0) {
      return Nothing(1.0f);
    }

    var cells = layer.GetUsedCells();
    if (cells.Count == 0) {
      return Nothing(cellSize);
    }

    var pieces = new List<(Vector2I Cell, int Slot, PieceKind Kind, Vector2I Size)>(cells.Count);
    var min = cells[0];
    var max = cells[0];
    foreach (var cell in cells) {
      var atlas = layer.GetCellAtlasCoords(cell);
      var size = _sizeInAtlas(tileSet!, layer.GetCellSourceId(cell), atlas);
      // The colour is which tile along its row this is, and a row's tiles are as wide as its piece.
      // A tile from some other tileset names neither a colour nor a piece we know, and a lethal
      // surface nobody meant to author is worse than a neutral one.
      var slot = atlas.X / size.X;
      pieces.Add((
        cell,
        slot is >= 0 and < SLOT_COUNT ? slot : NEUTRAL_SLOT,
        _kindAtlasRow(atlas.Y),
        size
      ));
      min = new Vector2I(Mathf.Min(min.X, cell.X), Mathf.Min(min.Y, cell.Y));
      max = new Vector2I(Mathf.Max(max.X, cell.X + size.X - 1), Mathf.Max(max.Y, cell.Y + size.Y - 1));
    }

    var columns = max.X - min.X + 1;
    var rows = max.Y - min.Y + 1;
    var slots = new int[columns * rows];
    var kinds = new PieceKind[columns * rows];
    var ends = new PieceEnds[columns * rows];
    Array.Fill(slots, EMPTY);

    foreach (var piece in pieces) {
      for (var down = 0; down < piece.Size.Y; down++) {
        for (var along = 0; along < piece.Size.X; along++) {
          var index = ((piece.Cell.Y + down - min.Y) * columns) + piece.Cell.X + along - min.X;
          slots[index] = piece.Slot;
          kinds[index] = piece.Kind;
          ends[index] = _endsOf(piece.Kind, piece.Cell + new Vector2I(along, down), along, down, piece.Size);
        }
      }
    }

    return new ToyBrickGrid(min, columns, rows, cellSize, slots, kinds, ends);
  }

  // How many cells the tile painted here covers. A tile the tileset has never heard of is one cell,
  // the same as the plainest brick there is.
  private static Vector2I _sizeInAtlas(TileSet tileSet, int sourceId, Vector2I atlas) {
    if (sourceId < 0 || tileSet.GetSource(sourceId) is not TileSetAtlasSource source || !source.HasTile(atlas)) {
      return Vector2I.One;
    }
    var size = source.GetTileSizeInAtlas(atlas);
    return new Vector2I(Mathf.Max(size.X, 1), Mathf.Max(size.Y, 1));
  }

  // Which piece a palette row names. Rows are counted off the pieces in order, because a piece
  // taller than one course takes up more than one of them.
  private static PieceKind _kindAtlasRow(int row) {
    var top = 0;
    for (var kind = PieceKind.SideBond; kind <= PieceKind.Head; kind++) {
      top += SizeOf(kind).Y;
      if (row < top) {
        return kind;
      }
    }
    return PieceKind.SideBond;
  }

  private static PieceEnds _endsOf(PieceKind kind, Vector2I cell, int along, int down, Vector2I size) {
    if (kind == PieceKind.SideBond) {
      // The bond: two studs long, and which pair a cell falls in alternates by row, so the joins of
      // one course never line up with the joins of the next.
      return ((cell.X + cell.Y) & 1) == 0 ? PieceEnds.Left : PieceEnds.Right;
    }
    if (ViewOf(kind) == PieceView.Head) {
      // A face rather than a run of studs, so what the cell has to know is which quarter of the
      // head it is drawing.
      return (PieceEnds)(along + (down * size.X));
    }

    var ends = PieceEnds.Middle;
    if (along == 0) {
      ends |= PieceEnds.Left;
    }
    if (along == size.X - 1) {
      ends |= PieceEnds.Right;
    }
    return ends;
  }

  // A brick standing on edge, a plate seen from above, or a head.
  public static PieceView ViewOf(PieceKind kind) => kind switch {
    PieceKind.Head => PieceView.Head,
    >= PieceKind.Top1 => PieceView.Top,
    _ => PieceView.Side,
  };

  // How many cells a piece painted with this tile covers, which is also how big its tile is in the
  // palette. ToyBricks.tres is laid out from exactly this, so the two cannot drift apart without a
  // test noticing.
  public static Vector2I SizeOf(PieceKind kind) => kind switch {
    PieceKind.Side2 or PieceKind.Top2 => new Vector2I(2, 1),
    PieceKind.Side4 or PieceKind.Top4 => new Vector2I(4, 1),
    PieceKind.Head => new Vector2I(2, 2),
    _ => Vector2I.One,
  };

  // Where in the palette the tile for a colour and a piece sits. A row's tiles are as wide as its
  // piece is, so the colours are spaced out along it rather than sitting one to a column - and the
  // rows a taller piece takes up are its own.
  public static Vector2I AtlasCoordsOf(int slot, PieceKind kind) {
    var row = 0;
    for (var earlier = PieceKind.SideBond; earlier < kind; earlier++) {
      row += SizeOf(earlier).Y;
    }
    return new Vector2I(slot * SizeOf(kind).X, row);
  }

  // The colour group a slot answers to, or null for the neutral one - which answers to all of them
  // rather than to none, the same way a neutral flat platform does.
  public static string? GroupOf(int slot) => ColorUtils.FromTileSourceId(slot);

  // The boxes the body is made of: the fewest rectangles the painted cells can be covered by,
  // rather than one per cell. Fewer, larger boxes is what keeps a wall of a few hundred bricks from
  // being a few hundred collision shapes.
  public List<Rect2> SolidBoxes() => _merge(slot => slot != EMPTY);

  // The boxes one colour covers, which is what a face is judged against. Taken per colour rather
  // than per cell for the same reason the body is.
  public List<Rect2> ColorBoxes(int slot) => _merge(cell => cell == slot);

  private List<Rect2> _merge(Func<int, bool> accepts) {
    var boxes = new List<Rect2>();
    if (IsEmpty) {
      return boxes;
    }

    var taken = new bool[_slots.Length];
    for (var row = 0; row < Rows; row++) {
      for (var column = 0; column < Columns; column++) {
        if (!_free(taken, accepts, column, row)) {
          continue;
        }

        var width = 1;
        while (_free(taken, accepts, column + width, row)) {
          width++;
        }
        var height = 1;
        while (_rowFree(taken, accepts, column, row + height, width)) {
          height++;
        }

        for (var y = row; y < row + height; y++) {
          for (var x = column; x < column + width; x++) {
            taken[(y * Columns) + x] = true;
          }
        }

        boxes.Add(new Rect2(
          new Vector2(Origin.X + column, Origin.Y + row) * CellSize,
          new Vector2(width, height) * CellSize
        ));
      }
    }
    return boxes;
  }

  private bool _free(bool[] taken, Func<int, bool> accepts, int column, int row) =>
    column < Columns && row < Rows && !taken[(row * Columns) + column] && accepts(SlotAt(column, row));

  private bool _rowFree(bool[] taken, Func<int, bool> accepts, int column, int row, int width) {
    if (row >= Rows) {
      return false;
    }
    for (var x = column; x < column + width; x++) {
      if (!_free(taken, accepts, x, row)) {
        return false;
      }
    }
    return true;
  }

  // One texel per cell holding everything the shader draws that cell from - the palette slot, which
  // way up the piece is seen, and which of its ends the cell is - padded by an empty cell all round
  // so the studs standing on the top row have somewhere to be drawn and the outer bricks are shaded
  // like the edge of a wall. Held as floats rather than bytes: a colour texture would be taken
  // through the renderer's own colour handling on the way in, and none of this is a colour.
  public ImageTexture BuildMap() {
    var image = Image.CreateEmpty(Columns + 2, Rows + 2, false, Image.Format.Rgbf);
    for (var row = 0; row < Rows; row++) {
      for (var column = 0; column < Columns; column++) {
        var slot = SlotAt(column, row);
        if (slot == EMPTY) {
          continue;
        }
        image.SetPixel(column + 1, row + 1, new Color(
          slot + 1,
          (int)ViewOf(KindAt(column, row)),
          (int)EndsAt(column, row)
        ));
      }
    }
    return ImageTexture.CreateFromImage(image);
  }

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
  // they have turned down. The neutral is left out of it: it answers to every face, so it is safe
  // to walk from any colour onto and back off again.
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
