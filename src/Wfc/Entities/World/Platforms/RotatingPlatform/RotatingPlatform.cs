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

// A flat platform that turns about its own centre: a bar that sweeps out of the player's way and
// back, or one that goes round the same way for as long as the level is open. It turns a leg of
// Sweep degrees at a time and stands still between them, so a quarter turn at a time is a surface
// the player can wait for and step onto. Everything a level author has to set is on this one node,
// and the angle the platform is left at in the editor is the angle the level starts it at.
//
// It is a flat platform in every other respect - the same sliced corners, the same shade band, the
// same colour group deciding which face may land on it. The player only keeps their footing while
// the surface is within a few degrees of level, so a wide sweep is a wall that opens rather than a
// ride; a narrow one is a ride.
[Tool]
[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class RotatingPlatform : FlatPlatform, IPersistent {
  private AutoChannel.Binding? _checkpointBinding;

  #region Constants
  // How much of the platform's own depth the arrow takes up, so it sits inside the surface rather
  // than hanging off a thin bar.
  private const float ARROW_SHARE = 0.7f;
  #endregion Constants

  #region Exports
  // Each of these is the turn itself, and PlatformSpin is where they are described.
  [Export]
  public PlatformSpin.SpinMode Mode {
    get => _spin.Mode;
    set {
      _spin.Mode = value;
      _reread();
    }
  }

  // Degrees per second, signed: negative turns the other way, which is also the way the arrow ends
  // up pointing.
  [Export]
  public float Speed {
    get => _spin.Speed;
    set {
      _spin.Speed = value;
      _reread();
    }
  }

  // How far one leg of the turn goes, in degrees. The platform stands still for WaitTime at the end
  // of every one of them.
  [Export]
  public float Sweep {
    get => _spin.Sweep;
    // How far the platform turns rather than which way: the direction is Speed's to give, and a
    // sweep of its own would only cancel it out.
    set {
      _spin.Sweep = Mathf.Abs(value);
      _reread();
    }
  }

  // Which end of its sweep the platform is left standing at in the editor. Only a platform that
  // comes back has two ends to be left at.
  [Export]
  public PlatformSpin.SpinOrigin StartAt {
    get => _spin.StartAt;
    set => _spin.StartAt = value;
  }

  // How long the platform stands still at the end of every leg. Zero turns it without a pause.
  [Export]
  public float WaitTime {
    get => _spin.WaitTime;
    set => _spin.WaitTime = value;
  }

  [Export]
  public float StartDelay {
    get => _spin.StartDelay;
    // Never negative: a platform owed time before it sets off is what this is for, and a debt the
    // other way would have it start partway through a cycle it has not run yet.
    set => _spin.StartDelay = Mathf.Max(value, 0.0f);
  }

  [Export]
  public bool StartsStopped {
    get => _spin.StartsStopped;
    set => _spin.StartsStopped = value;
  }

  [Export]
  public bool OneShot {
    get => _spin.OneShot;
    set => _spin.OneShot = value;
  }

  // Which phase of a sweep the platform stops for good on. A platform going one way passes through
  // only two of them, and one shot of it is a single leg.
  [Export]
  public PlatformSpin.SpinPhase OneShotPhase {
    get => _spin.OneShotPhase;
    set => _spin.OneShotPhase = value;
  }

  [Export]
  public bool RestoreDelayedStop {
    get => _spin.RestoreDelayedStop;
    set => _spin.RestoreDelayedStop = value;
  }

  // The arrow is the only thing that says this surface is going to turn, and which way. Off for a
  // platform a level means as a surprise.
  [Export]
  public bool ShowArrow {
    get => _showArrow;
    set {
      _showArrow = value;
      _applyArrow();
    }
  }
  private bool _showArrow = true;

  // The rumble a platform makes while it is actually turning. Off for one that is meant to move
  // unheard, and for a level that already has a cue of its own for the same movement.
  [Export]
  public bool PlaySound { get; set; } = true;
  #endregion Exports

  #region Fields
  private readonly PlatformSpin _spin = new PlatformSpin();
  private bool _isSpinSubscribed;
  #endregion Fields

  #region Nodes
  [NodePath("Spin")]
  private AudioStreamPlayer2D _soundNode = default!;
  [NodePath("Arrow")]
  private RotatingPlatformArrow _arrowNode = default!;
  #endregion Nodes

  public override void _Ready() {
    base._Ready();

    if (Engine.IsEditorHint()) {
      SetPhysicsProcess(false);
      _reread();
      return;
    }
    _spin.Begin(this);
    // Pointed before the first tick, so a platform that starts out waiting is already saying which
    // way it is going to go.
    _pointArrow();
  }

  public override string[] _GetConfigurationWarnings() {
    var warnings = new List<string>();
    if (Mathf.IsZeroApprox(Speed)) {
      warnings.Add("Speed is zero, so the platform never turns. Use a plain FlatPlatform for a surface that stands still.");
    }
    if (Mathf.IsZeroApprox(Sweep)) {
      warnings.Add("Sweep is zero, so the platform has no leg to turn and stays where it is.");
    }
    return [.. base._GetConfigurationWarnings(), .. warnings];
  }

  public override void _PhysicsProcess(double delta) {
    _spin.Step(delta);
    var moving = !Mathf.IsZeroApprox(_spin.Turned);
    // Pointed on every tick rather than only on the ticks it turns: which way it points changes
    // while the platform is standing still at the end of a leg, which is exactly when the player is
    // reading it.
    _pointArrow();
    _hum(moving);
    if (_spin.IsResting) {
      SetPhysicsProcess(false);
    }
  }

  // Only while the platform is actually turning: one waiting out its stop, or parked for good, is
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

  public void StopSpinner(bool immediately) => _spin.Stop(immediately);

  public void ResumeSpinner() {
    _spin.Resume();
    SetPhysicsProcess(true);
  }

  #region Checkpoints
  public override void _EnterTree() {
    base._EnterTree();
    if (Engine.IsEditorHint() || _isSpinSubscribed) {
      return;
    }
    _checkpointBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.CheckpointReached m) => _onCheckpointReached(m.Position, m.ColorGroup))
      .On((in IGameEvents.CheckpointLoaded _) => _onRespawn());
    _isSpinSubscribed = true;
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (!_isSpinSubscribed) {
      return;
    }
    _checkpointBinding?.Dispose();
    _checkpointBinding = null;
    _isSpinSubscribed = false;
  }

  private void _onCheckpointReached(Vector2 _position, string _colorGroup) => _spin.OnCheckpointReached();

  private void _onRespawn() {
    _spin.Reset();
    SetPhysicsProcess(true);
  }

  public string GetSaveId() => GetPath();

  public string Save(ISerializer serializer) => _spin.Save(serializer);

  public void Load(ISerializer serializer, string data) {
    _spin.Load(serializer, data);
    SetPhysicsProcess(true);
  }
  #endregion Checkpoints

  // Sized along with everything else the platform's own size decides.
  protected override void _applyShape() {
    base._applyShape();
    _applyArrow();
  }

  private void _applyArrow() {
    // Reached through the base class's own size handling, which runs before this class has anything
    // wired.
    if (_arrowNode is null) {
      return;
    }
    _arrowNode.Visible = _showArrow;
    _arrowNode.FitTo(Mathf.Min(Size.X, Size.Y) * ARROW_SHARE);
    _pointArrow();
  }

  private void _pointArrow() {
    if (_arrowNode is null || !_showArrow) {
      return;
    }
    _arrowNode.Point(_spin.Heading);
  }

  // Nothing about the turn can be re-read once the level is playing, so the only thing listening is
  // the editor.
  private void _reread() {
    if (Engine.IsEditorHint()) {
      UpdateConfigurationWarnings();
      _pointArrow();
    }
  }
}
