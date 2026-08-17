namespace Wfc.Entities.World.BreakerBricks;

using System.Collections.Generic;
using Godot;
using Wfc.Core.Event;
using Wfc.Entities.World.Enemies;
using Wfc.Screens.Levels;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;
using EventHandler = Wfc.Core.Event.EventHandler;

// A wall of brick-breaker bricks, whatever shape the author paints it. The shape is a tilemap and
// nothing else: paint a tile to lay a brick, and the colour it was painted with is the colour that
// brick wears - so one platform can be as many colours as it has bricks.
//
// What makes it worth being its own platform is that a canon's shot takes a brick out of it. A wall
// across the way is not a wall the player climbs but one they get something else to open for them,
// and a floor of these is a floor that can be shot out from under whoever is standing on it. So the
// brick is the unit all the way down - its own box on the body, its own box in its colour's area -
// where a toy-brick platform merges its cells into as few boxes as cover them.
//
// A hole is not permanent: every death lays the wall again exactly as the level authored it. A wall
// that kept its holes across deaths only ever gets thinner - the canon shooting at the player goes
// on chewing it whether they are winning or losing - and a bridge that has been shot away for good
// is a level that cannot be finished from its own checkpoint.
//
// It is an animatable body rather than a static one, which is what carries a player standing on it
// along with it instead of leaving them in the air where it set off from. A wall that never moves is
// the same body holding still.
[Tool]
[ScenePath]
public partial class BreakerBrickPlatform : AnimatableBody2D, IShootable {
  #region Constants
  // What the colour areas are on: the same layer and mask a flat platform's is, so a brick surface
  // is seen by exactly what a flat one is seen by.
  private const uint COLOR_AREA_LAYER = 4;
  private const uint COLOR_AREA_MASK = 16;

  // How far past the point a shot reached the wall may still be taken to have been hit, as a share
  // of a brick's height. A bullet stops against the face of the wall rather than inside it, so the
  // brick it broke is never quite the brick it is standing on.
  private const float SHOT_REACH = 1.0f;
  #endregion Constants

  #region Fields
  private BreakerBrickGrid _grid = BreakerBrickGrid.Nothing(1.0f);
  private readonly List<Node> _built = [];
  private readonly List<BuiltBrick> _bricks = [];
  // Which bricks are gone, by their place in the grid's list - which is where they were painted, so
  // a rebuild names the same bricks the shots did.
  private readonly HashSet<int> _broken = [];
  private bool _isSubscribed;
  private bool _isWired;
  #endregion Fields

  #region Nodes
  [NodePath("Bricks")]
  private TileMapLayer _bricksNode = default!;
  // The brick every brick in the wall is a copy of, so the art the platform is made of is part of
  // the scene rather than a path written into the code.
  [NodePath("Brick")]
  private Sprite2D _brickNode = default!;
  #endregion Nodes

  private sealed record BuiltBrick(Sprite2D Sprite, CollisionShape2D Body, CollisionShape2D Area);

  // What the wall was painted as, for anything built on it that has to know how big it turned out -
  // a box that has to cover it, a run measured against it.
  protected BreakerBrickGrid Grid => _grid;

  public override void _Ready() {
    base._Ready();
    _standUp();
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
    EventHandler.Instance.Events.CheckpointLoaded += _onRespawn;
    _isSubscribed = true;
  }

  public override void _ExitTree() {
    base._ExitTree();
    _watchBricks(false);
    if (!_isSubscribed) {
      return;
    }
    EventHandler.Instance.Events.CheckpointLoaded -= _onRespawn;
    _isSubscribed = false;
  }

  // Read off the tilemap rather than off the grid the platform last built, so a platform that has
  // been instantiated but never stood up - which is how a whole level's worth of them are checked at
  // once - is asked about the bricks it was authored with rather than about nothing.
  public override string[] _GetConfigurationWarnings() {
    var warnings = new List<string>();
    var layer = GetNodeOrNull<TileMapLayer>("Bricks");
    if (layer is null) {
      warnings.Add("There is no Bricks tilemap under this platform, so there is nothing to build it out of.");
      return [.. warnings];
    }

    if (!Scale.IsEqualApprox(Vector2.One)) {
      warnings.Add("Scale stretches the bricks out of step with the ground. Leave Scale at (1, 1) and paint more bricks instead.");
    }

    var grid = BreakerBrickGrid.Read(layer);
    if (grid.IsEmpty) {
      warnings.Add("No bricks are painted on the Bricks tilemap, so the platform is nothing at all.");
      return [.. warnings];
    }
    warnings.AddRange(grid.SurfaceWarnings());

    return [.. warnings];
  }

  // Re-reads the bricks. The platform does this for itself whenever the layer is painted in the
  // editor; anything laying bricks while the game is running says so here. Idempotent, because it is
  // also how a platform the editor never called _Ready on gets built at all.
  public void Rebuild() => _standUp();

  #region Breaking
  // A shot landed. The brick it landed on is the one that goes, and a shot that stopped against the
  // face of the wall counts as having landed on the brick behind it.
  //
  // Only bricks still standing are candidates. A shot that flew in through a hole an earlier one
  // made comes to rest at the bottom of it, inside the gap where a brick used to be: read against
  // the shape the wall was painted as, every such shot lands on a brick that is already gone and
  // breaks nothing, so a wall stops taking damage exactly where the player is shooting it.
  public void OnShot(Vector2 globalPosition) {
    var point = ToLocal(globalPosition);
    var index = _grid.IndexAt(point, _isStanding);
    if (index == BreakerBrickGrid.EMPTY) {
      index = _grid.NearestTo(point, SHOT_REACH * _grid.CellSize, _isStanding);
    }
    if (index == BreakerBrickGrid.EMPTY) {
      return;
    }
    Break(index);
  }

