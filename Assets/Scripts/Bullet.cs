using Godot;
using System;

public class Bullet : Node2D, IBullet
{
    private const float SPEED = 10.0f * Constants.WORLD_TO_SCREEN;
    private const float MAX_DISTANCE = 5000.0f;
    private const float MAX_DISTANCE_SQUARED = MAX_DISTANCE * MAX_DISTANCE;

    private KinematicBody2D bodyNode;
    private Sprite spriteNode;
    private Area2D colorAreaNode;

    private float gravity = 1.0f * Constants.WORLD_TO_SCREEN;
    private Vector2 movement = new Vector2();
    private Vector2 initialPosition = new Vector2();

    public override void _Ready()
    {
        bodyNode = GetNode<KinematicBody2D>("KinematicBody2D");
        spriteNode = GetNode<Sprite>("KinematicBody2D/BulletSpr");
        colorAreaNode = GetNode<Area2D>("KinematicBody2D/ColorArea");

        initialPosition = GlobalPosition;
    }

    public void Shoot(Vector2 shootDirection)
    {
        movement = shootDirection * SPEED;
    }

    public void SetColorGroup(string groupName)
    {
        colorAreaNode.AddToGroup(groupName);
        int colorIndex = ColorUtils.GetGroupColorIndex(groupName);
        spriteNode.Modulate = ColorUtils.GetBasicColor(colorIndex);
    }

    public override void _PhysicsProcess(float delta)
    {
        movement.y += delta * gravity;
        bodyNode.MoveAndSlide(movement);

        if ((GlobalPosition - initialPosition).LengthSquared() > MAX_DISTANCE_SQUARED)
        {
            QueueFree();
        }
    }

    private void _on_ColorArea_body_entered(Node body)
    {
        QueueFree();
    }

    private void _on_ColorArea_body_shape_entered(RID bodyRid, Node body, uint bodyShapeIndex, int localShapeIndex)
    {
        if (body != Global.Instance().Player)
        {
            return;
        }
        
        // Assuming the body has an appropriate method `OnFastAreaCollidingWithPlayerShape`
        if (body is Player player)
        {
            player.OnFastAreaCollidingWithPlayerShape(bodyShapeIndex, colorAreaNode, Constants.EntityType.BULLET);
        }
    }
}
