namespace Wfc.Entities.World.ToyBricks;

using Chickensoft.Sync.Primitives;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Entities.World.Platforms;
using Wfc.Screens.Levels;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;

// A platform built out of interlocking toy bricks, whatever shape the author paints it. The shape
// is a tilemap and nothing else: paint a cell to lay a brick, and the colour you paint it with is
// the colour that brick wears - so one platform can be as many colours as it has bricks.
//
// The tilemap is what it is authored as rather than what it is made of. The body the cube stands
// on, the areas its faces are judged against and the wall the shader draws are all built off the
// painted cells, which is what lets a platform of a few hundred bricks be a handful of collision
// boxes and one quad.
//
// A colour is only ever as safe as it looks: a stretch of surface at one height has to be a single
// colour, because the cube's face is wider than a cell and dies on touching a colour it does not
// accept. _GetConfigurationWarnings holds the author to it.
//
// It is an animatable body rather than a static one, which is what carries a player standing on a
// platform along with it instead of leaving them in the air where it set off from. A platform that
// never moves is the same body holding still.
[Tool]
[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class ToyBrickPlatform : AnimatableBody2D {
  private AutoChannel.Binding? _landedBinding;

  #region Constants
  // What the colour areas are on: the same layer and mask a flat platform's is, so a brick surface
  // is seen by exactly what a flat one is seen by.
  private const uint COLOR_AREA_LAYER = 4;
  private const uint COLOR_AREA_MASK = 16;

  private static readonly StringName SizeParam = "u_size";
  private static readonly StringName CellParam = "u_cell";
  private static readonly StringName CellsParam = "u_cells";
  private static readonly StringName MapParam = "u_map";

  // In palette order, which is the order ColorUtils declares the groups in, then the neutral.
  private static readonly StringName[] ColorParams = [
    "u_blue", "u_pink", "u_yellow", "u_purple", "u_neutral"
  ];
  #endregion Constants

  #region Dependencies
  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public IGameLevel GameLevel => this.DependOn<IGameLevel>();
  #endregion Dependencies

  #region Exports
  [Export]
  public float SplashDarkness { get; set; } = 0.78f;
  #endregion Exports

  #region Fields
  private ToyBrickGrid _grid = ToyBrickGrid.Nothing(1.0f);
  // Everything built off the painted cells, so a rebuild can take back exactly what it laid down.
  private readonly List<Node> _built = [];
  private readonly HashSet<Area2D> _colorAreas = [];
  private float _animationTimer = 10.0f;
  private Vector2 _contactPosition = Vector2.Zero;
  private bool _isSubscribed;
  private bool _isWired;
  #endregion Fields

  #region Nodes
  [NodePath("Bricks")]
  private TileMapLayer _bricksNode = default!;
  [NodePath("Surface")]
  private ColorRect _surfaceNode = default!;
  #endregion Nodes

  // What the platform was painted as, for anything built on it that has to know how big it turned
  // out - a run measured against it, a box that has to cover it.
  protected ToyBrickGrid Grid => _grid;

  public override void _Ready() {
    base._Ready();
    _standUp();

    // Nothing to feed the shader until something lands; OnPlayerLanded turns this back on.
    SetProcess(false);
  }

  public override void _ExitTree() {
    base._ExitTree();
    _watchBricks(false);
    if (!_isSubscribed) {
      return;
    }
    _landedBinding?.Dispose();
    _landedBinding = null;
    _isSubscribed = false;
  }

  public override void _EnterTree() {
    base._EnterTree();
    if (Engine.IsEditorHint()) {
      // The editor stands a node up again in ways that do not all run _Ready - a scene reloaded
      // after a build, an instance dragged into another scene - and a platform that missed its one
      // chance to build shows the empty scene it was saved as. Deferred, so the tree has finished
      // settling before the platform starts hanging bodies off itself.
      CallDeferred(MethodName.Rebuild);
      return;
    }
    if (_isSubscribed) {
      return;
    }
    _landedBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.PlayerLandedOn m) => OnPlayerLanded(m.Area, m.Position));
    _isSubscribed = true;
  }

  // Read off the tilemap rather than off the grid the platform last built, so a platform that has
  // been instantiated but never stood up - which is how a whole level's worth of them are checked
  // at once - is asked about the bricks it was authored with rather than about nothing.
  public override string[] _GetConfigurationWarnings() {
    var warnings = new List<string>();
    var layer = GetNodeOrNull<TileMapLayer>("Bricks");
    if (layer is null) {
      warnings.Add("There is no Bricks tilemap under this platform, so there is nothing to build it out of.");
      return [.. warnings];
    }

    if (!Scale.IsEqualApprox(Vector2.One)) {
      warnings.Add("Scale stretches the bricks out of step with the ground. Leave Scale at (1, 1) and paint more cells instead.");
    }

    var grid = ToyBrickGrid.Read(layer);
    if (grid.IsEmpty) {
      warnings.Add("No bricks are painted on the Bricks tilemap, so the platform is nothing at all.");
      return [.. warnings];
    }
    warnings.AddRange(grid.SurfaceWarnings());

    return [.. warnings];
  }

  public void OnPlayerLanded(Node area, Vector2 position) {
    if (area is not Area2D landedOn || !_colorAreas.Contains(landedOn)) {
      return;
    }
    _animationTimer = 0.0f;
    // Kept in the platform's own frame rather than the world's, so a platform that is moving carries
    // the splash with it instead of leaving the paint behind where the cube touched down.
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
      PlatformSplash.Write(material, camera, ToGlobal(_contactPosition), _animationTimer, SplashDarkness);
    }

    if (_animationTimer > PlatformSplash.Duration(SplashDarkness)) {
      SetProcess(false);
    }
  }

  // Re-reads the bricks. The platform does this for itself whenever the layer is painted in the
  // editor; anything laying bricks while the game is running says so here. Idempotent, because it
  // is also how a platform the editor never called _Ready on gets built at all.
  public void Rebuild() => _standUp();

  // Everything that has to be true of a platform standing in a scene, done in whatever order the
  // editor happens to stand it up in.
  private void _standUp() {
    if (!_isWired) {
      if (GetNodeOrNull<TileMapLayer>("Bricks") is null) {
        return;
      }
      this.WireNodes();
      _isWired = true;
    }

    // The tilemap is the shape rather than the surface: it is painted on, and what the player sees
    // is drawn from the cells it holds. It stays visible while the platform is being authored,
    // because a layer with nothing on it is a layer nobody can paint.
    _bricksNode.Visible = Engine.IsEditorHint();
    _rebuild();

    if (Engine.IsEditorHint()) {
      _watchBricks(true);
    }
  }

  // Named rather than handed over as a delegate. A C# delegate becomes a custom callable holding a
  // GC handle, and rebuilding the assembly - which is what happens every time the game is run from
  // the editor - leaves the connection in place with its handle dead: every stroke of the tile
  // brush then prints "Can't get method on CallableCustom". A callable that names its method is
  // looked up again each time it is called, so it survives the reload.
  private void _watchBricks(bool watch) {
    if (_bricksNode is null) {
      return;
    }
    var watcher = new Callable(this, MethodName.Rebuild);
    var connected = _bricksNode.IsConnected(TileMapLayer.SignalName.Changed, watcher);
    if (watch && !connected) {
      _bricksNode.Connect(TileMapLayer.SignalName.Changed, watcher);
    }
    else if (!watch && connected) {
      _bricksNode.Disconnect(TileMapLayer.SignalName.Changed, watcher);
    }
  }

  // The one hook a platform built on this one gets: everything sized off the painted cells is sized
  // here, and the cells are painted rather than announced by a notification anything can hear.
  protected virtual void _rebuild() {
    if (!_isWired) {
      return;
    }

    _grid = ToyBrickGrid.Read(_bricksNode);
    _clear();
    _buildBody();
    _buildColorAreas();
    _applySurface();

    if (Engine.IsEditorHint()) {
      UpdateConfigurationWarnings();
    }
  }

  private void _clear() {
    foreach (var node in _built) {
      // Removed as well as freed: a queued free still leaves the node a child for the rest of the
      // frame, and in the editor a rebuild per painted cell stacks those up on top of each other.
      RemoveChild(node);
      node.QueueFree();
    }
    _built.Clear();
    _colorAreas.Clear();
  }

  private void _buildBody() {
    foreach (var box in _grid.SolidBoxes()) {
      var shape = _boxShape(box);
      AddChild(shape);
      _built.Add(shape);
    }
  }

  private void _buildColorAreas() {
    for (var slot = 0; slot < ToyBrickGrid.SLOT_COUNT; slot++) {
      var boxes = _grid.ColorBoxes(slot);
      if (boxes.Count == 0) {
        continue;
      }

      var area = new Area2D {
        CollisionLayer = COLOR_AREA_LAYER,
        CollisionMask = COLOR_AREA_MASK,
      };
      AddChild(area);
      _built.Add(area);
      _colorAreas.Add(area);

      var group = ToyBrickGrid.GroupOf(slot);
      if (group is null) {
        // Neutral bricks answer to every face rather than to none, the same way the ground does.
        foreach (var colorGroup in ColorUtils.COLOR_GROUPS) {
          area.AddToGroup(colorGroup);
        }
      }
      else {
        area.AddToGroup(group);
      }

      foreach (var box in boxes) {
        area.AddChild(_boxShape(box));
      }
    }
  }

  private static CollisionShape2D _boxShape(Rect2 box) => new CollisionShape2D {
    Shape = new RectangleShape2D { Size = box.Size },
    Position = box.Position + (box.Size / 2.0f),
  };

  private void _applySurface() {
    _surfaceNode.Visible = !_grid.IsEmpty;
    if (_grid.IsEmpty || _surfaceNode.Material is not ShaderMaterial material) {
      return;
    }

    // A cell wider all round than the bricks themselves: the studs on the top row stand above the
    // brick they are on, and the outermost bricks are cut and shaded against the empty cells past
    // them.
    var drawn = _grid.Bounds.Grow(_grid.CellSize);
    _surfaceNode.Position = drawn.Position;
    _surfaceNode.Size = drawn.Size;

    material.SetShaderParameter(SizeParam, drawn.Size);
    material.SetShaderParameter(CellParam, _grid.CellSize);
    material.SetShaderParameter(CellsParam, new Vector2(_grid.Columns + 2, _grid.Rows + 2));
    material.SetShaderParameter(MapParam, _grid.BuildMap());

    var skin = SkinManager.Instance.CurrentSkin;
    for (var slot = 0; slot < ToyBrickGrid.SLOT_COUNT; slot++) {
      var group = ToyBrickGrid.GroupOf(slot);
      var color = group is null
        ? new Color(1.0f, 1.0f, 1.0f)
        : skin.GetColor(GameSkin.ColorGroupToSkinColor(group), SkinColorIntensity.Basic);
      material.SetShaderParameter(ColorParams[slot], color);
    }
  }
}
