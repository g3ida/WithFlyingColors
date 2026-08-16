namespace Wfc.Entities.World.Platforms;

using System.Collections.Generic;
using Godot;
using Wfc.Core.Logger;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

// Puts a body the level already has on a run: the brick breaker's door, the tetris pool's floor,
// anything whose own sprites and shape are its own business. Parent one to the body and it drives
// it; for a platform that carries a surface of its own there is SlidingPlatform.
[Tool]
[ScenePath]
public partial class PlatformSlider : Node2D, IPersistent {
  #region Exports
  // Each of these is the run itself, and PlatformSlide is where they are described.
  [Export]
  public PlatformSlide.SlideAxis Axis {
    get => _slide.Axis;
    set {
      _slide.Axis = value;
      _reread();
    }
  }

  [Export]
  public float Distance {
    get => _slide.Distance;
    // Held to whole pixels: the far end is where the platform stands still and is walked off, and
    // half a pixel of lip there is enough to stop the player dead against it.
    set {
      _slide.Distance = Mathf.Round(value);
      _reread();
    }
  }

  // Where the body is placed is where it stands when the level starts, and this says which end of
  // its run that is: the track is drawn ahead of it or behind it accordingly.
  [Export]
  public PlatformSlide.SlideOrigin StartAt {
    get => _slide.StartAt;
    set {
      _slide.StartAt = value;
      _reread();
    }
  }

  [Export]
  public float Speed {
    get => _slide.Speed;
    set {
      _slide.Speed = value;
      _reread();
    }
  }

  [Export]
  public float WaitTime {
    get => _slide.WaitTime;
    set => _slide.WaitTime = value;
  }

  [Export]
  public float StartDelay {
    get => _slide.StartDelay;
    // Never negative: a platform owed time before it sets off is what this is for, and a debt the
    // other way would have it start partway through a cycle it has not run yet.
    set => _slide.StartDelay = Mathf.Max(value, 0.0f);
  }

  [Export]
  public bool StartsStopped {
    get => _slide.StartsStopped;
    set => _slide.StartsStopped = value;
  }

  [Export]
  public bool OneShot {
    get => _slide.OneShot;
    set => _slide.OneShot = value;
  }

  [Export]
  public PlatformSlide.SlidePhase OneShotPhase {
    get => _slide.OneShotPhase;
    set => _slide.OneShotPhase = value;
  }

  [Export]
  public bool SmoothLanding {
    get => _slide.SmoothLanding;
    set => _slide.SmoothLanding = value;
  }

  [Export]
  public bool RestoreDelayedStop {
    get => _slide.RestoreDelayedStop;
    set => _slide.RestoreDelayedStop = value;
  }

  [Export]
  public PlatformSlide.TrackDisplay Track {
    get => _track;
    set {
      _track = value;
      _reread();
    }
  }
  private PlatformSlide.TrackDisplay _track = PlatformSlide.TrackDisplay.EditorOnly;

  [Export]
  public bool ShowGear {
    get => _showGear;
    set {
      _showGear = value;
      if (_isWired) {
        _gearNode.Visible = _showGear;
      }
    }
  }
  private bool _showGear = true;

  // The rumble a platform makes while it is actually travelling. Off for one that is meant to move
  // unheard, and for a level that already has a cue of its own for the same movement.
  [Export]
  public bool PlaySound { get; set; } = true;
  #endregion Exports

  #region Fields
  private readonly PlatformSlide _slide = new PlatformSlide();
  private PhysicsBody2D? _body;
  private bool _isSubscribed;
  // The exported setters fire while the scene is still loading, before there are any nodes to push
  // the new value into.
  private bool _isWired;
  private bool _settleOnStart;
  #endregion Fields

  #region Nodes
  [NodePath("Slide")]
  private AudioStreamPlayer2D _soundNode = default!;
  [NodePath("Gear")]
  private SlidingPlatformGear _gearNode = default!;
  [NodePath("Track")]
  private SlideTrack _trackNode = default!;
  #endregion Nodes

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _isWired = true;
    _body = GetParent() as PhysicsBody2D;
    _gearNode.Visible = _showGear;

    if (Engine.IsEditorHint()) {
      SetPhysicsProcess(false);
      // Only the editor drags a platform around, and the track is the only thing listening.
      SetNotifyTransform(true);
      _reread();
      return;
    }

