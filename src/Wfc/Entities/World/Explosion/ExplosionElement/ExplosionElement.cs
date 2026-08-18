namespace Wfc.Entities.World.Explosion;

using System;
using Godot;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class ExplosionElement : RigidBody2D {
  private bool _shouldDetonate = false;
  private float _impulse = 0.0f;
  private float _peakFallSpeed = 0.0f;
  private bool _hasStruck = false;

  public override void _Ready() { }

  public void SetupSprite(Texture2D texture, int vFrames, int hFrames, int currentFrame) {
    var sprite = GetNode<Sprite2D>("Sprite2D");
    sprite.Texture = texture;
    sprite.Vframes = vFrames;
    sprite.Hframes = hFrames;
    sprite.Frame = currentFrame;
  }

  public void SetColliderShape(RectangleShape2D shape) {
    var collisionShape = GetCollider();
    collisionShape.Shape = shape;
  }

  public CollisionShape2D GetCollider() {
    return GetNode<CollisionShape2D>("CollisionShape2D");
  }

  public void Detonate(float _impulse) {
    this._impulse = _impulse;
    _shouldDetonate = true;
  }

  // Claims the one impact this shard is allowed to announce. Whatever it lands on hears about the
  // overlap only after the solver has stopped the shard dead, so the fastest it has ever fallen
  // stands in for the speed it arrived at: a shard the blast has not launched yet has never
  // fallen, and cannot claim an impact on whatever it was born inside. One claim per shard,
  // because something that dips away under a resting shard drops it onto itself again.
  public bool TryStrike(float minFallSpeed) {
    if (_hasStruck || _peakFallSpeed < minFallSpeed) {
      return false;
    }
    _hasStruck = true;
    return true;
  }

  public override void _IntegrateForces(PhysicsDirectBodyState2D state) {
    _peakFallSpeed = Math.Max(_peakFallSpeed, state.LinearVelocity.Y);
    if (_shouldDetonate) {
      ApplyCentralImpulse(new Vector2((float)GD.RandRange(-_impulse, _impulse), -_impulse));
      _shouldDetonate = false;
    }
  }
}
