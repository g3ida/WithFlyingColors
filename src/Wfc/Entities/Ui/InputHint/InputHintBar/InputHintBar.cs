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
  private bool _subscribed;
  private UITransition? _transition;

  // Cached so the per-frame check stays allocation free: asking Godot for the
  // connected joypads builds an array, and only a connect/disconnect can change
  // the answer.
  private static bool _gamepadConnected;

  // Slides the bar in / out from the bottom of the screen.
  public void Enter() => _transition?.Enter();

  public void Exit() => _transition?.Exit();

  public override void _Ready() {
    base._Ready();
    _transition = GetNodeOrNull<UITransition>("UITransition");
    _collectCards(this);
    _gamepadConnected = InputUtils.IsGamepadConnected();
    _lastType = _effectiveControllerType();
    _refreshAll();
    _subscribe();
  }

  public override void _ExitTree() {
    base._ExitTree();
    _unsubscribe();
  }

  public override void _Process(double delta) {
    base._Process(delta);
    // Cheap poll: GameSettings.LastUsedController has no change notification, so
    // this reads it (and the cached pad connection) and only rebuilds when the
    // effective controller type actually changes.
    var type = _effectiveControllerType();
    if (type != _lastType) {
      _lastType = type;
      _refreshAll();
    }
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
    if (type == ControllerType.Gamepad && !_gamepadConnected) {
      return ControllerType.Keyboard;
    }
    return type;
  }

  private void _subscribe() {
    if (_subscribed) {
      return;
    }
    EventHandler.Instance.Events.OnActionBound += _onActionRebound;
    EventHandler.Instance.Events.OnGamepadActionBound += _onGamepadActionRebound;
    Input.JoyConnectionChanged += _onJoyConnectionChanged;
    _subscribed = true;
  }

  private void _unsubscribe() {
    if (!_subscribed) {
      return;
    }
    EventHandler.Instance.Events.OnActionBound -= _onActionRebound;
    EventHandler.Instance.Events.OnGamepadActionBound -= _onGamepadActionRebound;
    Input.JoyConnectionChanged -= _onJoyConnectionChanged;
    _subscribed = false;
  }

  private void _onJoyConnectionChanged(long device, bool connected) =>
      _gamepadConnected = InputUtils.IsGamepadConnected();

  private void _onActionRebound(string action, int key) => _refreshAll();

  private void _onGamepadActionRebound(string action, int buttonOrAxis, bool isAxis, float axisDirection) => _refreshAll();
}
