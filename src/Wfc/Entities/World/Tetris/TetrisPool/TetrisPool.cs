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

  private void GenerateBlocks() {
    _haveActiveBlock = true;
    _shape = AiSpawnBlock();

    if (!_shape.CanMoveDown()) {
      _isPaused = true;
      EmitSignal(TetrisPool.SignalName.GameOver);
    }
  }

  public override void _PhysicsProcess(double delta) {
    if (_isPaused || _nbQueuedLinesToRemove > 0)
      return;

    if (!_haveActiveBlock) {
      GenerateBlocks();
    }

    if (_shape != null && !_shapeIsInWaitTime) {
      MoveShapeDown();
    }
  }

  private async void MoveShapeDown() {
    _shapeIsInWaitTime = true;
    if (_shape?.MoveDownSafe() == true) {
      _shapeWaitTimerNode.Start();
      await ToSignal(_shapeWaitTimerNode, Timer.SignalName.Timeout);
    }
    else {
      _shape?.AddToGrid();
      RemoveLines();
      _haveActiveBlock = false;
    }
    _shapeIsInWaitTime = false;
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
    _nbQueuedLinesToRemove += 1;
    _removeLinesDurationTimerNode.WaitTime = Block.BLINK_ANIMATION_DURATION;
    for (int i = 0; i < Constants.TETRIS_POOL_WIDTH; i++) {
      _grid[i, line]?.Destroy();
      _grid[i, line] = null;
    }
    _removeLinesDurationTimerNode.Start();
    await ToSignal(_removeLinesDurationTimerNode, Timer.SignalName.Timeout);
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

  private void _on_TriggerEnterArea_body_entered(Node body) {
    if (body != Global.Instance().Player)
      return;

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
