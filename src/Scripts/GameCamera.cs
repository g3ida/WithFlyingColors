using Godot;
using System;
using System.Collections.Generic;
using Wfc.Core.Event;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class GameCamera : Camera2D, IPersistent {
  public const float CAMERA_DRAG_JUMP = 0.45f;

  [Export] public NodePath follow_path { get; set; }

  public Node2D follow;
  public float targetZoom = 1.0f;
  private Tween zoomTweener;

  private Dictionary<string, object> defaultSaveData;

  private Dictionary<string, object> save_data;

  // Used for tuning camera
  private float cachedDragMarginTop;
  private float cachedDragMarginBottom;
  private float cachedDragMarginLeft;
  private float cachedDragMarginRight;

  public override void _Ready() {
    defaultSaveData = new Dictionary<string, object>()
        {
            {"zoom_factor", 1.0f},
            {"bottom_limit", 10000},
            {"top_limit", 0},
            {"left_limit", 0},
            {"right_limit", 10000},
            {"drag_margin_bottom", Constants.DEFAULT_DRAG_MARGIN_TB},
            {"drag_margin_left", Constants.DEFAULT_DRAG_MARGIN_LR},
            {"drag_margin_right", Constants.DEFAULT_DRAG_MARGIN_LR},
            {"drag_margin_top", Constants.DEFAULT_DRAG_MARGIN_TB},
            {"follow_path", follow_path}
        };

    follow = GetNode<Node2D>(follow_path);
    save_data = new Dictionary<string, object>(defaultSaveData);
    if (IsCurrent()) {
      Global.Instance().Camera = this;
    }
    CacheDragMargins();
  }

  public override void _Process(double delta) {
    if (IsCurrent()) {
      Global.Instance().Camera = this;
    }
  }

  public override void _PhysicsProcess(double delta) {
    if (follow != null) {
      GlobalPosition = follow.GlobalPosition;
    }
  }

  private void _OnCheckpointHit(Node checkpoint) {
    // FIXME: better save data as json after migration
    save_data = new Dictionary<string, object>();
    save_data["zoom_factor"] = targetZoom;
    save_data["bottom_limit"] = LimitBottom;
    save_data["top_limit"] = LimitTop;
    save_data["left_limit"] = LimitLeft;
    save_data["right_limit"] = LimitRight;
    save_data["drag_margin_bottom"] = cachedDragMarginBottom;
    save_data["drag_margin_left"] = cachedDragMarginLeft;
    save_data["drag_margin_right"] = cachedDragMarginRight;
    save_data["drag_margin_top"] = cachedDragMarginTop;
    save_data["follow_path"] = follow.GetPath().ToString();
  }

  public void reset() {
    if (save_data != null) {
      zoom_by(Convert.ToSingle(save_data["zoom_factor"]));

      LimitBottom = Helpers.ParseSaveDataInt(save_data, "bottom_limit");
      LimitTop = Helpers.ParseSaveDataInt(save_data, "top_limit");
      LimitLeft = Helpers.ParseSaveDataInt(save_data, "left_limit");
      LimitRight = Helpers.ParseSaveDataInt(save_data, "right_limit");


      DragBottomMargin = Helpers.ParseSaveDataFloat(save_data, "drag_margin_bottom");
      DragLeftMargin = Helpers.ParseSaveDataFloat(save_data, "drag_margin_left");
      DragRightMargin = Helpers.ParseSaveDataFloat(save_data, "drag_margin_right");
      DragTopMargin = Helpers.ParseSaveDataFloat(save_data, "drag_margin_top");
      follow_path = Helpers.ParseSaveDataNodePath(save_data, "follow_path");
      follow = GetNode<Node2D>(follow_path);
    }
  }

  public Dictionary<string, object> save() {
    return save_data != null ? save_data : new Dictionary<string, object>(defaultSaveData);
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

  private void _OnPlayerDying(Node area, Vector2 position, int entityType) {
    RestoreDragMargins();
  }

  private void CacheDragMargins() {
    cachedDragMarginBottom = DragBottomMargin;
    cachedDragMarginTop = DragTopMargin;
    cachedDragMarginLeft = DragLeftMargin;
    cachedDragMarginRight = DragRightMargin;
  }

  private void RestoreDragMargins() {
    DragBottomMargin = cachedDragMarginBottom;
    DragTopMargin = cachedDragMarginTop;
    DragLeftMargin = cachedDragMarginLeft;
    DragRightMargin = cachedDragMarginRight;
  }

  public void zoom_by(float factor) {
    targetZoom = factor;
    if (zoomTweener != null) {
      zoomTweener.Kill();
    }
    zoomTweener = CreateTween();
    zoomTweener.TweenProperty(this, "zoom", new Vector2(factor, factor), 1.0f);
  }

  private void ConnectSignals() {
    EventHandler.Instance.Connect(EventType.CheckpointReached, new Callable(this, nameof(_OnCheckpointHit)));
    EventHandler.Instance.Connect(EventType.CheckpointLoaded, new Callable(this, nameof(reset)));
    EventHandler.Instance.Connect(EventType.PlayerJumped, new Callable(this, nameof(_OnPlayerJump)));
    EventHandler.Instance.Connect(EventType.PlayerLand, new Callable(this, nameof(_OnPlayerLand)));
    EventHandler.Instance.Connect(EventType.PlayerDying, new Callable(this, nameof(_OnPlayerDying)));
  }

  private void DisconnectSignals() {
    EventHandler.Instance.Disconnect(EventType.CheckpointReached, new Callable(this, nameof(_OnCheckpointHit)));
    EventHandler.Instance.Disconnect(EventType.CheckpointLoaded, new Callable(this, nameof(reset)));
    EventHandler.Instance.Disconnect(EventType.PlayerJumped, new Callable(this, nameof(_OnPlayerJump)));
    EventHandler.Instance.Disconnect(EventType.PlayerLand, new Callable(this, nameof(_OnPlayerLand)));
    EventHandler.Instance.Disconnect(EventType.PlayerDying, new Callable(this, nameof(_OnPlayerDying)));
  }

  public override void _EnterTree() {
    ConnectSignals();
  }

  public override void _ExitTree() {
    DisconnectSignals();
  }

  public async void update_position(Vector2 pos) {
    PositionSmoothingEnabled = false;
    GlobalPosition = pos;
    await ToSignal(GetTree(), "process_frame");
    SetDeferred("smoothing_enabled", true);
  }

  public void set_follow_node(Node2D followNode) {
    follow = followNode;
    follow_path = followNode.GetPath();
  }

  public void set_drag_margin_top(float value) {
    DragTopMargin = value;
    cachedDragMarginTop = value;
  }

  public void set_drag_margin_bottom(float value) {
    DragBottomMargin = value;
    cachedDragMarginBottom = value;
  }

  public void set_drag_margin_left(float value) {
    DragLeftMargin = value;
    cachedDragMarginLeft = value;
  }

  public void set_drag_margin_right(float value) {
    DragRightMargin = value;
    cachedDragMarginRight = value;
  }

  public void load(Dictionary<string, object> save_data) {
    this.save_data = save_data;
    reset();
  }
}
