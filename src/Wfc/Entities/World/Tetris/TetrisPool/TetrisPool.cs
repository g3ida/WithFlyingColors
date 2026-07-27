namespace Wfc.Entities.Tetris;

using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Audio;
using Wfc.Core.Event;
using Wfc.Entities.Tetris.Tetrominos;
using Wfc.Entities.World.Platforms;
using Wfc.Entities.World.Player;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class TetrisPool : Node2D {

  #region Signals
  public override void _Notification(int what) => this.Notify(what);
  [Signal]
  public delegate void LinesRemovedEventHandler(int count);

  [Signal]
  public delegate void GameOverEventHandler();
  #endregion Signals

  private static readonly List<PackedScene> _tetrominos = new List<PackedScene> {
        SceneHelpers.LoadScene<S_Block>(),
        SceneHelpers.LoadScene<Z_Block>(),
        SceneHelpers.LoadScene<L_Block>(),
        SceneHelpers.LoadScene<J_Block>(),
        SceneHelpers.LoadScene<O_Block>(),
        SceneHelpers.LoadScene<T_Block>(),
        SceneHelpers.LoadScene<I_Block>()
    };

  private Godot.Collections.Array<PackedScene> _randomBag = new Godot.Collections.Array<PackedScene>();
  private int _score = 0;
  private int _level = 1;
  private int _highScore = 40;

  private bool _isPaused = false;
  private bool _haveActiveBlock = false;
  private int _nbQueuedLinesToRemove = 0;

  // Bumped by every reset. The line-clear and piece-drop routines below are async and hold
  // state across an await on a timer, and reset runs on CheckpointLoaded - i.e. on every
  // death, which does not reload the level. A continuation that resumes into a pool that has
  // already been reset used to decrement a counter reset had zeroed, leaving it at -1; since
  // _PhysicsProcess only gates on "> 0", the pool then kept spawning and dropping pieces while
  // rows were shifted out from under them, overwriting live grid entries.
  private int _resetGeneration = 0;
  private TetrisAI _ai = new TetrisAI();
  private bool _shapeIsInWaitTime = false;
  private Tetromino? _shape = null;
  private Block?[,] _grid = new Block?[Constants.TETRIS_POOL_WIDTH, Constants.TETRIS_POOL_HEIGHT];
  private bool _isVirgin = true;

  #region Nodes
  [NodePath("SpawnPosition")]
  private Marker2D _spawnPosNode = default!;
  [NodePath("ScoreBoard")]
  private ScoreBoard _scoreBoardNode = default!;
  [NodePath("ShapeWaitTimer")]
  private Timer _shapeWaitTimerNode = default!;
  [NodePath("RemoveLinesDurationTimer")]
  private Timer _removeLinesDurationTimerNode = default!;
  [NodePath("NextPiece")]
  private NextPiece _nextPieceNode = default!;
  [NodePath("LevelUpPosition")]
  private Marker2D _levelUpPositionNode = default!;
  [NodePath("SlidingFloor/SlidingPlatform")]
  private SlidingPlatform _slidingFloorSliderNode = default!;
  [NodePath("TriggerEnterArea")]
  private Area2D _triggerEnterAreaNode = default!;
  #endregion Nodes

  #region Dependencies
  [Dependency]
  public IMusicTrackManager MusicTrackManager => this.DependOn<IMusicTrackManager>();
  #endregion Dependencies

  public void OnResolved() { }


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
    for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++) {
      for (int j = 0; j < Constants.TETRIS_POOL_HEIGHT; j++) {
        if (_grid[i, j] != null) {
          _grid[i, j]?.QueueFree();
          _grid[i, j] = null;
        }
      }
    }
  }

  private void InitGrid() {
    for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++) {
      for (int j = 0; j < Constants.TETRIS_POOL_HEIGHT; j++) {
        _grid[i, j] = null;
      }
    }
  }

  private Dictionary<string, PackedScene> GetRandomTetrominoWithNext() {
    if (_randomBag.Count > 1) {
      var current = _randomBag[_randomBag.Count - 1];
      _randomBag.RemoveAt(_randomBag.Count - 1);
      var next = _randomBag[_randomBag.Count - 1];
      return new Dictionary<string, PackedScene> { { "current", current }, { "next", next } };
    }
    else if (_randomBag.Count == 0) {
      _randomBag = new Godot.Collections.Array<PackedScene>(_tetrominos);
      _randomBag.Shuffle();
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
    shape.MoveBy(pos, Constants.TETRIS_SPAWN_J);
    AddChild(shape);
    shape.Owner = this;
    for (int i = 0; i < rot; i++) {
      shape.RotateLeft();
    }
    shape.Position = _spawnPosNode.Position + new Vector2(Constants.TETRIS_BLOCK_SIZE * (pos - Constants.TETRIS_SPAWN_I), 0);
    return shape;
  }

  // False when the new piece has nowhere to go, i.e. when the spawn is the game over.
  private bool GenerateBlocks() {
    _haveActiveBlock = true;
    _shape = AiSpawnBlock();

    if (!_shape.CanMoveDown()) {
      _isPaused = true;
      EmitSignal(TetrisPool.SignalName.GameOver);
      return false;
    }
    return true;
  }

  public override void _PhysicsProcess(double delta) {
    if (_isPaused || _nbQueuedLinesToRemove > 0)
      return;

    // Returning on game over rather than falling through: the frame that ends the run used to
    // go on to lock the piece it had just declared unplaceable, and could score a line with it.
    if (!_haveActiveBlock && !GenerateBlocks()) {
      return;
    }

    if (_shape != null && !_shapeIsInWaitTime) {
      MoveShapeDown();
    }
  }

  private async void MoveShapeDown() {
    var generation = _resetGeneration;
    _shapeIsInWaitTime = true;

    if (_shape?.MoveDownSafe() == true) {
      _shapeWaitTimerNode.Start();
      await ToSignal(_shapeWaitTimerNode, Timer.SignalName.Timeout);
      // A reset while this was waiting has already cleared the flag and freed the piece; the
      // pool has a new one falling by now and clearing the flag again would let it skip a step.
      if (generation != _resetGeneration) {
        return;
      }
    }
    else {
      LockShape();
    }

    _shapeIsInWaitTime = false;
  }

  private void LockShape() {
    var shape = _shape;
    // Nulled before anything else: the piece belongs to the grid from here on, and leaving the
    // field pointing at it let a later frame re-run MoveShapeDown on a locked piece and shrink
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

  private void RemoveLines() {
    var lines = DetectLines();
    if (lines.Count > 0) {
      EmitSignal(TetrisPool.SignalName.LinesRemoved, lines.Count);
      EventHandler.Instance.EmitTetrisLinesRemoved();
    }
    foreach (var line in lines) {
      RemoveLineCells(line);
    }
  }

  private async void RemoveLineCells(int line) {
    var generation = _resetGeneration;
    _nbQueuedLinesToRemove += 1;
    _removeLinesDurationTimerNode.WaitTime = Block.BLINK_ANIMATION_DURATION;
    for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++) {
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
      for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++) {
        Block? currentBlock = _grid[i, j];
        if (currentBlock != null) {
          currentBlock.J += 1;
          currentBlock.Position += new Vector2(0, Constants.TETRIS_BLOCK_SIZE);
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
    if (_isVirgin && !firstTime)
      return;

    _resetGeneration += 1;
    _isPaused = true;
    _nbQueuedLinesToRemove = 0;
    _score = 0;
    _haveActiveBlock = false;
    _shapeIsInWaitTime = false;
    _shapeWaitTimerNode.WaitTime = Constants.TETRIS_SPEEDS[0];
    _randomBag.Clear();
    _shape?.QueueFree();
    _shape = null;
    if (!firstTime) {
      ClearGrid();
      _isPaused = false;
    }
    InitGrid();
    UpdateScoreboard();
  }

  private void UpdateScoreboard() {
    _scoreBoardNode.SetHighScore(_highScore);
    _scoreBoardNode.SetScore(_score);
    int oldLevel = _level;
    _level = _score / 10 + 1;
    if (oldLevel != _level) {
      _scoreBoardNode.SetLevel(_level);
      int speed = Math.Min(_level, Constants.TETRIS_MAX_LEVELS);
      _shapeWaitTimerNode.WaitTime = Constants.TETRIS_SPEEDS[speed];
      MusicTrackManager.SetPitchScale(1 + (speed - 1) * 0.1f);
      if (_level > 1) {
        var levelUpNode = SceneHelpers.InstantiateNode<LevelUp>();
        AddChild(levelUpNode);
        levelUpNode.Owner = this;
        levelUpNode.Position = _levelUpPositionNode.Position;
      }
    }
  }

  private void _onPlayerDying(Node? area, Vector2 position, int entityType) {
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
    if (body is not Player) {
      return;
    }

    _isPaused = false;
    _slidingFloorSliderNode.SetLooping(false);
    _slidingFloorSliderNode.StopSlider(false);
    _isVirgin = false;

    MusicTrackManager.LoadTrack("tetris");
    MusicTrackManager.PlayTrack("tetris");

    _triggerEnterAreaNode.QueueFree();
  }

  private void ConnectSignals() {
    EventHandler.Instance.Events.PlayerDying += _onPlayerDying;
    EventHandler.Instance.Events.CheckpointLoaded += reset;
  }

  private void DisconnectSignals() {
    EventHandler.Instance.Events.PlayerDying -= _onPlayerDying;
    EventHandler.Instance.Events.CheckpointLoaded -= reset;
  }
}
