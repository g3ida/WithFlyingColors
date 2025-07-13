using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Wfc.Utils;

public partial class CameraLocalizer : Node2D {
  private static readonly CameraLimit[] X_AXIS_LIMITS =
  {
        CameraLimit.FULL_LIMIT,
        CameraLimit.LIMIT_X_AXIS,
        CameraLimit.LIMIT_ALL_BUT_TOP,
        CameraLimit.LIMIT_ALL_BUT_BOTTOM
    };

  private static readonly CameraLimit[] Y_AXIS_LIMITS =
  {
        CameraLimit.FULL_LIMIT,
        CameraLimit.LIMIT_Y_AXIS,
        CameraLimit.LIMIT_ALL_BUT_LEFT,
        CameraLimit.LIMIT_ALL_BUT_RIGHT
    };

  [Export] public bool full_viewport_drag_margin = false;
  [Export] public CameraLimit position_clipping_mode = CameraLimit.FULL_LIMIT;
  [Export] public float zoom = 1.0f;
  [Export] public NodePath follow_node = null;
  [Export] public bool limit_x_axis_to_view_size = false;
  [Export] public bool limit_y_axis_to_view_size = false;

  private List<Marker2D> positionsNodes = new List<Marker2D>();
  private List<Area2D> areaNodes = new List<Area2D>();

  private int top;
  private int bottom;
  private int left;
  private int right;

  public override void _Ready() {
    foreach (Node child in GetChildren()) {
      if (child is Marker2D position2D) {
        positionsNodes.Add(position2D);
      }
      else if (child is Area2D area2D) {
        areaNodes.Add(area2D);
        area2D.Connect(Area2D.SignalName.BodyEntered, new Callable(this, nameof(_OnBodyEntered)));
      }
    }
    SetupLimiting();
  }

