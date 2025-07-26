namespace Wfc.Entities.World.Enemies;

using System;
using Godot;
using Wfc.Entities.World;
using Wfc.Entities.World.Player;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;

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

  private void _on_ColorArea_body_entered(Node body) {
    QueueFree();
  }

  private void _onColorAreaBodyShapeEntered(Rid bodyRid, Node body, uint bodyShapeIndex, int localShapeIndex) {
    if (body != Global.Instance().Player) {
      return;
    }
    if (body is Player player) {
      player.OnFastAreaCollidingWithPlayerShape(bodyShapeIndex, _colorAreaNode, EntityType.Bullet);
    }
  }
}
