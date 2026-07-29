namespace Wfc.Entities.World.Enemies;

using System;
using Godot;
using Wfc.Autoload;
using Wfc.Entities.World;
using Wfc.Entities.World.Player;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

[ScenePath]
public partial class Bullet : Node2D, IBullet {
  #region Constants
  private const float SPEED = 10.0f * Constants.WORLD_TO_SCREEN;
  private const float MAX_DISTANCE = 5000.0f;
  private const float MAX_DISTANCE_SQUARED = MAX_DISTANCE * MAX_DISTANCE;
  #endregion Constants

  [NodePath("CharacterBody2D")]
  private CharacterBody2D _bodyNode = default!;
  [NodePath("CharacterBody2D/BulletSpr")]
  private Sprite2D _spriteNode = default!;
  [NodePath("CharacterBody2D/ColorArea")]
  private Area2D _colorAreaNode = default!;

  private float _gravity = 1.0f * Constants.WORLD_TO_SCREEN;
  private Vector2 _movement = new Vector2();
  private Vector2 _initialPosition = new Vector2();

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _initialPosition = GlobalPosition;
  }

  public void Shoot(Vector2 shootDirection) {
    _movement = shootDirection * SPEED;
  }

  public void SetColorGroup(string groupName) {
    _colorAreaNode.AddToGroup(groupName);
    Color color = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(groupName),
      SkinColorIntensity.Basic
    );
    _spriteNode.Modulate = color;
  }

  public override void _PhysicsProcess(double delta) {
    base._PhysicsProcess(delta);
    _movement.Y += (float)delta * _gravity;
    _bodyNode.Velocity = _movement;
    _bodyNode.MoveAndSlide();

    if ((GlobalPosition - _initialPosition).LengthSquared() > MAX_DISTANCE_SQUARED) {
      QueueFree();
    }
  }

  // The cube's own color query, at the point the bullet reached it, rather than whichever of the
  // cube's collision shapes the contact was reported against. A shape index says nothing about
  // how near a corner the bullet struck, so the corner seam every other collision partner is
  // judged against never reached bullets at all.
  private void _onColorAreaBodyEntered(Node body) {
    if (body == Global.Instance().Player && body is Player player && !player.IsDying()
        && !player.AcceptsColorOfAt(_colorAreaNode.GlobalPosition, _colorAreaNode)) {
      EventHandler.Instance.EmitPlayerDying(_colorAreaNode, player.GlobalPosition, EntityType.Bullet);
    }
    QueueFree();
  }
}
