namespace Wfc.Entities.World.Player;

using Godot;

public partial class BaseFace : Area2D {
  protected CollisionShape2D CollisionShapeNode { get; private set; } = null!;
  protected RectangleShape2D? ShapeRect { get; private set; }

  // The area's extent along the edge it lies on, as authored. The seam is laid out from it, so
  // it stays at the authored value however the areas are later resized.
  public float EdgeLength { get; private set; }

  public override void _Ready() {
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

  // Along the edge only. Thickness is authored and stays put: it sets how deep a contact has to
  // be before it registers, which has nothing to do with how forgiving a corner is.
  public void SetEdgeLength(float length) {
    if (ShapeRect is not null) {
      ShapeRect.Size = new Vector2(length, ShapeRect.Size.Y);
    }
  }
}