  private void SetupLimiting() {
    switch (position_clipping_mode) {
      case CameraLimit.NO_LIMITS:
        break;
      case CameraLimit.FULL_LIMIT:
        if (positionsNodes.Count != 2) {
          GD.PushError("Position limiting FULL mode requires you to add two child position nodes");
        }
        else {
          top = (int)Mathf.Min(positionsNodes[0].GlobalPosition.Y, positionsNodes[1].GlobalPosition.Y);
          bottom = (int)Mathf.Max(positionsNodes[0].GlobalPosition.Y, positionsNodes[1].GlobalPosition.Y);
          left = (int)Mathf.Min(positionsNodes[0].GlobalPosition.X, positionsNodes[1].GlobalPosition.X);
          right = (int)Mathf.Max(positionsNodes[0].GlobalPosition.X, positionsNodes[1].GlobalPosition.X);
        }
        break;
      case CameraLimit.LIMIT_BOTTOM_RIGHT:
        if (positionsNodes.Count != 1) {
          GD.PushError("Position limiting LIMIT_BOTTOM_RIGHT mode requires you to add ONLY one child position node");
        }
        else {
          bottom = (int)positionsNodes[0].GlobalPosition.Y;
          right = (int)positionsNodes[0].GlobalPosition.X;
        }
        break;
      case CameraLimit.LIMIT_BOTTOM_LEFT:
        if (positionsNodes.Count != 1) {
          GD.PushError("Position limiting LIMIT_BOTTOM_LEFT mode requires you to add ONLY one child position node");
        }
        else {
          bottom = (int)positionsNodes[0].GlobalPosition.Y;
          left = (int)positionsNodes[0].GlobalPosition.X;
        }
        break;
      case CameraLimit.LIMIT_TOP_RIGHT:
        if (positionsNodes.Count != 1) {
          GD.PushError("Position limiting LIMIT_TOP_RIGHT mode requires you to add ONLY one child position node");
        }
        else {
          top = (int)positionsNodes[0].GlobalPosition.Y;
          right = (int)positionsNodes[0].GlobalPosition.X;
        }
        break;
      case CameraLimit.LIMIT_TOP_LEFT:
        if (positionsNodes.Count != 1) {
          GD.PushError("Position limiting LIMIT_TOP_LEFT mode requires you to add ONLY one child position node");
        }
        else {
          top = (int)positionsNodes[0].GlobalPosition.Y;
          left = (int)positionsNodes[0].GlobalPosition.X;
        }
        break;
      case CameraLimit.LIMIT_X_AXIS:
        if (positionsNodes.Count != 1) {
          GD.PushError("Position limiting LIMIT_X_AXIS mode requires you to add ONLY one child position node");
        }
        else {
          left = (int)Math.Min(positionsNodes[0].GlobalPosition.X, positionsNodes[1].GlobalPosition.X);
          right = (int)Math.Max(positionsNodes[0].GlobalPosition.X, positionsNodes[1].GlobalPosition.X);
        }
        break;
      case CameraLimit.LIMIT_Y_AXIS:
        if (positionsNodes.Count != 1) {
          GD.PushError("Position limiting LIMIT_Y_AXIS mode requires you to add ONLY one child position node");
        }
        else {
          top = (int)Math.Min(positionsNodes[0].GlobalPosition.Y, positionsNodes[1].GlobalPosition.Y);
          bottom = (int)Math.Max(positionsNodes[0].GlobalPosition.Y, positionsNodes[1].GlobalPosition.Y);
        }
        break;
      case CameraLimit.LIMIT_ALL_BUT_TOP:
        if (positionsNodes.Count != 2) {
          GD.PushError("Position limiting ALL BUT TOP mode requires you to add two child position nodes");
        }
        else {
          bottom = (int)Math.Max(positionsNodes[0].GlobalPosition.Y, positionsNodes[1].GlobalPosition.Y);
          left = (int)Math.Min(positionsNodes[0].GlobalPosition.X, positionsNodes[1].GlobalPosition.X);
          right = (int)Math.Max(positionsNodes[0].GlobalPosition.X, positionsNodes[1].GlobalPosition.X);
        }
        break;
      case CameraLimit.LIMIT_ALL_BUT_BOTTOM:
        if (positionsNodes.Count != 2) {
          GD.PushError("Position limiting ALL BUT BOTTOM mode requires you to add two child position nodes");
        }
        else {
          top = (int)Math.Min(positionsNodes[0].GlobalPosition.Y, positionsNodes[1].GlobalPosition.Y);
          left = (int)Math.Min(positionsNodes[0].GlobalPosition.X, positionsNodes[1].GlobalPosition.X);
          right = (int)Math.Max(positionsNodes[0].GlobalPosition.X, positionsNodes[1].GlobalPosition.X);
        }
        break;
      case CameraLimit.LIMIT_ALL_BUT_LEFT:
        if (positionsNodes.Count != 2) {
          GD.PushError("Position limiting ALL BUT LEFT mode requires you to add two child position nodes");
        }
        else {
          top = (int)Math.Min(positionsNodes[0].GlobalPosition.Y, positionsNodes[1].GlobalPosition.Y);
          bottom = (int)Math.Max(positionsNodes[0].GlobalPosition.Y, positionsNodes[1].GlobalPosition.Y);
          right = (int)Math.Max(positionsNodes[0].GlobalPosition.X, positionsNodes[1].GlobalPosition.X);
        }
        break;
      case CameraLimit.LIMIT_ALL_BUT_RIGHT:
        if (positionsNodes.Count != 2) {
          GD.PushError("Position limiting ALL BUT RIGHT mode requires you to add two child position nodes");
        }
        else {
          top = (int)Math.Min(positionsNodes[0].GlobalPosition.Y, positionsNodes[1].GlobalPosition.Y);
          bottom = (int)Math.Max(positionsNodes[0].GlobalPosition.Y, positionsNodes[1].GlobalPosition.Y);
          left = (int)Math.Min(positionsNodes[0].GlobalPosition.X, positionsNodes[1].GlobalPosition.X);
        }
        break;
      case CameraLimit.LIMIT_LEFT:
        if (positionsNodes.Count != 1) {
          GD.PushError("Position limiting LIMIT_LEFT mode requires you to add one child position node");
        }
        else {
          left = (int)positionsNodes[0].GlobalPosition.X;
        }
        break;
      case CameraLimit.LIMIT_RIGHT:
        if (positionsNodes.Count != 1) {
          GD.PushError("Position limiting LIMIT_RIGHT mode requires you to add one child position node");
        }
        else {
          right = (int)positionsNodes[0].GlobalPosition.X;
        }
        break;
      case CameraLimit.LIMIT_TOP:
        if (positionsNodes.Count != 1) {
          GD.PushError("Position limiting LIMIT_TOP mode requires you to add one child position node");
        }
        else {
          top = (int)positionsNodes[0].GlobalPosition.Y;
        }
        break;
      case CameraLimit.LIMIT_BOTTOM:
        if (positionsNodes.Count != 1) {
          GD.PushError("Position limiting LIMIT_BOTTOM mode requires you to add one child position node");
        }
        else {
          bottom = (int)positionsNodes[0].GlobalPosition.Y;
        }
        break;
    }
  }

