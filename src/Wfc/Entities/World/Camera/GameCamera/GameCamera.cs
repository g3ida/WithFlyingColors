namespace Wfc.Entities.World.Camera;

using System.Collections.Generic;
using Chickensoft.Sync.Primitives;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Logger;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using Wfc.Screens.Levels;
using Wfc.Utils;
using Wfc.Utils.Attributes;

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
  // Every beat the camera moves to is read off its follow speed, in time constants of that same
  // chase, so a room that follows slowly settles and zooms slowly with it rather than mixing one
  // room's pace with a fixed one. The chase is exponential and never actually arrives: what it has
  // is a rate, and these read durations off it.
  private const float SETTLE_TIME_CONSTANTS = 3.0f;
  private const float ZOOM_TIME_CONSTANTS = 5.0f;
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

  // A shot runs for longer than it aims the camera - it is under way from the moment the bars come
  // in - and a room the player walks into on that step waits for the whole of it.
  private bool _isShotRunning;
  private ICameraRoom? _pendingRoom;
  private bool _hasTakenPendingRoom;

  // The rooms the player stands in, the last one walked into last. Entering cannot on its own say
  // which room the camera is in: rooms that share a border overlap under a cube wide enough to
  // stand in both, so a step back over that border enters nothing.
  private readonly List<ICameraRoom> _rooms = [];

  // An eased shot walks the camera along a curve itself. The engine's smoothing is suspended
  // for as long as it does, so the two cannot both be moving the camera at once.
  private bool _isTravelling;
  private bool _hasSuspendedSmoothing;
  private Vector2 _travelFrom;
  private float _travelTime;
  private float _travelElapsed;
  private Tween.TransitionType _travelTransition;
  private Tween.EaseType _travelEase;
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
    if (!IsInstanceValid(FollowNode)) {
      return;
    }
    if (_isTravelling) {
      _advanceTravel((float)delta);
      return;
    }
    GlobalPosition = FollowNode!.GlobalPosition;
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
    _isShotRunning = false;
    _clearPendingRoom();
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
    // A borrow taken over the top of one already running retires it here: the token that started
    // that travel goes stale on the line below, and a stale token can neither stop the curve nor
    // give smoothing back, so nothing else ever would.
    _endTravel();
    if (!_hasFocusOverride) {
      _focusReturnNode = FollowNode;
      _hasFocusOverride = true;
    }
    FollowNode = target;
    PositionSmoothingSpeed = smoothingSpeed;
    _takeUpTheDragSlack();
    return ++_focusGeneration;
  }

  // The same borrow, walked along a curve instead. Smoothing has no duration and no easing to
  // give, so a shot that wants either drives the position itself over a time it fixes up front.
  public int BeginFocusOverride(Node2D target, float travelTime, CameraEasing easing, Tween.EaseType ease) {
    // Read before the borrow: taking up the drag slack puts the position on the target, and what
    // the travel has to start from is where the camera is being seen, not where it is being sent.
    var from = GetScreenCenterPosition();
    var token = BeginFocusOverride(target, _authoredSmoothingSpeed);
    _beginTravel(from, travelTime, easing, ease);
    return token;
  }

  private void _beginTravel(Vector2 from, float travelTime, CameraEasing easing, Tween.EaseType ease) {
    // Suspended rather than reset: the curve is the whole of the motion, and smoothing on top of
    // it would draw the camera somewhere the shot did not ask for.
    PositionSmoothingEnabled = false;
    _hasSuspendedSmoothing = true;
    // Placed on the curve before anything can be drawn from it - the borrow has already moved the
    // position onto the target, which with smoothing off would be seen as a jump.
    GlobalPosition = from;
    Align();
    _isTravelling = true;
    _travelFrom = from;
    _travelTime = Mathf.Max(travelTime, 0.0f);
    _travelElapsed = 0.0f;
    _travelTransition = _toTransitionType(easing);
    _travelEase = ease;
  }

  // Drag would hold the camera a margin behind the curve and leave it there once the travel
  // stopped, so an eased shot collapses it every tick and lands exactly where it was aimed.
  private void _advanceTravel(float delta) {
    _travelElapsed += delta;
    var progress = _travelTime > 0.0f
      ? Tween.InterpolateValue(0.0f, 1.0f, Mathf.Min(_travelElapsed, _travelTime), _travelTime, _travelTransition, _travelEase).AsSingle()
      : 1.0f;
    GlobalPosition = _travelFrom.Lerp(_restingCentreFor(FollowNode!.GlobalPosition), progress);
    Align();
    if (_travelElapsed >= _travelTime) {
      _isTravelling = false;
    }
  }

  // Where the camera can actually come to rest looking at a position. A leg aimed past what the
  // limits allow reaches the wall partway through and then stands still for the rest of its time,
  // which reads as the shot hanging before it hands back - so it is aimed at the wall instead, and
  // its motion and its clock run out together. Clamped in the order the engine clamps, so that a
  // room too small for the view resolves the same way here as it does there.
  private Vector2 _restingCentreFor(Vector2 target) {
    var half = GetViewportRect().Size * 0.5f / Zoom;
    return new Vector2(
      Mathf.Min(Mathf.Max(target.X, LimitLeft + half.X), LimitRight - half.X),
      Mathf.Min(Mathf.Max(target.Y, LimitTop + half.Y), LimitBottom - half.Y)
    );
  }

  private static Tween.TransitionType _toTransitionType(CameraEasing easing) => easing switch {
    CameraEasing.Sine => Tween.TransitionType.Sine,
    CameraEasing.Quad => Tween.TransitionType.Quad,
    CameraEasing.Cubic => Tween.TransitionType.Cubic,
    CameraEasing.Quart => Tween.TransitionType.Quart,
    CameraEasing.Quint => Tween.TransitionType.Quint,
    CameraEasing.Expo => Tween.TransitionType.Expo,
    CameraEasing.Circ => Tween.TransitionType.Circ,
    CameraEasing.Back => Tween.TransitionType.Back,
    CameraEasing.Elastic => Tween.TransitionType.Elastic,
    CameraEasing.Bounce => Tween.TransitionType.Bounce,
    _ => Tween.TransitionType.Linear,
  };

  // The drag box is left wherever the chase dragged it, a margin off the node the camera was
  // following, and the shot would ease that margin before it showed anything. Taken up as the
  // borrow begins, so a shot aimed at the frame the camera is already on holds it still.
  private void _takeUpTheDragSlack() {
    if (!IsInsideTree() || !IsInstanceValid(FollowNode)) {
      return;
    }
    GlobalPosition = FollowNode!.GlobalPosition;
    Align();
  }

  // Aims back at what the camera was following before the borrow, still at the borrowed travel
  // speed: the way back is part of the shot, so the camera is not handed over yet.
  public void ReturnFocus(int token) {
    if (_holdsFocus(token)) {
      _takePendingRoom();
      FollowNode = _focusReturnTarget();
    }
  }

  // The way back on a curve of its own. Aimed at a target that is moving under it, so the travel
  // reads the position live rather than settling on where the player was when the shot turned around.
  public void ReturnFocus(int token, float travelTime, CameraEasing easing, Tween.EaseType ease) {
    if (!_holdsFocus(token)) {
      return;
    }
    var from = GetScreenCenterPosition();
    // Before the leg is aimed and paced, so the room decides where the camera is going: the way back
    // is the only travel there needs to be.
    _takePendingRoom();
    FollowNode = _focusReturnTarget();
    _beginTravel(from, travelTime, easing, ease);
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
    _endTravel();
  }

  private void _endTravel() {
    _isTravelling = false;
    if (!_hasSuspendedSmoothing) {
      return;
    }
    _hasSuspendedSmoothing = false;
    PositionSmoothingEnabled = _authoredSmoothingEnabled;
    // Taken up again on the position the curve left the camera on. Smoothing kept its own copy from
    // before the shot, and without this it would ease in from wherever the camera then was.
    ResetSmoothing();
  }
  #endregion Focus override

  #region Room framing
  // A shot claims the camera here rather than at the borrow, so the beat it opens on is covered too.
  public void BeginShot() => _isShotRunning = true;

  // The view the camera is left on, taken once its last leg has landed and while the shot still has
  // its bars in: this is the only moment a view may change, since one changing under a travelling
  // leg re-clamps the camera every frame and drags it off its own curve. Answers with how long the
  // change takes, which is what the shot holds for before it hands back.
  //
  // The room the player walked into while the shot ran, if one is waiting - and a shot that never
  // got as far as turning around takes the whole of that room here. Otherwise the view the camera
  // was on before the shot widened it, so a shot never leaves the camera pulled out.
  public float SettleViewAfterShot(float openingZoom) {
    var room = _takeStockOfPendingRoom();
    var hasTaken = _hasTakenPendingRoom;
    _clearPendingRoom();
    if (room is null) {
      return _easeViewTo(openingZoom);
    }
    if (!hasTaken) {
      room.TakeTheCamera();
    }
    return room.ShowTheRoom(aPanIsStillToCome: !hasTaken);
  }

  public void EndShot() {
    _isShotRunning = false;
    _clearPendingRoom();
  }

  // The camera zooms while it is still and moves at a fixed view, never both at once: a view
  // changing under a travelling shot re-clamps it against the room's limits every frame and drags
  // it somewhere the curve never asked for. So the shot opens on its view here, holds for the beat
  // this answers with, and only then starts moving. The way back is the mirror of it: the view is
  // left alone until the leg has landed, and EndShot tightens it.
  //
  // The view is the shot's own if it was given one, otherwise the room the player has just walked
  // into, otherwise the one the camera already has.
  public float SettleViewForShot(float shotZoom) =>
    _easeViewTo(shotZoom > 0.0f ? shotZoom : _takeStockOfPendingRoom()?.Zoom ?? TargetZoom);

  private float _easeViewTo(float zoom) => ZoomTo(zoom);

  // A room's framing, taken now or held for the shot on the camera. Only one room can be waiting,
  // so the last one walked into is the one the camera settles into.
  public void ApplyRoomFraming(ICameraRoom room) {
    _rooms.Remove(room);
    _rooms.Add(room);
    _frameTheRoomInCharge();
  }

  // Walked out of rather than into. The room underneath takes over, which is the whole of stepping
  // back over a border the cube was standing on both sides of. A doorway has nothing underneath it
  // and hands nothing back: what it framed is meant to outlast the step that crossed it.
  public void ReleaseRoomFraming(ICameraRoom room) {
    var wasInCharge = _rooms.Count > 0 && ReferenceEquals(_rooms[^1], room);
    if (_rooms.Remove(room) && wasInCharge) {
      _frameTheRoomInCharge();
    }
  }

  private void _frameTheRoomInCharge() {
    _rooms.RemoveAll(room => room is GodotObject node && !IsInstanceValid(node));
    if (_rooms.Count == 0) {
      return;
    }
    var inCharge = _rooms[^1];
    if (_isShotRunning) {
      _pendingRoom = inCharge;
      _hasTakenPendingRoom = false;
      return;
    }
    _clearPendingRoom();
    inCharge.TakeTheCamera();
    // Walked into rather than settled into: the pan the room's limits just caused is still to come.
    inCharge.ShowTheRoom(aPanIsStillToCome: true);
  }

  // As the shot starts its way home: the leg travels into the room's limits and absorbs the clamp,
  // instead of settling on the player and being snapped into them afterwards. The room stays
  // pending, since what it shows is not due until the leg has landed.
  private void _takePendingRoom() {
    if (_takeStockOfPendingRoom() is { } room && !_hasTakenPendingRoom) {
      _hasTakenPendingRoom = true;
      room.TakeTheCamera();
    }
  }

  // A room is a node, and a shot outlives any number of ways for one to be freed under it. Call this
  // rather than reading the field, so a room that is gone is dropped instead of thrown on.
  private ICameraRoom? _takeStockOfPendingRoom() {
    if (_pendingRoom is GodotObject node && !IsInstanceValid(node)) {
      _clearPendingRoom();
    }
    return _pendingRoom;
  }

  private void _clearPendingRoom() {
    _pendingRoom = null;
    _hasTakenPendingRoom = false;
  }

  // A room names what gameplay follows. Under a shot that is what the camera comes back to rather
  // than what it is looking at now, so a room settled into mid-shot cannot take the camera off it.
  public void SetFollowNode(Node2D followNode) {
    if (_hasFocusOverride) {
      _focusReturnNode = followNode;
      return;
    }
    FollowNode = followNode;
  }
  #endregion Room framing

  #region Framing
  // Gives the camera back to the level for a player who has walked out of every room that had an
  // opinion about it. Not the same as no limits at all: what the level was authored with is a
  // framing of its own, and opening the limits right up would let the camera travel off the end of
  // it. The zoom is eased like any other room's; the rest has nothing to travel from.
  public void RestoreAuthoredFraming(bool zoomAfterMoving = false) {
    RestoreAuthoredLimits();
    RestoreAuthoredZoom(zoomAfterMoving);
  }

  public void RestoreAuthoredLimits() {
    _applyFraming(_baseline with { Zoom = TargetZoom });
    FollowNode = _resolveAuthoredFollowTarget();
  }

  public float RestoreAuthoredZoom(bool zoomAfterMoving = false) => ZoomTo(_baseline.Zoom, zoomAfterMoving);

  private CameraFraming _captureFraming() => new(
    Zoom: TargetZoom,
    TopLimit: LimitTop,
    BottomLimit: LimitBottom,
    LeftLimit: LimitLeft,
    RightLimit: LimitRight,
    DragTopMargin: _authoredDragTop,
    DragBottomMargin: _authoredDragBottom,
    DragLeftMargin: _authoredDragLeft,
    DragRightMargin: _authoredDragRight,
    FollowSpeed: _authoredSmoothingSpeed
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
    // Through the setter rather than onto the node: a framing restored while a shot holds the camera
    // must not change the speed the shot is travelling at, only the one it hands back to.
    SetFollowSpeed(framing.FollowSpeed);
    _applyDragMargins();
  }

  private Node2D? _resolveAuthoredFollowTarget() {
    if (FollowPath is null or { IsEmpty: true }) {
      return FollowNode;
    }
    var target = GetNodeOrNull<Node2D>(FollowPath);
    if (target == null) {
      Log.Error($"Camera follow target '{FollowPath}' resolves to no Node2D; the camera keeps following what it had.");
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
  // How fast the camera closes on what it follows. Written like a drag margin rather than
  // straight onto the node: a shot hands the camera back at the speed it was borrowed from,
  // and that has to be the room's rather than the one the level opened with.
  // What the level opened with, for a room that has no speed of its own to put back.
  public float AuthoredFollowSpeed => _baseline.FollowSpeed;

  public void SetFollowSpeed(float value) {
    _authoredSmoothingSpeed = value;
    if (!_hasFocusOverride) {
      PositionSmoothingSpeed = value;
    }
  }

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
  public float ZoomTo(float zoom) => ZoomTo(zoom, afterMoving: false);

  // Re-framing and re-zooming at once reads as neither, so a room may hold its zoom back until the
  // travel its limits caused has been absorbed. How long that takes is the camera's own business.
  // Answers with how long the camera takes to get there, for a shot holding its stripes in for it.
  public float ZoomTo(float zoom, bool afterMoving) {
    // Already showing it and not on the way anywhere else: re-asking would kill the tween in flight
    // and start one that does nothing, and a shot holding its bars for that would hold for nothing.
    if (Mathf.IsEqualApprox(zoom, TargetZoom) && Mathf.IsEqualApprox(Zoom.X, zoom)) {
      return 0.0f;
    }
    TargetZoom = zoom;
    _zoomTweener?.Kill();
    _zoomTweener = CreateTween();
    var settle = afterMoving ? _settleTime() : 0.0f;
    if (settle > 0.0f) {
      _zoomTweener.TweenInterval(settle);
    }
    _zoomTweener.TweenProperty(this, "zoom", new Vector2(zoom, zoom), _zoomTime());
    return settle + _zoomTime();
  }

  // Read off the chase rather than off any actual pan: whether the room's limits moved the camera at
  // all is not asked, so a room entered already framed still waits out a beat it did not need. A
  // caller that knows there is no pan coming says so instead, and gets no wait.
  private float _settleTime() => PositionSmoothingEnabled ? SETTLE_TIME_CONSTANTS / _followSpeed() : 0.0f;

  private float _zoomTime() => ZOOM_TIME_CONSTANTS / _followSpeed();

  // What the room asked for rather than what the camera is running at: a shot borrows the camera at
  // a travel speed of its own, and a zoom taken mid-shot still belongs to the room it is in. Never
  // zero, since every beat above is read off it.
  private float _followSpeed() =>
    _authoredSmoothingSpeed > 0.0f ? _authoredSmoothingSpeed : Constants.DEFAULT_CAMERA_FOLLOW_SPEED;

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
  private AutoChannel.Binding? _cameraBinding;

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

  private void _OnPlayerDying() {
    _isAirborne = false;
    _applyDragMargins();
  }

  private void _connectSignals() {
    _cameraBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.CameraShakeRequested message) => OnCameraShakeRequest(message.Amplitude))
      .On((in IGameEvents.CameraZoomPunchRequested message) => OnCameraZoomPunchRequest(message.Strength))
      .On((in IGameEvents.PlayerJumped _) => _OnPlayerJump())
      .On((in IGameEvents.PlayerLanded _) => _OnPlayerLand())
      .On((in IGameEvents.PlayerDying _) => _OnPlayerDying())
      .On((in IGameEvents.CheckpointReached m) => _OnCheckpointHit(m.Position, m.ColorGroup))
      .On((in IGameEvents.CheckpointLoaded _) => Reset());
  }

  private void _disconnectSignals() {
    _cameraBinding?.Dispose();
    _cameraBinding = null;
  }
  #endregion Signals

  public string GetSaveId() => GetPath();
  public string Save(ISerializer serializer) => serializer.Serialize(_checkpoint);
  public void Load(ISerializer serializer, string data) {
    _checkpoint = serializer.Deserialize<CameraFraming>(data) ?? _baseline;
    Reset();
  }
}