    if (_body is null) {
      Log.Error($"{Name} has no physics body to move: a slider drives the body it is parented to.");
      SetPhysicsProcess(false);
      return;
    }
    _slide.Begin(_body);
    _showTrack();
    if (_settleOnStart) {
      _settleOnStart = false;
      SettleAtEnd();
    }
  }

  public override string[] _GetConfigurationWarnings() {
    var warnings = new List<string>();
    if (GetParent() is not AnimatableBody2D) {
      warnings.Add(
        "A slider moves the body it is parented to, and only an AnimatableBody2D carries the player "
        + "while it moves. Parent this to one, or use SlidingPlatform for a platform that brings its "
        + "own surface."
      );
    }
    if (Mathf.IsZeroApprox(Distance)) {
      warnings.Add("Distance is zero, so the platform stays where it is.");
    }
    if (Speed <= 0.0f) {
      warnings.Add("Speed is zero, so the platform never reaches the far end of its run.");
    }
    return [.. warnings];
  }

  public override void _Notification(int what) {
    base._Notification(what);
    // Only while the platform is being authored: once it is running, the body's transform changes
    // are the run itself, and the track is not measured from where it has got to.
    if (what == CanvasItem.NotificationTransformChanged && Engine.IsEditorHint()) {
      _reread();
    }
  }

  public override void _PhysicsProcess(double delta) {
    _slide.Step(delta);
    var moving = !Mathf.IsZeroApprox(_slide.Travelled);
    if (_showGear && moving) {
      _gearNode.Spin(_slide.Travelled);
    }
    _hum(moving);
    if (_slide.IsResting) {
      SetPhysicsProcess(false);
    }
  }
  // Only while the platform is actually travelling: one waiting out its stop, or parked for good, is
  // silent. Started and stopped rather than left looping under a volume, so a level with several of
  // these is quiet until something moves.
  private void _hum(bool moving) {
    var wanted = moving && PlaySound;
    if (wanted && !_soundNode.Playing) {
      _soundNode.Play();
    }
    else if (!wanted && _soundNode.Playing) {
      _soundNode.Stop();
    }
  }


  public void StopSlider(bool immediately) => _slide.Stop(immediately);

  public void ResumeSlider() {
    _slide.Resume();
    SetPhysicsProcess(true);
  }

  // Found already at the far end rather than travelling there. A level restoring something the
  // player opened before they died asks for this instead of setting it going again.
  public void SettleAtEnd() {
    // A save is loaded before the slider has taken its body over, and there is nothing to place
    // until it has. _Ready settles it once the run has been measured.
    if (_body is null) {
      _settleOnStart = true;
      return;
    }
    _slide.SettleAtEnd();
    _hum(false);
    SetPhysicsProcess(false);
  }

  #region Checkpoints
  public override void _EnterTree() {
    base._EnterTree();
    if (Engine.IsEditorHint() || _isSubscribed) {
      return;
    }
    EventHandler.Instance.Events.CheckpointReached += _onCheckpointReached;
    EventHandler.Instance.Events.CheckpointLoaded += _onRespawn;
    _isSubscribed = true;
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (!_isSubscribed) {
      return;
    }
    EventHandler.Instance.Events.CheckpointReached -= _onCheckpointReached;
    EventHandler.Instance.Events.CheckpointLoaded -= _onRespawn;
    _isSubscribed = false;
  }

  private void _onCheckpointReached(Vector2 _position, string _colorGroup) => _slide.OnCheckpointReached();

  private void _onRespawn() {
    _slide.Reset();
    SetPhysicsProcess(true);
  }

  public string GetSaveId() => GetPath();

  public string Save(ISerializer serializer) => _slide.Save(serializer);

  public void Load(ISerializer serializer, string data) {
    _slide.Load(serializer, data);
    SetPhysicsProcess(true);
  }
  #endregion Checkpoints

  // The run is fixed once the level is playing, so the track is only ever re-read while it is being
  // authored.
  private void _reread() {
    if (!_isWired) {
      return;
    }
    if (Engine.IsEditorHint()) {
      if (_body is not null) {
        _slide.Remeasure(_body);
      }
      UpdateConfigurationWarnings();
    }
    _showTrack();
  }

  private void _showTrack() {
    _trackNode.Visible = _track switch {
      PlatformSlide.TrackDisplay.Always => true,
      PlatformSlide.TrackDisplay.EditorOnly => Engine.IsEditorHint(),
      _ => false,
    };
    if (_trackNode.Visible) {
      _trackNode.Trace(_slide.Start, _slide.End);
    }
  }
}
