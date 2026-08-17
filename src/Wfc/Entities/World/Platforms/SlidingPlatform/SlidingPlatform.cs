namespace Wfc.Entities.World.Platforms;

using Chickensoft.Sync.Primitives;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using Wfc.Utils.Attributes;

// A flat platform that runs back and forth on its own: everything a level author has to set is on
// this one node, and the dashed track between its two ends says where it goes without playing the
// level to find out.
//
// It is a flat platform in every other respect - the same sliced corners, the same shade band, the
// same colour group deciding which face may land on it - so a run of static platforms and a moving
// one read as the same surface.
[Tool]
[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class SlidingPlatform : FlatPlatform, IPersistent {
  private AutoChannel.Binding? _checkpointBinding;

  #region Constants
  // How much of the platform's own depth the cog takes up, so it sits inside the surface rather than
  // hanging off a thin ledge.
  private const float GEAR_SHARE = 0.7f;
  #endregion Constants

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

  // Where the platform is placed is where it stands when the level starts, and this says which end
  // of its run that is: the track is drawn ahead of it or behind it accordingly.
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

  // The cog the older platforms carry, kept as the one thing that tells the player this surface is
  // going somewhere: a sliding platform is drawn exactly like a static one otherwise.
  [Export]
  public bool ShowGear {
    get => _showGear;
    set {
      _showGear = value;
      _applyGear();
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
  private bool _isSlideSubscribed;
  private bool _isSlideWired;
  #endregion Fields

  #region Nodes
  [NodePath("Slide")]
  private AudioStreamPlayer2D _soundNode = default!;
  [NodePath("Track")]
  private SlideTrack _trackNode = default!;
  [NodePath("Gear")]
  private SlidingPlatformGear _gearNode = default!;
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
      warnings.Add("Distance is zero, so the platform stays where it is. Use a plain FlatPlatform for a surface that does not move.");
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

  #region Checkpoints
  public override void _EnterTree() {
    base._EnterTree();
    if (Engine.IsEditorHint() || _isSlideSubscribed) {
      return;
    }
    _checkpointBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.CheckpointReached m) => _onCheckpointReached(m.Position, m.ColorGroup))
      .On((in IGameEvents.CheckpointLoaded _) => _onRespawn());
    _isSlideSubscribed = true;
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (!_isSlideSubscribed) {
      return;
    }
    _checkpointBinding?.Dispose();
    _checkpointBinding = null;
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

  // Sized along with everything else the platform's own size decides.
  protected override void _applyShape() {
    base._applyShape();
    _applyGear();
  }

  private void _applyGear() {
    // Reached through the base class's own size handling, which runs before this class has anything
    // wired.
    if (_gearNode is null) {
      return;
    }
    _gearNode.Visible = _showGear;
    _gearNode.FitTo(Mathf.Min(Size.X, Size.Y) * GEAR_SHARE);
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
    if (_trackNode.Visible) {
      _trackNode.Modulate = SurfaceColor;
      _trackNode.Trace(_slide.Start, _slide.End);
    }
  }
}
