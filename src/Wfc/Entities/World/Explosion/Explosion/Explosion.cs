namespace Wfc.Entities.World.Explosion;

using System;
using System.Collections.Generic;
using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class Explosion : Node2D {
  private Texture2D playerTexture = Global.Instance().GetPlayerSprite();
  private const int BLOCKS_PER_SIDE = 6;
  private const int BLOCKS_IMPULSE = 400;
  private const int BLOCKS_GRAVITY_SCALE = 3;
  private const float DEBRIS_MAX_TIME = 1.5f;

  [Signal]
  public delegate void ObjectDetonatedEventHandler(Explosion self);

  private readonly bool _isRandomizeSeed = false;
  private ExplosionInfo _explosionInfo = new ExplosionInfo();

  public required CharacterBody2D player;

  public override void _Ready() {
    player = GetParent<CharacterBody2D>();
  }

  public void FireExplosion() {
    if (_explosionInfo.CanDetonate) {
      _explosionInfo.Detonate = true;
      CallDeferred(nameof(_explode));
    }
  }

  private ExplosionElement _instanceExplosionElement(int n) {
    var explosionElement = SceneHelpers.InstantiateNode<ExplosionElement>();
    explosionElement.Name = Name + "_block_" + n;
    var shape = new RectangleShape2D {
      Size = _explosionInfo.CollisionExtents,
    };
    // explosionElement.Mode = RigidBody2D.ModeEnum.Static;
    _setRigidBodyMode(explosionElement, isStatic: true);
    explosionElement.SetupSprite(playerTexture, _explosionInfo.VFrames, _explosionInfo.HFrames, n);
    explosionElement.SetColliderShape(shape);
    explosionElement.GetCollider().Disabled = true;
    explosionElement.Visible = false;
    return explosionElement;
  }

  private static void _setRigidBodyMode(RigidBody2D element, bool isStatic) {
    if (isStatic) {
      element.Freeze = true;
      element.LockRotation = true;
    }
    else {
      element.Freeze = false;
      element.LockRotation = false;
    }
  }

  public void Setup() {
    _explosionInfo = new ExplosionInfo();
    if (_isRandomizeSeed) {
      GD.Randomize();
    }
    _setDebrisTimer();
    _explosionInfo.VFrames = BLOCKS_PER_SIDE;
    _explosionInfo.HFrames = BLOCKS_PER_SIDE;
    _explosionInfo.Width = playerTexture.GetWidth();
    _explosionInfo.Height = playerTexture.GetHeight();
    _explosionInfo.Offset = new Vector2(_explosionInfo.Width * 0.5f, _explosionInfo.Height * 0.5f);
    _explosionInfo.CollisionExtents = new Vector2(
      _explosionInfo.Width * 0.5f / _explosionInfo.HFrames,
      _explosionInfo.Height * 0.5f / _explosionInfo.VFrames);

    int idx = 0;
    var elems = new Godot.Collections.Array<Node2D>();

    for (int x = 0; x < _explosionInfo.HFrames; x++) {
      for (int y = 0; y < _explosionInfo.VFrames; y++) {
        var explosionElement = _instanceExplosionElement(idx);
        elems.Add(explosionElement);
        explosionElement.Position = new Vector2(
            y * (_explosionInfo.Width / _explosionInfo.HFrames) - _explosionInfo.Offset.X + _explosionInfo.CollisionExtents.X + Position.Y,
            x * (_explosionInfo.Height / _explosionInfo.VFrames) - _explosionInfo.Offset.Y + _explosionInfo.CollisionExtents.Y + Position.Y);
        idx++;
      }
    }
    CallDeferred(nameof(_addChildren), elems);
  }

  private void _setDebrisTimer() {
    _explosionInfo.DebrisTimer.Connect(Timer.SignalName.Timeout, new Callable(this, nameof(_onDebrisTimerTimeout)), (uint)ConnectFlags.OneShot);
    _explosionInfo.DebrisTimer.OneShot = true;
    _explosionInfo.DebrisTimer.WaitTime = DEBRIS_MAX_TIME;
    _explosionInfo.DebrisTimer.Name = "debris_timer";
    AddChild(_explosionInfo.DebrisTimer, true);
  }

  private void _explode() {
    var container = _explosionInfo.BlocksContainer;
    for (int i = 0; i < container.GetChildCount(); i++) {
      var child = (ExplosionElement)container.GetChild(i);
      child.Detonate(BLOCKS_IMPULSE);
    }
  }

  public override void _PhysicsProcess(double delta) {
    if (_explosionInfo.CanDetonate && _explosionInfo.Detonate) {
      _detonate();
    }
  }

  private void _addChildren(Godot.Collections.Array<Node2D> elems) {
    foreach (var elem in elems) {
      _explosionInfo.BlocksContainer.AddChild(elem, true);
    }
    AddChild(_explosionInfo.BlocksContainer);
    _explosionInfo.BlocksContainer.Owner = this;
  }

  private void _detonate() {
    _explosionInfo.CanDetonate = false;
    _explosionInfo.HasDetonated = true;
    _explosionInfo.Detonate = false;
    var container = _explosionInfo.BlocksContainer;
    for (int i = 0; i < container.GetChildCount(); i++) {
      var child = (ExplosionElement)container.GetChild(i);
      child.GravityScale = BLOCKS_GRAVITY_SCALE;
      float childScale = (float)GD.RandRange(0.5, 1.5);
      child.Scale = new Vector2(childScale, childScale);
      child.Mass = childScale;
      child.CollisionLayer = GD.Randf() < 0.08f ? 0 : player.CollisionLayer;
      child.CollisionMask = GD.Randf() < 0.08f ? 0 : player.CollisionMask;
      child.ZIndex = GD.Randf() < 0.5f ? 0 : -1;
      _setRigidBodyMode(child, isStatic: false);

      child.GetCollider().Disabled = false;
      child.Visible = true;
    }
    _explosionInfo.DebrisTimer.Start();
  }

  private void _onDebrisTimerTimeout() {
    var container = _explosionInfo.BlocksContainer;
    for (int i = 0; i < container.GetChildCount(); i++) {
      var child = (RigidBody2D)container.GetChild(i);
      _setRigidBodyMode(child, isStatic: true);
    }
    EmitSignal(nameof(ObjectDetonated), this);
  }
}
