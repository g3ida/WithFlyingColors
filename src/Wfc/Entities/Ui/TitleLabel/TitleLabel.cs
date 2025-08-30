namespace Wfc.Entities.Ui;

using System;
using Godot;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[Tool]
[ScenePath]
public partial class TitleLabel : Label {
  #region Exports
  [Export]
  public string content { get; set; } = "";
  [Export]
  public SkinColor UnderlineSkinColor { get; set; } = SkinColor.LeftFace;

  #endregion Exports

  #region Nodes
  [NodePath("Underline")]
  private Control _underlineNode = default!;
  [NodePath("UnderlineShadow")]
  private Control _underlineShadowNode = default!;
  [NodePath("Shadow")]
  private Label _shadowNode = default!;
  #endregion Nodes

  #region Constants
  private SkinColorIntensity UNDERLINE_COLOR_INTENSITY = SkinColorIntensity.Light;
  private const SkinColorIntensity UNDERLINE_SHADOW_COLOR_INTENSITY = SkinColorIntensity.Dark;
  #endregion Constants

  public void UpdatePositionX(float value) {
    Position = new Vector2(value, Position.Y);
  }

  public float getEstimatedWidth() {
    return _underlineNode.Size.X * _underlineNode.Scale.X;
  }

  public override void _EnterTree() {
    this.WireNodes();
    Text = content;
    _shadowNode.Text = content;
    SetProcess(false);

    var skin = SkinManager.Instance.CurrentSkin;
    var underlineColor = skin.GetColor(UnderlineSkinColor, UNDERLINE_COLOR_INTENSITY);
    var underlineShadowColor = skin.GetColor(UnderlineSkinColor, UNDERLINE_SHADOW_COLOR_INTENSITY);

    var scale = GetMinimumSize().X / _underlineNode.Size.X;
    _underlineNode.Scale = new Vector2(scale, _underlineNode.Scale.Y);
    _underlineNode.Modulate = underlineColor;

    var shadowScale = GetMinimumSize().X / _underlineShadowNode.Size.X;
    _underlineShadowNode.Scale = new Vector2(shadowScale, _underlineShadowNode.Scale.Y);
    _underlineShadowNode.Modulate = underlineShadowColor;
  }
}
