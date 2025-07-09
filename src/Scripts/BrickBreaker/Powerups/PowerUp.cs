using System;
using Godot;

public partial class PowerUp : Node2D {
  [Export]
  public string color_group { get; set; }

  [Export]
  public Texture2D texture { get; set; }

  [Export]
  public PackedScene on_hit_script { get; set; }

  [Signal]
  public delegate void on_player_hitEventHandler(PowerUp emitter, PackedScene onHitScript);

  private Area2D AreaNode;
  private Node2D BackgroundNode;
  private Sprite2D SpriteNode;

  private const float Speed = 3.0f * Constants.WORLD_TO_SCREEN;

  public override void _Ready() {
    AreaNode = GetNode<Area2D>("Area2D");
    BackgroundNode = GetNode<Node2D>("Background");
    SpriteNode = GetNode<Sprite2D>("Spr");

    SpriteNode.Texture = texture;
    int colorIndex = ColorUtils.GetGroupColorIndex(color_group);
    Color color = ColorUtils.GetBasicColor(colorIndex);
    BackgroundNode.Modulate = color;
    AreaNode.AddToGroup(color_group);
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
        EmitSignal(nameof(on_player_hit), this, on_hit_script);
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
