using System;
using Godot;
using Wfc.Skin;

public partial class BouncingBallSprite : Sprite2D {
  public void SetColor(string colorGroup) {
    Color color = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(colorGroup),
      SkinColorIntensity.Basic
    );
    this.Modulate = color;
  }

  public override void _Ready() {
    // Optional: Add any initialization code here.
  }
}
