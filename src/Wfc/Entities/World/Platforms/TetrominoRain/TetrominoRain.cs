namespace Wfc.Entities.World.Platforms;

using Chickensoft.Sync.Primitives;
using System;
using System.Collections.Generic;
using Godot;
using Wfc.Core.Event;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

// The staircase of falling tetrominos a stretch of level is crossed on. Pieces come down fixed
// lanes on a fixed clock - what is random is which of the seven shapes it is and which way round -
// so the level is a rhythm to be read rather than a sky to be waited out.
//
// Three spacings are the level design, and all three are counted in grid cells, origin to origin -
// the same units the pieces themselves are built in:
//
//   LaneSpacing  how far apart the lanes stand. Wider than the widest piece may stand, so there is
//                always a gap between two lanes, and inside one jump of the next lane.
//   RowSpacing   how far apart two pieces stacked in the same lane stand. Taller than a piece by
//                enough to stand under the one above and jump out sideways.
//   Climb        how much higher each lane carries its pieces than the one before it. This is what
//                makes the crossing climb: a hop rightwards is a hop upwards.
//
// What the player actually crosses is those less the pieces themselves, which is NarrowestGap and
// ClearAir. The clock underneath - which lane drops when - is derived from the three of them, so a
// piece always lands on an exact row and the climb per hop does not move when the lanes are opened
// out. _GetConfigurationWarnings checks the lot against the cube's own jump rather than leaving it
// to a playtest.
//
// The rain is seeded, and it is replayed from the start on every respawn, so a crossing that killed
// the player is the same crossing when they come back to it.
[Tool]
[ScenePath]
public partial class TetrominoRain : Node2D {
  private AutoChannel.Binding? _dyingBinding;

  #region Constants
  // The curtain outline and the lane centres, drawn for the author only.
  private static readonly Color BOUNDS_COLOR = new Color(1.0f, 1.0f, 1.0f, 0.18f);
  private static readonly Color LANE_COLOR = new Color(1.0f, 1.0f, 1.0f, 0.35f);
  // The rungs of the staircase: one per piece the lane will be holding.
  private static readonly Color RUNG_COLOR = new Color(0.45f, 1.0f, 0.8f, 0.55f);
  private const float RUNG_REACH = 46.0f;
  private const float BOUNDS_WIDTH = 2.0f;

  private const float MIN_CELL_SIZE = 8.0f;

  // Every piece has a rotation at least this flat, so a lane always has something to drop.
  private const int MIN_PIECE_HEIGHT = 2;

  // Roughly what the cube stands, for the headroom check. Exact enough for a warning.
  private const float PLAYER_HEIGHT = 96.0f;

  // The cube's jump as the arc it actually flies. Mirrored rather than read off the player: these
  // are the one thing about the cube a curtain has to be designed against, and reaching into its
  // state machine for them would tie the rain to how jumping happens to be implemented.
  //
  // The cube is pulled down at this rate on the way up as well as on the way down, which is most of
  // why the arc is so much flatter than a jump speed of this size suggests: it clears about a fifth
  // of what it would under the rise-only gravity, and runs out of ground long before it runs out of
  // height. A curtain designed against the wrong one of those two numbers deals hops that simply
  // cannot be taken.
  private const float PLAYER_JUMP_SPEED = 1200.0f;
  private const float PLAYER_GRAVITY = 2450.0f;
  private const float PLAYER_RUN_SPEED = 350.0f;

  // The narrowest a piece ever stands, in cells. Two of them either side of a gap is the widest that
  // gap ever opens, which is the hop that has to be makeable.
  private const int MIN_PIECE_CELLS = 2;

  // How much of what a hop could reach the worst hop is allowed to ask for. One that asks for all of
  // it is one the player has to fly perfectly, and there is a worst hop on every crossing - which
  // pieces come down is not theirs to choose.
  private const float JUMP_BUDGET = 0.75f;
  #endregion Constants

  #region Exports
  [Export]
  public int LaneCount {
    get => _laneCount;
    set {
      _laneCount = Mathf.Max(value, 1);
      _reread();
    }
  }
  private int _laneCount = 5;

