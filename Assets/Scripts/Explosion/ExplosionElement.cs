using Godot;
using System;

public class ExplosionElement : RigidBody2D
{
    private bool _shouldDetonate = false;
    private float _impulse = 0.0f;

    public override void _Ready() {}

    public void SetupSprite(Texture texture, int vframes, int hframes, int currentFrame) {
        var sprite = GetNode<Sprite>("Sprite");
        sprite.Texture = texture;
        sprite.Vframes = vframes;
        sprite.Hframes = hframes;
        sprite.Frame = currentFrame;
    }

    public void SetColliderShape(RectangleShape2D shape) {
        var collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        collisionShape.Shape = shape;
    }

    public CollisionShape2D GetCollider() {
        return GetNode<CollisionShape2D>("CollisionShape2D");
    }

    public void Detonate(float _impulse) {
        this._impulse = _impulse;
        _shouldDetonate = true;
    }

    public override void _IntegrateForces(Physics2DDirectBodyState state) {
        if (_shouldDetonate) {
            ApplyCentralImpulse(new Vector2((float)GD.RandRange(-_impulse, _impulse), -_impulse));
            _shouldDetonate = false;
        }
    }
}
