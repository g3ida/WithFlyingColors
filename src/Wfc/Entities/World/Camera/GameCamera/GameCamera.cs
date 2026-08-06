namespace Wfc.Entities.World.Camera;

using Godot;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

// The camera has three layers of state and nothing outside it writes its properties directly.
//
//   baseline   what the level authored, read once and never changed afterwards
//   checkpoint the framing the last checkpoint saw, seeded from the baseline
//   override   the shot a cutscene has borrowed the camera for, revocable at any moment
//
// A respawn revokes the override and reinstates the checkpoint whole, so nothing can survive
// a death by accident: work left in flight is dropped rather than eased out of, and the follow
// target goes back to the one the level named rather than wherever the run had aimed it.
[ScenePath]
public partial class GameCamera : Camera2D, IPersistent {
  #region Constants
  // The room the camera is given while the player is off the ground, so a jump reads as the
  // player leaving rather than the world sliding under them.
  public const float CAMERA_DRAG_JUMP = 0.45f;

  // A punch snaps out and eases back in over a longer beat, so what the player reads is the
  // leaving rather than the returning.
  private const float PUNCH_ATTACK = 0.06f;
  private const float PUNCH_RELEASE = 0.3f;
  private const float ZOOM_TRAVEL = 1.0f;
  #endregion Constants

  #region Exports
  // The node gameplay follows. A localizer or a cutscene may aim the camera elsewhere while
  // the player is alive, but this is what every respawn comes back to.
  [Export] public NodePath FollowPath { get; set; } = default!;
  #endregion Exports

  #region Fields
  public Node2D? FollowNode { get; private set; }
  public float TargetZoom { get; private set; } = 1.0f;

  private CameraFraming _baseline = new();
  private CameraFraming _checkpoint = new();
  private Tween? _zoomTweener;

  // The margins the level asked for. What the camera runs with is these widened while the
  // player is airborne, so a second jump before landing and a localizer taking effect
  // mid-jump both compose instead of overwriting the value there is to come back to.
  private float _authoredDragTop;
  private float _authoredDragBottom;
  private float _authoredDragLeft;
  private float _authoredDragRight;
  private bool _isAirborne;

  private float _authoredSmoothingSpeed;
  private bool _authoredSmoothingEnabled;

  // Bumped by every borrow and every hand-back, so a shot that outlives the respawn which
  // cancelled it finds its token stale and cannot write to a camera the reload has restored.
  private int _focusGeneration;
  private bool _hasFocusOverride;
  private Node2D? _focusReturnNode;
  #endregion Fields

  public override void _EnterTree() {
    base._EnterTree();
    _connectSignals();
  }

  public override void _ExitTree() {
    base._ExitTree();
    _disconnectSignals();
  }

  public override void _Ready() {
    base._Ready();
    FollowNode = _resolveAuthoredFollowTarget();
    TargetZoom = Zoom.X;
    _authoredDragTop = DragTopMargin;
    _authoredDragBottom = DragBottomMargin;
    _authoredDragLeft = DragLeftMargin;
    _authoredDragRight = DragRightMargin;
    _authoredSmoothingSpeed = PositionSmoothingSpeed;
    _authoredSmoothingEnabled = PositionSmoothingEnabled;
    // The level as authored is what a death before the first checkpoint restores. Left at the
    // record's own defaults it would restore limits no level ever asked for, and drop the view
    // somewhere the player has never seen it.
    _baseline = _captureFraming();
    _checkpoint = _baseline;
  }

  public override void _PhysicsProcess(double delta) {
    base._PhysicsProcess(delta);
    if (IsInstanceValid(FollowNode)) {
      GlobalPosition = FollowNode!.GlobalPosition;
    }
  }

  public void Reset() {
    // Subscribed in _EnterTree, one step before the level as authored has been read: a reload
    // arriving in between has no baseline to restore and would go on to make one out of the
    // defaults it had just written over the scene.
    if (!IsNodeReady()) {
      return;
    }

    // A respawn is a cut, not a transition. A zoom punch mid-release, a cutscene still walking
    // the camera somewhere, a drag margin a jump had widened: all dropped, none eased out of.
    // The offset is CameraShake's to clear, on the same signal.
    _revokeFocusOverride();
    _zoomTweener?.Kill();
    _zoomTweener = null;
    PositionSmoothingEnabled = _authoredSmoothingEnabled;
    _isAirborne = false;
    _applyFraming(_checkpoint);
    FollowNode = _resolveAuthoredFollowTarget();

    // Snapped after every other CheckpointLoaded handler has run - the player's teleport among
    // them - and clamped by the restored limits from there. A room that wants a particular
    // framing expresses it in its limits (a localizer that freezes the camera collapses them to
    // exactly one legal view), so aligning and clamping is the whole restore.
    Callable.From(_snapToFollowTarget).CallDeferred();
  }