  // How far apart the lanes stand, origin to origin, in cells. This is the jump the crossing is
  // made of. The clear air actually left between two pieces is this less however wide they happen
  // to stand, which is NarrowestGap at its tightest.
  [Export(PropertyHint.Range, "1,12,1,or_greater")]
  public int LaneSpacing {
    get => _laneSpacing;
    set {
      _laneSpacing = Mathf.Max(value, 1);
      _reread();
    }
  }
  private int _laneSpacing = 5;

  // How tall a piece is allowed to stand, in cells. It is the single biggest thing deciding whether
  // the crossing can be made, for three reasons at once:
  //
  //  - a tall piece is a tower rather than a step, and its top is a ledge to be threaded rather than
  //    a surface to be landed on;
  //  - how far a hop has to climb is the stagger plus the difference in height between the piece
  //    being left and the piece being landed on, so the taller pieces are allowed to be, the more
  //    the same hop varies from one pair of pieces to the next;
  //  - two pieces stacked in a lane have to leave room for the taller of them plus the jump out, so
  //    flattening them lets the whole curtain pack closer together.
  //
  // Held flat by default: every shape still comes down, and still either way up, but the ones that
  // stand on end do not.
  [Export(PropertyHint.Range, "2,4,1")]
  public int MaxPieceHeight {
    get => _maxPieceHeight;
    set {
      _maxPieceHeight = Mathf.Clamp(value, MIN_PIECE_HEIGHT, TetrominoShape.MAX_SPAN_CELLS);
      _reread();
    }
  }
  private int _maxPieceHeight = MIN_PIECE_HEIGHT;

  // How far apart two pieces stacked in the same lane stand, origin to origin, in cells. The clear
  // air left over a piece is this less how tall it stands, which is ClearAir at its tightest, and
  // that is what has to be stood up in and jumped out of.
  //
  // In cells rather than as the interval it used to be: the rain is a clock underneath, so this was
  // a number of seconds whose effect on the crossing depended on the fall speed, and it only landed
  // pieces on exact rows at whole multiples of StepInterval. Counting rows makes both automatic.
  // Floored at one rather than at something that clears a piece: a spacing too tight to stand in is
  // warned about below rather than silently raised, so what is typed is what the curtain runs.
  [Export(PropertyHint.Range, "1,16,1,or_greater")]
  public int RowSpacing {
    get => _rowSpacing;
    set {
      _rowSpacing = Mathf.Max(value, 1);
      _reread();
    }
  }
  private int _rowSpacing = 7;

  // How much higher than its left-hand neighbour each lane carries its pieces, in cells. This is the
  // climb the crossing is made of, and it is held from both ends: too little and a hop loses more
  // height to the piece's own fall than the climb buys back, too much and the hop cannot be flown.
  //
  // In cells rather than the share of the interval it used to be, which was the worst of the lot:
  // it silently kept only its fractional part, so a whole number meant no climb at all, and because
  // it was a share, opening the lanes out vertically steepened every hop along with them.
  [Export(PropertyHint.Range, "0,8,1,or_greater")]
  public int Climb {
    get => _climb;
    set {
      _climb = Mathf.Max(value, 0);
      _reread();
    }
  }
  private int _climb = 2;

  // How far a piece falls through the curtain before it frees itself.
  [Export]
  public float FallHeight {
    get => _fallHeight;
    set {
      _fallHeight = value;
      _reread();
    }
  }
  private float _fallHeight = 2400.0f;

  [Export]
  public float CellSize {
    get => _cellSize;
    set {
      _cellSize = Mathf.Max(value, MIN_CELL_SIZE);
      _reread();
    }
  }
  private float _cellSize = Constants.TETRIS_BLOCK_SIZE;

  // How long a piece spends on each row it reaches, the drop onto it and the pause on it together.
  // The drop itself is bounded by a fall speed, so nearly all of this is the pause - lengthen it and
  // the piece sits longer on each row, which is the whole reason a falling tetromino can be stood on
  // at all.
  //
  // It also sets how fast a piece descends, so SpawnInterval has to move with it to keep the same
  // room between two pieces stacked in a lane.
  [Export]
  public float StepInterval {
    get => _stepInterval;
    set {
      _stepInterval = value;
      _reread();
    }
  }
  private float _stepInterval = 0.8f;

