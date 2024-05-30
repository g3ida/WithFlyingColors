using Godot;
using System;

public class BaseFace : Area2D
{
    protected CollisionShape2D collisionShapeNode;
    protected Vector2 extents;

    protected float positionX;
    protected float positionY;

    public override void _Ready()
    {
        collisionShapeNode = GetNode<CollisionShape2D>("CollisionShape2D");
        extents = (collisionShapeNode.Shape as RectangleShape2D).Extents;
        positionX = Position.x;
        positionY = Position.y;
    }

    public bool CheckGroup(Area2D area, string[] groups)
    {
        // FIXME: remove reddundant code
        if (area is Gem gem) {
            foreach (string group in groups) {
                if (gem.IsInGroup(group)) {
                    return true;
                }
            }
        } else {
            foreach (string group in groups) {
                if (area.IsInGroup(group)) {
                    return true;
                }
            }
        }
        return false;
    }

    public virtual void ScaleBy(float factor)
    {
        float scaleFactor = factor;
        Scale = new Vector2(scaleFactor, scaleFactor);
        Position = new Vector2(
            positionX - extents.x * (scaleFactor - 1.0f) * Helpers.SignOf(Position.x),
            positionY - extents.y * (scaleFactor - 1.0f) * Helpers.SignOf(Position.y)
        );
    }
}
