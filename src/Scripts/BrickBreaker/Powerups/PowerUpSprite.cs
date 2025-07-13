using Godot;
using Wfc.Skin;
using Wfc.Utils.Colors;

public partial class PowerUpSprite : Sprite2D {
  public override void _Ready() {
    var colorGroup = GetParent().Get("color_group").ToString();
    Color color = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(colorGroup),
      SkinColorIntensity.Basic
    );
    Modulate = ColorUtils.DarkenRGB(color, 0.4f);
  }
}
