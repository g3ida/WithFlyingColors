namespace Wfc.Entities.World.ToyBricks;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using Wfc.Entities.World.Platforms;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

// A brick platform that runs back and forth on its own. It is a brick platform in every other
// respect - the same painted shape, the same brick to a cell, the same colour deciding which face
// may land on which part of it - so a wall the level was built out of and a slab of it that moves
// read as the same thing.
//
// The run itself is PlatformSlide, shared with the flat sliding platform, so a moving brick shape
// waits, travels, crushes and is put back by a checkpoint exactly like every other moving surface
// in the game.
[Tool]
[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class SlidingToyBrickPlatform : ToyBrickPlatform, IPersistent {
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
  public bool PlaySound { get; set; } = true;
  #endregion Exports

  #region Fields
  private readonly PlatformSlide _slide = new PlatformSlide();
  private bool _isSlideSubscribed;
  private bool _isSlideWired;
  #endregion Fields

  #region Nodes
  // The box the crush check reads, which is why it is the first shape on the body and why it is
  // disabled: what the cube stands on is the boxes built off the painted cells, and what a platform
  // arriving at the cube is measured as is the whole of it.
  [NodePath("Bounds")]
  private CollisionShape2D _boundsNode = default!;
  [NodePath("Slide")]
  private AudioStreamPlayer2D _soundNode = default!;
  [NodePath("Track")]
  private SlideTrack _trackNode = default!;
  #endregion Nodes

  public override void _Ready() {
    base._Ready();
    _isSlideWired = true;

    if (Engine.IsEditorHint()) {
      SetPhysicsProcess(false);
      _reread();
      return;
    }
    _slide.Begin(this);
    _showTrack();
  }

  public override string[] _GetConfigurationWarnings() {
    var warnings = new List<string>();
    if (Mathf.IsZeroApprox(Distance)) {
      warnings.Add("Distance is zero, so the platform stays where it is. Use a plain ToyBrickPlatform for a surface that does not move.");
    }
    if (Speed <= 0.0f) {
      warnings.Add("Speed is zero, so the platform never reaches the far end of its run.");
    }
    return [.. base._GetConfigurationWarnings(), .. warnings];
  }

  public override void _Notification(int what) {
    base._Notification(what);
    // Only while the platform is being authored: once it is running, its own transform changes are
    // the run itself, and the track is not measured from where it has got to.
    if (what == CanvasItem.NotificationTransformChanged && Engine.IsEditorHint()) {
      _reread();
    }
  }

  public override void _PhysicsProcess(double delta) {
    _slide.Step(delta);
    _hum(!Mathf.IsZeroApprox(_slide.Travelled));
    if (_slide.IsResting) {
      SetPhysicsProcess(false);
    }
  }

  // Only while the platform is actually travelling: one waiting out its stop, or parked for good, is
  // silent.
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

  #region Checkpoints
  public override void _EnterTree() {
    base._EnterTree();
    if (Engine.IsEditorHint() || _isSlideSubscribed) {
      return;
    }
    EventHandler.Instance.Events.CheckpointReached += _onCheckpointReached;
    EventHandler.Instance.Events.CheckpointLoaded += _onRespawn;
    _isSlideSubscribed = true;
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (!_isSlideSubscribed) {
      return;
    }
    EventHandler.Instance.Events.CheckpointReached -= _onCheckpointReached;
    EventHandler.Instance.Events.CheckpointLoaded -= _onRespawn;
    _isSlideSubscribed = false;
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

  // The crush box covers whatever was painted, so it is re-measured along with everything else the
  // painted cells decide.
  protected override void _rebuild() {
    base._rebuild();
    if (_boundsNode is null) {
      return;
    }
    var bounds = Grid.Bounds;
    if (_boundsNode.Shape is RectangleShape2D rectangle) {
      rectangle.Size = bounds.Size;
    }
    _boundsNode.Position = bounds.Position + (bounds.Size / 2.0f);
  }

  // The run is fixed once the level is playing, so the track is only ever re-read while it is being
  // authored.
  private void _reread() {
    if (!_isSlideWired) {
      return;
    }
    if (Engine.IsEditorHint()) {
      _slide.Remeasure(this);
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
    if (!_trackNode.Visible) {
      return;
    }
    // The run is measured from the node, and the node is the cell the bricks were painted from
    // rather than the middle of them - so the track is drawn through the middle of the platform
    // instead, where it reads as the line the platform travels along.
    var middle = (Grid.Bounds.Position + (Grid.Bounds.Size / 2.0f)) * GlobalScale;
    _trackNode.Trace(_slide.Start + middle, _slide.End + middle);
  }
}