  // Opens the camera on a position instead of travelling to it, for a level entered from a save.
  public void SnapTo(Vector2 position) {
    GlobalPosition = position;
    ResetSmoothing();
    ResetPhysicsInterpolation();
  }

  #region Focus override
  // A cutscene borrows the camera rather than assigning to it. There is one borrow at a time
  // with one owner, and the returned token is what proves that ownership is still current.
  public int BeginFocusOverride(Node2D target, float smoothingSpeed) {
    if (!_hasFocusOverride) {
      _focusReturnNode = FollowNode;
      _hasFocusOverride = true;
    }
    FollowNode = target;
    PositionSmoothingSpeed = smoothingSpeed;
    return ++_focusGeneration;
  }

  // Aims back at what the camera was following before the borrow, still at the borrowed travel
  // speed: the way back is part of the shot, so the camera is not handed over yet.
  public void ReturnFocus(int token) {
    if (_holdsFocus(token)) {
      FollowNode = _focusReturnTarget();
    }
  }

  public void EndFocusOverride(int token) {
    if (_holdsFocus(token)) {
      FollowNode = _focusReturnTarget();
      _revokeFocusOverride();
    }
  }

  private bool _holdsFocus(int token) => _hasFocusOverride && token == _focusGeneration;

  // Whatever the shot took the camera from, or the level's own target if that node is gone by
  // now: the camera is never handed back with nothing to follow.
  private Node2D? _focusReturnTarget() =>
    IsInstanceValid(_focusReturnNode) ? _focusReturnNode : _resolveAuthoredFollowTarget();

  // Every token issued so far is stale from here on, so a shot that has been called off can
  // neither aim the camera nor hand it back.
  private void _revokeFocusOverride() {
    _hasFocusOverride = false;
    _focusReturnNode = null;
    _focusGeneration++;
    PositionSmoothingSpeed = _authoredSmoothingSpeed;
  }
  #endregion Focus override

  #region Framing
  // Gives the camera back to the level for a player who has walked out of every room that had an
  // opinion about it. Not the same as no limits at all: what the level was authored with is a
  // framing of its own, and opening the limits right up would let the camera travel off the end of
  // it. The zoom is eased like any other room's; the rest has nothing to travel from.
  public void RestoreAuthoredFraming() {
    _applyFraming(_baseline with { Zoom = TargetZoom });
    FollowNode = _resolveAuthoredFollowTarget();
    ZoomTo(_baseline.Zoom);
  }

  private CameraFraming _captureFraming() => new(
    Zoom: TargetZoom,
    TopLimit: LimitTop,
    BottomLimit: LimitBottom,
    LeftLimit: LimitLeft,
    RightLimit: LimitRight,
    DragTopMargin: _authoredDragTop,
    DragBottomMargin: _authoredDragBottom,
    DragLeftMargin: _authoredDragLeft,
    DragRightMargin: _authoredDragRight
  );

  private void _applyFraming(CameraFraming framing) {
    TargetZoom = framing.Zoom;
    Zoom = new Vector2(TargetZoom, TargetZoom);
    LimitTop = framing.TopLimit;
    LimitBottom = framing.BottomLimit;
    LimitLeft = framing.LeftLimit;
    LimitRight = framing.RightLimit;
    _authoredDragTop = framing.DragTopMargin;
    _authoredDragBottom = framing.DragBottomMargin;
    _authoredDragLeft = framing.DragLeftMargin;
    _authoredDragRight = framing.DragRightMargin;
    _applyDragMargins();
  }

  private Node2D? _resolveAuthoredFollowTarget() {
    if (FollowPath is null or { IsEmpty: true }) {
      return FollowNode;
    }
    var target = GetNodeOrNull<Node2D>(FollowPath);
    if (target == null) {
      GD.PushError($"Camera follow target '{FollowPath}' resolves to no Node2D; the camera keeps following what it had.");
      return FollowNode;
    }
    return target;
  }

  private void _snapToFollowTarget() {
    if (!IsInsideTree() || !IsInstanceValid(FollowNode)) {
      return;
    }
    GlobalPosition = FollowNode!.GlobalPosition;
    Align();
    ResetSmoothing();
    ResetPhysicsInterpolation();
  }
  #endregion Framing

