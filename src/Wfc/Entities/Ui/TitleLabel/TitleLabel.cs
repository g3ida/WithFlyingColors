namespace Wfc.Entities.Ui;

using System;
using Godot;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

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

  #region Fields
  private bool _isSubscribed;
  #endregion Fields

  public void UpdatePositionX(float value) {
    Position = new Vector2(value, Position.Y);
  }

  public float getEstimatedWidth() {
    return _underlineNode.Size.X * _underlineNode.Scale.X * Scale.X;
  }

  // Width of this word at the size the design asks for. Answerable before the label
  // has entered the tree - the font comes from a theme override rather than from an
  // ancestor - which is when the title above works out how far it has to scale the
  // whole line down to fit the room it was given.
  public float MeasureContentWidth() {
    var font = GetThemeFont("font");
    return font is null ? 0f : font.GetStringSize(content, fontSize: GetThemeFontSize("font_size")).X;
  }

  // Writes a new word into the title. The underline is drawn as a stretched texture
  // rather than a border, so it has to be measured again whenever the word changes
  // length - which is every time a screen is shown in another language.
  public void SetContent(string value) {
    content = value;
    Text = value;
    _shadowNode.Text = value;
    _fitUnderlines();
  }

  public override void _EnterTree() {
    this.WireNodes();
    Text = content;
    _shadowNode.Text = content;
    SetProcess(false);
    _fitUnderlines();
    // The editor has no autoloads, so there is no event bus to listen to there.
    if (!_isSubscribed && !Engine.IsEditorHint()) {
      EventHandler.Instance.Events.SkinChanged += _onSkinChanged;
      _isSubscribed = true;
    }
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (_isSubscribed) {
      EventHandler.Instance.Events.SkinChanged -= _onSkinChanged;
      _isSubscribed = false;
    }
  }

  // The underline is the palette showing through the title, so a player changing
  // palettes on the settings screen has to see it on the title of that screen.
  private void _onSkinChanged(string skin) => _fitUnderlines();

  private void _fitUnderlines() {
    var skin = SkinManager.Instance.CurrentSkin;

    var scale = GetMinimumSize().X / _underlineNode.Size.X;
    _underlineNode.Scale = new Vector2(scale, _underlineNode.Scale.Y);
    _underlineNode.Modulate = skin.GetColor(UnderlineSkinColor, UNDERLINE_COLOR_INTENSITY);

    var shadowScale = GetMinimumSize().X / _underlineShadowNode.Size.X;
    _underlineShadowNode.Scale = new Vector2(shadowScale, _underlineShadowNode.Scale.Y);
    _underlineShadowNode.Modulate = skin.GetColor(UnderlineSkinColor, UNDERLINE_SHADOW_COLOR_INTENSITY);
  }
}