  // Which rain this is. Two curtains in the same level want different seeds, or they drop the same
  // shapes in the same order as each other.
  [Export]
  public int Seed { get; set; }
  #endregion Exports

  #region Fields
  private readonly RandomNumberGenerator _rng = new RandomNumberGenerator();
  private readonly List<TetrominoPlatform> _pieces = new List<TetrominoPlatform>();
  private readonly List<int> _fittingRotations = new List<int>(TetrominoShape.ROTATION_COUNT);
  // How many pieces each lane has dropped. With the lane's own share of the interval it is the whole
  // clock: piece k of lane i is due at (k + phase) * SpawnInterval.
  private int[] _dropped = [];
  private float _elapsed;
  private bool _isPaused;
  private bool _isSubscribed;
  #endregion Fields

  // How fast a piece comes down on average, rows and the pauses on them together.
  public float FallSpeed => StepInterval > 0.0f ? CellSize / StepInterval : 0.0f;

  #region Derived
  // The clock the curtain actually runs on. It is all read off the spacings above rather than
  // authored: a lane drops every RowSpacing rows, and each lane is Climb rows behind the last.
  public float SpawnInterval => RowSpacing * StepInterval;
  public float LaneStagger => RowSpacing > 0 ? (float)Climb / RowSpacing : 0.0f;

  public float LaneSpacingPx => LaneSpacing * CellSize;

  // The drop between two pieces stacked in the same lane.
  public float LanePitch => RowSpacing * CellSize;

  // How much higher the next lane's piece stands than the one being stood on, measured between the
  // two pieces rather than between the surfaces the player actually leaves and lands on.
  public float StaggerRise => Climb * CellSize;

  // The widest a piece is ever allowed to stand, given how tall they may be: flattening pieces is
  // what makes them wide, so this moves against MaxPieceHeight.
  public int WidestPieceCells {
    get {
      var widest = 1;
      foreach (var kind in TetrominoShape.KINDS) {
        for (var rotation = 0; rotation < TetrominoShape.ROTATION_COUNT; rotation++) {
          if (TetrominoShape.HeightOf(kind, rotation) <= MaxPieceHeight) {
            widest = Mathf.Max(widest, TetrominoShape.WidthOf(kind, rotation));
          }
        }
      }
      return widest;
    }
  }

  // What the two spacings actually leave between pieces once the pieces themselves are accounted
  // for - the numbers the crossing is really made of, and the ones worth reading back.
  public float NarrowestGap => (LaneSpacing - WidestPieceCells) * CellSize;
  public float ClearAir => (RowSpacing - MaxPieceHeight) * CellSize;
  #endregion Derived

  // How far the worst hop the curtain can deal has to climb. A hop leaves the top of one piece and
  // lands on the top of another, so how tall each of them happens to stand is part of it: the worst
  // is the flattest piece under the player and the tallest one to land on.
  public float WorstHopRise => StaggerRise + ((MaxPieceHeight - 1) * CellSize);

  // The widest a gap between two lanes ever opens: both pieces standing at their narrowest, so each
  // reaches only half its own width out of its lane towards the other.
  public float WidestGap => LaneSpacingPx - (MIN_PIECE_CELLS * CellSize);

  // How much climb a hop across that gap can actually buy. Two things pay for it: how much height
  // the cube has left once it has covered the ground, and how far the piece it is aimed at has come
  // down to meet it while the cube was in the air.
  public float ReachableRise {
    get {
      var flight = WidestGap / PLAYER_RUN_SPEED;
      var height = (PLAYER_JUMP_SPEED * flight) - (0.5f * PLAYER_GRAVITY * flight * flight);
      return height + (FallSpeed * flight);
    }
  }

  public float LaneX(int lane) => lane * LaneSpacingPx;

  public override void _Ready() {
    base._Ready();
    if (Engine.IsEditorHint()) {
      SetPhysicsProcess(false);
      QueueRedraw();
      return;
    }
    _restart();
  }