  #region Drag margins
  public void SetDragMarginTop(float value) {
    _authoredDragTop = value;
    _applyDragMargins();
  }

  public void SetDragMarginBottom(float value) {
    _authoredDragBottom = value;
    _applyDragMargins();
  }

  public void SetDragMarginLeft(float value) {
    _authoredDragLeft = value;
    _applyDragMargins();
  }

  public void SetDragMarginRight(float value) {
    _authoredDragRight = value;
    _applyDragMargins();
  }

  private void _applyDragMargins() {
    DragTopMargin = _isAirborne ? Mathf.Max(_authoredDragTop, CAMERA_DRAG_JUMP) : _authoredDragTop;
    DragBottomMargin = _isAirborne ? Mathf.Max(_authoredDragBottom, CAMERA_DRAG_JUMP) : _authoredDragBottom;
    DragLeftMargin = _authoredDragLeft;
    DragRightMargin = _authoredDragRight;
  }
  #endregion Drag margins

  #region Zoom
  public void ZoomTo(float zoom) {
    TargetZoom = zoom;
    _zoomTweener?.Kill();
    _zoomTweener = CreateTween();
    _zoomTweener.TweenProperty(this, "zoom", new Vector2(zoom, zoom), ZOOM_TRAVEL);
  }

  // A pulse around the zoom the camera is already meant to be at, and never a new zoom of its
  // own: TargetZoom is left alone, so a real zoom change taken mid-punch kills the pulse and
  // wins outright rather than being pulled back to where the punch started.
  public void OnCameraZoomPunchRequest(float strength) {
    var punched = TargetZoom * (1.0f - strength);
    _zoomTweener?.Kill();
    _zoomTweener = CreateTween();
    _zoomTweener.TweenProperty(this, "zoom", new Vector2(punched, punched), PUNCH_ATTACK)
      .SetTrans(Tween.TransitionType.Quad)
      .SetEase(Tween.EaseType.Out);
    _zoomTweener.TweenProperty(this, "zoom", new Vector2(TargetZoom, TargetZoom), PUNCH_RELEASE)
      .SetTrans(Tween.TransitionType.Back)
      .SetEase(Tween.EaseType.Out);
  }
  #endregion Zoom

  #region Signals
  public void SetFollowNode(Node2D followNode) => FollowNode = followNode;

  public void OnCameraShakeRequest(float amplitude) {
    GetNode<CameraShake>("CameraShake").Start(amplitude: amplitude);
  }

  private void _OnCheckpointHit(Vector2 _position, string _colorGroup) => _checkpoint = _captureFraming();

  private void _OnPlayerJump() {
    _isAirborne = true;
    _applyDragMargins();
  }

  private void _OnPlayerLand() {
    _isAirborne = false;
    _applyDragMargins();
  }

  private void _OnPlayerDying(Node? area, Vector2 position, int entityType) {
    _isAirborne = false;
    _applyDragMargins();
  }

  private void _connectSignals() {
    EventHandler.Instance.Events.CheckpointReached += _OnCheckpointHit;
    EventHandler.Instance.Events.CheckpointLoaded += Reset;
    EventHandler.Instance.Events.PlayerJumped += _OnPlayerJump;
    EventHandler.Instance.Events.PlayerLand += _OnPlayerLand;
    EventHandler.Instance.Events.PlayerDying += _OnPlayerDying;
    EventHandler.Instance.Events.CameraShakeRequest += OnCameraShakeRequest;
    EventHandler.Instance.Events.CameraZoomPunchRequest += OnCameraZoomPunchRequest;
  }

  private void _disconnectSignals() {
    EventHandler.Instance.Events.CheckpointReached -= _OnCheckpointHit;
    EventHandler.Instance.Events.CheckpointLoaded -= Reset;
    EventHandler.Instance.Events.PlayerJumped -= _OnPlayerJump;
    EventHandler.Instance.Events.PlayerLand -= _OnPlayerLand;
    EventHandler.Instance.Events.PlayerDying -= _OnPlayerDying;
    EventHandler.Instance.Events.CameraShakeRequest -= OnCameraShakeRequest;
    EventHandler.Instance.Events.CameraZoomPunchRequest -= OnCameraZoomPunchRequest;
  }
  #endregion Signals

  public string GetSaveId() => GetPath();
  public string Save(ISerializer serializer) => serializer.Serialize(_checkpoint);
  public void Load(ISerializer serializer, string data) {
    _checkpoint = serializer.Deserialize<CameraFraming>(data) ?? _baseline;
    Reset();
  }
}
