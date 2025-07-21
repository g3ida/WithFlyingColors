namespace Wfc.Entities.World.BrickBreaker.Powerups;

using System;
using Godot;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;

public partial class PowerUp : Node2D {

  #region Export
  [Export]
  public string ColorGroup { get; set; } = "blue";

  [Export]
  public Texture2D Texture { get; set; } = default!;

  [Export]
  public PackedScene OnHitScript { get; set; } = default!;
  #endregion Export

  #region Events
  [Signal]
  public delegate void OnPlayerHitEventHandler(PowerUp emitter, PackedScene onHitScript);
  #endregion Events

  #region Nodes
  [NodePath("Area2D")]
  private Area2D AreaNode = default!;
  [NodePath("Background")]
  private Node2D BackgroundNode = default!;
  [NodePath("Spr")]
  private Sprite2D SpriteNode = default!;
  #endregion Nodes

  private const float Speed = 3.0f * Constants.WORLD_TO_SCREEN;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    SpriteNode.Texture = Texture;
    Color color = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(ColorGroup),
      SkinColorIntensity.Basic
    );
    BackgroundNode.Modulate = color;
    AreaNode.AddToGroup(ColorGroup);
  }


  public override void _Process(double delta) {
    Position += new Vector2(0, Speed * (float)delta);

    // Check collision with dead zone
  }

  private void _on_Area2D_body_entered(Node body) {
    if (body == Global.Instance().Player) {
      if (!Global.Instance().Player.IsDying()) {
        AreaNode.SetDeferred(Area2D.PropertyName.Monitorable, false);
        AreaNode.SetDeferred(Area2D.PropertyName.Monitoring, false);
        EmitSignal(nameof(OnPlayerHit), this, OnHitScript);
        QueueFree();
      }
    }
  }

  private void _on_Area2D_area_entered(Area2D area) {
    if (area.IsInGroup("death_zone")) {
      QueueFree();
    }
  }
}