  public override void _EnterTree() {
    base._EnterTree();
    if (Engine.IsEditorHint() || _isSubscribed) {
      return;
    }
    _dyingBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.PlayerDying _) => _onPlayerDying());
    EventHandler.Instance.Events.CheckpointLoaded += _restart;
    _isSubscribed = true;
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (!_isSubscribed) {
      return;
    }
    _dyingBinding?.Dispose();
    _dyingBinding = null;
    EventHandler.Instance.Events.CheckpointLoaded -= _restart;
    _isSubscribed = false;
  }

  public override string[] _GetConfigurationWarnings() {
    var warnings = new List<string>();
    if (SpawnInterval <= 0.0f) {
      warnings.Add("SpawnInterval is zero, so a lane drops a piece on every tick.");
    }
    if (NarrowestGap <= 0.0f) {
      warnings.Add($"LaneSpacing of {LaneSpacing} cells is no wider than the {WidestPieceCells} cells a piece may stand, so pieces in adjacent lanes touch and there is no gap to jump.");
    }

    if (ClearAir <= 0.0f) {
      warnings.Add($"RowSpacing of {RowSpacing} cells is no taller than the {MaxPieceHeight} cells a piece may stand, so pieces stacked in a lane touch and there is nothing to stand in.");
    }
    else if (ClearAir - PLAYER_HEIGHT < StaggerRise) {
      warnings.Add($"RowSpacing of {RowSpacing} cells leaves less clear air over a piece than a hop to the next lane needs. Raise it, drop Climb, or flatten MaxPieceHeight.");
    }

    // Which pieces come down is not the player's to choose, so the hop that has to be makeable is
    // the worst pairing the curtain can deal: the flattest piece to stand on and the tallest to land
    // on, whose tops are that much further apart than the stagger alone.
    if (WorstHopRise > ReachableRise * JUMP_BUDGET) {
      warnings.Add("The worst pairing of pieces this curtain can deal asks for more climb than a hop across its widest gap can buy, so some hops cannot be made at all. Lower Climb or MaxPieceHeight, or close up LaneSpacing.");
    }

    // The piece being jumped to is coming down as well, so a hop only gains height while the stagger
    // is worth more than the fall the jump costs.
    var flight = PLAYER_RUN_SPEED > 0.0f ? LaneSpacingPx / PLAYER_RUN_SPEED : 0.0f;
    if (Climb * StepInterval <= flight) {
      warnings.Add("Climb is too low to outrun the fall, so a hop to the next lane loses height instead of gaining it.");
    }

    return [.. warnings];
  }

  public override void _PhysicsProcess(double delta) {
    if (_isPaused || SpawnInterval <= 0.0f) {
      return;
    }
    _elapsed += (float)delta;
    _dropDuePieces();
  }

  public override void _Draw() {
    if (!Engine.IsEditorHint()) {
      return;
    }
    var half = LaneSpacingPx / 2.0f;
    DrawRect(
      new Rect2(-half, 0.0f, LaneCount * LaneSpacingPx, FallHeight),
      BOUNDS_COLOR,
      filled: false,
      width: BOUNDS_WIDTH
    );
    for (var lane = 0; lane < LaneCount; lane++) {
      var x = LaneX(lane);
      DrawLine(new Vector2(x, 0.0f), new Vector2(x, FallHeight), LANE_COLOR, BOUNDS_WIDTH);
      _drawLaneRungs(lane, x);
    }
  }

  // Where this lane's pieces will actually stand. Without them the two knobs that decide the whole
  // shape of the crossing - how far apart the pieces come and how far out of step the lanes are -
  // are numbers with nothing on screen answering to them, and the only way to see what they did is
  // to play the level.
  private void _drawLaneRungs(int lane, float x) {
    var pitch = LanePitch;
    if (pitch <= 0.0f) {
      return;
    }
    // Later lanes carry their pieces higher, so their rungs sit back up the lane by that much.
    for (var y = Mathf.PosMod(-_phaseOf(lane) * pitch, pitch); y <= FallHeight; y += pitch) {
      DrawLine(new Vector2(x - RUNG_REACH, y), new Vector2(x + RUNG_REACH, y), RUNG_COLOR, BOUNDS_WIDTH);
    }
  }

  // Everything the curtain is holding is dropped and the dice go back to where they started, which
  // is what makes the retry the same crossing.
  //
  // It reopens with the pieces that would already be on their way rather than with an empty sky: a
  // curtain that filled from the top would leave the player standing at the edge for a whole fall
  // before the first piece reached them, on the first attempt and on every retry after it.
  private void _restart() {
    foreach (var piece in _pieces) {
      if (IsInstanceValid(piece)) {
        piece.QueueFree();
      }
    }
    _pieces.Clear();
    _rng.Seed = (ulong)Seed;
    _dropped = new int[LaneCount];
    _elapsed = FallSpeed > 0.0f ? FallHeight / FallSpeed : 0.0f;
    _isPaused = false;
    _dropDuePieces();
  }

  private void _onPlayerDying() => _isPaused = true;

  private void _dropDuePieces() {
    _pieces.RemoveAll(piece => !IsInstanceValid(piece));

    for (var lane = 0; lane < _dropped.Length; lane++) {
      var phase = _phaseOf(lane);
      // While rather than if: reopening the curtain has a lane's whole backlog fall due at once, and
      // a frame long enough to owe two pieces owes them both.
      while ((_dropped[lane] + phase) * SpawnInterval <= _elapsed) {
        var secondsAgo = _elapsed - ((_dropped[lane] + phase) * SpawnInterval);
        _dropped[lane] += 1;
        _drop(lane, secondsAgo);
      }
    }
  }

  // How far into its own cycle a lane is, as a share of the interval. Later than the lane before it,
  // which is what leaves its pieces higher up and makes a hop rightwards a hop upwards.
  private float _phaseOf(int lane) {
    var phase = lane * LaneStagger;
    return phase - Mathf.Floor(phase);
  }

  private void _drop(int lane, float secondsAgo) {
    var kind = TetrominoShape.KINDS[_rng.RandiRange(0, TetrominoShape.KINDS.Length - 1)];
    var rotation = _pickRotation(kind);
    var span = TetrominoShape.SpanOf(kind, rotation);

    // How far below the top of the curtain a piece has to be past before there is nothing of it
    // left to see. Anything already there was dropped before the window the curtain reopened into.
    var fallDistance = FallHeight + ((span.Size.Y + 1) * CellSize);
    if (secondsAgo * FallSpeed >= fallDistance) {
      return;
    }

    var piece = SceneHelpers.InstantiateNode<TetrominoPlatform>();
    piece.CellSize = CellSize;
    piece.StepInterval = StepInterval;
    piece.Kind = kind;
    piece.RotationIndex = rotation;
    piece.FallDistance = fallDistance;
    // Placed by the middle of its own box rather than by its origin, so a lane is a lane whatever
    // shape comes down it - a piece whose cells hang to one side of its origin would stand off
    // centre and eat into the gap beside it.
    piece.Position = new Vector2(
      LaneX(lane) - ((span.Position.X + span.End.X) * 0.5f * CellSize),
      -(span.End.Y + 0.5f) * CellSize
    );

    AddChild(piece);
    piece.DropFrom(secondsAgo);
    _pieces.Add(piece);
  }

  // Rolled among the rotations that stand within MaxPieceHeight rather than among all four, and
  // rolled after the shape rather than instead of it - the colour that comes down is the shape's,
  // so picking the shape first keeps all seven colours evenly likely however many of a shape's
  // rotations are too tall to use.
  private int _pickRotation(TetrominoShape.Kind kind) {
    _fittingRotations.Clear();
    for (var rotation = 0; rotation < TetrominoShape.ROTATION_COUNT; rotation++) {
      if (TetrominoShape.HeightOf(kind, rotation) <= MaxPieceHeight) {
        _fittingRotations.Add(rotation);
      }
    }
    return _fittingRotations[_rng.RandiRange(0, _fittingRotations.Count - 1)];
  }

  // Nothing about the lanes can be re-read once the level is playing, so the only thing listening is
  // the editor.
  private void _reread() {
    if (Engine.IsEditorHint()) {
      UpdateConfigurationWarnings();
      QueueRedraw();
    }
  }
}
