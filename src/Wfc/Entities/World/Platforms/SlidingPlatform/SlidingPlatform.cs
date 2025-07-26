namespace Wfc.Entities.World.Platforms;

using System;
using System.Collections.Generic;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using Wfc.Utils;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class SlidingPlatform : Node2D, IPersistent {
  public enum State {
    Wait1,
    SlidingForth,
    Wait2,
    SlidingBack
  }

  [Export] public float wait_time { get; set; } = 4.0f;
  [Export] public float speed { get; set; } = 3.0f;
  [Export] public bool is_stopped { get; set; } = false;
  [Export] public bool one_shot { get; set; } = false;
  [Export] public State one_shot_state { get; set; } = State.SlidingBack;
  [Export] public bool smooth_landing { get; set; } = false;
  [Export] public bool show_gear { get; set; } = true;
  [Export] public bool restore_delayed_stop { get; set; } = false;

  private AnimatableBody2D _platform = default!;
  private Node2D? _gearNone;
  private Vector2 _follow;
  private Vector2 _destination;

  private Tween? _tweener;
  private bool _isTweenLooping = false;

  private bool _delayedStop = false;
  private State _currentState = State.Wait1;

  private float _distance;
  private float _duration;
  private Vector2 _startPos;
  private Vector2 _endPos;

  private sealed record SaveData(
    State State = State.Wait1,
    float PositionX = 0f,
    float PositionY = 0f,
    bool Looping = true,
    bool IsStopped = false,
    bool DelayedStop = false
  );
  private SaveData _saveData = new SaveData();

  public override void _Ready() {
    _platform = GetParent<AnimatableBody2D>();
    _gearNone = GetNode<Node2D>("Gear");
    _follow = _platform.GlobalPosition;
    _destination = ParseDestination();

    Setup();
    ProcessTween();
  }

  public override void _PhysicsProcess(double delta) {
    if (smooth_landing) {
      _platform.GlobalPosition = _platform.GlobalPosition.Lerp(_follow, 0.075f);
    }
    else {
      _platform.GlobalPosition = _follow;
    }
  }

  private Vector2 ParseDestination() {
    foreach (Node child in GetChildren()) {
      if (child is Marker2D pos2D) {
        return pos2D.Position * _platform.GlobalScale + _follow;
      }
    }
    return _platform.GlobalPosition;
  }

  private void Setup() {
    _distance = (_destination - _follow).Length();
    _duration = _distance / (speed * Constants.WORLD_TO_SCREEN);
    _startPos = _follow;
    _endPos = _destination;
    if (!show_gear && _gearNone != null) {
      _gearNone.Visible = false;
    }
  }

  private void CleanTween() {
    _tweener?.Kill();
    _tweener = CreateTween();
  }

  private void ProcessTween() {
    if (is_stopped)
      return;
    CleanTween();

    switch (_currentState) {
      case State.Wait1:
        Slide(_startPos, _startPos, wait_time, 0);
        break;
      case State.SlidingForth:
        Slide(_startPos, _endPos, _duration, 0);
        break;
      case State.Wait2:
        Slide(_endPos, _endPos, wait_time, 0);
        break;
      case State.SlidingBack:
        Slide(_endPos, _startPos, _duration, 0);
        break;
    }
  }

  private void Slide(Vector2 start, Vector2 end, float duration, float wait) {
    if (_tweener != null) {
      _tweener.TweenProperty(this, "_follow", end, duration)
          .SetTrans(Tween.TransitionType.Linear)
          .SetEase(Tween.EaseType.InOut)
          .SetDelay(wait)
          .From(start)
          .Connect(
            Tween.SignalName.Finished,
            new Callable(this, nameof(OnTweenCompleted)),
            (uint)ConnectFlags.OneShot
          );
    }
  }

  public void SetLooping(bool looping) {
    _isTweenLooping = looping;
    if (_tweener != null) {
      _tweener.SetLoops(looping ? -1 : 1);
    }
  }

  private void ConnectSignals() {
    EventHandler.Instance.Events.CheckpointReached += OnCheckpointHit;
    EventHandler.Instance.Events.CheckpointLoaded += Reset;
  }

  private void DisconnectSignals() {
    EventHandler.Instance.Events.CheckpointReached -= OnCheckpointHit;
    EventHandler.Instance.Events.CheckpointLoaded -= Reset;
  }

  public override void _EnterTree() {
    ConnectSignals();
  }

  public override void _ExitTree() {
    DisconnectSignals();
  }

  private void OnCheckpointHit(Node checkpoint) {
    if (_delayedStop && !restore_delayed_stop) {
      var dest = _getDestinationPosition();
      _saveData = new SaveData(
        State: _getNextState(_currentState),
        PositionX: dest.X,
        PositionY: dest.Y,
        Looping: _isTweenLooping,
        IsStopped: true,
        DelayedStop: false
      );
    }
    else {
      var dest = _getSourcePosition();
      _saveData = new SaveData(
        State: _currentState,
        PositionX: dest.X,
        PositionY: dest.Y,
        Looping: _isTweenLooping,
        IsStopped: is_stopped,
        DelayedStop: _delayedStop
      );
    }
  }

  public void Reset() {
    _tweener?.Kill();
    _currentState = _saveData.State;
    _platform.GlobalPosition = new Vector2(_saveData.PositionX, _saveData.PositionY);
    _follow = _platform.GlobalPosition;
    SetLooping(_saveData.Looping);
    is_stopped = _saveData.IsStopped;
    _delayedStop = _saveData.DelayedStop;
    ProcessTween();
  }

  public void StopSlider(bool stopDirectly) {
    if (is_stopped)
      return;
    if (stopDirectly) {
      is_stopped = true;
      _tweener?.Kill();
    }
    else {
      _delayedStop = true;
    }
  }

  public void ResumeSlider() {
    is_stopped = false;
    ProcessTween();
  }

  private static State _getNextState(State state) {
    switch (state) {
      case State.Wait1:
        return State.SlidingForth;
      case State.SlidingForth:
        return State.Wait2;
      case State.Wait2:
        return State.SlidingBack;
      case State.SlidingBack:
        return State.Wait1;
      default:
        return state;
    }
    ;
  }

  private void OnTweenCompleted() {
    _currentState = _getNextState(_currentState);
    if (_delayedStop) {
      _delayedStop = false;
      StopSlider(true);
    }
    if (one_shot && _currentState == one_shot_state) {
      _delayedStop = true;
    }
    ProcessTween();
  }

  private Vector2 _getDestinationPosition() {
    switch (_currentState) {
      case State.Wait1:
      case State.SlidingBack:
        return _startPos;
      case State.SlidingForth:
      case State.Wait2:
        return _endPos;
      default:
        return GlobalPosition;
    }
    ;
  }

  private Vector2 _getSourcePosition() {
    switch (_currentState) {
      case State.Wait2:
      case State.SlidingBack:
        return _endPos;
      case State.SlidingForth:
      case State.Wait1:
        return _startPos;
      default:
        return GlobalPosition;
    }
    ;
  }

  public string GetSaveId() => this.GetPath();
  public string Save(ISerializer serializer) => serializer.Serialize(_saveData);
  public void Load(ISerializer serializer, string data) {
    var deserializedData = serializer.Deserialize<SaveData>(data);
    this._saveData = deserializedData ?? new SaveData();
    Reset();
  }
}
