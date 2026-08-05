namespace Wfc.Entities.World.Platforms;

using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Screens.Levels;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;
using EventHandler = Wfc.Core.Event.EventHandler;

// The plain building block the levels are laid out with: a flat skin-coloured rectangle of any
// size, carrying the sliced corners and the edge shade the ground tileset uses, so a run of them
// butts together and meets the ground without a seam.
//
// Size it through Size, in pixels, and leave the node's scale alone - the slice and the shade are
// pixel widths the shader reads straight off it.
[Tool]
[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class FlatPlatform : StaticBody2D {
  // Which corners are sliced. The four are independent so a platform can square off exactly the
  // corners a neighbour hides.
  [Flags]
  public enum Corners {
    TopLeft = 1,
    TopRight = 2,
    BottomRight = 4,
    BottomLeft = 8,
  }

  // Which edges carry the shade band. The light comes from the top-left, so only these two ever
  // do; clear the one a neighbour covers or the band reads as a seam between them.
  [Flags]
  public enum Edges {
    Right = 1,
    Bottom = 2,
  }

  #region Constants
  private const Corners ALL_CORNERS = Corners.TopLeft | Corners.TopRight | Corners.BottomRight | Corners.BottomLeft;
  private const Edges ALL_EDGES = Edges.Right | Edges.Bottom;

  // Both are what the ground tileset bakes into its edge tiles. A platform only reads as part of
  // the same surface while they agree, so they are not the level author's to set.
  private const float CHAMFER = 4f;
  private const float SHADE_WIDTH = 3f;

  // Any smaller and the shade band starts eating the body it is supposed to edge.
  private const float MIN_SIZE = 8f;

  // Every tileset in the game is on this cell, and level geometry is laid out against it.
  private const float CELL_SIZE = 32f;

  // The platforms that belong to no colour. The ground draws itself the same way - plain white,
  // never taken through the skin - so a neutral platform set against it is the same surface.
  public const string NEUTRAL = "white";

  private static readonly StringName SizeParam = "u_size";
  private static readonly StringName ChamferParam = "u_chamfer";
  private static readonly StringName ShadedEdgesParam = "u_shaded_edges";
  private static readonly StringName ShadeWidthParam = "u_shade_width";
  #endregion Constants

  #region Dependencies
  public override void _Notification(int what) {
    this.Notify(what);
    if (what == CanvasItem.NotificationTransformChanged) {
      _alignToGrid();
    }
  }

  [Dependency]
  public IGameLevel GameLevel => this.DependOn<IGameLevel>();
  #endregion Dependencies

  #region Exports
  // The platform's footprint in pixels, shade band included. It is also the collision box, the
  // same way the ground counts its edge tiles as solid.
  [Export]
  public Vector2 Size {
    get => _size;
    set {
      _size = _evenSize(value);
      _applyShape();
      _alignToGrid();
    }
  }
  private Vector2 _size = new Vector2(256f, 32f);

  // Holds the top-left corner on the tileset's cell, which is what makes a platform meet the
  // ground: level geometry is laid out on that grid, and a platform that misses it leaves a lip
  // the player walks into and stops dead against - half a pixel is enough. Only the corner is
  // held, so a platform is still free to be any height. Turn it off for a ledge that hangs in the
  // air and lines up with nothing.
  [Export]
  public bool SnapToGrid {
    get => _snapToGrid;
    set {
      _snapToGrid = value;
      // Turning it on has to bring a platform that was placed by hand onto the grid.
      Size = _size;
    }
  }
  private bool _snapToGrid = true;

  // Which colour the platform takes, and so which face of the player may land on it. NEUTRAL is
  // the ground's own white, landed on by every face - what the stretches between the puzzles want.
  [Export(PropertyHint.Enum, "blue,pink,yellow,purple,white")]
  public string Group {
    get => _group;
    set {
      _group = value;
      _applyColorGroups();
      _applyColor();
    }
  }
  private string _group = ColorUtils.BLUE;

  [Export]
  public Corners SlicedCorners {
    get => _slicedCorners;
    set {
      _slicedCorners = value;
      _applyShape();
    }
  }
  private Corners _slicedCorners = ALL_CORNERS;

  [Export]
  public Edges ShadedEdges {
    get => _shadedEdges;
    set {
      _shadedEdges = value;
      _applyShape();
    }
  }
  private Edges _shadedEdges = ALL_EDGES;

  [Export]
  public float SplashDarkness { get; set; } = 0.78f;
  #endregion Exports

  #region Fields
  private float _animationTimer = 10f;
  private Vector2 _contactPosition = Vector2.Zero;
  private bool _isSubscribed;
  // The exported setters fire while the scene is still loading, before there are any nodes to
  // push the new value into.
  private bool _isWired;
  #endregion Fields

  #region Nodes
  [NodePath("Surface")]
  private ColorRect _surfaceNode = default!;
  [NodePath("CollisionShape")]
  private CollisionShape2D _collisionShapeNode = default!;
  [NodePath("Area2D")]
  private Area2D _areaNode = default!;
  [NodePath("Area2D/ColorAreaShape")]
  private CollisionShape2D _colorAreaShapeNode = default!;
  #endregion Nodes

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _isWired = true;

    _applyShape();
    _applyColorGroups();
    _applyColor();

    // Only the editor drags a platform around, and the snap is the only thing listening.
    SetNotifyTransform(Engine.IsEditorHint());

    // Nothing to feed the shader until something lands; OnPlayerLanded turns this back on.
    SetProcess(false);
  }

  public override string[] _GetConfigurationWarnings() {
    var warnings = new List<string>();

    // Scale is the one way to break a platform that leaves it looking fine on its own: the slice
    // and the band come out a different size from the ground's and only read as wrong beside it.
    if (!Scale.IsEqualApprox(Vector2.One)) {
      warnings.Add("Scale stretches the slice and the shade band out of step with the ground. Leave Scale at (1, 1) and set Size instead.");
    }

    var topLeft = Position - (Size / 2f);
    if (!topLeft.IsEqualApprox(topLeft.Round())) {
      warnings.Add("The edges fall between pixels, which leaves a lip the player walks into and stops dead against. Turn SnapToGrid back on, or move the platform to a whole pixel.");
    }

    return [.. warnings];
  }

  public override void _EnterTree() {
    base._EnterTree();
    if (Engine.IsEditorHint() || _isSubscribed) {
      return;
    }
    EventHandler.Instance.Events.PlayerLanded += OnPlayerLanded;
    _isSubscribed = true;
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (!_isSubscribed) {
      return;
    }
    EventHandler.Instance.Events.PlayerLanded -= OnPlayerLanded;
    _isSubscribed = false;
  }

  public void OnPlayerLanded(Node area, Vector2 position) {
    if (area != _areaNode) {
      return;
    }
    _animationTimer = 0f;
    _contactPosition = position;
    SetProcess(true);
  }

  public override void _Process(double delta) {
    base._Process(delta);
    if (Engine.IsEditorHint()) {
      return;
    }

    _animationTimer += (float)delta;

    var camera = GameLevel.CameraNode;
    if (camera != null && _surfaceNode.Material is ShaderMaterial material) {
      var resolution = GetViewport().GetVisibleRect().Size;
      var cameraPosition = camera.GetScreenCenterPosition();
      var onScreen = _contactPosition + (resolution / 2f) - cameraPosition;

      material.SetShaderParameter(PlatformSplash.ContactPosParam, onScreen / resolution);
      material.SetShaderParameter(PlatformSplash.TimerParam, _animationTimer);
      material.SetShaderParameter(PlatformSplash.AspectRatioParam, resolution.Y / resolution.X);
      material.SetShaderParameter(PlatformSplash.DarknessParam, SplashDarkness);
    }

    if (_animationTimer > PlatformSplash.Duration(SplashDarkness)) {
      SetProcess(false);
    }
  }

  private void _applyShape() {
    if (!_isWired) {
      return;
    }

    // The body is centred on the node, the way every other platform in the game is placed.
    _surfaceNode.Position = -Size / 2f;
    _surfaceNode.Size = Size;
    _resizeShape(_collisionShapeNode, Size);
    _resizeShape(_colorAreaShapeNode, Size);

    if (_surfaceNode.Material is ShaderMaterial material) {
      material.SetShaderParameter(SizeParam, Size);
      material.SetShaderParameter(ChamferParam, _chamferWidths());
      material.SetShaderParameter(ShadedEdgesParam, _shadeWeights());
      material.SetShaderParameter(ShadeWidthParam, SHADE_WIDTH);
    }

    if (Engine.IsEditorHint()) {
      UpdateConfigurationWarnings();
    }
  }

  // A centred box with an odd size puts its own edges on half-pixels however carefully it is
  // placed, so the size is held even whether or not the platform is snapped to anything.
  private static Vector2 _evenSize(Vector2 size) {
    // Rounding straight to the nearest even lands on a half for every odd size, where the rounding
    // mode decides it - so the size is taken to a whole number first and an odd one grows by one.
    var even = (size.Round() / 2f).Ceil() * 2f;
    return new Vector2(Mathf.Max(even.X, MIN_SIZE), Mathf.Max(even.Y, MIN_SIZE));
  }

  // The corner rather than the centre: the tileset counts cells from its own origin, so the
  // top-left is the one that has to land on the same grid for the surfaces to meet.
  private void _alignToGrid() {
    if (!SnapToGrid) {
      return;
    }

    var topLeft = (Position - (Size / 2f)).Snapped(new Vector2(CELL_SIZE, CELL_SIZE));
    var target = topLeft + (Size / 2f);
    // The transform notification that brought us here fires again on the way out, so moving only
    // when there is somewhere to move is what ends it.
    if (!Position.IsEqualApprox(target)) {
      Position = target;
    }

    if (Engine.IsEditorHint()) {
      UpdateConfigurationWarnings();
    }
  }

  private void _applyColor() {
    if (!_isWired) {
      return;
    }
    _surfaceNode.Color = _isNeutral()
      ? new Color(1f, 1f, 1f)
      : SkinManager.Instance.CurrentSkin.GetColor(GameSkin.ColorGroupToSkinColor(Group), SkinColorIntensity.Basic);
  }

  // Anything the four colour groups do not name is neutral, so a platform left blank can be landed
  // on rather than being a lethal surface nobody meant to author.
  private bool _isNeutral() => Array.IndexOf(ColorUtils.COLOR_GROUPS, Group) < 0;

  // The groups follow the export rather than being fixed at load: a platform given a new colour in
  // the inspector that still answers to its old one kills whoever lands on what they can see.
  private void _applyColorGroups() {
    if (!_isWired) {
      return;
    }

    foreach (var colorGroup in ColorUtils.COLOR_GROUPS) {
      if (_areaNode.IsInGroup(colorGroup)) {
        _areaNode.RemoveFromGroup(colorGroup);
      }
    }

    if (!_isNeutral()) {
      _areaNode.AddToGroup(Group);
      return;
    }
    foreach (var colorGroup in ColorUtils.COLOR_GROUPS) {
      _areaNode.AddToGroup(colorGroup);
    }
  }

  private Vector4 _chamferWidths() => new Vector4(
    SlicedCorners.HasFlag(Corners.TopLeft) ? CHAMFER : 0f,
    SlicedCorners.HasFlag(Corners.TopRight) ? CHAMFER : 0f,
    SlicedCorners.HasFlag(Corners.BottomRight) ? CHAMFER : 0f,
    SlicedCorners.HasFlag(Corners.BottomLeft) ? CHAMFER : 0f
  );

  private Vector2 _shadeWeights() => new Vector2(
    ShadedEdges.HasFlag(Edges.Right) ? 1f : 0f,
    ShadedEdges.HasFlag(Edges.Bottom) ? 1f : 0f
  );

  private static void _resizeShape(CollisionShape2D collisionShape, Vector2 size) {
    if (collisionShape.Shape is RectangleShape2D rectangle) {
      rectangle.Size = size;
    }
  }
}
