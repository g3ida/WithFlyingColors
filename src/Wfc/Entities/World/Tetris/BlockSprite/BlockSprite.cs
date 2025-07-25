namespace Wfc.Entities.Tetris;

using System;
using Godot;
using Wfc.Skin;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class BlockSprite : Node2D {
  [Export]
  public string ColorGroup {
    get => _colorGroup;
    set => SetGroup(value);
  }
  private string _colorGroup = "blue";

  public void SetGroup(string group) {
    _colorGroup = group;
    Color baseColor = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(_colorGroup),
      SkinColorIntensity.Basic
    );
    Color lightColor = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(_colorGroup),
      SkinColorIntensity.Light
    );
    Color darkColor = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(_colorGroup),
      SkinColorIntensity.Dark
    );
    // Apply colors to the sprite layers
    GetNode<Sprite2D>("Layer1").Modulate = lightColor;
    GetNode<Sprite2D>("Layer2").Modulate = darkColor;
    GetNode<Sprite2D>("TopLayer").Modulate = baseColor;
  }

  public string GetGroup() {
    return _colorGroup;
  }
}
