using System;
using System.Collections.Generic;
using Godot;
using Wfc.Core.Event;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class SlidingPlatform : Node2D {
  public enum State {
    WAIT_1,
    SLIDING_FORTH,
    WAIT_2,
    SLIDING_BACK
  }

  [Export] public float wait_time { get; set; } = 4.0f;
  [Export] public float speed { get; set; } = 3.0f;
  [Export] public bool is_stopped { get; set; } = false;
  [Export] public bool one_shot { get; set; } = false;
  [Export] public State one_shot_state { get; set; } = State.SLIDING_BACK;
  [Export] public bool smooth_landing { get; set; } = false;
  [Export] public bool show_gear { get; set; } = true;
  [Export] public bool restore_delayed_stop { get; set; } = false;

  private AnimatableBody2D _platform;
  private Node2D _gearNone;
  private Vector2 _follow;
  private Vector2 _destination;

  private Tween _tweener;
  private bool _isTweenLooping = false;

  private bool _delayedStop = false;
  private State _currentState = State.WAIT_1;

  private float _distance;
  private float _duration;
  private Vector2 _startPos;
  private Vector2 _endPos;

  private Dictionary<string, object> save_data = new Dictionary<string, object>
    {
        { "state", (int)State.WAIT_1 },
        { "position_x", 0f },
        { "position_y", 0f },
        { "looping", true },
        { "is_stopped", false },
        { "delayed_stop", false }
    };

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
    if (!show_gear) {
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
      case State.WAIT_1:
        Slide(_startPos, _startPos, wait_time, 0);
        break;
      case State.SLIDING_FORTH:
        Slide(_startPos, _endPos, _duration, 0);
        break;
      case State.WAIT_2:
        Slide(_endPos, _endPos, wait_time, 0);
        break;
      case State.SLIDING_BACK:
        Slide(_endPos, _startPos, _duration, 0);
        break;
    }
  }

  private void Slide(Vector2 start, Vector2 end, float duration, float wait) {
    _tweener.TweenProperty(this, "_follow", end, duration)
        .SetTrans(Tween.TransitionType.Linear)
        .SetEase(Tween.EaseType.InOut)
        .SetDelay(wait)
        .From(start)
        .Connect("finished", new Callable(this, nameof(OnTweenCompleted)), (uint)ConnectFlags.OneShot);
  }

  public void SetLooping(bool looping) {
    _isTweenLooping = looping;
    if (_tweener != null) {
      _tweener.SetLoops(looping ? -1 : 1);
    }
  }

  private void ConnectSignals() {
    EventHandler.Instance.Connect(EventType.CheckpointReached, new Callable(this, nameof(OnCheckpointHit)));
    EventHandler.Instance.Connect(EventType.CheckpointLoaded, new Callable(this, nameof(reset)));
  }

  private void DisconnectSignals() {
    EventHandler.Instance.Disconnect(EventType.CheckpointReached, new Callable(this, nameof(OnCheckpointHit)));
    EventHandler.Instance.Disconnect(EventType.CheckpointLoaded, new Callable(this, nameof(reset)));
  }

  public override void _EnterTree() {
    ConnectSignals();
  }

  public override void _ExitTree() {
    DisconnectSignals();
  }

  private void OnCheckpointHit(Node checkpoint) {
    if (_delayedStop && !restore_delayed_stop) {
      var dest = GetDestinationPosition();
      save_data["state"] = (int)GetNextState(_currentState);
      save_data["is_stopped"] = true;
      save_data["delayed_stop"] = false;
      save_data["position_x"] = dest.X;
      save_data["position_y"] = dest.Y;
      save_data["looping"] = _isTweenLooping;
    }
    else {
      save_data["state"] = (int)_currentState;
      var dest = GetSourcePosition();
      save_data["position_x"] = dest.X;
      save_data["position_y"] = dest.Y;
      save_data["looping"] = _isTweenLooping;
      save_data["is_stopped"] = is_stopped;
      save_data["delayed_stop"] = _delayedStop;
    }
  }

  public Dictionary<string, object> save() {
    return save_data;
  }

  public void reset() {
    _tweener?.Kill();
    _currentState = (State)Helpers.ParseSaveDataInt(save_data, "state");
    _platform.GlobalPosition = new Vector2(Convert.ToSingle(save_data["position_x"]), Convert.ToSingle(save_data["position_y"]));
    _follow = _platform.GlobalPosition;
    SetLooping((bool)save_data["looping"]);
    is_stopped = (bool)save_data["is_stopped"];
    _delayedStop = (bool)save_data["delayed_stop"];
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

  private State GetNextState(State state) {
    switch (state) {
      case State.WAIT_1:
        return State.SLIDING_FORTH;
      case State.SLIDING_FORTH:
        return State.WAIT_2;
      case State.WAIT_2:
        return State.SLIDING_BACK;
      case State.SLIDING_BACK:
        return State.WAIT_1;
      default:
        return state;
    }
    ;
  }

  private void OnTweenCompleted() {
    _currentState = GetNextState(_currentState);
    if (_delayedStop) {
      _delayedStop = false;
      StopSlider(true);
    }
    if (one_shot && _currentState == one_shot_state) {
      _delayedStop = true;
    }
    ProcessTween();
  }

  private Vector2 GetDestinationPosition() {
    switch (_currentState) {
      case State.WAIT_1:
      case State.SLIDING_BACK:
        return _startPos;
      case State.SLIDING_FORTH:
      case State.WAIT_2:
        return _endPos;
      default:
        return GlobalPosition;
    }
    ;
  }

  private Vector2 GetSourcePosition() {
    switch (_currentState) {
      case State.WAIT_2:
      case State.SLIDING_BACK:
        return _endPos;
      case State.SLIDING_FORTH:
      case State.WAIT_1:
        return _startPos;
      default:
        return GlobalPosition;
    }
    ;
  }

  public void load(Dictionary<string, object> save_data) {
    this.save_data = save_data;
    reset();
  }
}
