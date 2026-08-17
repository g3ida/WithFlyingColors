namespace Wfc.Entities.Tetris;

using Chickensoft.Sync.Primitives;
using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Audio;
using Wfc.Core.Event;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using Wfc.Entities.Tetris.Tetrominos;
using Wfc.Entities.World.Platforms;
using Wfc.Entities.World.Player;
using Wfc.Screens.Levels;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class TetrisPool : Node2D, IPersistent {
  private AutoChannel.Binding? _dyingBinding;


  #region Signals
  public override void _Notification(int what) => this.Notify(what);
  [Signal]
  public delegate void LinesRemovedEventHandler(int count);

  [Signal]
  public delegate void GameOverEventHandler();
  #endregion Signals

  // Kept apart from the bag so the opening rule below can recognise them.
  private static readonly PackedScene _sBlock = SceneHelpers.LoadScene<S_Block>();
  private static readonly PackedScene _zBlock = SceneHelpers.LoadScene<Z_Block>();

  private static readonly List<PackedScene> _tetrominos = new List<PackedScene> {
        _sBlock,
        _zBlock,
        SceneHelpers.LoadScene<L_Block>(),
        SceneHelpers.LoadScene<J_Block>(),
        SceneHelpers.LoadScene<O_Block>(),
        SceneHelpers.LoadScene<T_Block>(),
        SceneHelpers.LoadScene<I_Block>()
    };

  private Godot.Collections.Array<PackedScene> _randomBag = new Godot.Collections.Array<PackedScene>();
  private bool _isFirstPiece = true;
  private int _score = 0;
  private int _level = 1;
  private int _highScore = 40;

  private bool _isPaused = false;
  private bool _haveActiveBlock = false;
  private int _nbQueuedLinesToRemove = 0;

  // Bumped by every reset. The line-clear routine below is async and holds state across an
  // await on a timer, and reset runs on CheckpointLoaded - i.e. on every death, which does
  // not reload the level. A continuation that resumes into a pool that has already been reset
  // used to decrement a counter reset had zeroed, leaving it at -1; since _PhysicsProcess only
  // gates on "> 0", the pool then kept spawning and dropping pieces while rows were shifted
  // out from under them, overwriting live grid entries.
  private int _resetGeneration = 0;
  private TetrisAI _ai = new TetrisAI();
  private float _stepInterval = _stepIntervalForLevel(1);
  internal float StepInterval => _stepInterval;

  // The speeds are listed one per level from the first, so a level - which counts from one -
  // indexes from zero. Past the last of them the pool simply stays at its quickest.
  private static float _stepIntervalForLevel(int level) =>
    Constants.TETRIS_SPEEDS[Math.Min(level, Constants.TETRIS_SPEEDS.Length) - 1];
  private float _phaseElapsed = 0.0f;
  private bool _isTravelling = false;
  private Tetromino? _shape = null;
  private Block?[,] _grid = new Block?[Constants.TETRIS_GRID_WIDTH, Constants.TETRIS_POOL_HEIGHT];
  internal Block?[,] Grid => _grid;
  private bool _isVirgin = true;

  // The wall the player has to wear down to reach the room beside the pool, one brick per row.
  private static readonly string[] ESCAPE_WALL_COLORS = [
    ColorUtils.YELLOW, ColorUtils.PINK, ColorUtils.BLUE, ColorUtils.PURPLE
  ];
  private bool _hasEscaped = false;

  private sealed record SaveData(bool hasEscaped = false);
  private SaveData _saveData = new SaveData();

  #region Nodes
  [NodePath("SpawnPosition")]
  private Marker2D _spawnPosNode = default!;
  [NodePath("ScoreBoard")]
  private ScoreBoard _scoreBoardNode = default!;
  [NodePath("RemoveLinesDurationTimer")]
  private Timer _removeLinesDurationTimerNode = default!;
  [NodePath("NextPiece")]
  private NextPiece _nextPieceNode = default!;
  [NodePath("LevelUpPosition")]
  private Marker2D _levelUpPositionNode = default!;
  [NodePath("SlidingFloor/Slider")]
  private PlatformSlider _slidingFloorSliderNode = default!;
  [NodePath("TriggerEnterArea")]
  private Area2D _triggerEnterAreaNode = default!;
  #endregion Nodes

  #region Dependencies
  [Dependency]
  public IMusicTrackManager MusicTrackManager => this.DependOn<IMusicTrackManager>();
  #endregion Dependencies

  public void OnResolved() {
    if (_hasEscaped) {
      _stopForEscape();
    }
  }


  public override void _Ready() {
    base._Ready();
    this.WireNodes();

    GD.Randomize();
    InitGrid();
    reset(true);
  }

  public override void _EnterTree() {
    ConnectSignals();
  }

  public override void _ExitTree() {
    DisconnectSignals();
  }

  private void ClearGrid() {
    for (int i = 0; i < Constants.TETRIS_GRID_WIDTH; i++) {
      for (int j = 0; j < Constants.TETRIS_POOL_HEIGHT; j++) {
        if (_grid[i, j] != null) {
          _grid[i, j]?.QueueFree();
          _grid[i, j] = null;
        }
      }
    }
  }

  private void InitGrid() {
    for (int i = 0; i < Constants.TETRIS_GRID_WIDTH; i++) {
      for (int j = 0; j < Constants.TETRIS_POOL_HEIGHT; j++) {
        _grid[i, j] = null;
      }
    }
  }

  private Vector2 _cellPosition(int i, int j) =>
    _spawnPosNode.Position + new Vector2(
      Constants.TETRIS_BLOCK_SIZE * (i - Constants.TETRIS_SPAWN_I),
      Constants.TETRIS_BLOCK_SIZE * (j - Constants.TETRIS_SPAWN_J)
    );

  private void _buildEscapeWall() {
    for (int j = 0; j < Constants.TETRIS_POOL_HEIGHT; j++) {
      var brick = SceneHelpers.InstantiateNode<Block>();
      brick.ColorGroup = ESCAPE_WALL_COLORS[j % ESCAPE_WALL_COLORS.Length];
      brick.Grid = _grid;
      brick.MoveTo(Constants.TETRIS_ESCAPE_WALL_I, j);
      brick.Position = _cellPosition(Constants.TETRIS_ESCAPE_WALL_I, j);
      AddChild(brick);
      brick.Owner = this;
      brick.AddToGrid();
    }
  }

  private void _crumbleEscapeWall() {
    for (int j = 0; j < Constants.TETRIS_POOL_HEIGHT; j++) {
      _grid[Constants.TETRIS_ESCAPE_WALL_I, j]?.Destroy();
      _grid[Constants.TETRIS_ESCAPE_WALL_I, j] = null;
    }
  }

  // The run never opens with an S or a Z. Dealt onto an empty floor either one settles into a
  // step the cube can stand in the crook of, out of reach of everything that follows.
  private void _keepTheOpeningPlayable() {
    if (!_isFirstPiece) {
      return;
    }
    var top = _randomBag.Count - 1;
    for (var i = top; i >= 0; i--) {
      if (_randomBag[i] != _sBlock && _randomBag[i] != _zBlock) {
        (_randomBag[top], _randomBag[i]) = (_randomBag[i], _randomBag[top]);
        return;
      }
    }
  }

  private Dictionary<string, PackedScene> GetRandomTetrominoWithNext() {
    if (_randomBag.Count > 1) {
      var current = _randomBag[_randomBag.Count - 1];
      _randomBag.RemoveAt(_randomBag.Count - 1);
      var next = _randomBag[_randomBag.Count - 1];
      _isFirstPiece = false;
      return new Dictionary<string, PackedScene> { { "current", current }, { "next", next } };
    }
    else if (_randomBag.Count == 0) {
      _randomBag = new Godot.Collections.Array<PackedScene>(_tetrominos);
      _randomBag.Shuffle();
      _keepTheOpeningPlayable();
      return GetRandomTetrominoWithNext();
    }
    else {
      var current = _randomBag[_randomBag.Count - 1];
      _randomBag.RemoveAt(_randomBag.Count - 1);
      _randomBag = new Godot.Collections.Array<PackedScene>(_tetrominos);
      _randomBag.Shuffle();
      _randomBag.Add(current);
      return GetRandomTetrominoWithNext();
    }
  }

  private Tetromino AiSpawnBlock() {
    var pick = GetRandomTetrominoWithNext();
    var currentTetromino = pick["current"];
    _nextPieceNode.SetNextPiece(pick["next"]);
    var best = _ai.Best(_grid, currentTetromino);
    var pos = (int)best["position"];
    var rot = (int)best["rotation"];
    var shape = currentTetromino.Instantiate<Tetromino>();
    shape.SetGrid(_grid);
    // The same call the search scored this candidate with, so the piece that spawns is the one
    // that was checked against the walls. Stepping RotateLeft to get there instead walks the
    // rotation map backwards: an odd rotation spawns the mirrored shape, which for a placement
    // hard against either wall starts a column outside the grid, where it can neither fall nor
    // be moved back.
    shape.PlaceAt(pos, Constants.TETRIS_SPAWN_J, rot);
    shape.SetShape();
    // Placed before it joins the tree, and told to forget where it has been: interpolation
    // otherwise draws the new piece sweeping up to the spawn row from the pool's own origin,
    // through the stack, for the frame or two after the previous piece locked.
    shape.Position = _spawnPosNode.Position + new Vector2(Constants.TETRIS_BLOCK_SIZE * (pos - Constants.TETRIS_SPAWN_I), 0);
    AddChild(shape);
    shape.Owner = this;
    shape.ResetPhysicsInterpolation();
    return shape;
  }

  // False when the new piece has nowhere to go, i.e. when the spawn is the game over.
  private bool GenerateBlocks() {
    _haveActiveBlock = true;
    _isTravelling = false;
    _phaseElapsed = 0.0f;
    _shape = AiSpawnBlock();

    if (!_shape.CanMoveDown()) {
      _isPaused = true;
      EmitSignal(TetrisPool.SignalName.GameOver);
      return false;
    }
    return true;
  }

  public override void _PhysicsProcess(double delta) {
    if (_isPaused) {
      _finishStartedDescent((float)delta);
      return;
    }

    if (_nbQueuedLinesToRemove > 0) {
      return;
    }

    // Returning on game over rather than falling through: the frame that ends the run used to
    // go on to lock the piece it had just declared unplaceable, and could score a line with it.
    if (!_haveActiveBlock && !GenerateBlocks()) {
      return;
    }

    if (_shape != null) {
      AdvanceShape((float)delta);
    }
  }

  // A death pauses the pool with the piece that caused it hanging mid-step, half sunk into
  // the cube it caught - and the squash tracks the crusher's face, so the press would hang
  // with it until its timeout bursts the cube under a hovering piece. The step already
  // underway is played out instead: the piece comes down flush and the press follows its
  // real travel. Nothing new starts, and the piece is never locked - the reset that follows
  // every death clears it.
  private void _finishStartedDescent(float delta) {
    if (_isTravelling && _shape is not null) {
      _stepDescent(delta);
    }
  }

  // One frame of a descent already under way. True once the piece has arrived on its row; the
  // caller decides whether anything follows.
  private bool _stepDescent(float delta) {
    _phaseElapsed += delta;
    var travel = TravelDuration;
    if (_phaseElapsed < travel) {
      _shape!.SetFallOffset(Constants.TETRIS_BLOCK_SIZE * (_phaseElapsed / travel));
      _shape.ResolveDescentContact();
      return false;
    }
    _isTravelling = false;
    _phaseElapsed -= travel;
    _shape!.SetFallOffset(0.0f);
    _shape.MoveDown();
    _shape.ResolveDescentContact();
    return true;
  }

  // A row's period splits into the descent and a hold on the row it arrives at. Bounding the
  // descent by speed rather than by a fraction of the period keeps the per-frame displacement
  // small at every level; past the point where a row's period is shorter than the descent the
  // hold vanishes and the fall becomes continuous, which is as slow as it can be made.
  private float TravelDuration =>
    Math.Min(_stepInterval, Constants.TETRIS_BLOCK_SIZE / Constants.TETRIS_MAX_FALL_SPEED);

  private void AdvanceShape(float delta) {
    if (_isTravelling) {
      if (!_stepDescent(delta)) {
        return;
      }
    }
    else {
      _phaseElapsed += delta;
    }

    var hold = _stepInterval - TravelDuration;
    if (_phaseElapsed < hold) {
      return;
    }
    _phaseElapsed -= hold;

    // Asked while the piece sits squarely on its row and nothing else is moving in the grid,
    // so the descent this starts cannot carry it into an occupied cell, and the piece is
    // always grid-aligned on the frame it locks.
    if (_shape!.CanMoveDown()) {
      _isTravelling = true;
    }
    else {
      LockShape();
    }
  }

  private void LockShape() {
    _isTravelling = false;
    _phaseElapsed = 0.0f;

    var shape = _shape;
    // Nulled before anything else: the piece belongs to the grid from here on, and leaving the
    // field pointing at it let a later frame re-run AdvanceShape on a locked piece and shrink
    // its blocks' color areas a second time.
    _shape = null;
    _haveActiveBlock = false;

    if (shape != null) {
      shape.AddToGrid();
      shape.ReleaseBlocksTo(this);
      shape.QueueFree();
    }

    RemoveLines();
  }

  internal void RemoveLines() {
    var lines = DetectLines();
    if (lines.Count > 0) {
      EmitSignal(TetrisPool.SignalName.LinesRemoved, lines.Count);
      GameEvents.Instance.OnTetrisLinesRemoved();
    }
    foreach (var line in lines) {
      RemoveLineCells(line);
    }
  }

  private async void RemoveLineCells(int line) {
    var generation = _resetGeneration;
    _nbQueuedLinesToRemove += 1;
    _removeLinesDurationTimerNode.WaitTime = Block.BLINK_ANIMATION_DURATION;
    for (int i = 0; i < Constants.TETRIS_GRID_WIDTH; i++) {
      _grid[i, line]?.Destroy();
      _grid[i, line] = null;
    }
    _removeLinesDurationTimerNode.Start();
    await ToSignal(_removeLinesDurationTimerNode, Timer.SignalName.Timeout);

    // The grid this line belonged to is gone: shifting rows in the new one would move somebody
    // else's blocks, and the counter has already been zeroed on our behalf.
    if (generation != _resetGeneration) {
      return;
    }

    MoveDownLinesAbove(line);
    _nbQueuedLinesToRemove -= 1;
  }

  private void MoveDownLinesAbove(int line) {
    for (int j = line - 1; j >= 0; j--) {
      for (int i = 0; i < Constants.TETRIS_GRID_WIDTH; i++) {
        Block? currentBlock = _grid[i, j];
        if (currentBlock != null) {
          currentBlock.J += 1;
          currentBlock.QueueDrop(Constants.TETRIS_BLOCK_SIZE);
        }
        Block? belowBlock = _grid[i, j + 1];
        if (belowBlock != null) {
          belowBlock.QueueFree();
        }
        _grid[i, j + 1] = _grid[i, j];
        _grid[i, j] = null;
      }
    }
  }

  private List<int> DetectLines() {
    var linesToRemove = new List<int>();
    for (int j = 0; j < Constants.TETRIS_POOL_HEIGHT; j++) {
      bool completeLine = true;
      for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++) {
        if (_grid[i, j] == null) {
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

  public void reset() { // FIXME: use optional params after c# migration.
    reset(false);
  }

  public void reset(bool firstTime) {
    // The run is over for good once the player is through the wall, so a death anywhere later in
    // the level must not start the pool up again behind them.
    if (!firstTime && (_isVirgin || _hasEscaped))
      return;

    _resetGeneration += 1;
    _isPaused = true;
    _nbQueuedLinesToRemove = 0;
    _score = 0;
    _haveActiveBlock = false;
    _isTravelling = false;
    _phaseElapsed = 0.0f;
    // Not set here: UpdateScoreboard owns the speed, and a second writer is how a retry came
    // back faster than the run it was retrying. Cleared rather than left alone so the level it
    // recomputes always counts as a change and always reapplies the opening speed.
    _level = 0;
    _randomBag.Clear();
    _isFirstPiece = true;
    _shape?.QueueFree();
    _shape = null;
    if (!firstTime) {
      ClearGrid();
      _isPaused = false;
    }
    InitGrid();
    if (!_hasEscaped) {
      _buildEscapeWall();
    }
    UpdateScoreboard();
  }

  private void UpdateScoreboard() {
    _highScore = Math.Max(_highScore, _score);
    _scoreBoardNode.SetHighScore(_highScore);
    _scoreBoardNode.SetScore(_score);
    int oldLevel = _level;
    _level = _score / 10 + 1;
    if (oldLevel != _level) {
      _scoreBoardNode.SetLevel(_level);
      _stepInterval = _stepIntervalForLevel(_level);
      // The music climbs over fewer levels than the speed does, so it is clamped separately.
      var pitchLevel = Math.Min(_level, Constants.TETRIS_MAX_LEVELS);
      MusicTrackManager.SetPitchScale(1 + (pitchLevel - 1) * 0.1f);
      if (_level > 1) {
        var levelUpNode = SceneHelpers.InstantiateNode<LevelUp>();
        levelUpNode.Level = _level;
        levelUpNode.Position = _levelUpPositionNode.Position;
        AddChild(levelUpNode);
        levelUpNode.Owner = this;
        levelUpNode.ResetPhysicsInterpolation();
      }
    }
  }

  private void _onPlayerDying() {
    _isPaused = true;
  }

  private void _onTetrisPoolLinesRemoved(int count) {
    _score += count;
    UpdateScoreboard();
  }

  private static void _onTetrisPoolGameOver() {
    // Handle game over logic
  }

  private void _onTriggerEnterAreaBodyEntered(Node body) {
    if (body is not Player || _hasEscaped) {
      return;
    }

    _isPaused = false;
    _slidingFloorSliderNode.StopSlider(false);
    _isVirgin = false;

    MusicTrackManager.LoadTrack("tetris");
    MusicTrackManager.PlayTrack("tetris");

    _triggerEnterAreaNode.QueueFree();
  }

  private void _onEscapeCheckpointHit() {
    if (_hasEscaped) {
      return;
    }
    _hasEscaped = true;
    _saveData = new SaveData(hasEscaped: true);
    GameEvents.Instance.OnTetrisPoolEscaped();
    _stopForEscape();
  }

  // The piece in the air is dropped rather than left hanging over a pool nobody is playing any
  // more, and the floor the player rode up on is set going again so the room has a way out of it.
  //
  // What is left of the wall goes with it. The player drops into the room off the top of the wall,
  // and from the room's own floor that face is sheer: leaving it standing would seal them in.
  private void _stopForEscape() {
    _isPaused = true;
    _isTravelling = false;
    _haveActiveBlock = false;
    _shape?.QueueFree();
    _shape = null;
    _crumbleEscapeWall();
    MusicTrackManager.SetPitchScale(1.0f);
    _slidingFloorSliderNode.ResumeSlider();
  }

  private void ConnectSignals() {
    _dyingBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.PlayerDying _) => _onPlayerDying())
      .On((in IGameEvents.CheckpointLoaded _) => reset());
  }

  private void DisconnectSignals() {
    _dyingBinding?.Dispose();
    _dyingBinding = null;
  }

  public string GetSaveId() => this.GetPath();
  public string Save(ISerializer serializer) => serializer.Serialize(_saveData);
  public void Load(ISerializer serializer, string data) {
    var deserializedData = serializer.Deserialize<SaveData>(data);
    _saveData = deserializedData ?? new SaveData();
    _hasEscaped = _saveData.hasEscaped;
    // A pool that has already been beaten comes back empty: the wall it was won by is the only
    // way back into it from the room the player is standing in.
    if (_hasEscaped && IsNodeReady()) {
      ClearGrid();
      InitGrid();
      _stopForEscape();
    }
  }
}
