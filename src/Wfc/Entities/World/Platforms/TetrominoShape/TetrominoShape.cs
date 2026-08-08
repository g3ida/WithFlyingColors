namespace Wfc.Entities.World.Platforms;

using System;
using Godot;
using Wfc.Utils.Colors;

// The seven tetrominos as plain data: the cells a piece covers in each of its four rotations,
// and the colour it wears. Both are taken from the pieces the tetris minigame drops, so a piece
// raining through a level and a piece falling down the pool are recognisably the same piece - and
// a player who has learned which shape is safe to stand on in one is right about the other.
//
// Cells are offsets from the piece's own origin, in whole cells, y downwards.
public static class TetrominoShape {
  // Exported on TetrominoPlatform, so the ordinals are serialized into .tscn files: only ever
  // append members here.
  public enum Kind {
    I,
    J,
    L,
    O,
    S,
    T,
    Z,
  }

  public const int ROTATION_COUNT = 4;
  public const int CELL_COUNT = 4;

  // The most cells a piece ever reaches across, which is the I lying flat. Anything spacing pieces
  // out has to clear it, since a spacing that only clears the average piece fails on the one piece
  // that does not fit.
  public const int MAX_SPAN_CELLS = 4;

  private static readonly Vector2I[][] I_ROTATIONS = [
    [new(-1, 0), new(0, 0), new(1, 0), new(2, 0)],
    [new(0, 1), new(0, 0), new(0, -1), new(0, -2)],
    [new(1, 0), new(0, 0), new(-1, 0), new(-2, 0)],
    [new(0, -1), new(0, 0), new(0, 1), new(0, 2)],
  ];

  private static readonly Vector2I[][] J_ROTATIONS = [
    [new(1, 0), new(0, 0), new(-1, 0), new(-1, -1)],
    [new(0, 1), new(0, 0), new(0, -1), new(1, -1)],
    [new(-1, 0), new(0, 0), new(1, 0), new(1, 1)],
    [new(0, -1), new(0, 0), new(0, 1), new(-1, 1)],
  ];

  private static readonly Vector2I[][] L_ROTATIONS = [
    [new(-1, 0), new(0, 0), new(1, 0), new(1, -1)],
    [new(0, -1), new(0, 0), new(0, 1), new(1, 1)],
    [new(1, 0), new(0, 0), new(-1, 0), new(-1, 1)],
    [new(0, 1), new(0, 0), new(0, -1), new(-1, -1)],
  ];

  private static readonly Vector2I[][] O_ROTATIONS = [
    [new(-1, 0), new(0, 0), new(-1, 1), new(0, 1)],
    [new(-1, 0), new(0, 0), new(-1, 1), new(0, 1)],
    [new(-1, 0), new(0, 0), new(-1, 1), new(0, 1)],
    [new(-1, 0), new(0, 0), new(-1, 1), new(0, 1)],
  ];

  private static readonly Vector2I[][] S_ROTATIONS = [
    [new(-1, 0), new(0, 0), new(0, -1), new(1, -1)],
    [new(0, -1), new(0, 0), new(1, 0), new(1, 1)],
    [new(1, 0), new(0, 0), new(0, 1), new(-1, 1)],
    [new(0, 1), new(0, 0), new(-1, 0), new(-1, -1)],
  ];

  private static readonly Vector2I[][] T_ROTATIONS = [
    [new(-1, 0), new(0, 0), new(1, 0), new(0, -1)],
    [new(0, -1), new(0, 0), new(0, 1), new(1, 0)],
    [new(1, 0), new(0, 0), new(-1, 0), new(0, 1)],
    [new(0, -1), new(0, 0), new(0, 1), new(-1, 0)],
  ];

  private static readonly Vector2I[][] Z_ROTATIONS = [
    [new(1, 0), new(0, 0), new(0, -1), new(-1, -1)],
    [new(0, 1), new(0, 0), new(1, 0), new(1, -1)],
    [new(1, 0), new(0, 0), new(0, -1), new(-1, -1)],
    [new(0, 1), new(0, 0), new(1, 0), new(1, -1)],
  ];

  public static readonly Kind[] KINDS = [Kind.I, Kind.J, Kind.L, Kind.O, Kind.S, Kind.T, Kind.Z];

  // The rotation is taken modulo the four rather than rejected, so a caller is free to count turns
  // from wherever it likes.
  public static Vector2I[] CellsOf(Kind kind, int rotation) =>
    _rotationsOf(kind)[((rotation % ROTATION_COUNT) + ROTATION_COUNT) % ROTATION_COUNT];

  // The box a piece fills, in whole cells around its own origin. End is the last cell it reaches
  // rather than one past it, which is what a spacing is measured against.
  public static Rect2I SpanOf(Kind kind, int rotation) {
    var cells = CellsOf(kind, rotation);
    var min = cells[0];
    var max = cells[0];
    foreach (var cell in cells) {
      min = new Vector2I(Math.Min(min.X, cell.X), Math.Min(min.Y, cell.Y));
      max = new Vector2I(Math.Max(max.X, cell.X), Math.Max(max.Y, cell.Y));
    }
    return new Rect2I(min, max - min);
  }

  // How tall the piece stands in this rotation, in cells. A piece standing taller than the room
  // between two of them is a wall rather than a platform, and the I on its end is the one that does
  // it: four cells tall and one cell wide, which is a tower to be jumped over and a ledge narrower
  // than the cube that has to land on it.
  public static int HeightOf(Kind kind, int rotation) => SpanOf(kind, rotation).Size.Y + 1;

  public static int WidthOf(Kind kind, int rotation) => SpanOf(kind, rotation).Size.X + 1;

  public static string ColorGroupOf(Kind kind) => kind switch {
    Kind.I or Kind.O => ColorUtils.YELLOW,
    Kind.J or Kind.L => ColorUtils.PINK,
    Kind.S or Kind.Z => ColorUtils.BLUE,
    _ => ColorUtils.PURPLE,
  };

  private static Vector2I[][] _rotationsOf(Kind kind) => kind switch {
    Kind.I => I_ROTATIONS,
    Kind.J => J_ROTATIONS,
    Kind.L => L_ROTATIONS,
    Kind.O => O_ROTATIONS,
    Kind.S => S_ROTATIONS,
    Kind.T => T_ROTATIONS,
    _ => Z_ROTATIONS,
  };
}
