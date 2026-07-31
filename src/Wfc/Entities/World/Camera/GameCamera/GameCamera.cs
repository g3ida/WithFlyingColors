namespace Wfc.Entities.World.Camera;

using System;
using System.Collections.Generic;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

[ScenePath]
public partial class GameCamera : Camera2D, IPersistent {
  public const float CAMERA_DRAG_JUMP = 0.45f;

  // A punch snaps out and eases back in over a longer beat, so what the player reads is the
  // leaving rather than the returning.
  private const float PUNCH_ATTACK = 0.06f;
  private const float PUNCH_RELEASE = 0.3f;

  [Export] public NodePath FollowPath { get; set; } = default!;

  public Node2D FollowNode = default!;
  public float TargetZoom = 1.0f;
  private Tween? ZoomTweener = null;

  private sealed record SaveData(
    float Zoom = 1f,
    int BottomLimit = 10000,
    int TopLimit = 0,
    int LeftLimit = 0,
    int RightLimit = 10000,
    float DragBottomMargin = Constants.DEFAULT_DRAG_MARGIN_TB,
    float DragLeftMargin = Constants.DEFAULT_DRAG_MARGIN_LR,
    float DragRightMargin = Constants.DEFAULT_DRAG_MARGIN_LR,
    float DragTopMargin = Constants.DEFAULT_DRAG_MARGIN_TB,
    string FollowPath = ""
    );
  private SaveData _saveData = new SaveData();

