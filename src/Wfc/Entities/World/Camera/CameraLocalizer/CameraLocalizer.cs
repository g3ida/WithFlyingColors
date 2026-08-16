namespace Wfc.Entities.World.Camera;

using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Logger;
using Wfc.Screens.Levels;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Layers;

// The framing one room of a level asks for: how far the camera may travel, how much of the world it
// shows and how closely it follows.
//
// Drop one in, draw LimitRect over the part of the level it frames, and tick the edges that hold the
// camera in. Walking into the room is what hands the camera over. A way in that is not the room -
// a doorway the room is entered through, or more than one - is authored as Area2D children, and
// those become the ways in instead.
//
// The room is drawn while the level is being authored, along with the screenful the camera comes to
// rest on, so a framing can be read off the scene instead of played for.
[Tool]
[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class CameraLocalizer : Node2D {
  #region Constants
  public const CameraEdges ALL_EDGES = CameraEdges.Left | CameraEdges.Right | CameraEdges.Top | CameraEdges.Bottom;

  // The drag box a frozen camera is given: one whole screen, which the player cannot reach the
  // edge of.
  private const float FROZEN_DRAG_MARGIN = 1.0f;

  private const float CLAMPED_EDGE_WIDTH = 3.0f;
  private const float OPEN_EDGE_WIDTH = 2.0f;
  private const float OPEN_EDGE_DASH = 16.0f;
  private const float VIEW_WIDTH = 2.0f;

  // A clamped edge is a wall and reads as one; an open edge is barely there. The screenful stands
  // apart from both: it is what the room shows rather than part of its shape.
  private static readonly Color CLAMPED_EDGE_COLOR = new(0.35f, 0.75f, 1.0f, 0.9f);
  private static readonly Color OPEN_EDGE_COLOR = new(1.0f, 1.0f, 1.0f, 0.25f);
  private static readonly Color ROOM_COLOR = new(0.35f, 0.75f, 1.0f, 0.06f);
  private static readonly Color VIEW_COLOR = new(1.0f, 0.8f, 0.25f, 0.7f);
  #endregion Constants

  #region Dependencies
  public override void _Notification(int what) {
    this.Notify(what);
    // Only while the room is being authored: dragging the localizer takes the room with it, and the
    // room is drawn where it ends up.
    if (what == CanvasItem.NotificationTransformChanged && Engine.IsEditorHint()) {
      QueueRedraw();
    }
  }

  [Dependency]
  public IGameLevel GameLevel => this.DependOn<IGameLevel>();
  #endregion Dependencies

  #region Exports
  // Walking in here gives the camera back to the level as it was authored, for stepping out of a
  // room rather than into one. The room is only the way in then, and nothing below applies: what a
  // level is framed by when no room has an opinion is the level's own business.
  [Export]
  public bool RestoreLevelFraming {
    get => _restoreLevelFraming;
    set {
      _restoreLevelFraming = value;
      _refreshEditorView();
      if (Engine.IsEditorHint()) {
        NotifyPropertyListChanged();
      }
    }
  }

  // The room, in this node's own space: move or scale the localizer and the room goes with it.
  [ExportGroup("Room")]
  [Export]
  public Rect2 LimitRect {
    get => _limitRect;
    set {
      _limitRect = value;
      _refreshEditorView();
    }
  }

  [Export]
  public CameraEdges LimitedEdges {
    get => _limitedEdges;
    set {
      _limitedEdges = value;
      _refreshEditorView();
    }
  }

  // These squeeze the room to exactly one screenful about its own centre, which leaves the camera a
  // single legal position on that axis and the room deciding what it shows outright. An axis with
  // an open edge has no band to squeeze and is left alone.
  [Export]
  public bool FitWidthToView {
    get => _fitWidthToView;
    set {
      _fitWidthToView = value;
      _refreshEditorView();
    }
  }

  [Export]
  public bool FitHeightToView {
    get => _fitHeightToView;
    set {
      _fitHeightToView = value;
      _refreshEditorView();
    }
  }

  // Camera2D semantics: above 1 magnifies, below 1 pulls back.
  [ExportGroup("Camera")]
  [Export(PropertyHint.Range, "0.1,4,0.0001,or_greater")]
  public float Zoom {
    get => _zoom;
    set {
      _zoom = value;
      _refreshEditorView();
    }
  }

  // The camera holds the framing its limits give it instead of following the player around the
  // room, so the limits had better decide that framing whole.
  [Export]
  public bool FreezeCamera {
    get => _freezeCamera;
    set {
      _freezeCamera = value;
      _refreshEditorView();
    }
  }

  // What the camera follows while the player is in this room. Left empty it keeps what it had.
  [Export]
  public NodePath? FollowPath { get; set; }
  #endregion Exports

  #region Fields
  private bool _restoreLevelFraming;
  private Rect2 _limitRect = _oneScreenful();
  private CameraEdges _limitedEdges = ALL_EDGES;
  private bool _fitWidthToView;
  private bool _fitHeightToView;
  private float _zoom = 1.0f;
  private bool _freezeCamera;
  #endregion Fields

  public override void _Ready() {
    base._Ready();
    if (Engine.IsEditorHint()) {
      // The room is drawn where the localizer stands, so dragging it has to redraw it.
      SetNotifyTransform(true);
      QueueRedraw();
      return;
    }

    var waysIn = _authoredWaysIn();
    if (waysIn.Count == 0) {
      waysIn.Add(_buildRoomWayIn());
    }
    foreach (var area in waysIn) {
      // Authored trigger volumes never set a mask, so they would sit on the default layer while the
      // player is on their own and never report a body. The node that depends on the contract is the
      // one that gets to enforce it, rather than every scene remembering.
      area.CollisionMask = PhysicsLayers.Player.Mask;
      area.BodyEntered += _onBodyEntered;
    }
  }

  // The whole framing: what the camera follows, how far it may travel, how closely it follows and
  // how much it shows.
  public void ApplyToCamera() {
    if (RestoreLevelFraming) {
      GameLevel.CameraNode.RestoreAuthoredFraming();
      return;
    }
    _applyFollowNode();
    ApplyLimitsToCamera();
    _applyDragMargins();
    GameLevel.CameraNode.ZoomTo(Zoom);
  }

  // The travel on its own, for a room that re-frames itself with the player already inside it.
  public void ApplyLimitsToCamera() {
    if (RestoreLevelFraming) {
      GameLevel.CameraNode.RestoreAuthoredFraming();
      return;
    }
    var room = FramedRoom();
    var camera = GameLevel.CameraNode;
    camera.LimitLeft = _limitOf(CameraEdges.Left, room.Position.X, Constants.DEFAULT_CAMERA_LIMIT_LEFT);
    camera.LimitTop = _limitOf(CameraEdges.Top, room.Position.Y, Constants.DEFAULT_CAMERA_LIMIT_TOP);
    camera.LimitRight = _limitOf(CameraEdges.Right, room.End.X, Constants.DEFAULT_CAMERA_LIMIT_RIGHT);
    camera.LimitBottom = _limitOf(CameraEdges.Bottom, room.End.Y, Constants.DEFAULT_CAMERA_LIMIT_BOTTOM);
  }

  #region Room
  // What the camera is clamped to, in world coordinates: the room as placed, with the axes that ask
  // for it squeezed to a screenful. Measured live rather than read once, so a localizer that is
  // moved frames what it covers now.
  public Rect2 FramedRoom() {
    var room = _worldRoom();
    var view = _viewSize() / Zoom;
    var position = room.Position;
    var size = room.Size;
    if (_isFittedToView(FitWidthToView, CameraEdges.Left | CameraEdges.Right)) {
      position.X += (size.X - view.X) * 0.5f;
      size.X = view.X;
    }
    if (_isFittedToView(FitHeightToView, CameraEdges.Top | CameraEdges.Bottom)) {
      position.Y += (size.Y - view.Y) * 0.5f;
      size.Y = view.Y;
    }
    return new Rect2(position, size);
  }

  private Rect2 _worldRoom() {
    var rect = LimitRect.Abs();
    var transform = GlobalTransform;
    var room = new Rect2(transform * rect.Position, Vector2.Zero);
    room = room.Expand(transform * (rect.Position + new Vector2(rect.Size.X, 0.0f)));
    room = room.Expand(transform * (rect.Position + new Vector2(0.0f, rect.Size.Y)));
    return room.Expand(transform * rect.End);
  }

  private bool _isFittedToView(bool asked, CameraEdges edges) => asked && LimitedEdges.HasFlag(edges);

  private int _limitOf(CameraEdges edge, float edgePosition, int open) =>
    LimitedEdges.HasFlag(edge) ? Mathf.RoundToInt(edgePosition) : open;

  // The screen the game runs at, which in the editor is the project's rather than whatever
  // viewport the level is being drawn in.
  private Vector2 _viewSize() =>
    Engine.IsEditorHint() ? _oneScreenful().Size : GetViewport().GetVisibleRect().Size;

  private static Rect2 _oneScreenful() {
    var size = new Vector2(
      ProjectSettings.GetSetting("display/window/size/viewport_width").AsSingle(),
      ProjectSettings.GetSetting("display/window/size/viewport_height").AsSingle()
    );
    return new Rect2(-size * 0.5f, size);
  }
  #endregion Room

  #region Camera
  private List<Area2D> _authoredWaysIn() => [.. GetChildren().OfType<Area2D>()];

  // A room with no doorway authored on it is entered by walking into the room, so the level author
  // has nothing to build: the volume the camera is framing is the volume that hands it over.
  private Area2D _buildRoomWayIn() {
    var room = LimitRect.Abs();
    var area = new Area2D {
      Name = "RoomWayIn",
      CollisionLayer = 0,
      Monitorable = false,
    };
    area.AddChild(new CollisionShape2D {
      Shape = new RectangleShape2D { Size = room.Size },
      Position = room.GetCenter(),
    });
    AddChild(area);
    return area;
  }

  private void _onBodyEntered(Node2D body) {
    if (body != GameLevel.PlayerNode) {
      return;
    }
    ApplyToCamera();
    _widenLimitsToIncludeThePlayer();
  }

  private void _applyFollowNode() {
    if (FollowPath is null or { IsEmpty: true }) {
      return;
    }
    if (GetNodeOrNull<Node2D>(FollowPath) is { } node) {
      GameLevel.CameraNode.SetFollowNode(node);
    }
    else {
      Log.Error($"{Name} points at '{FollowPath}', which is no Node2D; the camera keeps following what it had.");
    }
  }

  private void _applyDragMargins() {
    var camera = GameLevel.CameraNode;
    camera.SetDragMarginLeft(FreezeCamera ? FROZEN_DRAG_MARGIN : Constants.DEFAULT_DRAG_MARGIN_LR);
    camera.SetDragMarginRight(FreezeCamera ? FROZEN_DRAG_MARGIN : Constants.DEFAULT_DRAG_MARGIN_LR);
    camera.SetDragMarginTop(FreezeCamera ? FROZEN_DRAG_MARGIN : Constants.DEFAULT_DRAG_MARGIN_TB);
    camera.SetDragMarginBottom(FreezeCamera ? FROZEN_DRAG_MARGIN : Constants.DEFAULT_DRAG_MARGIN_TB);
  }

  // A room may frame itself however it likes, but it may not put the player outside the view: the
  // camera would clamp behind them and they would walk on with nothing following. Widened by the
  // least that includes them rather than dropped, so a room that misses by the width of its own
  // rounding keeps its framing.
  //
  // Only from the trigger, never from ApplyLimitsToCamera: BrickBreaker drives that directly to
  // frame its own arena, and this has no business touching limits code set on purpose.
  private void _widenLimitsToIncludeThePlayer() {
    if (GameLevel.PlayerNode is not { } playerNode) {
      return;
    }
    var half = playerNode.GetCollisionHalfExtents();
    var min = playerNode.GlobalPosition - half;
    var max = playerNode.GlobalPosition + half;

    var camera = GameLevel.CameraNode;
    camera.LimitRight = Mathf.Max(camera.LimitRight, Mathf.CeilToInt(max.X));
    camera.LimitLeft = Mathf.Min(camera.LimitLeft, Mathf.FloorToInt(min.X));
    camera.LimitBottom = Mathf.Max(camera.LimitBottom, Mathf.CeilToInt(max.Y));
    camera.LimitTop = Mathf.Min(camera.LimitTop, Mathf.FloorToInt(min.Y));
  }
  #endregion Camera

  #region Editor
  public override void _Draw() {
    if (!Engine.IsEditorHint()) {
      return;
    }

    // World units throughout: a localizer placed at a scale stretches the room it covers, but it
    // must not stretch the lines that describe it or the screenful drawn against them.
    DrawSetTransformMatrix(GlobalTransform.AffineInverse());
    if (RestoreLevelFraming) {
      // Nothing here holds the camera, so there is no wall to draw and no screenful to promise:
      // only the volume that hands it back.
      _drawRoomOutline(_worldRoom(), clamped: 0);
      return;
    }

    var room = FramedRoom();
    DrawRect(room, ROOM_COLOR);
    _drawRoomOutline(room, LimitedEdges);

    // One screenful resting in the middle of the room: what the player standing there sees, and all
    // this room ever shows once both axes are fitted to the view.
    var view = _viewSize() / Zoom;
    DrawRect(new Rect2(room.GetCenter() - (view * 0.5f), view), VIEW_COLOR, filled: false, VIEW_WIDTH);
  }

  // A room that hands the camera back has no framing of its own, so the inspector stops offering to
  // shape one: those fields are still on the node, and a level author reading them would believe
  // them.
  public override void _ValidateProperty(Godot.Collections.Dictionary property) {
    base._ValidateProperty(property);
    if (!RestoreLevelFraming) {
      return;
    }
    var name = property["name"].AsStringName();
    if (name == PropertyName.LimitedEdges
      || name == PropertyName.FitWidthToView
      || name == PropertyName.FitHeightToView
      || name == PropertyName.Zoom
      || name == PropertyName.FreezeCamera
      || name == PropertyName.FollowPath) {
      var usage = (PropertyUsageFlags)property["usage"].AsInt64();
      property["usage"] = (int)(usage | PropertyUsageFlags.ReadOnly);
    }
  }

  public override string[] _GetConfigurationWarnings() {
    var warnings = new List<string>();
    if (RestoreLevelFraming) {
      return LimitRect.Abs().Size is { X: <= 0.0f } or { Y: <= 0.0f }
        ? ["The room has no area, so there is nothing for the player to walk into."]
        : [];
    }
    if (LimitedEdges == 0) {
      warnings.Add("No edge is clamped, so this room lets the camera travel anywhere.");
    }
    if (LimitRect.Abs().Size is { X: <= 0.0f } or { Y: <= 0.0f }) {
      warnings.Add("The room has no area: nothing to frame, and nothing for the player to walk into.");
    }
    if (FreezeCamera && (LimitedEdges != ALL_EDGES || !FitWidthToView || !FitHeightToView)) {
      warnings.Add(
        "A frozen camera never follows the player, so nothing but the limits decides what it shows: "
        + "clamp every edge and fit both axes to the view, or the room is framed wherever the camera "
        + "happened to be left."
      );
    }
    return [.. warnings];
  }

  private void _drawRoomOutline(Rect2 room, CameraEdges clamped) {
    var topRight = new Vector2(room.End.X, room.Position.Y);
    var bottomLeft = new Vector2(room.Position.X, room.End.Y);
    _drawEdge(room.Position, topRight, clamped.HasFlag(CameraEdges.Top));
    _drawEdge(bottomLeft, room.End, clamped.HasFlag(CameraEdges.Bottom));
    _drawEdge(room.Position, bottomLeft, clamped.HasFlag(CameraEdges.Left));
    _drawEdge(topRight, room.End, clamped.HasFlag(CameraEdges.Right));
  }

  private void _drawEdge(Vector2 from, Vector2 to, bool isClamped) {
    if (isClamped) {
      DrawLine(from, to, CLAMPED_EDGE_COLOR, CLAMPED_EDGE_WIDTH);
    }
    else {
      DrawDashedLine(from, to, OPEN_EDGE_COLOR, OPEN_EDGE_WIDTH, OPEN_EDGE_DASH);
    }
  }

  // The exported setters fire while the scene is still loading, before there is a tree to draw in.
  private void _refreshEditorView() {
    if (!Engine.IsEditorHint() || !IsInsideTree()) {
      return;
    }
    QueueRedraw();
    UpdateConfigurationWarnings();
  }
  #endregion Editor
}
