namespace Wfc.Entities.World.Paint;

using Godot;
using Wfc.Autoload;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;
using Wfc.Utils.Layers;
using EventHandler = Wfc.Core.Event.EventHandler;

// A gobbet of paint in the air. It coats the first thing it runs into, and the cube that meets it
// on the way lives or dies by the face it has toward it, like everything else here that is a
// colour.
//
// It finds what it hits by asking where it is about to be rather than by being a body that is
// pushed there: the paint has to be laid on the surface it struck and parented to it, so what is
// wanted is the surface and the point, which a collision that has already been resolved no longer
// says.
[ScenePath]
public partial class SplashShot : Node2D {
  #region Constants
  private const float RADIUS = 11f;

  // Paint is thrown, not fired: it drops as it goes, which is what tells the player it is paint and
  // not a bullet. Gentle, because the gun has to lead its aim by however much this is - drop enough
  // to be worth seeing, little enough that the barrel still looks pointed at what it is shooting.
  public const float GRAVITY = 220f;

  // A shot that meets nothing has left the room.
  private const float RANGE = 3000f;
  #endregion Constants

  [NodePath("ColorArea")]
  private Area2D _colorAreaNode = default!;

  private string _group = ColorUtils.PURPLE;
  private float _width = 180f;
  private float _life = 6f;
  private Vector2 _going;
  private Vector2 _from;
  private Color _color = Colors.White;
  private PhysicsRayQueryParameters2D? _rayQuery;
  private readonly System.Collections.Generic.List<PaintSplat> _laid = [];

  // Told before it is in the tree, so what it is told waits for _Ready to have the nodes to put
  // it on.
  public void Setup(string colorGroup, float width, float life) {
    _group = colorGroup;
    _width = width;
    _life = life;
  }

  public void Fire(Vector2 velocity) => _going = velocity;

  // Paint in the air is part of the run too, so a death takes it with it rather than letting it
  // land in a room that has just been put back.
  public override void _EnterTree() {
    base._EnterTree();
    EventHandler.Instance.Events.CheckpointLoaded += QueueFree;
  }

  public override void _ExitTree() {
    base._ExitTree();
    EventHandler.Instance.Events.CheckpointLoaded -= QueueFree;
  }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _from = GlobalPosition;
    _colorAreaNode.AddToGroup(_group);
    _colorAreaNode.BodyEntered += _onBodyEntered;
    _color = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(_group), SkinColorIntensity.Basic);
    QueueRedraw();
  }

  public override void _PhysicsProcess(double delta) {
    var step = (float)delta;
    _going += new Vector2(0f, GRAVITY * step);
    var next = GlobalPosition + (_going * step);

    // Asked along the whole step rather than at the end of it: at this speed a shot covers more
    // than its own width in a tick, so a test at the new position alone goes through thin floors.
    _rayQuery ??= new PhysicsRayQueryParameters2D {
      CollisionMask = PhysicsLayers.Platform.Mask,
      CollideWithAreas = false,
      CollideWithBodies = true,
    };
    _rayQuery.From = GlobalPosition;
    _rayQuery.To = next;
    using var hit = GetWorld2D().DirectSpaceState.IntersectRay(_rayQuery);
    if (hit.Count > 0) {
      _land(hit["position"].AsVector2(), hit["collider"].As<Node>());
      return;
    }

    GlobalPosition = next;
    if ((GlobalPosition - _from).LengthSquared() > RANGE * RANGE) {
      QueueFree();
    }
  }

  public override void _Draw() => DrawCircle(Vector2.Zero, RADIUS, _color);

  // The paint goes onto what was hit rather than into the room, so a shot that lands on something
  // that moves is carried by it - the same way the paint a dropped bucket leaves is.
  private void _land(Vector2 where, Node? surface) {
    PaintSpread.Lay(this, where, surface, _group, _width, _life, _laid);
    EventHandler.Instance.EmitPaintSplashed(where);
    QueueFree();
  }

  // The cube's own colour question, asked at the point the paint reached it rather than against
  // whichever of its shapes the contact was reported on - a shape index says nothing about how
  // near a corner the paint struck.
  private void _onBodyEntered(Node2D body) {
    if (body != Global.Instance().Player || body is not Player.Player player || player.IsDying()) {
      return;
    }
    if (!player.AcceptsColorOfAt(_colorAreaNode.GlobalPosition, _colorAreaNode)) {
      EventHandler.Instance.EmitPlayerDying(_colorAreaNode, player.GlobalPosition, EntityType.Platform);
    }
    // Gone either way. A gobbet of paint that meets the cube has met something: on a face that
    // takes it, it is a harmless splash, and it has still stopped. Left flying it sails on through
    // the cube and lands on the floor beyond, which reads as the cube not being there at all.
    QueueFree();
  }
}