  // Used for tuning camera
  private float _cachedDragMarginTop;
  private float _cachedDragMarginBottom;
  private float _cachedDragMarginLeft;
  private float _cachedDragMarginRight;

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
    FollowNode = GetNode<Node2D>(FollowPath);
    CacheDragMargins();
  }

  public void OnCameraShakeRequest(float amplitude) {
    GetNode<CameraShake>("CameraShake").Start(amplitude: amplitude);
  }

  // A pulse around the zoom the camera is already meant to be at, and never a new zoom of its
  // own: TargetZoom is left alone, so a real zoom change taken mid-punch kills the pulse and
  // wins outright rather than being pulled back to where the punch started.
  public void OnCameraZoomPunchRequest(float strength) {
    var punched = TargetZoom * (1.0f - strength);
    ZoomTweener?.Kill();
    ZoomTweener = CreateTween();
    ZoomTweener.TweenProperty(this, "zoom", new Vector2(punched, punched), PUNCH_ATTACK)
      .SetTrans(Tween.TransitionType.Quad)
      .SetEase(Tween.EaseType.Out);
    ZoomTweener.TweenProperty(this, "zoom", new Vector2(TargetZoom, TargetZoom), PUNCH_RELEASE)
      .SetTrans(Tween.TransitionType.Back)
      .SetEase(Tween.EaseType.Out);
  }

  public override void _Process(double delta) {
    base._Process(delta);
  }

  public override void _PhysicsProcess(double delta) {
    base._PhysicsProcess(delta);
    if (FollowNode != null) {
      GlobalPosition = FollowNode.GlobalPosition;
    }
  }

  private void _OnCheckpointHit(Vector2 _position, string _colorGroup) {
    _saveData = new SaveData(
      Zoom: TargetZoom,
      BottomLimit: LimitBottom,
      TopLimit: LimitTop,
      LeftLimit: LimitLeft,
      RightLimit: LimitRight,
      DragBottomMargin: _cachedDragMarginBottom,
      DragLeftMargin: _cachedDragMarginLeft,
      DragRightMargin: _cachedDragMarginRight,
      DragTopMargin: _cachedDragMarginTop,
      FollowPath: FollowNode.GetPath().ToString()
    );
  }

  public void Reset() {
    // A respawn is a cut, not a transition. The death that led here may have left a zoom punch
    // mid-release, and easing back from it would carry the death's camera into the next life.
    // The checkpoint saved no such transient, so none is kept: the zoom snaps before the limits
    // are trusted again. The offset is CameraShake's to clear, on the same signal.
    ZoomTweener?.Kill();
    TargetZoom = _saveData.Zoom;
    Zoom = new Vector2(TargetZoom, TargetZoom);
    LimitBottom = _saveData.BottomLimit;
    LimitTop = _saveData.TopLimit;
    LimitLeft = _saveData.LeftLimit;
    LimitRight = _saveData.RightLimit;

    DragBottomMargin = _saveData.DragBottomMargin;
    DragLeftMargin = _saveData.DragLeftMargin;
    DragRightMargin = _saveData.DragRightMargin;
    DragTopMargin = _saveData.DragTopMargin;

    FollowNode = FollowPath.IsEmpty ? FollowNode : GetNode<Node2D>(FollowPath);

    // Snapped to the follow target after every other CheckpointLoaded handler has run - the
    // player's teleport among them - and clamped by the restored limits from there. A room
    // that wants a particular framing expresses it in its limits (a localizer that freezes
    // the camera collapses them to exactly one legal view), so aligning and clamping is the
    // whole restore: there is no hidden camera state worth carrying over a death.
    Callable.From(_snapToFollowTarget).CallDeferred();
  }

  private void _snapToFollowTarget() {
    if (FollowNode == null || !IsInsideTree()) {
      return;
    }
    GlobalPosition = FollowNode.GlobalPosition;
    Align();
    ResetSmoothing();
  }

  private void _OnPlayerJump() {
    CacheDragMargins();
    if (DragBottomMargin < CAMERA_DRAG_JUMP) {
      DragBottomMargin = CAMERA_DRAG_JUMP;
    }
    if (DragTopMargin < CAMERA_DRAG_JUMP) {
      DragTopMargin = CAMERA_DRAG_JUMP;
    }
  }

  private void _OnPlayerLand() {
    RestoreDragMargins();
  }

  private void _OnPlayerDying(Node? area, Vector2 position, int entityType) {
    RestoreDragMargins();
  }

  private void CacheDragMargins() {
    _cachedDragMarginBottom = DragBottomMargin;
    _cachedDragMarginTop = DragTopMargin;
    _cachedDragMarginLeft = DragLeftMargin;
    _cachedDragMarginRight = DragRightMargin;
  }

  private void RestoreDragMargins() {
    DragBottomMargin = _cachedDragMarginBottom;
    DragTopMargin = _cachedDragMarginTop;
    DragLeftMargin = _cachedDragMarginLeft;
    DragRightMargin = _cachedDragMarginRight;
  }

  public void zoom_by(float factor) {
    TargetZoom = factor;
    if (ZoomTweener != null) {
      ZoomTweener.Kill();
    }
    ZoomTweener = CreateTween();
    ZoomTweener.TweenProperty(this, "zoom", new Vector2(factor, factor), 1.0f);
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

  public async void UpdatePosition(Vector2 pos) {
    PositionSmoothingEnabled = false;
    GlobalPosition = pos;
    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    SetDeferred(Camera2D.PropertyName.PositionSmoothingEnabled, true);
  }

  public void SetFollowNode(Node2D followNode) {
    FollowNode = followNode;
    FollowPath = followNode.GetPath();
  }

  public void SetDragMarginTop(float value) {
    DragTopMargin = value;
    _cachedDragMarginTop = value;
  }

  public void SetDragMarginBottom(float value) {
    DragBottomMargin = value;
    _cachedDragMarginBottom = value;
  }

  public void SetDragMarginLeft(float value) {
    DragLeftMargin = value;
    _cachedDragMarginLeft = value;
  }

  public void SetDragMarginRight(float value) {
    DragRightMargin = value;
    _cachedDragMarginRight = value;
  }

  public string GetSaveId() => this.GetPath();
  public string Save(ISerializer serializer) => serializer.Serialize(_saveData);
  public void Load(ISerializer serializer, string data) {
    var deserializedData = serializer.Deserialize<SaveData>(data);
    this._saveData = deserializedData ?? new SaveData();
    Reset();
  }
}
