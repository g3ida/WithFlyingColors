using Godot;
using System;

public partial class BaseFace : Area2D {
    protected CollisionShape2D collisionShapeNode;
    protected Vector2 extents;

    protected float positionX;
    protected float positionY;

    public override void _Ready() {
        collisionShapeNode = GetNode<CollisionShape2D>("CollisionShape2D");
        extents = (collisionShapeNode.Shape as RectangleShape2D).Size;
        positionX = Position.X;
        positionY = Position.Y;
    }

    public bool CheckGroup(Area2D area, string[] groups) {
        // FIXME: remove redundant code
        if (area is Gem gem) {
            foreach (string group in groups) {
                if (gem.IsInGroup(group)) {
                    return true;
                }
            }
        }
        else {
            foreach (string group in groups) {
                if (area.IsInGroup(group)) {
                    return true;
                }
            }
        }
        return false;
    }

    public virtual void ScaleBy(float factor) {
        float scaleFactor = factor;
        Scale = new Vector2(scaleFactor, scaleFactor);
        Position = new Vector2(
            positionX - extents.X * (scaleFactor - 1.0f) * Math.Sign(Position.X) * 0.5f,
            positionY - extents.Y * (scaleFactor - 1.0f) * Math.Sign(Position.Y) * 0.5f
        );
    }
}