  private bool _isStanding(int index) => !_broken.Contains(index);

  public void Break(int index) {
    if (index < 0 || index >= _bricks.Count || !_broken.Add(index)) {
      return;
    }
    _show(index, false);

    var brick = _grid.Bricks[index];
    var group = BreakerBrickGrid.GroupOf(brick.Slot);
    var box = _grid.BoxOf(brick);
    GameEvents.Instance.OnBrickBroken(group ?? string.Empty, ToGlobal(box.Position + (box.Size / 2.0f)));
  }

  public bool IsBroken(int index) => _broken.Contains(index);

  // Freeing a brick would mean building it again to put it back, and every death puts every brick
  // back. It is taken out of the wall instead: nothing to draw, nothing to stand on, nothing to be
  // judged against.
  private void _show(int index, bool visible) {
    var brick = _bricks[index];
    brick.Sprite.Visible = visible;
    // Deferred: a brick breaks from inside the shot's own collision callback, and a shape cannot be
    // taken out of the physics server while it is flushing.
    brick.Body.SetDeferred(CollisionShape2D.PropertyName.Disabled, !visible);
    brick.Area.SetDeferred(CollisionShape2D.PropertyName.Disabled, !visible);
  }

  // Lays every brick that has been shot out back where it was painted.
  public void Mend() {
    foreach (var index in _broken) {
      if (index < _bricks.Count) {
        _show(index, true);
      }
    }
    _broken.Clear();
  }

  private void _onRespawn() => Mend();
  #endregion Breaking

  #region Building
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
    // is a brick laid per painted tile. It stays visible while the platform is being authored,
    // because a layer with nothing on it is a layer nobody can paint.
    _bricksNode.Visible = Engine.IsEditorHint();
    _brickNode.Visible = false;
    _rebuild();

    if (Engine.IsEditorHint()) {
      _watchBricks(true);
    }
  }

  // Named rather than handed over as a delegate. A C# delegate becomes a custom callable holding a
  // GC handle, and rebuilding the assembly - which is what happens every time the game is run from
  // the editor - leaves the connection in place with its handle dead: every stroke of the tile brush
  // then prints "Can't get method on CallableCustom". A callable that names its method is looked up
  // again each time it is called, so it survives the reload.
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

  // The one hook a platform built on this one gets: everything sized off the painted bricks is sized
  // here, and the bricks are painted rather than announced by a notification anything can hear.
  protected virtual void _rebuild() {
    if (!_isWired) {
      return;
    }

    _grid = BreakerBrickGrid.Read(_bricksNode);
    _clear();
    _buildBricks();
    // A wall read again while the game is running keeps the holes shot in it: reading the tilemap
    // is not the player un-shooting anything.
    foreach (var index in _broken) {
      if (index < _bricks.Count) {
        _show(index, false);
      }
    }

    if (Engine.IsEditorHint()) {
      UpdateConfigurationWarnings();
    }
  }

  private void _clear() {
    foreach (var node in _built) {
      // Removed as well as freed: a queued free still leaves the node a child for the rest of the
      // frame, and in the editor a rebuild per painted tile stacks those up on top of each other.
      RemoveChild(node);
      node.QueueFree();
    }
    _built.Clear();
    _bricks.Clear();
  }

  private void _buildBricks() {
    var areas = new Dictionary<int, Area2D>();
    var skin = SkinManager.Instance.CurrentSkin;

    foreach (var brick in _grid.Bricks) {
      var box = _grid.BoxOf(brick);
      var group = BreakerBrickGrid.GroupOf(brick.Slot);

      var sprite = (Sprite2D)_brickNode.Duplicate();
      sprite.Visible = true;
      sprite.Position = box.Position;
      sprite.Scale = box.Size / sprite.Texture.GetSize();
      sprite.Modulate = group is null
        ? new Color(1.0f, 1.0f, 1.0f)
        : skin.GetColor(GameSkin.ColorGroupToSkinColor(group), SkinColorIntensity.Basic);
      AddChild(sprite);
      _built.Add(sprite);

      var body = _boxShape(box);
      AddChild(body);
      _built.Add(body);

      if (!areas.TryGetValue(brick.Slot, out var area)) {
        area = _colorArea(group);
        areas[brick.Slot] = area;
      }
      var judged = _boxShape(box);
      area.AddChild(judged);

      _bricks.Add(new BuiltBrick(sprite, body, judged));
    }
  }

  private Area2D _colorArea(string? group) {
    var area = new Area2D {
      CollisionLayer = COLOR_AREA_LAYER,
      CollisionMask = COLOR_AREA_MASK,
    };
    AddChild(area);
    _built.Add(area);

    if (group is null) {
      // Neutral bricks answer to every face rather than to none, the same way the ground does.
      foreach (var colorGroup in ColorUtils.COLOR_GROUPS) {
        area.AddToGroup(colorGroup);
      }
    }
    else {
      area.AddToGroup(group);
    }
    return area;
  }

  private static CollisionShape2D _boxShape(Rect2 box) => new CollisionShape2D {
    Shape = new RectangleShape2D { Size = box.Size },
    Position = box.Position + (box.Size / 2.0f),
  };
  #endregion Building
}
