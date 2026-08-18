namespace Wfc.Entities.Ui.InputHint;

using System.Collections.Generic;
using System.Linq;
using Godot;
using Wfc.Core.Input.Controllers;
using Wfc.Core.Localization;
using Wfc.Utils;
using Wfc.Utils.Fonts;

// A single "action -> button" hint chip (e.g. "SELECT [A]").
// Shows a gamepad glyph when a gamepad is active, or a keyboard key cap
// otherwise. The glyph is resolved live from the current InputMap binding, so
// rebinding in the settings updates it automatically.
//
// A card can stand for several actions at once (e.g. SWITCH TAB is bound to
// both shoulder buttons); each one contributes its own glyph.
public partial class InputHintCard : PanelContainer {
  public enum HintKind {
    // Resolves the glyphs from the InputMap actions listed in Actions.
    Action,
    // Stands for "any direction": the whole d-pad, or the arrow keys.
    Navigation
  }

  // What the card advertises. Navigation ignores Actions.
  [Export]
  public HintKind Kind { get; set; } = HintKind.Action;

  // The InputMap actions this card represents (e.g. "ui_accept"). Every action
  // that resolves to a binding contributes a glyph, in the order listed.
  [Export]
  public string[] Actions { get; set; } = ["ui_accept"];

  // The localized caption shown before the glyphs (e.g. "SELECT").
  [Export]
  public TranslationKey CaptionKey { get; set; } = TranslationKey.menu_hint_select;

  // Which surface the card is drawn on, set by the bar it sits in. The caption
  // goes white and the gamepad glyphs come from the inverted set; a key cap is
  // its own light surface either way, so it is left alone.
  public bool OnDarkBackground {
    get => _onDarkBackground;
    set {
      _onDarkBackground = value;
      _applyCaptionColor();
    }
  }

  private static readonly PackedScene _glyphViewScene =
      GD.Load<PackedScene>("res://src/Wfc/Entities/Ui/InputHint/InputGlyphView/InputGlyphView.tscn");

  private Label _caption = default!;
  private HBoxContainer _inputs = default!;
  private bool _wired;
  private bool _onDarkBackground;

  // The caption colour the scene ships, so the light shade can be put back after
  // the card has been drawn dark.
  private Color _captionColorOnLight;

  public override void _Ready() {
    base._Ready();
    _caption = GetNode<Label>("HBox/CaptionBox/Caption");
    _inputs = GetNode<HBoxContainer>("HBox/Inputs");
    _captionColorOnLight = _caption.GetThemeColor("font_color");
    _wired = true;

    _applyCaption();
    _applyCaptionColor();
    _centerCaption();
  }

  // The caption is translated once into the label's own text, which leaves the
  // engine's auto-translation nothing to redo when the player picks another
  // language, so it is written again here.
  public override void _Notification(int what) {
    if (what == NotificationTranslationChanged && _wired) {
      _applyCaption();
    }
  }

  private void _applyCaption() =>
      _caption.Text = TranslationServer.Translate(CaptionKey.ToTranslationKeyStringSafe());

  private void _applyCaptionColor() {
    if (!_wired) {
      return;
    }
    _caption.AddThemeColorOverride(
        "font_color", _onDarkBackground ? Colors.White : _captionColorOnLight);
  }

  // For screens that reword a card per mode (the slot picker's SELECT vs LOAD).
  // The bar re-equalizes card widths on its next refresh, so callers go through
  // InputHintBar.RelabelCard rather than this directly.
  public void SetCaption(TranslationKey key) {
    CaptionKey = key;
    if (_wired) {
      _applyCaption();
    }
  }

  // Rebuilds the glyphs to match the given controller type.
  public void Refresh(ControllerType type) {
    if (!_wired) {
      return;
    }

    _clearInputs();
    var provider = InputIconProvider.For(type, _onDarkBackground);

    if (Kind == HintKind.Navigation) {
      _addGlyph(provider.GetNavigationGlyph());
    }
    else {
      // Two actions on one card can resolve to the same binding on a device (the
      // pause menu's card stands for both pause and back, and a keyboard has them
      // on the same key): the card advertises it once.
      var shown = new HashSet<InputGlyph>();
      foreach (var action in Actions) {
        if (provider.GetGlyph(_eventsOf(action)) is { } glyph && shown.Add(glyph)) {
          _addGlyph(glyph);
        }
      }
    }

    // Nothing is bound to this card on the active device: hide it entirely.
    Visible = _inputs.GetChildCount() > 0;
  }

  // Sits the caption's capitals on the same centre line as the glyphs beside it.
  // The margin is doubled because the box it pads is itself centred in the row,
  // which halves any padding put on one side of it.
  private void _centerCaption() {
    var nudge = FontUtils.OpticalCenterOffset(
        _caption.GetThemeFont("font"), _caption.GetThemeFontSize("font_size"));
    GetNode<MarginContainer>("HBox/CaptionBox")
        .AddThemeConstantOverride("margin_top", Mathf.RoundToInt(nudge * 2f));
  }

  private static IEnumerable<InputEvent> _eventsOf(string action) =>
      InputMap.HasAction(action)
          ? InputMap.ActionGetEvents(action).Cast<InputEvent>()
          : Enumerable.Empty<InputEvent>();

  private void _clearInputs() {
    foreach (var child in _inputs.GetChildren()) {
      _inputs.RemoveChild(child);
      child.QueueFree();
    }
  }

  private void _addGlyph(InputGlyph? glyph) {
    if (glyph == null) {
      return;
    }

    var view = _glyphViewScene.Instantiate<InputGlyphView>();
    // Added first so the view resolves the menu theme before it measures itself.
    _inputs.AddChild(view);
    view.SetGlyph(glyph.Value);
  }
}
