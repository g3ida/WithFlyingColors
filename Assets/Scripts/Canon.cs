using Godot;
using System;
using System.Collections.Generic;

public class Canon : Node2D
{
    [Export]
    public string followNodeName { get; set; }
    [Export]
    public PackedScene bullet_scene { get; set; }
    [Export]
    public NodePath objectToFollow { get; set; }
    [Export]
    public float cooldown { get; set; } = 1.5f;
    [Export]
    public string color_group { get; set; }

    private Node2D StandNode;
    private Node2D CanonNode;
    private Node2D CanonMuzzle;
    private AnimationPlayer CanonAnimation;
    private Node2D StandColorAreaNode;
    private Node2D CanonColorAreaNode;
    private AudioStreamPlayer2D ShootSound;
    private Timer CooldownTimerNode;

    private Node2D follow = null;
    private float angle = 0f;
    private bool canShoot = true;

    private const float ANGULAR_VELOCITY = 0.5f;
    private const float VIEW_LIMIT_1 = 179.0f * Mathf.Pi / 180.0f;
    private const float VIEW_LIMIT_2 = 1.0f * Mathf.Pi / 180.0f;
    private const float DISTANCE_LIMIT = 6.0f * Constants.WORLD_TO_SCREEN;
    private const float SHOOT_PRECISION = 2.0f * Mathf.Pi / 180.0f;

    public override void _Ready()
    {
        StandNode = GetNode<Node2D>("Stand");
        CanonNode = GetNode<Node2D>("Canon");
        CanonMuzzle = GetNode<Node2D>("Canon/Muzzle");
        CanonAnimation = GetNode<AnimationPlayer>("Canon/ShootAnimation");
        StandColorAreaNode = GetNode<Node2D>("Body/StandColorArea");
        CanonColorAreaNode = GetNode<Node2D>("Body/CanonColorArea");
        ShootSound = GetNode<AudioStreamPlayer2D>("ShoutSound");
        CooldownTimerNode = GetNode<Timer>("CooldownTimer");

        follow = GetNode<Node2D>(objectToFollow);
        AddToGroup(color_group);
        StandColorAreaNode.AddToGroup(color_group);
        CanonColorAreaNode.AddToGroup(color_group);
        UpdateColor();
        CooldownTimerNode.WaitTime = cooldown;
    }

    private void UpdateColor()
    {
        int colorIndex = ColorUtils.GetGroupColorIndex(color_group);
        Color color = ColorUtils.GetBasicColor(colorIndex);
        StandNode.Modulate = color;
        CanonNode.Modulate = color;
    }

    private Node2D SpawnBullet()
    {
        Node2D bullet = (Node2D)bullet_scene.Instance();
        bullet.GlobalPosition = CanonMuzzle.GlobalPosition;
        GetParent().AddChild(bullet);
        bullet.Owner = GetParent();
        if (bullet is IBullet bulletScript)
        {
            bulletScript.SetColorGroup(color_group);
        }
        return bullet;
    }

    private async void Shoot(Vector2 direction)
    {
        canShoot = false;
        CanonAnimation.Play("Shoot");
        await ToSignal(CanonAnimation, "animation_finished");

        var bullet = SpawnBullet();
        if (bullet is IBullet bulletScript)
        {
            bulletScript.Shoot(direction);
        }
        ShootSound.Play();
        CooldownTimerNode.Start();
        await ToSignal(CooldownTimerNode, "timeout");
        canShoot = true;
    }

    private float SignOf(float x)
    {
        return x < 0 ? -1.0f : 1.0f;
    }

    private bool CanFollow(float targetAngle, float distanceSquared)
    {
        return !(targetAngle > VIEW_LIMIT_1 || targetAngle < VIEW_LIMIT_2) && distanceSquared < DISTANCE_LIMIT * DISTANCE_LIMIT;
    }

    public override void _PhysicsProcess(float delta)
    {
        Vector2 direction = follow.GlobalPosition - CanonMuzzle.GlobalPosition;
        angle = CanonNode.Rotation + Mathf.Pi / 2.0f;
        float targetAngle = direction.Angle();
        float rotationAmount = targetAngle - angle;

        if (CanFollow(targetAngle, direction.LengthSquared()))
        {
            float amount = ANGULAR_VELOCITY * delta;
            if (Mathf.Abs(rotationAmount) < Mathf.Abs(amount))
            {
                amount = rotationAmount;
            }
            CanonNode.Rotate(rotationAmount * delta);
        }

        if (Mathf.Abs(targetAngle - angle) < SHOOT_PRECISION && canShoot)
        {
            Shoot(direction.Normalized());
        }
    }
}
