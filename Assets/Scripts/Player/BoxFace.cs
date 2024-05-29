using Godot;
using System;

public class BoxFace : BaseFace
{
    public float edgeLength;

    public override void _Ready()
    {
        collisionShapeNode = GetNode<CollisionShape2D>("CollisionShape2D");
        edgeLength = (collisionShapeNode.Shape as RectangleShape2D).Extents.x;
    }

    public override void ScaleBy(float factor)
    {
        float scaleFactor = factor;
        Scale = new Vector2(scaleFactor, scaleFactor);

        Position = new Vector2(
            positionX + extents.y * (scaleFactor - 1.0f) * Math.Sign(Position.y) * Mathf.Sin(Rotation),
            positionY - extents.y * (scaleFactor - 1.0f) * Math.Sign(Position.y) * Mathf.Cos(Rotation)
        );
    }

    public void _on_bottomFace_area_entered(Area2D area)
    {
        var groups = GetGroups();
        //GD.Assert(groups.Count == 1);

        if (area.IsInGroup("fallzone"))
        {
            Event.Instance().EmitPlayerDiying(null, GlobalPosition, Constants.EntityType.FALLZONE);
            return;
        }

        if (!area.IsInGroup((string)groups[0]))
        {
            Event.Instance().EmitPlayerDiying(area, GlobalPosition, Constants.EntityType.PLATFORM);
        }
        // FIXME: correct this after c# migration
        else if (area.HasMethod("_on_Gem_area_entered"))
        //else if (area is Gem gem)
        {
            area.Call("_on_Gem_area_entered", this);
            //gem.OnGemAreaEntered(this);
        }
        else if (!Global.Instance().Player.IsStanding())
        {
            Event.Instance().EmitPlayerLanded(area, GlobalPosition);
        }
    }
}