  private void _AdaptLimitsToScreenSize() {
    Vector2 viewportRect = GetViewport().GetVisibleRect().Size;
    float invZoom = 1.0f / zoom;

    if (limit_x_axis_to_view_size && X_AXIS_LIMITS.Contains(position_clipping_mode)) {
      float diff = (right - left) * invZoom - viewportRect.X;
      left += (int)(diff * 0.5f * zoom);
      right = left + (int)(viewportRect.X * zoom);
    }

    if (limit_y_axis_to_view_size && Y_AXIS_LIMITS.Contains(position_clipping_mode)) {
      float diff = (bottom - top) * invZoom - viewportRect.Y;
      bottom -= (int)(diff * 0.5f * zoom);
      top = bottom - (int)(viewportRect.Y * zoom);
    }
  }

  private void _OnBodyEntered(Node body) {
    if (body == Global.Instance().Player) {
      ApplyCameraChanges();
    }
  }

  public void ApplyCameraChanges() {
    SetFollowNode();
    SetCameraLimits();
    SetCameraDragMargins();
    Global.Instance().Camera.zoom_by(zoom);
  }

  private void SetFollowNode() {
    if (follow_node != null) {
      Node2D node = GetNode<Node2D>(follow_node);
      if (node != null) {
        Global.Instance().Camera.set_follow_node(node);
      }
    }
  }

  public void SetCameraLimits() {
    _AdaptLimitsToScreenSize();

    switch (position_clipping_mode) {
      case CameraLimit.NO_LIMITS:
        Global.Instance().Camera.LimitLeft = Constants.DEFAULT_CAMERA_LIMIT_LEFT;
        Global.Instance().Camera.LimitBottom = Constants.DEFAULT_CAMERA_LIMIT_BOTTOM;
        Global.Instance().Camera.LimitRight = Constants.DEFAULT_CAMERA_LIMIT_RIGHT;
        Global.Instance().Camera.LimitTop = Constants.DEFAULT_CAMERA_LIMIT_TOP;
        break;
      case CameraLimit.FULL_LIMIT:
        Global.Instance().Camera.LimitLeft = left;
        Global.Instance().Camera.LimitBottom = bottom;
        Global.Instance().Camera.LimitRight = right;
        Global.Instance().Camera.LimitTop = top;
        break;
      case CameraLimit.LIMIT_BOTTOM_RIGHT:
        Global.Instance().Camera.LimitLeft = Constants.DEFAULT_CAMERA_LIMIT_LEFT;
        Global.Instance().Camera.LimitBottom = bottom;
        Global.Instance().Camera.LimitRight = right;
        Global.Instance().Camera.LimitTop = Constants.DEFAULT_CAMERA_LIMIT_TOP;
        break;
      case CameraLimit.LIMIT_BOTTOM_LEFT:
        Global.Instance().Camera.LimitLeft = left;
        Global.Instance().Camera.LimitBottom = bottom;
        Global.Instance().Camera.LimitRight = Constants.DEFAULT_CAMERA_LIMIT_RIGHT;
        Global.Instance().Camera.LimitTop = Constants.DEFAULT_CAMERA_LIMIT_TOP;
        break;
      case CameraLimit.LIMIT_TOP_RIGHT:
        Global.Instance().Camera.LimitLeft = Constants.DEFAULT_CAMERA_LIMIT_LEFT;
        Global.Instance().Camera.LimitBottom = Constants.DEFAULT_CAMERA_LIMIT_BOTTOM;
        Global.Instance().Camera.LimitRight = right;
        Global.Instance().Camera.LimitTop = top;
        break;
      case CameraLimit.LIMIT_TOP_LEFT:
        Global.Instance().Camera.LimitLeft = left;
        Global.Instance().Camera.LimitBottom = Constants.DEFAULT_CAMERA_LIMIT_BOTTOM;
        Global.Instance().Camera.LimitRight = Constants.DEFAULT_CAMERA_LIMIT_RIGHT;
        Global.Instance().Camera.LimitTop = top;
        break;
      case CameraLimit.LIMIT_X_AXIS:
        Global.Instance().Camera.LimitLeft = left;
        Global.Instance().Camera.LimitBottom = Constants.DEFAULT_CAMERA_LIMIT_BOTTOM;
        Global.Instance().Camera.LimitRight = right;
        Global.Instance().Camera.LimitTop = Constants.DEFAULT_CAMERA_LIMIT_TOP;
        break;
      case CameraLimit.LIMIT_Y_AXIS:
        Global.Instance().Camera.LimitLeft = Constants.DEFAULT_CAMERA_LIMIT_LEFT;
        Global.Instance().Camera.LimitBottom = bottom;
        Global.Instance().Camera.LimitRight = Constants.DEFAULT_CAMERA_LIMIT_RIGHT;
        Global.Instance().Camera.LimitTop = top;
        break;
      case CameraLimit.LIMIT_ALL_BUT_TOP:
        Global.Instance().Camera.LimitLeft = left;
        Global.Instance().Camera.LimitBottom = bottom;
        Global.Instance().Camera.LimitRight = right;
        Global.Instance().Camera.LimitTop = Constants.DEFAULT_CAMERA_LIMIT_TOP;
        break;
      case CameraLimit.LIMIT_ALL_BUT_LEFT:
        Global.Instance().Camera.LimitLeft = Constants.DEFAULT_CAMERA_LIMIT_LEFT;
        Global.Instance().Camera.LimitBottom = bottom;
        Global.Instance().Camera.LimitRight = right;
        Global.Instance().Camera.LimitTop = top;
        break;
      case CameraLimit.LIMIT_ALL_BUT_RIGHT:
        Global.Instance().Camera.LimitLeft = left;
        Global.Instance().Camera.LimitBottom = bottom;
        Global.Instance().Camera.LimitRight = Constants.DEFAULT_CAMERA_LIMIT_RIGHT;
        Global.Instance().Camera.LimitTop = top;
        break;
      case CameraLimit.LIMIT_ALL_BUT_BOTTOM:
        Global.Instance().Camera.LimitLeft = left;
        Global.Instance().Camera.LimitBottom = Constants.DEFAULT_CAMERA_LIMIT_BOTTOM;
        Global.Instance().Camera.LimitRight = right;
        Global.Instance().Camera.LimitTop = top;
        break;
      case CameraLimit.LIMIT_LEFT:
        Global.Instance().Camera.LimitLeft = left;
        break;
      case CameraLimit.LIMIT_RIGHT:
        Global.Instance().Camera.LimitRight = right;
        break;
      case CameraLimit.LIMIT_TOP:
        Global.Instance().Camera.LimitTop = top;
        break;
      case CameraLimit.LIMIT_BOTTOM:
        Global.Instance().Camera.LimitBottom = bottom;
        break;
    }
  }

