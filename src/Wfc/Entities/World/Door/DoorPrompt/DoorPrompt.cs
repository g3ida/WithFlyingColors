namespace Wfc.Entities.World.Door;

using Chickensoft.Sync.Primitives;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input.Controllers;
using Wfc.Core.Input;
using Wfc.Core.Localization;
using Wfc.Entities.Ui.InputHint;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Fonts;

// The "press this to go in" line along the bottom of the screen while the player
// stands in a doorway.
//
// The caption is one translated sentence with a placeholder for the button
// rather than two half sentences, so a language that puts its verb last can
// still put the glyph where it belongs. The glyph itself is resolved live from
// the binding, and drawn in the inverted art: a hub door is a dark scene, not a
// menu panel.
[ScenePath]
public partial class DoorPrompt : CanvasLayer {
  private AutoChannel.Binding? _inputBinding;

  // Where the button goes inside the translated caption.
  private const string GLYPH_PLACEHOLDER = "{0}";

  // What the prompt is offering. The button is the same one everywhere in the hub, so
  // only the wording changes between the things it can be pressed at.
  [Export]
  public TranslationKey CaptionKey { get; set; } = TranslationKey.game_hint_enterDoor;

  #region Nodes
  [NodePath("Center/Panel/Hint/PrefixBox")]
  private MarginContainer _prefixBoxNode = default!;
  [NodePath("Center/Panel/Hint/PrefixBox/Prefix")]
  private Label _prefixNode = default!;
  [NodePath("Center/Panel/Hint/Glyph")]
  private InputGlyphView _glyphNode = default!;
  [NodePath("Center/Panel/Hint/SuffixBox")]
  private MarginContainer _suffixBoxNode = default!;
  [NodePath("Center/Panel/Hint/SuffixBox/Suffix")]
  private Label _suffixNode = default!;
  #endregion Nodes

  private bool _isSubscribed;
  private bool _isWired;

  public override void _EnterTree() {
    base._EnterTree();
    if (_isSubscribed) {
      return;
    }
    _inputBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.LastUsedControllerChanged m) => _onLastUsedControllerChanged(m.Controller))
      .On((in IGameEvents.ActionBound m) => _onActionRebound(m.Action, m.Key))
      .On((in IGameEvents.GamepadActionBound m) => _onGamepadActionRebound(m.Action, m.ButtonOrAxis, m.IsAxis, m.AxisDirection));
    Input.JoyConnectionChanged += _onJoyConnectionChanged;
    _isSubscribed = true;
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (!_isSubscribed) {
      return;
    }
    _inputBinding?.Dispose();
    _inputBinding = null;
    Input.JoyConnectionChanged -= _onJoyConnectionChanged;
    _isSubscribed = false;
  }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _isWired = true;
    _centerLabels();
    _refresh();
  }

  // Sits the capitals on the same centre line as the glyph beside them. The margin is
  // doubled because the box it pads is itself centred in the row, which halves any
  // padding put on one side of it.
  private void _centerLabels() {
    var nudge = FontUtils.OpticalCenterOffset(
        _prefixNode.GetThemeFont("font"), _prefixNode.GetThemeFontSize("font_size"));
    var margin = Mathf.RoundToInt(nudge * 2f);
    _prefixBoxNode.AddThemeConstantOverride("margin_top", margin);
    _suffixBoxNode.AddThemeConstantOverride("margin_top", margin);
  }

  // The caption is translated into the labels' own text, which leaves the engine's
  // auto-translation nothing to redo when the player picks another language.
  public override void _Notification(int what) {
    base._Notification(what);
    if (what == NotificationTranslationChanged && _isWired) {
      _refresh();
    }
  }

  private void _refresh() {
    var caption = TranslationServer
        .Translate(CaptionKey.ToTranslationKeyStringSafe())
        .ToString();
    var parts = caption.Split(GLYPH_PLACEHOLDER);
    _setLabel(_prefixBoxNode, _prefixNode, parts[0].TrimEnd());
    _setLabel(_suffixBoxNode, _suffixNode, parts.Length > 1 ? parts[1].TrimStart() : string.Empty);

    var provider = InputIconProvider.For(InputUtils.GetEffectiveControllerType(), onDarkBackground: true);
    var glyph = provider.GetGlyph(_enterActionEvents());
    _glyphNode.Visible = glyph != null;
    if (glyph is { } value) {
      _glyphNode.SetGlyph(value);
    }
  }

  private static void _setLabel(MarginContainer box, Label label, string text) {
    label.Text = text;
    // An empty half would still claim the row's separation on its side of the glyph.
    box.Visible = text.Length > 0;
  }

  private static IEnumerable<InputEvent> _enterActionEvents() {
    var action = InputManager.Actions[Door.ENTER_ACTION];
    return InputMap.HasAction(action)
        ? InputMap.ActionGetEvents(action).Cast<InputEvent>()
        : Enumerable.Empty<InputEvent>();
  }

  private void _onLastUsedControllerChanged(ControllerType controllerType) => _refresh();

  private void _onJoyConnectionChanged(long device, bool connected) => _refresh();

  private void _onActionRebound(string action, int key) => _refresh();

  private void _onGamepadActionRebound(string action, int buttonOrAxis, bool isAxis, float axisDirection) => _refresh();
}
