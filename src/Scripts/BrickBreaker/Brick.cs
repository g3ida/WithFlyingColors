using System;
using Godot;
using Wfc.Core.Event;
using Wfc.Skin;
using EventHandler = Wfc.Core.Event.EventHandler;

[Tool]
public partial class Brick : Node2D {
  [Signal]
  public delegate void brick_brokenEventHandler();

  [Export] public string color_group { get; set; } = "blue";

  private Area2D _areaNode = null!;
  private Sprite2D _spriteNode = null!;
  private CollisionShape2D _collisionShapeNode = null!;

  public override void _Ready() {
    _areaNode = GetNode<Area2D>("Area2D");
    _spriteNode = GetNode<Sprite2D>("BrickSpr");
    _collisionShapeNode = GetNode<CollisionShape2D>("CharacterBody2D/CollisionShape2D");

    _areaNode.AddToGroup(color_group);
    SetColor();
  }

  private void SetColor() {
    Color color = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(color_group),
      SkinColorIntensity.Basic
    );
    _spriteNode.Modulate = color;
  }

  private void _on_Area2D_area_entered(Area2D area) {
    Vector2 extents = (_collisionShapeNode.Shape as RectangleShape2D).Size;
    EmitSignal(nameof(brick_broken));
    EventHandler.Instance.EmitBrickBroken(color_group, Position + GetParent<Node2D>().Position + extents);
    QueueFree();
  }
}
