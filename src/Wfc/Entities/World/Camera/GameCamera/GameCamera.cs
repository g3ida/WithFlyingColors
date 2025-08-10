namespace Wfc.Entities.World.Camera;

using System;
using System.Collections.Generic;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using Wfc.Utils;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class GameCamera : Camera2D, IPersistent {
  public const float CAMERA_DRAG_JUMP = 0.45f;

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

  public void OnCameraShakeRequest() {
    GetNode<CameraShake>("CameraShake").Start();
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

  private void _OnCheckpointHit(Node checkpoint) {
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
    zoom_by(_saveData.Zoom);
    LimitBottom = _saveData.BottomLimit;
    LimitTop = _saveData.TopLimit;
    LimitLeft = _saveData.LeftLimit;
    LimitRight = _saveData.RightLimit;

    DragBottomMargin = _saveData.DragBottomMargin;
    DragLeftMargin = _saveData.DragLeftMargin;
    DragRightMargin = _saveData.DragRightMargin;
    DragTopMargin = _saveData.DragTopMargin;

    FollowNode = FollowPath.IsEmpty ? FollowNode : GetNode<Node2D>(FollowPath);
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
  }

  private void _disconnectSignals() {
    EventHandler.Instance.Events.CheckpointReached -= _OnCheckpointHit;
    EventHandler.Instance.Events.CheckpointLoaded -= Reset;
    EventHandler.Instance.Events.PlayerJumped -= _OnPlayerJump;
    EventHandler.Instance.Events.PlayerLand -= _OnPlayerLand;
    EventHandler.Instance.Events.PlayerDying -= _OnPlayerDying;
    EventHandler.Instance.Events.CameraShakeRequest -= OnCameraShakeRequest;
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
