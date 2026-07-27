namespace Wfc.Entities.World.Camera;

using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Screens.Levels;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Layers;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class CameraLocalizer : Node2D {
  public override void _Notification(int what) => this.Notify(what);
  [Dependency]
  public IGameLevel GameLevel => this.DependOn<IGameLevel>();


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
  // Godot 4 semantics, the same as Camera2D.Zoom: above 1 magnifies, below 1 pulls back.
  // Godot 3 read the number the other way round and the ported scenes still carried its
  // values, so a room asking to see more of itself was told to move closer instead.
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
        // These trigger volumes are authored per level and not one of them ever set a
        // mask, so they all sat on Godot's default layer 1 / mask 1 while the player is on
        // layer 2. body_entered could never fire, and no localizer in the game had ever
        // applied its zoom, limits or drag margins. The node that depends on the contract
        // is the one that gets to enforce it, rather than every scene remembering.
        area2D.CollisionMask = PhysicsLayers.Player.Mask;
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
        // Two, not one: the body below reads _positionsNodes[1]. The guard said one, so
        // any scene that actually used this mode threw an index error inside the guard
        // that was meant to prevent exactly that.
        if (_positionsNodes.Count != 2) {
          GD.PushError("Position limiting LIMIT_X_AXIS mode requires you to add two child position nodes");
        }
        else {
          _left = (int)Math.Min(_positionsNodes[0].GlobalPosition.X, _positionsNodes[1].GlobalPosition.X);
          _right = (int)Math.Max(_positionsNodes[0].GlobalPosition.X, _positionsNodes[1].GlobalPosition.X);
        }
        break;
      case CameraLimit.LimitYAxis:
        // Same as LIMIT_X_AXIS: the body reads _positionsNodes[1].
        if (_positionsNodes.Count != 2) {
          GD.PushError("Position limiting LIMIT_Y_AXIS mode requires you to add two child position nodes");
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
    // How much world the camera will actually show at this zoom. Godot 3 scaled the
    // viewport up by the zoom to get this; Godot 4 divides, so the ported arithmetic was
    // wrong by zoom squared and left the room's limits wider than the view it clamps.
    Vector2 visibleWorld = GetViewport().GetVisibleRect().Size / Zoom;

    if (LimitXAxisToViewSize && X_AXIS_LIMITS.Contains(PositionClippingMode)) {
      float diff = (_right - _left) - visibleWorld.X;
      _left += (int)(diff * 0.5f);
      _right = _left + (int)visibleWorld.X;
    }

    if (LimitYAxisToViewSize && Y_AXIS_LIMITS.Contains(PositionClippingMode)) {
      float diff = (_bottom - _top) - visibleWorld.Y;
      _bottom -= (int)(diff * 0.5f);
      _top = _bottom - (int)visibleWorld.Y;
    }
  }

  private void _onBodyEntered(Node body) {
    if (body == GameLevel.PlayerNode) {
      ApplyCameraChanges();
      _widenLimitsToIncludeThePlayer(GameLevel.CameraNode);
    }
  }

  public void ApplyCameraChanges() {
    _setFollowNode();
    SetCameraLimits();
    SetCameraDragMargins();
    GameLevel.CameraNode.zoom_by(Zoom);
  }

  private void _setFollowNode() {
    if (FollowNode != null) {
      Node2D node = GetNode<Node2D>(FollowNode);
      if (node != null) {
        GameLevel.CameraNode.SetFollowNode(node);
      }
    }
  }

  // A limit may frame the room however the level wants, but it may not put the player outside the
  // view: the camera clamps behind them and they keep walking with nothing following.
  //
  // Widened by the least that includes them rather than thrown away. A room whose limits are derived
  // from its markers and then fitted to the viewport can miss the player by a few tens of pixels
  // purely through that arithmetic - the brick breaker arena is a couple of pixels taller than the
  // view, so it cannot hold both its own top edge and the player's feet - and dropping the limit
  // outright turns a rounding-scale overshoot into a room with no framing at all.
  //
  // Only from the trigger, never from SetCameraLimits: BrickBreaker drives that directly to frame its
  // own arena, and this has no business touching limits that code set on purpose a moment earlier.
  private void _widenLimitsToIncludeThePlayer(Godot.Camera2D cameraNode) {
    if (GameLevel.PlayerNode is not { } playerNode) {
      return;
    }
    var half = playerNode.GetCollisionHalfExtents();
    var min = playerNode.GlobalPosition - half;
    var max = playerNode.GlobalPosition + half;

    cameraNode.LimitRight = Mathf.Max(cameraNode.LimitRight, Mathf.CeilToInt(max.X));
    cameraNode.LimitLeft = Mathf.Min(cameraNode.LimitLeft, Mathf.FloorToInt(min.X));
    cameraNode.LimitBottom = Mathf.Max(cameraNode.LimitBottom, Mathf.CeilToInt(max.Y));
    cameraNode.LimitTop = Mathf.Min(cameraNode.LimitTop, Mathf.FloorToInt(min.Y));
  }

  public void SetCameraLimits() {
    _adaptLimitsToScreenSize();
    var cameraNode = GameLevel.CameraNode;
    switch (PositionClippingMode) {
      case CameraLimit.NoLimits:
        cameraNode.LimitLeft = Constants.DEFAULT_CAMERA_LIMIT_LEFT;
        cameraNode.LimitBottom = Constants.DEFAULT_CAMERA_LIMIT_BOTTOM;
        cameraNode.LimitRight = Constants.DEFAULT_CAMERA_LIMIT_RIGHT;
        cameraNode.LimitTop = Constants.DEFAULT_CAMERA_LIMIT_TOP;
        break;
      case CameraLimit.FullLimit:
        cameraNode.LimitLeft = _left;
        cameraNode.LimitBottom = _bottom;
        cameraNode.LimitRight = _right;
        cameraNode.LimitTop = _top;
        break;
      case CameraLimit.LimitBottomRight:
        cameraNode.LimitLeft = Constants.DEFAULT_CAMERA_LIMIT_LEFT;
        cameraNode.LimitBottom = _bottom;
        cameraNode.LimitRight = _right;
        cameraNode.LimitTop = Constants.DEFAULT_CAMERA_LIMIT_TOP;
        break;
      case CameraLimit.LimitBottomLeft:
        cameraNode.LimitLeft = _left;
        cameraNode.LimitBottom = _bottom;
        cameraNode.LimitRight = Constants.DEFAULT_CAMERA_LIMIT_RIGHT;
        cameraNode.LimitTop = Constants.DEFAULT_CAMERA_LIMIT_TOP;
        break;
      case CameraLimit.LimitTopRight:
        cameraNode.LimitLeft = Constants.DEFAULT_CAMERA_LIMIT_LEFT;
        cameraNode.LimitBottom = Constants.DEFAULT_CAMERA_LIMIT_BOTTOM;
        cameraNode.LimitRight = _right;
        cameraNode.LimitTop = _top;
        break;
      case CameraLimit.LimitTopLeft:
        cameraNode.LimitLeft = _left;
        cameraNode.LimitBottom = Constants.DEFAULT_CAMERA_LIMIT_BOTTOM;
        cameraNode.LimitRight = Constants.DEFAULT_CAMERA_LIMIT_RIGHT;
        cameraNode.LimitTop = _top;
        break;
      case CameraLimit.LimitXAxis:
        cameraNode.LimitLeft = _left;
        cameraNode.LimitBottom = Constants.DEFAULT_CAMERA_LIMIT_BOTTOM;
        cameraNode.LimitRight = _right;
        cameraNode.LimitTop = Constants.DEFAULT_CAMERA_LIMIT_TOP;
        break;
      case CameraLimit.LimitYAxis:
        cameraNode.LimitLeft = Constants.DEFAULT_CAMERA_LIMIT_LEFT;
        cameraNode.LimitBottom = _bottom;
        cameraNode.LimitRight = Constants.DEFAULT_CAMERA_LIMIT_RIGHT;
        cameraNode.LimitTop = _top;
        break;
      case CameraLimit.LimitAllButTop:
        cameraNode.LimitLeft = _left;
        cameraNode.LimitBottom = _bottom;
        cameraNode.LimitRight = _right;
        cameraNode.LimitTop = Constants.DEFAULT_CAMERA_LIMIT_TOP;
        break;
      case CameraLimit.LimitAllButLeft:
        cameraNode.LimitLeft = Constants.DEFAULT_CAMERA_LIMIT_LEFT;
        cameraNode.LimitBottom = _bottom;
        cameraNode.LimitRight = _right;
        cameraNode.LimitTop = _top;
        break;
      case CameraLimit.LimitAllButRight:
        cameraNode.LimitLeft = _left;
        cameraNode.LimitBottom = _bottom;
        cameraNode.LimitRight = Constants.DEFAULT_CAMERA_LIMIT_RIGHT;
        cameraNode.LimitTop = _top;
        break;
      case CameraLimit.LimitAllButBottom:
        cameraNode.LimitLeft = _left;
        cameraNode.LimitBottom = Constants.DEFAULT_CAMERA_LIMIT_BOTTOM;
        cameraNode.LimitRight = _right;
        cameraNode.LimitTop = _top;
        break;
      case CameraLimit.LimitLeft:
        cameraNode.LimitLeft = _left;
        break;
      case CameraLimit.LimitRight:
        cameraNode.LimitRight = _right;
        break;
      case CameraLimit.LimitTop:
        cameraNode.LimitTop = _top;
        break;
      case CameraLimit.LimitBottom:
        cameraNode.LimitBottom = _bottom;
        break;
    }
  }

  private void SetCameraDragMargins() {
    var cameraNode = GameLevel.CameraNode;
    if (FullViewportDragMargin) {
      cameraNode.SetDragMarginBottom(1);
      cameraNode.SetDragMarginLeft(1);
      cameraNode.SetDragMarginRight(1);
      cameraNode.SetDragMarginTop(1);
    }
    else {
      cameraNode.SetDragMarginBottom(Constants.DEFAULT_DRAG_MARGIN_TB);
      cameraNode.SetDragMarginLeft(Constants.DEFAULT_DRAG_MARGIN_LR);
      cameraNode.SetDragMarginRight(Constants.DEFAULT_DRAG_MARGIN_LR);
      cameraNode.SetDragMarginTop(Constants.DEFAULT_DRAG_MARGIN_TB);
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
