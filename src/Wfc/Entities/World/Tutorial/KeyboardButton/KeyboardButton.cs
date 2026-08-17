namespace Wfc.Entities.World.Tutorial;

using Chickensoft.Sync.Primitives;
using System;
using System.Collections.Generic;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input.Controllers;
using Wfc.Utils;
using Wfc.Utils.Attributes;

// The button a tutorial line tells the player to press.
//
// Drawn as the key cap it is bound to on a keyboard, or as the pad's own art once
// the player picks up a gamepad, and it follows them between the two. The pad art
// comes from the inverted set: a tutorial line is painted onto the level itself,
// which is dark, rather than onto a menu panel.
[Tool]
public partial class KeyboardButton : Control {
  private AutoChannel.Binding? _inputBinding;

  // A cap belongs on the line of text that names it rather than towering over it, and
  // the nine patch cannot be laid out under its own border thickness - the art's
  // natural size. So the art is scaled instead, while the letter on it keeps the size
  // of the words around it.
  private const float CapHeight = 70f;
  private const float NaturalCapHeight = 108f;
  private const float CapPaddingX = 26f;

  // The drawn key carries a second outline offset down and to the left, so the face a
  // letter belongs on is up and to the right of the node's own centre. Measured off
  // the art in its own pixels and scaled with it.
  private static readonly Vector2 FaceCenterOffset = new(6.07f, -8.32f);
  // How much narrower the face is than the whole art, same pixels: what a cap has to
  // carry on top of its label before the face itself is square.
  private const float FaceInsetX = 14.75f;

  // Wide pad art (the shoulder buttons) is held back by the width instead of the
  // height, or it would tower over the round face buttons.
  private const float GamepadIconMaxWidth = 100f;

  private readonly uint[] ArrowKeys = {
        (uint)Key.Right,
        (uint)Key.Down,
        (uint)Key.Left,
        (uint)Key.Up
    };

  [Export]
  public string key_text { get; set; } = "";

  [NodePath("Label")]
  private Label _labelNode = default!;
  [NodePath("NinePatchRect")]
  private NinePatchRect _buttonTextureNode = default!;
  [NodePath("Arrow")]
  private Sprite2D _arrowSpriteNode = default!;
  [NodePath("GamepadIcon")]
  private TextureRect _gamepadIconNode = default!;

  private bool _isWired;
  private bool _isSubscribed;
  private bool _isShowingGamepadIcon;

  public override void _EnterTree() {
    base._EnterTree();
    if (Engine.IsEditorHint() || _isSubscribed) {
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
    _refresh();
  }

  private void _refresh() {
    if (!InputMap.HasAction(key_text)) {
      return;
    }
    var actionEvents = InputMap.ActionGetEvents(key_text);
    if (actionEvents.Count == 0) {
      return;
    }

    // The editor has no player and no settings loaded, so it always draws the
    // keyboard binding rather than guessing at a device.
    var isOnGamepad = !Engine.IsEditorHint()
        && InputUtils.GetEffectiveControllerType() == ControllerType.Gamepad;
    if (isOnGamepad && _showGamepadGlyph(actionEvents)) {
      return;
    }
    _showKeyboardKey(actionEvents);
  }

  private bool _showGamepadGlyph(IEnumerable<InputEvent> actionEvents) {
    var glyph = InputIconProvider
        .For(ControllerType.Gamepad, onDarkBackground: true)
        .GetGlyph(actionEvents);
    if (glyph is not { } value) {
      return false;
    }

    // A pad binding with no art of its own comes back as a cap carrying its name,
    // which is exactly what this scene's own cap already draws.
    if (value.Label is { } label) {
      _showCap();
      _labelNode.Text = label;
      _onLabelResized();
      return true;
    }

    _isShowingGamepadIcon = true;
    _buttonTextureNode.Visible = false;
    _labelNode.Visible = false;
    _arrowSpriteNode.Visible = false;
    _gamepadIconNode.Visible = true;
    _gamepadIconNode.Texture = value.Texture;

    var width = value.Texture.GetWidth();
    var height = value.Texture.GetHeight();
    var scale = width <= 0 || height <= 0
        ? 1f
        : Mathf.Min(CapHeight / height, GamepadIconMaxWidth / width);
    CustomMinimumSize = new Vector2(width * scale, height * scale);
    return true;
  }

  private void _showKeyboardKey(IEnumerable<InputEvent> actionEvents) {
    var key = InputUtils.GetFirstKeyKeyboardEventFromActionList(actionEvents);
    if (key == null) {
      return;
    }

    _showCap();
    var index = Array.IndexOf(ArrowKeys, (uint)key.Keycode);
    if (index != -1) {
      _arrowSpriteNode.Visible = true;
      _arrowSpriteNode.Rotation = index * Mathf.Pi / 2;
      // An arrow is drawn on the face instead of a letter, so the cap falls back to
      // its narrowest, which is square.
      _labelNode.Text = "";
      _labelNode.Visible = false;
    }
    else {
      _labelNode.Text = OS.GetKeycodeString(key.Keycode);
    }
    _onLabelResized();
  }

  private void _showCap() {
    _isShowingGamepadIcon = false;
    _gamepadIconNode.Visible = false;
    _buttonTextureNode.Visible = true;
    _labelNode.Visible = true;
    _arrowSpriteNode.Visible = false;
  }

  private void _onLabelResized() {
    // The label lays itself out before _Ready has wired anything up, and it keeps
    // resizing behind the pad art - where letting it through would drag the hint back
    // to a key cap's width around an icon that is no longer there.
    if (!_isWired || _isShowingGamepadIcon) {
      return;
    }

    // Pinned to the text it holds, every time: positioning a Control writes its
    // current size back into its offsets, so a box left over from a longer key name
    // would otherwise follow this cap around for good.
    var labelSize = _labelNode.GetCombinedMinimumSize();
    _labelNode.Size = labelSize;

    var scale = CapHeight / NaturalCapHeight;
    // A cap is never narrower than its face is tall, so a single letter gets a square
    // one however little room the letter itself asks for.
    var width = Mathf.Max(labelSize.X + CapPaddingX, CapHeight + (FaceInsetX * scale));

    _buttonTextureNode.Scale = new Vector2(scale, scale);
    _buttonTextureNode.Size = new Vector2(width / scale, NaturalCapHeight);
    var size = new Vector2(width, CapHeight);
    SetDeferred(PropertyName.Size, size);
    CustomMinimumSize = size;

    var faceCenter = (size * 0.5f) + (FaceCenterOffset * scale);
    _labelNode.Position = faceCenter - (labelSize * 0.5f);
    _arrowSpriteNode.Scale = new Vector2(scale, scale);
    _arrowSpriteNode.Position = faceCenter;
  }

  private void _onLastUsedControllerChanged(ControllerType controllerType) => _refreshIfWired();

  private void _onJoyConnectionChanged(long device, bool connected) => _refreshIfWired();

  private void _onActionRebound(string action, int key) => _refreshIfWired();

  private void _onGamepadActionRebound(string action, int buttonOrAxis, bool isAxis, float axisDirection) =>
      _refreshIfWired();

  private void _refreshIfWired() {
    if (_isWired) {
      _refresh();
    }
  }
}
