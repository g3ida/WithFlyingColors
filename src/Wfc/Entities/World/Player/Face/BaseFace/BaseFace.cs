namespace Wfc.Entities.World.Player;

using System.Collections.Generic;
using Godot;
using Wfc.Core.Event;

public partial class BaseFace : Area2D {
  protected CollisionShape2D CollisionShapeNode { get; private set; } = null!;
  protected RectangleShape2D? ShapeRect { get; private set; }

  // The area's extent along the edge it lies on, as authored. The seam is laid out from it, so
  // it stays at the authored value however the areas are later resized.
  public float EdgeLength { get; private set; }

  // Contacts that covered too little of the cube to count when they arrived. The cube can walk
  // further onto one, so each is measured again every frame it lasts.
  private readonly List<Area2D> _grazes = [];

  public override void _EnterTree() {
    base._EnterTree();
    AreaExited += _onGrazedAreaExited;
  }

  public override void _ExitTree() {
    base._ExitTree();
    AreaExited -= _onGrazedAreaExited;
    _stopWatchingGrazes();
  }

  public override void _Ready() {
    SetPhysicsProcess(false);
    CollisionShapeNode = GetNode<CollisionShape2D>("CollisionShape2D");
    // One shape resource is shared by every instance of this scene, and Godot keeps it cached
    // for the next level as well. Resizing a seam without duplicating first would resize all
    // four faces at once and outlive the player it was resized for.
    CollisionShapeNode.Shape = (Shape2D)CollisionShapeNode.Shape.Duplicate();
    ShapeRect = CollisionShapeNode.Shape as RectangleShape2D;
    EdgeLength = ShapeRect?.Size.X ?? 0f;
  }

  // A face's groups are the colors it is allowed to touch: one for a flat face, two for a
  // corner that straddles the seam between them. Contact is safe as soon as any of them
  // matches, which is what lets a corner graze either of its own colors. Answering this
  // question from GetGroups()[0] alone - as BoxFace and LazerBeam each used to - makes a
  // corner lethal on one of the two sides it exists to accept.
  public bool AcceptsColorOf(Area2D area) {
    foreach (string group in GetGroups()) {
      if (area.IsInGroup(group)) {
        return true;
      }
    }
    return false;
  }

  public bool AcceptsColor(string colorGroup) => IsInGroup(colorGroup);

  // Whether a contact with a color this face does not wear should kill now. One that has barely
  // reached the cube is remembered rather than answered, and asked again every frame: walking a
  // coloured run to its end reaches past it, and dying for that means the run cannot be walked to
  // its own edge, not that the player stepped onto the wrong thing.
  protected bool IsColorContactLethal(Player player, Area2D area) {
    if (!player.IsGrazedBy(area)) {
      return true;
    }
    if (!_grazes.Contains(area)) {
      _grazes.Add(area);
    }
    SetPhysicsProcess(true);
    return false;
  }

  public override void _PhysicsProcess(double delta) {
    var player = GetParent<Player>();
    // A death takes the whole cube with it, so nothing being watched outlives it. Coming back
    // re-enables these areas, and whatever the cube wakes up inside announces itself again.
    if (player is null || player.IsDying()) {
      _stopWatchingGrazes();
      return;
    }
    for (var i = _grazes.Count - 1; i >= 0; i--) {
      var area = _grazes[i];
      // Rotating the cube can bring a color it wears round to the contact, which settles it.
      if (!IsInstanceValid(area) || !area.IsInsideTree() || AcceptsColorOf(area)) {
        _grazes.RemoveAt(i);
        continue;
      }
      if (!player.IsGrazedBy(area)) {
        _grazes.RemoveAt(i);
        GameEvents.Instance.OnPlayerDying(area, GlobalPosition, EntityType.Platform);
        return;
      }
    }
    if (_grazes.Count == 0) {
      SetPhysicsProcess(false);
    }
  }

  private void _onGrazedAreaExited(Area2D area) {
    if (_grazes.Remove(area) && _grazes.Count == 0) {
      SetPhysicsProcess(false);
    }
  }

  private void _stopWatchingGrazes() {
    _grazes.Clear();
    SetPhysicsProcess(false);
  }

  // Along the edge only. Thickness is authored and stays put: it sets how deep a contact has to
  // be before it registers, which has nothing to do with how forgiving a corner is.
  public void SetEdgeLength(float length) {
    if (ShapeRect is not null) {
      ShapeRect.Size = new Vector2(length, ShapeRect.Size.Y);
    }
  }
}
