namespace Wfc.Entities.World.BrickBreaker;

using System;
using Godot;
using Wfc.Core.Event;
using Wfc.Entities.World.Player;
using Wfc.Skin;
using EventHandler = Wfc.Core.Event.EventHandler;

[Tool]
public partial class Brick : Node2D {
  [Signal]
  public delegate void brickBrokenEventHandler();

  [Export] public string ColorGroup { get; set; } = "blue";

  private Area2D _areaNode = null!;
  private Sprite2D _spriteNode = null!;
  private CollisionShape2D _collisionShapeNode = null!;
  private bool _isBroken;

  public override void _Ready() {
    _areaNode = GetNode<Area2D>("Area2D");
    _spriteNode = GetNode<Sprite2D>("BrickSpr");
    _collisionShapeNode = GetNode<CollisionShape2D>("CharacterBody2D/CollisionShape2D");

    _areaNode.AddToGroup(ColorGroup);
    SetColor();
  }

  private void SetColor() {
    Color color = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(ColorGroup),
      SkinColorIntensity.Basic
    );
    _spriteNode.Modulate = color;
  }

  // A brick can be told it was hit several times in the same frame: QueueFree is deferred
  // so this area keeps monitoring for the rest of the tick, and its mask admits the three
  // balls as well as all eight of the player's face and corner areas. Every one of those
  // used to decrement the room's brick counter, which steps straight over its exact-zero
  // test and leaves the player sealed in an empty arena that can never report itself
  // cleared. Breaking is a one-way door, so latch it and stop listening immediately.
  private void _on_Area2D_area_entered(Area2D area) {
    if (_isBroken) {
      return;
    }

    // The player only breaks a brick it can safely touch. A face of the wrong color is
    // already fatal - BoxFace raises the death from its own handler on the same contact -
    // so the brick has to survive it, or the player would smash the very brick that
    // killed them and the arena would clear itself as they died.
    //
    // Balls carry a color group too, but theirs is an echo of the last surface they
    // bounced off rather than an identity, so they go on breaking anything.
    if (area is BaseFace face && !face.IsInGroup(ColorGroup)) {
      return;
    }
    _isBroken = true;
    _areaNode.SetDeferred(Area2D.PropertyName.Monitoring, false);
    _areaNode.SetDeferred(Area2D.PropertyName.Monitorable, false);

    Vector2 extents = (_collisionShapeNode.Shape as RectangleShape2D)?.Size ?? Vector2.Zero;
    EmitSignal(Brick.SignalName.brickBroken);
    EventHandler.Instance.EmitBrickBroken(ColorGroup, Position + GetParent<Node2D>().Position + extents);
    QueueFree();
  }
}
