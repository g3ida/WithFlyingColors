using System;
using Godot;
using Wfc.Skin;

public partial class BlockSprite : Node2D {
  [Export]
  public string color_group {
    get => colorGroup;
    set => SetGroup(value);
  }
  private string colorGroup = "blue";

  public override void _Ready() { }

  public void SetGroup(string group) {
    colorGroup = group;
    Color baseColor = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(colorGroup),
      SkinColorIntensity.Basic
    );
    Color lightColor = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(colorGroup),
      SkinColorIntensity.Light
    );
    Color darkColor = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(colorGroup),
      SkinColorIntensity.Dark
    );
    // Apply colors to the sprite layers
    GetNode<Sprite2D>("Layer1").Modulate = lightColor;
    GetNode<Sprite2D>("Layer2").Modulate = darkColor;
    GetNode<Sprite2D>("TopLayer").Modulate = baseColor;
  }

  public string GetGroup() {
    return colorGroup;
  }
}
