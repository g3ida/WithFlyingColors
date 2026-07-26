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

  // Floor for the shrink-to-fit below. Nothing in the translation table comes near
  // it; it is here so a future word can only ever get small, never unbounded.
  private const int MIN_LABEL_FONT_SIZE = 80;
  private const int LABEL_FONT_SIZE_STEP = 2;
  #endregion Constants

  #region Dependencies
  // The label holds a string that was already translated when the face was built,
  // so the engine's own auto-translation has nothing left to redo once the player
  // picks another language. Writing it again - and measuring it again - is what
  // keeps the face in step.
  public override void _Notification(int what) {
    this.Notify(what);
    if (what == NotificationTranslationChanged && _designFontSize > 0) {
      _applyLabel();
    }
  }

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

  // The size the scene asks for, read once before the fit below starts writing its
  // own override on top of it. Doubles as the "wired up yet?" flag.
  private int _designFontSize;
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
  [NodePath("CenterLabel")]
  private Control _labelBoxNode = default!;
  [NodePath("CenterLabel/Label")]
  private Label _labelNode = default!;
  [NodePath("BlinkTimer")]
  private Timer _blinkTimer = default!;
  #endregion Nodes

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _designFontSize = _labelNode.GetThemeFontSize("font_size");
  }

  public void OnResolved() {
    _applyLabel();
    _updateLabelColor();
  }

  private void _applyLabel() {
    _labelNode.Text = LocalizationService.GetLocalizedString(LocalizationTextKey);
    _fitLabelToFace();
  }

  // A face is the triangle between the cube's outer edge and its centre, so the room
  // a word has narrows by two pixels for every pixel it sits further in. The binding
  // row is the baseline: this font is capitals only, so that is the deepest ink the
  // word puts on the face, and anything above it has more room, not less.
  //
  // Shrinking is the last resort - every word in the table is short enough to be
  // drawn at the size the scene asks for - but a face that quietly spills its label
  // across its two neighbours is worse than one that sets it a few points smaller.
  private void _fitLabelToFace() {
    if (GetParent() is not Sprite2D face || face.Texture is null) {
      return;
    }

    var font = _labelNode.GetThemeFont("font");
    var halfSize = face.Texture.GetSize().X * 0.5f;
    // Position is measured from the cube's centre and the label box sits further out
    // again, both along whichever axis this face was rotated onto.
    var centerDepth = halfSize - Position.Length() + _labelBoxNode.Position.Y;

    for (var size = _designFontSize; size > MIN_LABEL_FONT_SIZE; size -= LABEL_FONT_SIZE_STEP) {
      // How far the baseline falls below the centre line the label is placed on.
      var baselineDrop = font.GetAscent(size) - (font.GetHeight(size) * 0.5f);
      var available = (halfSize - centerDepth - baselineDrop) * 2f;
      if (font.GetStringSize(_labelNode.Text, fontSize: size).X <= available) {
        _labelNode.AddThemeFontSizeOverride("font_size", size);
        return;
      }
    }
    _labelNode.AddThemeFontSizeOverride("font_size", MIN_LABEL_FONT_SIZE);
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
