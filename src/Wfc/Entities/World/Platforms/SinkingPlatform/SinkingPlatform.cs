namespace Wfc.Entities.World.Platforms;

using Chickensoft.Sync.Primitives;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Utils.Attributes;

// A flat platform that will not hold the player up: it sinks under them, stops once it has given all
// the way, and comes back once it is left. A stair built out of these is a stair that has to be taken
// at a run, which a stair of static platforms never is.
//
// It is a flat platform in every other respect - the same sliced corners, the same shade band, the
// same colour group deciding which face may land on it - and it carries the cog a sliding platform
// does, because a surface that is going somewhere has to say so before it is stood on.
[Tool]
[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class SinkingPlatform : FlatPlatform {
  private AutoChannel.Binding? _checkpointBinding;

  #region Constants
  // How much of the platform's own depth the cog takes up, so it sits inside the surface rather than
  // hanging off a thin ledge.
  private const float GEAR_SHARE = 0.7f;

  // How far above its own surface the platform notices the player. Well clear of a resting contact:
  // a platform that sinks faster than the player falls leaves them a moment behind it, and one that
  // stopped sinking for it would judder down its whole run.
  private const float RIDE_REACH = 24.0f;

  // ...and how far into the surface, so that a body come to rest on it overlaps rather than touching.
  private const float RIDE_BITE = 2.0f;

  // Held clear of the platform's own sides, or the player pressed against the side of one on the way
  // past sinks a platform they never stood on.
  private const float RIDE_INSET = 6.0f;
  #endregion Constants

  #region Exports
  // Each of these is the give itself, and PlatformSink is where they are described.
  [Export]
  public float Depth {
    get => _sink.Depth;
    // Held to whole pixels: the bottom of the give is where the platform stands still and is walked
    // off, and half a pixel of lip there is enough to stop the player dead against it.
    set {
      _sink.Depth = Mathf.Max(Mathf.Round(value), 0.0f);
      _reread();
    }
  }

  [Export]
  public float SinkSpeed {
    get => _sink.SinkSpeed;
    set {
      _sink.SinkSpeed = value;
      _reread();
    }
  }

  [Export]
  public float RiseSpeed {
    get => _sink.RiseSpeed;
    set => _sink.RiseSpeed = Mathf.Max(value, 0.0f);
  }

  [Export]
  public float RiseDelay {
    get => _sink.RiseDelay;
    set => _sink.RiseDelay = Mathf.Max(value, 0.0f);
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

  // The cog the moving platforms carry, kept as the one thing that tells the player this surface is
  // not going to hold: a sinking platform is drawn exactly like a static one otherwise.
  [Export]
  public bool ShowGear {
    get => _showGear;
    set {
      _showGear = value;
      _applyGear();
    }
  }
  private bool _showGear = true;

  // The rumble the platform makes while it is actually giving. Off for one that is meant to sink
  // unheard, and for a level that already has a cue of its own for the same movement.
  [Export]
  public bool PlaySound { get; set; } = true;
  #endregion Exports

  #region Fields
  private readonly PlatformSink _sink = new PlatformSink();
  private Player.Player? _rider;
  private bool _isSinkSubscribed;
  private bool _isSinkWired;
  #endregion Fields

  #region Nodes
  [NodePath("Sink")]
  private AudioStreamPlayer2D _soundNode = default!;
  [NodePath("Track")]
  private SlideTrack _trackNode = default!;
  [NodePath("Gear")]
  private SlidingPlatformGear _gearNode = default!;
  [NodePath("RideArea")]
  private Area2D _rideAreaNode = default!;
  [NodePath("RideArea/RideAreaShape")]
  private CollisionShape2D _rideAreaShapeNode = default!;
  #endregion Nodes

  public override void _Ready() {
    base._Ready();
    _isSinkWired = true;
    _applyRideArea();

    if (Engine.IsEditorHint()) {
      SetPhysicsProcess(false);
      _reread();
      return;
    }
    _sink.Begin(this);
    _showTrack();
    _rideAreaNode.BodyEntered += _onRideAreaBodyEntered;
    _rideAreaNode.BodyExited += _onRideAreaBodyExited;
  }

  public override string[] _GetConfigurationWarnings() {
    var warnings = new List<string>();
    if (Mathf.IsZeroApprox(Depth)) {
      warnings.Add("Depth is zero, so the platform never gives. Use a plain FlatPlatform for a surface that holds.");
    }
    if (SinkSpeed <= 0.0f) {
      warnings.Add("SinkSpeed is zero, so the platform never sinks under the player.");
    }
    return [.. base._GetConfigurationWarnings(), .. warnings];
  }

  public override void _Notification(int what) {
    base._Notification(what);
    // Only while the platform is being authored: once it is running, its own transform changes are
    // the give itself, and the rest it comes back to is not measured from where it has got to.
    if (what == CanvasItem.NotificationTransformChanged && Engine.IsEditorHint()) {
      _reread();
    }
  }

  public override void _PhysicsProcess(double delta) {
    _sink.Step(delta, _isRidden());
    var moving = !Mathf.IsZeroApprox(_sink.Travelled);
    if (_showGear && moving) {
      _gearNode.Spin(_sink.Travelled);
    }
    _hum(moving);
    if (_sink.IsResting) {
      SetPhysicsProcess(false);
    }
  }

  // Only while the platform is actually giving. A platform sat at the bottom of its give under the
  // player's feet is silent, the same way one standing at its rest is.
  private void _hum(bool moving) {
    var wanted = moving && PlaySound;
    if (wanted && !_soundNode.Playing) {
      _soundNode.Play();
    }
    else if (!wanted && _soundNode.Playing) {
      _soundNode.Stop();
    }
  }

  // The platform is what the player is standing on, so a dying one is not weight on it: the cube
  // spends its death on the spot it died, and a step that went on sinking under it would take the
  // ground out from under the respawn.
  private bool _isRidden() =>
    _rider is { } rider && IsInstanceValid(rider) && !rider.IsDying();

  private void _onRideAreaBodyEntered(Node2D body) {
    if (body is not Player.Player player) {
      return;
    }
    _rider = player;
    SetPhysicsProcess(true);
  }

  private void _onRideAreaBodyExited(Node2D body) {
    if (body != _rider) {
      return;
    }
    _rider = null;
    SetPhysicsProcess(true);
  }

  #region Checkpoints
  public override void _EnterTree() {
    base._EnterTree();
    if (Engine.IsEditorHint() || _isSinkSubscribed) {
      return;
    }
    _checkpointBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.CheckpointLoaded _) => _onRespawn());
    _isSinkSubscribed = true;
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (!_isSinkSubscribed) {
      return;
    }
    _checkpointBinding?.Dispose();
    _checkpointBinding = null;
    _isSinkSubscribed = false;
  }

  private void _onRespawn() {
    _sink.Reset();
    // The overlap is re-read rather than dropped: a checkpoint on a sinking platform respawns the
    // player where they already were, and there is no entering to be heard for a body that never
    // left. Whichever way the engine has it a tick late, the signals correct it on the next one.
    _rider = null;
    foreach (var body in _rideAreaNode.GetOverlappingBodies()) {
      if (body is Player.Player player) {
        _rider = player;
      }
    }
    SetPhysicsProcess(true);
  }
  #endregion Checkpoints

  // Sized along with everything else the platform's own size decides.
  protected override void _applyShape() {
    base._applyShape();
    _applyGear();
    _applyRideArea();
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

  // The strip that answers whether the platform is being stood on. It stands proud of the surface and
  // just inside its sides, so what it catches is a body over the platform rather than one alongside
  // it - the platform's own collision box is what actually holds anything up.
  private void _applyRideArea() {
    if (_rideAreaShapeNode is null) {
      return;
    }
    _resizeShape(
      _rideAreaShapeNode,
      new Vector2(Mathf.Max(Size.X - (RIDE_INSET * 2.0f), RIDE_INSET), RIDE_REACH + RIDE_BITE)
    );
    _rideAreaShapeNode.Position = new Vector2(0.0f, (-Size.Y / 2.0f) + ((RIDE_BITE - RIDE_REACH) / 2.0f));
  }

  // The give is fixed once the level is playing, so the track is only ever re-read while it is being
  // authored.
  private void _reread() {
    if (!_isSinkWired) {
      return;
    }
    if (Engine.IsEditorHint()) {
      _sink.Remeasure(this);
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
      _trackNode.Trace(_sink.Rest, _sink.Bottom);
    }
  }
}
