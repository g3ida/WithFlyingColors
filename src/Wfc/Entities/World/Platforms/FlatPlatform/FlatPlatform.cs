namespace Wfc.Entities.World.Platforms;

using Chickensoft.Sync.Primitives;
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
  private AutoChannel.Binding? _landedBinding;

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

  // The coat of paint an inked platform wears, as a share of the platform's own depth: what covers
  // a ledge is a stripe on a wall, and what covers a wall swallows a ledge. Held between these so a
  // thin platform still wears a coat thick enough to read and a deep one is not painted to the
  // ground.
  private const float INK_POOL_SHARE = 0.42f;
  private const float INK_POOL_MIN = 16f;
  private const float INK_POOL_MAX = 52f;
  private const float INK_REACH_SHARE = 0.95f;
  private const float INK_REACH_MIN = 44f;
  private const float INK_REACH_MAX = 140f;

  // What the shader spaces and sizes the drips by at a setting of one, so both fields read as a
  // multiple of the coat the game already wears rather than as pixel counts.
  private const float INK_DRIP_SPACING = 26f;
  private const float INK_DRIP_WIDTH = 26f;
  private const float MIN_DRIP_SCALE = 0.1f;

  // The most of a platform's height the coat may answer for. The sides of a platform are not
  // painted, so however deep the paint is there has to be a side left to touch.
  private const float INK_COAT_MAX_SHARE = 0.5f;

  // What InkColor is set to when the coat is simply the platform's own colour.
  public const string INK_FOLLOWS_PLATFORM = "platform";

  private static readonly StringName InkSizeParam = "u_size";
  private static readonly StringName InkPoolParam = "u_pool";
  private static readonly StringName InkRunParam = "u_reach";
  private static readonly StringName InkSeedParam = "u_seed";
  private static readonly StringName InkSpacingParam = "u_spacing";
  private static readonly StringName InkWidthParam = "u_width";
  private static readonly StringName InkEndsParam = "u_ends";
  private static readonly StringName InkColorParam = "u_color";
  private static readonly StringName InkShadeParam = "u_shade";

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
    // Only while the platform is being authored. A platform that moves under its own power is told
    // about its own transform on every tick of its run, and snapping it back to the grid there
    // fights whatever is carrying it - the platform judders on the spot instead of travelling.
    if (what == CanvasItem.NotificationTransformChanged && Engine.IsEditorHint()) {
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

  // Wears its colour as paint: a coat of it lying along the top with the last of it running off
  // the underside. It says nothing new about the platform - the colour and the box that judges a
  // landing are the same either way - so a level can ink the platforms a puzzle is about and leave
  // the rest plain.
  [Export]
  public bool Inked {
    get => _inked;
    set {
      _inked = value;
      _applyColorGroups();
      _applyInk();
    }
  }
  private bool _inked;

  // Which colour the coat is, when it is not the platform's own. A coat is the top of the platform
  // rather than a decoration on it, so this is also the colour the cube is judged against: a white
  // platform under purple paint is landed on by the purple face alone.
  [Export(PropertyHint.Enum, "platform,blue,pink,yellow,purple")]
  public string InkColor {
    get => _inkColor;
    set {
      _inkColor = value;
      _applyColorGroups();
      _applyInk();
    }
  }
  private string _inkColor = INK_FOLLOWS_PLATFORM;

  // How far the longest drip may run below the coat. Left at zero it is taken from the platform's
  // own height, which is what a platform inked because it is thick wants. A thin ledge says
  // nothing about how far paint should run off it, so anything hanging over a drop is worth
  // saying outright.
  [Export(PropertyHint.Range, "0,400,1,or_greater")]
  public float InkDripLength {
    get => _inkDripLength;
    set {
      _inkDripLength = Mathf.Max(value, 0f);
      _applyInk();
    }
  }
  private float _inkDripLength;

  // How many drips run along the same stretch of coat, against the number the coat carries by
  // default. It says nothing about how thick they are - a denser coat is more of the same drips.
  [Export(PropertyHint.Range, "0.1,4,0.05,or_greater")]
  public float InkDripDensity {
    get => _inkDripDensity;
    set {
      _inkDripDensity = Mathf.Max(value, MIN_DRIP_SCALE);
      _applyInk();
    }
  }
  private float _inkDripDensity = 1f;

  // How thick the drips are, against the thickness the coat carries by default. Independent of how
  // many there are, so a coat can be a few heavy runs or a close fringe of fine ones.
  [Export(PropertyHint.Range, "0.1,4,0.05,or_greater")]
  public float InkDripWidth {
    get => _inkDripWidth;
    set {
      _inkDripWidth = Mathf.Max(value, MIN_DRIP_SCALE);
      _applyInk();
    }
  }
  private float _inkDripWidth = 1f;

  // Which run of paint this coat wears. Left at zero it is taken from where the platform stands, so
  // that two of them side by side never wear the same one. Set it to pick a run by hand - which
  // also holds that run still, where a coat sat by position redraws itself whenever it is nudged.
  [Export]
  public int InkSeed {
    get => _inkSeed;
    set {
      _inkSeed = value;
      _applyInk();
    }
  }
  private int _inkSeed;
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
  [NodePath("Ink")]
  private ColorRect _inkNode = default!;
  [NodePath("CollisionShape")]
  private CollisionShape2D _collisionShapeNode = default!;
  [NodePath("Area2D")]
  private Area2D _areaNode = default!;
  [NodePath("Area2D/ColorAreaShape")]
  private CollisionShape2D _colorAreaShapeNode = default!;
  [NodePath("InkArea")]
  private Area2D _inkAreaNode = default!;
  [NodePath("InkArea/InkAreaShape")]
  private CollisionShape2D _inkAreaShapeNode = default!;
  #endregion Nodes

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _isWired = true;

    _applyShape();
    _applyColorGroups();
    _applyColor();
    _applyInk();

    // The snap has to hear about a platform being dragged. Left to Godot's own setting for a
    // collision object rather than turned off outside the editor: a body that moves needs the same
    // notification to keep its shapes with it.
    SetNotifyTransform(true);

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

    if (Inked && _inkGroup().Length == 0) {
      warnings.Add("Inked, but neither the platform nor InkColor names a colour to wear. Set InkColor, or turn Inked off.");
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
    _landedBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.PlayerLandedOn m) => OnPlayerLanded(m.Area, m.Position));
    _isSubscribed = true;
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (!_isSubscribed) {
      return;
    }
    _landedBinding?.Dispose();
    _landedBinding = null;
    _isSubscribed = false;
  }

  public void OnPlayerLanded(Node area, Vector2 position) {
    if (area != _areaNode) {
      return;
    }
    _animationTimer = 0f;
    // Kept in the platform's own frame rather than the world's, so a platform that is moving
    // carries the splash with it instead of leaving the paint behind where the cube touched down.
    _contactPosition = ToLocal(position);
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
      PlatformSplash.Write(material, camera, ToGlobal(_contactPosition), _animationTimer);
    }

    if (_animationTimer > PlatformSplash.Duration) {
      SetProcess(false);
    }
  }

  // The one hook a platform built on this one gets: everything sized off the platform is sized here,
  // and Size is set from the inspector rather than announced by a notification anything can hear.
  protected virtual void _applyShape() {
    if (!_isWired) {
      return;
    }

    // The body is centred on the node, the way every other platform in the game is placed.
    _surfaceNode.Position = -Size / 2f;
    _surfaceNode.Size = Size;
    _applyInk();
    _resizeShape(_collisionShapeNode, Size);
    _applyColorAreas();

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

  // What the surface is painted, so anything drawn alongside a platform can be drawn in it.
  protected Color SurfaceColor => _isNeutral()
    ? new Color(1f, 1f, 1f)
    : SkinManager.Instance.CurrentSkin.GetColor(GameSkin.ColorGroupToSkinColor(Group), SkinColorIntensity.Basic);

  private void _applyColor() {
    if (!_isWired) {
      return;
    }
    _surfaceNode.Color = SurfaceColor;
    _applyInk();
  }

  // The colour of the coat: the platform's own unless the author named another. Empty when neither
  // names a colour, which is a platform with nothing to wear.
  private string _inkGroup() {
    var group = InkColor == INK_FOLLOWS_PLATFORM ? Group : InkColor;
    return Array.IndexOf(ColorUtils.COLOR_GROUPS, group) < 0 ? string.Empty : group;
  }

  // The coat is laid along the whole top edge, so it is the platform's own width and needs redoing
  // whenever the platform is resized or repainted. It starts exactly on the surface: a coat drawn
  // even a little above it stands on a step of its own and reads as a band laid over the platform
  // rather than as paint lying on it.
  private void _applyInk() {
    if (!_isWired) {
      return;
    }

    var inkGroup = _inkGroup();
    _inkNode.Visible = Inked && inkGroup.Length > 0;
    if (!_inkNode.Visible || _inkNode.Material is not ShaderMaterial material) {
      return;
    }

    var pool = _inkPoolDepth();
    var reach = InkDripLength > 0f
      ? InkDripLength
      : Mathf.Clamp(Size.Y * INK_REACH_SHARE, INK_REACH_MIN, INK_REACH_MAX);
    var size = new Vector2(Size.X, pool + reach);
    _inkNode.Position = new Vector2(-Size.X / 2f, -Size.Y / 2f);
    _inkNode.Size = size;

    var skin = SkinManager.Instance.CurrentSkin;
    var skinColor = GameSkin.ColorGroupToSkinColor(inkGroup);
    material.SetShaderParameter(InkSizeParam, size);
    material.SetShaderParameter(InkPoolParam, pool);
    material.SetShaderParameter(InkRunParam, reach);
    // A coat rather than a spill: it was painted onto the platform, so it reaches both ends of it
    // and stops where the platform does.
    material.SetShaderParameter(InkEndsParam, Vector2.Zero);
    material.SetShaderParameter(InkColorParam, skin.GetColor(skinColor, SkinColorIntensity.Basic));
    material.SetShaderParameter(InkShadeParam, skin.GetColor(skinColor, SkinColorIntensity.Dark));
    // Failing a run chosen by hand, where the platform stands: two of them side by side wear
    // different runs of it, and the same platform wears the same one every time the level is opened.
    material.SetShaderParameter(InkSeedParam, InkSeed != 0
      ? InkSeed
      : Mathf.Abs((Position.X * 0.017f) + (Position.Y * 0.083f)));
    material.SetShaderParameter(InkSpacingParam, INK_DRIP_SPACING / InkDripDensity);
    material.SetShaderParameter(InkWidthParam, INK_DRIP_WIDTH * InkDripWidth);
  }

  // How deep the paint lies on the surface, which is both what the shader draws and how far down
  // the platform the coat is the thing being touched.
  private float _inkPoolDepth() => Mathf.Clamp(Size.Y * INK_POOL_SHARE, INK_POOL_MIN, INK_POOL_MAX);

  // Anything the four colour groups do not name is neutral, so a platform left blank can be landed
  // on rather than being a lethal surface nobody meant to author.
  private bool _isNeutral() => Array.IndexOf(ColorUtils.COLOR_GROUPS, Group) < 0;

  // What the cube actually lands on, which is whatever it can see: a coat of paint if the platform
  // is wearing one, and the platform's own colour if it is not. A white platform under purple paint
  // is a purple surface - the paint is the top of it.
  public string LandingGroup {
    get {
      var ink = _inkGroup();
      return Inked && ink.Length > 0 ? ink : Group;
    }
  }

  // Whether the coat answers for anything the platform itself would not. One wearing its own
  // colour is judged the same whichever part of it is touched, so it is left as a single area.
  private bool _isCoated() {
    var ink = _inkGroup();
    return Inked && ink.Length > 0 && ink != Group;
  }

  // The groups follow the exports rather than being fixed at load: a platform given a new colour in
  // the inspector that still answers to its old one kills whoever lands on what they can see.
  private void _applyColorGroups() {
    if (!_isWired) {
      return;
    }

    // The paint lies on the top and nowhere else, so it is the top alone that answers for it. The
    // platform answers for the rest of itself: brushing the side of a painted platform is touching
    // the platform, not the coat, and dying against a colour that is not on the surface you touched
    // is a death the player has no way to read.
    _setColorGroups(_areaNode, Group);
    _setColorGroups(_inkAreaNode, _inkGroup());
    _applyColorAreas();
  }

  // Anything the four colour groups do not name is neutral, and a neutral surface answers to every
  // face rather than to none.
  private static void _setColorGroups(Area2D area, string group) {
    foreach (var colorGroup in ColorUtils.COLOR_GROUPS) {
      if (area.IsInGroup(colorGroup)) {
        area.RemoveFromGroup(colorGroup);
      }
    }

    if (Array.IndexOf(ColorUtils.COLOR_GROUPS, group) >= 0) {
      area.AddToGroup(group);
      return;
    }
    foreach (var colorGroup in ColorUtils.COLOR_GROUPS) {
      area.AddToGroup(colorGroup);
    }
  }

  // The two of them divide the platform between them rather than overlapping: a face is killed by
  // any area it enters whose colour is not its own, so a coat laid over the whole platform would
  // have the body underneath judging the same landing a second time.
  private void _applyColorAreas() {
    if (!_isWired) {
      return;
    }

    var coat = _isCoated() ? Mathf.Min(_inkPoolDepth(), Size.Y * INK_COAT_MAX_SHARE) : 0f;
    var top = -Size.Y / 2f;

    _inkAreaNode.Monitorable = coat > 0f;
    _resizeShape(_inkAreaShapeNode, new Vector2(Size.X, Mathf.Max(coat, 1f)));
    _inkAreaShapeNode.Position = new Vector2(0f, top + (coat / 2f));

    var body = Size.Y - coat;
    _resizeShape(_colorAreaShapeNode, new Vector2(Size.X, body));
    _colorAreaShapeNode.Position = new Vector2(0f, top + coat + (body / 2f));
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

  protected static void _resizeShape(CollisionShape2D collisionShape, Vector2 size) {
    if (collisionShape.Shape is RectangleShape2D rectangle) {
      rectangle.Size = size;
    }
  }
}
