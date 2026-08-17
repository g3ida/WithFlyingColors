namespace Wfc.Entities.World.BrickBreaker.Powerups;

using System;
using Godot;
using Wfc.Screens.Levels;
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
    var skinColor = GameSkin.ColorGroupToSkinColor(ColorGroup);
    BackgroundNode.Modulate = SkinManager.Instance.CurrentSkin.GetColor(skinColor, SkinColorIntensity.Light);
    SpriteNode.Modulate = SkinManager.Instance.CurrentSkin.GetColor(skinColor, SkinColorIntensity.ExtremelyDark);
    AreaNode.AddToGroup(ColorGroup);
  }


  public override void _Process(double delta) {
    Position += new Vector2(0, Speed * (float)delta);

    // Check collision with dead zone
  }

  private void _onArea2DBodyEntered(Node body) {
    var player = GameRepo.Instance.Player.Value;
    if (body != player || player.IsDying()) {
      return;
    }
    // A power-up wears the color of the brick it fell from, and the cube has to be showing that
    // color where it catches it. The tint was decorative until now, which reads as a rule in a
    // game whose only rule is that color decides what you may touch - so a wrong-colored one
    // falls straight through instead of killing, which would make the arena a gauntlet.
    if (!player.AcceptsColorOfAt(AreaNode.GlobalPosition, AreaNode)) {
      return;
    }
    AreaNode.SetDeferred(Area2D.PropertyName.Monitorable, false);
    AreaNode.SetDeferred(Area2D.PropertyName.Monitoring, false);
    EmitSignal(nameof(OnPlayerHit), this, OnHitScript);
    QueueFree();
  }

  private void _onArea2DAreaEntered(Area2D area) {
    if (area.IsInGroup("death_zone")) {
      QueueFree();
    }
  }
}
