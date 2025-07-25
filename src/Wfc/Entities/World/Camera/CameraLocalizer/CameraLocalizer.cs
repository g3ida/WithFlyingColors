namespace Wfc.Entities.World.Camera;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class CameraLocalizer : Node2D {
  private static readonly CameraLimit[] X_AXIS_LIMITS =
  {
        CameraLimit.FullLimit,
        CameraLimit.LimitXAxis,
        CameraLimit.LimitAllButTop,
        CameraLimit.LimitAllButBottom
    };

  private static readonly CameraLimit[] Y_AXIS_LIMITS =
  {
        CameraLimit.FullLimit,
        CameraLimit.LimitYAxis,
        CameraLimit.LimitAllButLeft,
        CameraLimit.LimitAllButRight
    };

  [Export] public bool FullViewportDragMargin = false;
  [Export] public CameraLimit PositionClippingMode = CameraLimit.FullLimit;
  [Export] public float Zoom = 1.0f;
  [Export] public NodePath? FollowNode = null;
  [Export] public bool LimitXAxisToViewSize = false;
  [Export] public bool LimitYAxisToViewSize = false;

  private List<Marker2D> _positionsNodes = new List<Marker2D>();
  private List<Area2D> _areaNodes = new List<Area2D>();

  private int _top;
  private int _bottom;
  private int _left;
  private int _right;

  public override void _Ready() {
    foreach (Node child in GetChildren()) {
      if (child is Marker2D position2D) {
        _positionsNodes.Add(position2D);
      }
      else if (child is Area2D area2D) {
        _areaNodes.Add(area2D);
        area2D.Connect(Area2D.SignalName.BodyEntered, new Callable(this, nameof(_onBodyEntered)));
      }
    }
    _setupLimiting();
  }

  private void _setupLimiting() {
    switch (PositionClippingMode) {
      case CameraLimit.NoLimits:
        break;
      case CameraLimit.FullLimit:
        if (_positionsNodes.Count != 2) {
          GD.PushError("Position limiting FULL mode requires you to add two child position nodes");
        }
        else {
          _top = (int)Mathf.Min(_positionsNodes[0].GlobalPosition.Y, _positionsNodes[1].GlobalPosition.Y);
          _bottom = (int)Mathf.Max(_positionsNodes[0].GlobalPosition.Y, _positionsNodes[1].GlobalPosition.Y);
          _left = (int)Mathf.Min(_positionsNodes[0].GlobalPosition.X, _positionsNodes[1].GlobalPosition.X);
          _right = (int)Mathf.Max(_positionsNodes[0].GlobalPosition.X, _positionsNodes[1].GlobalPosition.X);
        }
        break;
      case CameraLimit.LimitBottomRight:
        if (_positionsNodes.Count != 1) {
          GD.PushError("Position limiting LIMIT_BOTTOM_RIGHT mode requires you to add ONLY one child position node");
        }
        else {
          _bottom = (int)_positionsNodes[0].GlobalPosition.Y;
          _right = (int)_positionsNodes[0].GlobalPosition.X;
        }
        break;
      case CameraLimit.LimitBottomLeft:
        if (_positionsNodes.Count != 1) {
          GD.PushError("Position limiting LIMIT_BOTTOM_LEFT mode requires you to add ONLY one child position node");
        }
        else {
          _bottom = (int)_positionsNodes[0].GlobalPosition.Y;
          _left = (int)_positionsNodes[0].GlobalPosition.X;
        }
        break;
      case CameraLimit.LimitTopRight:
        if (_positionsNodes.Count != 1) {
          GD.PushError("Position limiting LIMIT_TOP_RIGHT mode requires you to add ONLY one child position node");
        }
        else {
          _top = (int)_positionsNodes[0].GlobalPosition.Y;
          _right = (int)_positionsNodes[0].GlobalPosition.X;
        }
        break;
      case CameraLimit.LimitTopLeft:
        if (_positionsNodes.Count != 1) {
          GD.PushError("Position limiting LIMIT_TOP_LEFT mode requires you to add ONLY one child position node");
        }
        else {
          _top = (int)_positionsNodes[0].GlobalPosition.Y;
          _left = (int)_positionsNodes[0].GlobalPosition.X;
        }
        break;
      case CameraLimit.LimitXAxis:
        if (_positionsNodes.Count != 1) {
          GD.PushError("Position limiting LIMIT_X_AXIS mode requires you to add ONLY one child position node");
        }
        else {
          _left = (int)Math.Min(_positionsNodes[0].GlobalPosition.X, _positionsNodes[1].GlobalPosition.X);
          _right = (int)Math.Max(_positionsNodes[0].GlobalPosition.X, _positionsNodes[1].GlobalPosition.X);
        }
        break;
      case CameraLimit.LimitYAxis:
        if (_positionsNodes.Count != 1) {
          GD.PushError("Position limiting LIMIT_Y_AXIS mode requires you to add ONLY one child position node");
        }
        else {
          _top = (int)Math.Min(_positionsNodes[0].GlobalPosition.Y, _positionsNodes[1].GlobalPosition.Y);
          _bottom = (int)Math.Max(_positionsNodes[0].GlobalPosition.Y, _positionsNodes[1].GlobalPosition.Y);
        }
        break;
      case CameraLimit.LimitAllButTop:
        if (_positionsNodes.Count != 2) {
          GD.PushError("Position limiting ALL BUT TOP mode requires you to add two child position nodes");
        }
        else {
          _bottom = (int)Math.Max(_positionsNodes[0].GlobalPosition.Y, _positionsNodes[1].GlobalPosition.Y);
          _left = (int)Math.Min(_positionsNodes[0].GlobalPosition.X, _positionsNodes[1].GlobalPosition.X);
          _right = (int)Math.Max(_positionsNodes[0].GlobalPosition.X, _positionsNodes[1].GlobalPosition.X);
        }
        break;
      case CameraLimit.LimitAllButBottom:
        if (_positionsNodes.Count != 2) {
          GD.PushError("Position limiting ALL BUT BOTTOM mode requires you to add two child position nodes");
        }
        else {
          _top = (int)Math.Min(_positionsNodes[0].GlobalPosition.Y, _positionsNodes[1].GlobalPosition.Y);
          _left = (int)Math.Min(_positionsNodes[0].GlobalPosition.X, _positionsNodes[1].GlobalPosition.X);
          _right = (int)Math.Max(_positionsNodes[0].GlobalPosition.X, _positionsNodes[1].GlobalPosition.X);
        }
        break;
      case CameraLimit.LimitAllButLeft:
        if (_positionsNodes.Count != 2) {
          GD.PushError("Position limiting ALL BUT LEFT mode requires you to add two child position nodes");
        }
        else {
          _top = (int)Math.Min(_positionsNodes[0].GlobalPosition.Y, _positionsNodes[1].GlobalPosition.Y);
          _bottom = (int)Math.Max(_positionsNodes[0].GlobalPosition.Y, _positionsNodes[1].GlobalPosition.Y);
          _right = (int)Math.Max(_positionsNodes[0].GlobalPosition.X, _positionsNodes[1].GlobalPosition.X);
        }
        break;
      case CameraLimit.LimitAllButRight:
        if (_positionsNodes.Count != 2) {
          GD.PushError("Position limiting ALL BUT RIGHT mode requires you to add two child position nodes");
        }
        else {
          _top = (int)Math.Min(_positionsNodes[0].GlobalPosition.Y, _positionsNodes[1].GlobalPosition.Y);
          _bottom = (int)Math.Max(_positionsNodes[0].GlobalPosition.Y, _positionsNodes[1].GlobalPosition.Y);
          _left = (int)Math.Min(_positionsNodes[0].GlobalPosition.X, _positionsNodes[1].GlobalPosition.X);
        }
        break;
      case CameraLimit.LimitLeft:
        if (_positionsNodes.Count != 1) {
          GD.PushError("Position limiting LIMIT_LEFT mode requires you to add one child position node");
        }
        else {
          _left = (int)_positionsNodes[0].GlobalPosition.X;
        }
        break;
      case CameraLimit.LimitRight:
        if (_positionsNodes.Count != 1) {
          GD.PushError("Position limiting LIMIT_RIGHT mode requires you to add one child position node");
        }
        else {
          _right = (int)_positionsNodes[0].GlobalPosition.X;
        }
        break;
      case CameraLimit.LimitTop:
        if (_positionsNodes.Count != 1) {
          GD.PushError("Position limiting LIMIT_TOP mode requires you to add one child position node");
        }
        else {
          _top = (int)_positionsNodes[0].GlobalPosition.Y;
        }
        break;
      case CameraLimit.LimitBottom:
        if (_positionsNodes.Count != 1) {
          GD.PushError("Position limiting LIMIT_BOTTOM mode requires you to add one child position node");
        }
        else {
          _bottom = (int)_positionsNodes[0].GlobalPosition.Y;
        }
        break;
    }
  }

  private void _adaptLimitsToScreenSize() {
    Vector2 viewportRect = GetViewport().GetVisibleRect().Size;
    float invZoom = 1.0f / Zoom;

    if (LimitXAxisToViewSize && X_AXIS_LIMITS.Contains(PositionClippingMode)) {
      float diff = (_right - _left) * invZoom - viewportRect.X;
      _left += (int)(diff * 0.5f * Zoom);
      _right = _left + (int)(viewportRect.X * Zoom);
    }

    if (LimitYAxisToViewSize && Y_AXIS_LIMITS.Contains(PositionClippingMode)) {
      float diff = (_bottom - _top) * invZoom - viewportRect.Y;
      _bottom -= (int)(diff * 0.5f * Zoom);
      _top = _bottom - (int)(viewportRect.Y * Zoom);
    }
  }

  private void _onBodyEntered(Node body) {
    if (body == Global.Instance().Player) {
      ApplyCameraChanges();
    }
  }

  public void ApplyCameraChanges() {
    _setFollowNode();
    SetCameraLimits();
    SetCameraDragMargins();
    Global.Instance().Camera.zoom_by(Zoom);
  }

  private void _setFollowNode() {
    if (FollowNode != null) {
      Node2D node = GetNode<Node2D>(FollowNode);
      if (node != null) {
        Global.Instance().Camera.SetFollowNode(node);
      }
    }
  }

  public void SetCameraLimits() {
    _adaptLimitsToScreenSize();

    switch (PositionClippingMode) {
      case CameraLimit.NoLimits:
        Global.Instance().Camera.LimitLeft = Constants.DEFAULT_CAMERA_LIMIT_LEFT;
        Global.Instance().Camera.LimitBottom = Constants.DEFAULT_CAMERA_LIMIT_BOTTOM;
        Global.Instance().Camera.LimitRight = Constants.DEFAULT_CAMERA_LIMIT_RIGHT;
        Global.Instance().Camera.LimitTop = Constants.DEFAULT_CAMERA_LIMIT_TOP;
        break;
      case CameraLimit.FullLimit:
        Global.Instance().Camera.LimitLeft = _left;
        Global.Instance().Camera.LimitBottom = _bottom;
        Global.Instance().Camera.LimitRight = _right;
        Global.Instance().Camera.LimitTop = _top;
        break;
      case CameraLimit.LimitBottomRight:
        Global.Instance().Camera.LimitLeft = Constants.DEFAULT_CAMERA_LIMIT_LEFT;
        Global.Instance().Camera.LimitBottom = _bottom;
        Global.Instance().Camera.LimitRight = _right;
        Global.Instance().Camera.LimitTop = Constants.DEFAULT_CAMERA_LIMIT_TOP;
        break;
      case CameraLimit.LimitBottomLeft:
        Global.Instance().Camera.LimitLeft = _left;
        Global.Instance().Camera.LimitBottom = _bottom;
        Global.Instance().Camera.LimitRight = Constants.DEFAULT_CAMERA_LIMIT_RIGHT;
        Global.Instance().Camera.LimitTop = Constants.DEFAULT_CAMERA_LIMIT_TOP;
        break;
      case CameraLimit.LimitTopRight:
        Global.Instance().Camera.LimitLeft = Constants.DEFAULT_CAMERA_LIMIT_LEFT;
        Global.Instance().Camera.LimitBottom = Constants.DEFAULT_CAMERA_LIMIT_BOTTOM;
        Global.Instance().Camera.LimitRight = _right;
        Global.Instance().Camera.LimitTop = _top;
        break;
      case CameraLimit.LimitTopLeft:
        Global.Instance().Camera.LimitLeft = _left;
        Global.Instance().Camera.LimitBottom = Constants.DEFAULT_CAMERA_LIMIT_BOTTOM;
        Global.Instance().Camera.LimitRight = Constants.DEFAULT_CAMERA_LIMIT_RIGHT;
        Global.Instance().Camera.LimitTop = _top;
        break;
      case CameraLimit.LimitXAxis:
        Global.Instance().Camera.LimitLeft = _left;
        Global.Instance().Camera.LimitBottom = Constants.DEFAULT_CAMERA_LIMIT_BOTTOM;
        Global.Instance().Camera.LimitRight = _right;
        Global.Instance().Camera.LimitTop = Constants.DEFAULT_CAMERA_LIMIT_TOP;
        break;
      case CameraLimit.LimitYAxis:
        Global.Instance().Camera.LimitLeft = Constants.DEFAULT_CAMERA_LIMIT_LEFT;
        Global.Instance().Camera.LimitBottom = _bottom;
        Global.Instance().Camera.LimitRight = Constants.DEFAULT_CAMERA_LIMIT_RIGHT;
        Global.Instance().Camera.LimitTop = _top;
        break;
      case CameraLimit.LimitAllButTop:
        Global.Instance().Camera.LimitLeft = _left;
        Global.Instance().Camera.LimitBottom = _bottom;
        Global.Instance().Camera.LimitRight = _right;
        Global.Instance().Camera.LimitTop = Constants.DEFAULT_CAMERA_LIMIT_TOP;
        break;
      case CameraLimit.LimitAllButLeft:
        Global.Instance().Camera.LimitLeft = Constants.DEFAULT_CAMERA_LIMIT_LEFT;
        Global.Instance().Camera.LimitBottom = _bottom;
        Global.Instance().Camera.LimitRight = _right;
        Global.Instance().Camera.LimitTop = _top;
        break;
      case CameraLimit.LimitAllButRight:
        Global.Instance().Camera.LimitLeft = _left;
        Global.Instance().Camera.LimitBottom = _bottom;
        Global.Instance().Camera.LimitRight = Constants.DEFAULT_CAMERA_LIMIT_RIGHT;
        Global.Instance().Camera.LimitTop = _top;
        break;
      case CameraLimit.LimitAllButBottom:
        Global.Instance().Camera.LimitLeft = _left;
        Global.Instance().Camera.LimitBottom = Constants.DEFAULT_CAMERA_LIMIT_BOTTOM;
        Global.Instance().Camera.LimitRight = _right;
        Global.Instance().Camera.LimitTop = _top;
        break;
      case CameraLimit.LimitLeft:
        Global.Instance().Camera.LimitLeft = _left;
        break;
      case CameraLimit.LimitRight:
        Global.Instance().Camera.LimitRight = _right;
        break;
      case CameraLimit.LimitTop:
        Global.Instance().Camera.LimitTop = _top;
        break;
      case CameraLimit.LimitBottom:
        Global.Instance().Camera.LimitBottom = _bottom;
        break;
    }
  }

  private void SetCameraDragMargins() {
    if (FullViewportDragMargin) {
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
