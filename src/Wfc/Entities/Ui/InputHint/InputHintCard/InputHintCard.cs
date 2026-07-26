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

  private static readonly PackedScene _glyphViewScene =
      GD.Load<PackedScene>("res://src/Wfc/Entities/Ui/InputHint/InputGlyphView/InputGlyphView.tscn");

  private Label _caption = default!;
  private HBoxContainer _inputs = default!;
  private bool _wired;

  public override void _Ready() {
    base._Ready();
    _caption = GetNode<Label>("HBox/CaptionBox/Caption");
    _inputs = GetNode<HBoxContainer>("HBox/Inputs");
    _wired = true;

    _caption.Text = TranslationServer.Translate(CaptionKey.ToTranslationKeyStringSafe());
    _centerCaption();
  }

  // Rebuilds the glyphs to match the given controller type.
  public void Refresh(ControllerType type) {
    if (!_wired) {
      return;
    }

    _clearInputs();
    var provider = InputIconProvider.For(type);

    if (Kind == HintKind.Navigation) {
      _addGlyph(provider.GetNavigationGlyph());
    }
    else {
      foreach (var action in Actions) {
        _addGlyph(provider.GetGlyph(_eventsOf(action)));
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
