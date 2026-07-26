namespace Wfc.Entities.Ui.InputHint;

using System.Collections.Generic;
using Godot;
using Wfc.Core.Input.Controllers;
using Wfc.Core.Settings;
using Wfc.Utils;
using EventHandler = Wfc.Core.Event.EventHandler;

// A bottom-of-screen bar of input-hint cards. Add it to any menu screen and it
// keeps every child InputHintCard in sync with the currently active controller
// type (keyboard vs. gamepad), including live switching and rebinding.
//
// The default cards (SELECT / BACK) live in InputHintBar.tscn. A screen can add
// extra InputHintCard children in its own scene to advertise more actions.
//
// The bar slides in from below the screen through its UITransition child, so a
// GameMenu screen picks it up with the rest of its transition elements. Screens
// that aren't a GameMenu (the pause overlay) drive it through Enter()/Exit().
//
// The scene carries a z_index so the bar always paints over the screen it sits
// on: tree order alone isn't enough, because menu content like MenuBox raises
// its own z_index. The bar ignores mouse input, so it never blocks what it covers.
public partial class InputHintBar : Control {
  // Floor for the shared card width, so short captions ("BACK") still get a
  // chip wide enough to survive a longer translation.
  private const float MIN_CARD_WIDTH = 200f;

  private readonly List<InputHintCard> _cards = new();
  private ControllerType _lastType = ControllerType.Keyboard;

  // Which house style the pad glyphs were last drawn in. Swapping a PlayStation
  // pad for an Xbox one leaves _lastType on Gamepad while changing every icon,
  // so the type alone isn't enough to tell whether a rebuild is needed.
  private GamepadIconHelper.ControllerIconType _lastIconType;
  private bool _subscribed;
  private UITransition? _transition;

  // Slides the bar in / out from the bottom of the screen.
  public void Enter() => _transition?.Enter();

  public void Exit() => _transition?.Exit();

  // Subscribed from _EnterTree rather than _Ready so it stays subscribed through
  // a reparent: _ExitTree fires every time the bar is moved, and _Ready only ever
  // runs once.
  public override void _EnterTree() {
    base._EnterTree();
    _subscribe();
  }

  public override void _Ready() {
    base._Ready();
    _transition = GetNodeOrNull<UITransition>("UITransition");
    _collectCards(this);
    _lastType = _effectiveControllerType();
    _lastIconType = GamepadIconHelper.DetectControllerType();
    _refreshAll();
  }

  public override void _ExitTree() {
    base._ExitTree();
    _unsubscribe();
  }

  private void _collectCards(Node node) {
    foreach (var child in node.GetChildren()) {
      if (child is InputHintCard card) {
        _cards.Add(card);
      }
      _collectCards(child);
    }
  }

  private void _refreshAll() {
    foreach (var card in _cards) {
      card.Refresh(_lastType);
    }
    _equalizeCardWidths();
  }

  // Every card gets the width of the widest one, so the bar reads as a row of
  // equal chips whatever the language or the bound glyphs.
  private void _equalizeCardWidths() {
    var widest = MIN_CARD_WIDTH;
    foreach (var card in _cards) {
      card.CustomMinimumSize = new Vector2(0f, card.CustomMinimumSize.Y);
      if (card.Visible) {
        widest = Mathf.Max(widest, card.GetCombinedMinimumSize().X);
      }
    }

    foreach (var card in _cards) {
      card.CustomMinimumSize = new Vector2(widest, card.CustomMinimumSize.Y);
    }
  }

  // Falls back to keyboard when gamepad is the stored preference but none is
  // connected (mirrors KeyBindingController's guard).
  private static ControllerType _effectiveControllerType() {
    var type = GameSettings.LastUsedController;
    if (type == ControllerType.Gamepad && !InputUtils.IsGamepadConnected()) {
      return ControllerType.Keyboard;
    }
    return type;
  }

  private void _subscribe() {
    if (_subscribed) {
      return;
    }
    EventHandler.Instance.Events.LastUsedControllerChanged += _onLastUsedControllerChanged;
    EventHandler.Instance.Events.OnActionBound += _onActionRebound;
    EventHandler.Instance.Events.OnGamepadActionBound += _onGamepadActionRebound;
    Input.JoyConnectionChanged += _onJoyConnectionChanged;
    _subscribed = true;
  }

  private void _unsubscribe() {
    if (!_subscribed) {
      return;
    }
    EventHandler.Instance.Events.LastUsedControllerChanged -= _onLastUsedControllerChanged;
    EventHandler.Instance.Events.OnActionBound -= _onActionRebound;
    EventHandler.Instance.Events.OnGamepadActionBound -= _onGamepadActionRebound;
    Input.JoyConnectionChanged -= _onJoyConnectionChanged;
    _subscribed = false;
  }

  // The player picked up another device: swap every glyph over to it.
  private void _onLastUsedControllerChanged(int controllerType) => _refreshIfDeviceChanged();

  // Unplugging the pad sends the hints back to the keyboard, plugging one in
  // leaves them alone until the player actually presses something on it.
  private void _onJoyConnectionChanged(long device, bool connected) => _refreshIfDeviceChanged();

  private void _refreshIfDeviceChanged() {
    var type = _effectiveControllerType();
    var iconType = GamepadIconHelper.DetectControllerType();
    if (type == _lastType && iconType == _lastIconType) {
      return;
    }
    _lastType = type;
    _lastIconType = iconType;
    _refreshAll();
  }

  private void _onActionRebound(string action, int key) => _refreshAll();

  private void _onGamepadActionRebound(string action, int buttonOrAxis, bool isAxis, float axisDirection) => _refreshAll();
}
