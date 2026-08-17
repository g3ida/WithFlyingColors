namespace Wfc.Entities.World.Platforms;

using System;
using System.Collections.Generic;
using Godot;
using Wfc.Core.Event;
using Wfc.Screens.Levels;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

// A tetromino coming down through a level the way a piece comes down the pool: a cell at a time,
// standing still on every row it reaches. Each of its cells is a flat platform in its own right,
// so the piece is landed on, walked along and killed by exactly like any other surface of its
// colour - what makes it a level of its own is that the surface is only there for a moment, and is
// on its way to the fallzone with whoever stayed on it.
//
// The piece is dropped from wherever it is placed, falls FallDistance and frees itself. Nothing
// stops it on the way: it has no floor to land on and no stack to settle into, and a level that
// wants a piece to be caught puts something under it.
[Tool]
[ScenePath]
public partial class TetrominoPlatform : Node2D {
  #region Exports
  // Which of the seven pieces, and which of its four rotations. Both are fixed for the piece's
  // whole life - a tetromino nobody is playing has no reason to turn.
  [Export]
  public TetrominoShape.Kind Kind {
    get => _kind;
    set {
      _kind = value;
      _rebuild();
    }
  }
  private TetrominoShape.Kind _kind = TetrominoShape.Kind.T;

  [Export]
  public int RotationIndex {
    get => _rotationIndex;
    set {
      _rotationIndex = value;
      _rebuild();
    }
  }
  private int _rotationIndex;

  // How big one cell of the piece is, in pixels. It is the row the piece descends by as well as
  // the square it is drawn as, so the two cannot drift apart.
  [Export]
  public float CellSize {
    get => _fall.CellSize;
    set {
      _fall.CellSize = Mathf.Max(value, MIN_CELL_SIZE);
      _rebuild();
    }
  }

  // How long the piece spends on each row, the descent to it and the pause on it together.
  [Export]
  public float StepInterval {
    get => _fall.StepInterval;
    set => _fall.StepInterval = value;
  }

  // How far the piece falls before it frees itself, measured from where it was dropped. This is
  // what keeps a level's worth of pieces from piling up below the fallzone forever.
  [Export]
  public float FallDistance { get; set; } = 2400.0f;
  #endregion Exports

  #region Constants
  private const float MIN_CELL_SIZE = 8.0f;

  #endregion Constants

  #region Fields
  private readonly TetrominoFall _fall = new TetrominoFall();
  private readonly List<TetrominoCell> _cells = new List<TetrominoCell>();
  // Where each cell sits on the piece, before any of the descent is added on. The piece's own node
  // never moves - see _PhysicsProcess - so this is what the drop is measured out from.
  private readonly List<Vector2> _cellOrigins = new List<Vector2>();
  // Every cell of the piece, for the escape probe to leave out. The engine answers an overlap the
  // cube starts out in with a collision whichever way the test motion points, and while a piece is
  // into the cube its other cells are exactly such an overlap.
  private readonly Godot.Collections.Array<Rid> _cellBodies = new Godot.Collections.Array<Rid>();
  // The exported setters fire while the scene is still loading, before there is a tree to hang
  // cells off.
  private bool _isBuilt;
  #endregion Fields

  public override void _Ready() {
    base._Ready();
    _isBuilt = true;
    _rebuild();

    if (Engine.IsEditorHint()) {
      SetPhysicsProcess(false);
    }
  }

  // How far the piece has come down since it was dropped.
  public float Descended => _fall.Descended;

  // Starts the piece partway through the fall it would have made had it been dropped this long ago.
  // What lets a curtain open with pieces already on their way down rather than with an empty sky
  // the player has to wait out.
  public void DropFrom(float secondsAgo) => _fall.Skip(secondsAgo);

  // The box the piece's cells fill, in the frame it was placed in. What a spawner reads to keep
  // from dropping a piece into one already on its way down: every piece descends at the same rate,
  // so two that do not overlap when the second is dropped never will.
  public Rect2 Bounds {
    get {
      var size = new Vector2(CellSize, CellSize);
      if (_cells.Count == 0) {
        return new Rect2(Position, Vector2.Zero);
      }
      var bounds = new Rect2(Position + _cells[0].Position - (size * 0.5f), size);
      foreach (var cell in _cells) {
        bounds = bounds.Merge(new Rect2(Position + cell.Position - (size * 0.5f), size));
      }
      return bounds;
    }
  }

