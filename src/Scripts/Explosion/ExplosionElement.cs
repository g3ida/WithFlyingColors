using Godot;
using System;

public partial class ExplosionElement : RigidBody2D {
    private bool _shouldDetonate = false;
    private float _impulse = 0.0f;

    public override void _Ready() { }

    public void SetupSprite(Texture2D texture, int vFrames, int hFrames, int currentFrame) {
        var sprite = GetNode<Sprite2D>("Sprite2D");
        sprite.Texture = texture;
        sprite.Vframes = vFrames;
        sprite.Hframes = hFrames;
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

    public override void _IntegrateForces(PhysicsDirectBodyState2D state) {
        if (_shouldDetonate) {
            ApplyCentralImpulse(new Vector2((float)GD.RandRange(-_impulse, _impulse), -_impulse));
            _shouldDetonate = false;
        }
    }
}
