namespace Wfc.Entities.World.Platforms;

using System.Collections.Generic;
using System.Linq;
using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;

// Tetrominos that have already settled, laid into a level as ground: the stack at the bottom of a
// tetris well rather than the pieces still coming down it. The cells are the ones TetrominoRain
// drops - the same block art, the same rule about which face may touch which colour - and all that
// is different about them is that the author put them where they are and they stay there.
//
// A stack is written out as a grid of characters, one per cell and a line per row:
//
//     ........SSZZ....
//     ....LLLLOOOOTTTT
//     IIIIZZSSTTTTOOOO
//
// Each letter names one of the seven pieces, and so the colour that cell wears; a dot is a hole.
// Nothing asks that the letters spell whole tetrominos - a settled stack is what a well looks like
// after lines have been cleared out of it, not a bag of pieces still in one piece each. The node's
// own position is the top-left corner of the grid, so the first character of the first line is the
// cell it sits on.
//
// What is asked is that the surface can be crossed. The cube dies on touching a colour its downward
// face does not accept, and walking is touching, so a stretch of surface that changes colour
// partway along kills whoever walks it whichever face they have turned down. Every stretch at one
// height therefore has to be a single colour, and a change of colour has to come with a change of
// height - which is crossed by jumping, and a jump is where the cube can turn a new face down.
// _GetConfigurationWarnings holds the author to it.
[Tool]
[ScenePath]
public partial class TetrominoStack : Node2D {
  #region Constants
  // A cell nothing was laid into. A space reads as one too, so a map can be indented to line up.
  public const char HOLE = '.';

  private const float MIN_CELL_SIZE = 8.0f;
  #endregion Constants

  #region Exports
  // The grid, a line per row. Held as written rather than tidied up: the row and column a character
  // is on is where its cell goes, so trimming the map would move the stack.
  [Export(PropertyHint.MultilineText)]
  public string Map {
    get => _map;
    set {
      _map = value;
      _rebuild();
    }
  }
  private string _map = "";

  [Export]
  public float CellSize {
    get => _cellSize;
    set {
      _cellSize = Mathf.Max(value, MIN_CELL_SIZE);
      _rebuild();
    }
  }
  private float _cellSize = Constants.TETRIS_BLOCK_SIZE;
  #endregion Exports

  #region Fields
  private readonly List<TetrominoCell> _cells = new List<TetrominoCell>();
  // The exported setters fire while the scene is still loading, before there is a tree to hang
  // cells off.
  private bool _isBuilt;
  #endregion Fields

  public override void _Ready() {
    base._Ready();
    _isBuilt = true;
    _rebuild();
  }

  // The box the stack's cells fill, in the frame it was placed in.
  public Rect2 Bounds {
    get {
      if (_cells.Count == 0) {
        return new Rect2(Position, Vector2.Zero);
      }
      var size = new Vector2(CellSize, CellSize);
      var bounds = new Rect2(Position + _cells[0].Position - (size * 0.5f), size);
      foreach (var cell in _cells) {
        bounds = bounds.Merge(new Rect2(Position + cell.Position - (size * 0.5f), size));
      }
      return bounds;
    }
  }

  public override string[] _GetConfigurationWarnings() {
    var warnings = new List<string>();
    var rows = _rows();

    var unknown = new SortedSet<char>(
      rows.SelectMany(row => row).Where(character => !_isHole(character) && _kindOf(character) is null)
    );
    if (unknown.Count > 0) {
      warnings.Add(
        $"Map has characters naming no piece ({string.Join(' ', unknown)}), and their cells are left as holes. A cell is one of I J L O S T Z, and a hole is a dot."
      );
    }

    if (!rows.Any(row => row.Any(character => _kindOf(character) is not null))) {
      warnings.Add("Map has no cells in it, so the stack is empty.");
      return [.. warnings];
    }

    foreach (var run in _surfaceRuns(rows)) {
      var group = TetrominoShape.ColorGroupOf(_kindOf(rows[run.Row][run.Start])!.Value);
      for (var column = run.Start + 1; column <= run.End; column++) {
        if (TetrominoShape.ColorGroupOf(_kindOf(rows[run.Row][column])!.Value) == group) {
          continue;
        }
        warnings.Add(
          $"The surface on row {run.Row} changes colour between columns {column - 1} and {column} without changing height, so whoever walks along it is killed partway across whichever face they have turned down. Step the colours up or down instead of butting them together."
        );
        break;
      }
    }

    return [.. warnings];
  }

  // The stretches of surface the cube walks: cells with nothing laid on top of them, taken in runs
  // of neighbours at the same height. Two of them meeting is a step, which is jumped rather than
  // walked - so a run is exactly as far as the cube gets without a chance to turn a new face down.
  private static List<(int Row, int Start, int End)> _surfaceRuns(List<string> rows) {
    var runs = new List<(int Row, int Start, int End)>();
    for (var row = 0; row < rows.Count; row++) {
      var start = -1;
      for (var column = 0; column <= rows[row].Length; column++) {
        if (_isExposed(rows, row, column)) {
          start = start < 0 ? column : start;
          continue;
        }
        if (start >= 0) {
          runs.Add((row, start, column - 1));
          start = -1;
        }
      }
    }
    return runs;
  }

  private static bool _isExposed(List<string> rows, int row, int column) =>
    _isFilled(rows, row, column) && !_isFilled(rows, row - 1, column);

  private static bool _isFilled(List<string> rows, int row, int column) =>
    row >= 0
    && row < rows.Count
    && column >= 0
    && column < rows[row].Length
    && _kindOf(rows[row][column]) is not null;

  private List<string> _rows() => [.. Map.Replace("\r", "").Split('\n')];

  private static bool _isHole(char character) => character is HOLE or ' ';

  private static TetrominoShape.Kind? _kindOf(char character) => char.ToUpperInvariant(character) switch {
    'I' => TetrominoShape.Kind.I,
    'J' => TetrominoShape.Kind.J,
    'L' => TetrominoShape.Kind.L,
    'O' => TetrominoShape.Kind.O,
    'S' => TetrominoShape.Kind.S,
    'T' => TetrominoShape.Kind.T,
    'Z' => TetrominoShape.Kind.Z,
    _ => null,
  };

  private void _rebuild() {
    if (!_isBuilt) {
      return;
    }

    foreach (var cell in _cells) {
      // Removed as well as freed: a queued free still leaves the cell a child for the rest of the
      // frame, and in the editor a rebuild per keystroke stacks those up on top of each other.
      RemoveChild(cell);
      cell.QueueFree();
    }
    _cells.Clear();

    var rows = _rows();
    for (var row = 0; row < rows.Count; row++) {
      for (var column = 0; column < rows[row].Length; column++) {
        if (_kindOf(rows[row][column]) is not { } kind) {
          continue;
        }
        var cell = SceneHelpers.InstantiateNode<TetrominoCell>();
        cell.Group = TetrominoShape.ColorGroupOf(kind);
        cell.Size = CellSize;
        // Nothing here ever moves, so there is no transform for the physics server to chase.
        cell.SyncToPhysics = false;
        cell.Position = new Vector2(column + 0.5f, row + 0.5f) * CellSize;
        AddChild(cell);
        _cells.Add(cell);
      }
    }

    if (Engine.IsEditorHint()) {
      UpdateConfigurationWarnings();
    }
  }
}