  private void SetCameraDragMargins() {
    if (full_viewport_drag_margin) {
      Global.Instance().Camera.set_drag_margin_bottom(1);
      Global.Instance().Camera.set_drag_margin_left(1);
      Global.Instance().Camera.set_drag_margin_right(1);
      Global.Instance().Camera.set_drag_margin_top(1);
    }
    else {
      Global.Instance().Camera.set_drag_margin_bottom(Constants.DEFAULT_DRAG_MARGIN_TB);
      Global.Instance().Camera.set_drag_margin_left(Constants.DEFAULT_DRAG_MARGIN_LR);
      Global.Instance().Camera.set_drag_margin_right(Constants.DEFAULT_DRAG_MARGIN_LR);
      Global.Instance().Camera.set_drag_margin_top(Constants.DEFAULT_DRAG_MARGIN_TB);
    }
  }

  // public override void _Draw()
  // {
  //     DrawLine(
  //         new Vector2(Constants.DEFAULT_CAMERA_LIMIT_LEFT, top),
  //         new Vector2(Constants.DEFAULT_CAMERA_LIMIT_RIGHT, top),
  //         Colors.Red, 2.0f
  //     );

  //     DrawLine(
  //         new Vector2(Constants.DEFAULT_CAMERA_LIMIT_LEFT, bottom),
  //         new Vector2(Constants.DEFAULT_CAMERA_LIMIT_RIGHT, bottom),
  //         Colors.Green, 2.0f
  //     );

  //     DrawLine(
  //         new Vector2(right, Constants.DEFAULT_CAMERA_LIMIT_TOP),
  //         new Vector2(right, Constants.DEFAULT_CAMERA_LIMIT_BOTTOM),
  //         Colors.Yellow, 2.0f
  //     );

  //     DrawLine(
  //         new Vector2(left, Constants.DEFAULT_CAMERA_LIMIT_TOP),
  //         new Vector2(left, Constants.DEFAULT_CAMERA_LIMIT_BOTTOM),
  //         Colors.Blue, 2.0f
  //     );
  // }

}
