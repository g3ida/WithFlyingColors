namespace Wfc.Entities.Ui.Menubox;

using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Localization;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[Meta(typeof(IAutoNode))]
public partial class MenuBoxButton : Control {

  #region Constants
  private static readonly SkinColorIntensity LABEL_COLOR_INTENSITY = SkinColorIntensity.SuperDark;
  private static readonly SkinColorIntensity LABEL_COLOR_HOVER_INTENSITY = SkinColorIntensity.ExtremelyDark;
  private static readonly SkinColorIntensity LABEL_COLOR_CLICK_INTENSITY = SkinColorIntensity.Background;
  private static readonly SkinColorIntensity LABEL_COLOR_DISABLED_INTENSITY = SkinColorIntensity.VeryDark;
  #endregion Constants

  #region Dependencies
  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public ILocalizationService LocalizationService => this.DependOn<ILocalizationService>();
  #endregion Dependencies

  #region Signals
  [Signal] public delegate void pressedEventHandler();
  #endregion Signals

  #region Fields
  private string _text = string.Empty;
  private bool _hovering = false;
  private GameSkin _skin = SkinManager.Instance.CurrentSkin;
  private bool _disabled;
  #endregion Fields

  #region Exports
  [Export]
  public SkinColor SkinColor { get; set; }
  [Export]
  public TranslationKey LocalizationTextKey { get; set; }
  [Export]
  public bool disabled {
    get => _disabled;
    set => _setDisabled(value);
  }
  #endregion Exports

  #region Nodes
  [NodePath("CenterTexture/TextureButton")]
  private TextureButton _textureButtonNode = default!;
  [NodePath("CenterLabel/Label")]
  private Label _labelNode = default!;
  [NodePath("BlinkTimer")]
  private Timer _blinkTimer = default!;
  #endregion Nodes

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
  }

  public void OnResolved() {
    _labelNode.Text = LocalizationService.GetLocalizedString(LocalizationTextKey);
    _updateLabelColor();
  }

  private void _onTextureButtonPressed() {
    _labelNode.Modulate = _skin.GetColor(SkinColor, LABEL_COLOR_CLICK_INTENSITY);
    _blinkTimer.Start();
    EmitSignal(nameof(pressed));
  }

  private void _onTextureButtonMouseEntered() {
    _hovering = true;
    if (_disabled)
      return;
    _labelNode.Modulate = _skin.GetColor(SkinColor, LABEL_COLOR_HOVER_INTENSITY);
  }

  private void _onTextureButtonMouseExited() {
    _hovering = false;
    if (_disabled)
      return;
    _updateLabelColor();
  }

  private void _onBlinkTimerTimeout() {
    _labelNode.Modulate = _skin.GetColor(SkinColor, _hovering ? LABEL_COLOR_HOVER_INTENSITY : LABEL_COLOR_INTENSITY);
    if (_disabled)
      _labelNode.Modulate = _skin.GetColor(SkinColor, LABEL_COLOR_DISABLED_INTENSITY);
  }

  private void _setDisabled(bool value) {
    _disabled = value;
    if (_disabled) {
      _textureButtonNode.Disabled = true;
      _labelNode.Modulate = _skin.GetColor(SkinColor, LABEL_COLOR_DISABLED_INTENSITY);
    }
    else {
      _textureButtonNode.Disabled = false;
      _labelNode.Modulate = _skin.GetColor(SkinColor, _hovering ? LABEL_COLOR_HOVER_INTENSITY : LABEL_COLOR_INTENSITY);

    }
  }

  private void _updateLabelColor() {
    _labelNode.Modulate = _skin.GetColor(SkinColor, _disabled ? LABEL_COLOR_DISABLED_INTENSITY : LABEL_COLOR_INTENSITY);

  }
}