  public override string[] _GetConfigurationWarnings() {
    var warnings = new List<string>();
    if (StepInterval <= 0.0f) {
      warnings.Add("StepInterval is zero, so the piece never pauses on a row and falls as one continuous drop.");
    }
    if (FallDistance <= 0.0f) {
      warnings.Add("FallDistance is zero, so the piece frees itself on its first tick and is never seen.");
    }
    return [.. warnings];
  }

  // Every cell is carried by its own transform rather than the piece's. A body that syncs to
  // physics is driven by the physics server, and a parent moved out from over it only takes the
  // drawing along: the collider stays where the server last put it, so the piece descends on screen
  // while the player stands on the row it appears to have left.
  public override void _PhysicsProcess(double delta) {
    _fall.Step(delta);
    var drop = new Vector2(0.0f, _fall.Descended);
    for (var index = 0; index < _cells.Count; index++) {
      _cells[index].Position = _cellOrigins[index] + drop;
    }
    _reportCrushedPlayer();

    if (_fall.Descended >= FallDistance) {
      QueueFree();
    }
  }

  // A piece is carried by transform, so nothing in the engine stops it at the player: left to
  // itself it comes down through the cube, and the death reported is whichever colour happened to
  // be touched on the way in. So the piece does what a real kinematic pusher would - shoves the
  // cube along ahead of it while the cube has anywhere left to go, and reports the crush once it
  // has not.
  //
  // Which face the cube has turned upwards is not asked about here. A face the piece can kill is
  // killed by the colour areas on contact, the same as walking into the piece's side, and a face
  // it cannot is the one being carried; either way what decides a crush is only ever whether there
  // is room left.
  private void _reportCrushedPlayer() {
    if (Mathf.IsZeroApprox(_fall.Travelled)) {
      return;
    }
    var player = GameRepo.Instance.Player.Value;
    if (player is null || !IsInstanceValid(player) || !player.IsInsideTree() || player.IsDying()) {
      return;
    }

    var half = player.GetCollisionHalfExtents();
    var body = new Rect2(player.GlobalPosition - half, half * 2.0f);

    foreach (var cell in _cells) {
      var crusher = _worldRectOf(cell);
      if (!PlatformCrush.HasArrivedInto(crusher, body, Vector2.Down)) {
        continue;
      }
      if (_hasSomewhereToGo(player, PlatformCrush.EscapeMotion(crusher, body, Vector2.Down))) {
        continue;
      }
      GameEvents.Instance.OnPlayerDying(
        this,
        PlatformCrush.ContactPoint(crusher, body, Vector2.Down),
        EntityType.Crusher
      );
      return;
    }
  }

  // A cell's size is its collision box, so the box needs no hunting for.
  private static Rect2 _worldRectOf(TetrominoCell cell) {
    var size = new Vector2(cell.Size, cell.Size) * cell.GlobalScale.Abs();
    return new Rect2(cell.GlobalPosition - (size * 0.5f), size);
  }

  private bool _hasSomewhereToGo(CharacterBody2D body, Vector2 escape) {
    var probe = new PhysicsTestMotionParameters2D {
      From = body.GlobalTransform,
      Motion = escape,
      ExcludeBodies = _cellBodies,
    };
    return !PhysicsServer2D.BodyTestMotion(body.GetRid(), probe);
  }

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
    _cellOrigins.Clear();
    _cellBodies.Clear();

    var group = TetrominoShape.ColorGroupOf(Kind);
    foreach (var offset in TetrominoShape.CellsOf(Kind, RotationIndex)) {
      var cell = SceneHelpers.InstantiateNode<TetrominoCell>();
      cell.Group = group;
      cell.Size = CellSize;
      var origin = new Vector2(offset.X, offset.Y) * CellSize;
      cell.Position = origin;
      AddChild(cell);
      // A piece is spawned into a level already playing and its cells move from their first tick.
      // Without this a cell is drawn sweeping in from wherever the interpolation last saw it.
      cell.ResetPhysicsInterpolation();
      _cells.Add(cell);
      _cellOrigins.Add(origin);
      _cellBodies.Add(cell.GetRid());
    }
  }
}
