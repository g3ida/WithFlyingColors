namespace Wfc.Entities.Ui.InputHint;

using Chickensoft.Sync.Primitives;
using System.Collections.Generic;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input.Controllers;
using Wfc.Core.Localization;
using Wfc.Core.Logger;
using Wfc.Core.Settings;
using Wfc.Utils;

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
  private AutoChannel.Binding? _inputBinding;

  // Floor for the shared card width, so short captions ("BACK") still get a
  // chip wide enough to survive a longer translation.
  private const float MIN_CARD_WIDTH = 200f;

  // The surface the bar is drawn on. Menu screens lay it over a light panel; a
  // bar drawn over the level itself (the pause overlay) sets this, and every
  // card it holds swaps to the shades that read on a dark background.
  [Export]
  public bool OnDarkBackground {
    get => _onDarkBackground;
    set {
      _onDarkBackground = value;
      _applyBackgroundShade();
    }
  }

  private readonly List<InputHintCard> _cards = new();
  private bool _onDarkBackground;
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

  // Rewords one card and re-lays the row out, so a screen can reuse the default
  // cards across its modes instead of stacking near-identical bars.
  public void RelabelCard(string cardName, TranslationKey captionKey) {
    var card = _findCard(cardName);
    if (card == null) {
      return;
    }
    card.SetCaption(captionKey);
    _refreshAll();
  }

  // Drops one of the default cards, for a screen that does not offer the action it
  // stands for - the first-launch language screen has nothing behind it to go back to.
  //
  // Taken out rather than hidden: whether a card shows is its own to decide, from
  // whether the action it stands for resolves to a binding at all, and it decides
  // that again on every refresh.
  public void RemoveCard(string cardName) {
    var card = _findCard(cardName);
    if (card == null) {
      return;
    }
    _cards.Remove(card);
    // Out of the tree now rather than whenever the free lands, so the row it was in
    // is laid out again without it in the same frame.
    card.GetParent().RemoveChild(card);
    card.QueueFree();
    _equalizeCardWidths();
  }

  private InputHintCard? _findCard(string cardName) {
    var card = _cards.Find(candidate => candidate.Name == cardName);
    if (card == null) {
      // Silence here would leave the screen advertising the wrong action with nothing
      // to say why, so a renamed or misspelt card is loud instead.
      Log.Error($"{nameof(InputHintBar)} has no card named '{cardName}'.");
    }
    return card;
  }

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
    _cards.AddRange(this.FindDescendants<InputHintCard>());
    _lastType = InputUtils.GetEffectiveControllerType();
    _lastIconType = GamepadIconHelper.DetectControllerType();
    // The exported flag arrives before the cards are collected, so the shade is
    // handed down here rather than where it was set.
    _applyBackgroundShade();
  }

  public override void _ExitTree() {
    base._ExitTree();
    _unsubscribe();
  }

  private void _applyBackgroundShade() {
    foreach (var card in _cards) {
      card.OnDarkBackground = _onDarkBackground;
    }
    _refreshAll();
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

  private void _subscribe() {
    if (_subscribed) {
      return;
    }
    _inputBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.LastUsedControllerChanged m) => _onLastUsedControllerChanged(m.Controller))
      .On((in IGameEvents.ActionBound m) => _onActionRebound(m.Action, m.Key))
      .On((in IGameEvents.GamepadActionBound m) => _onGamepadActionRebound(m.Action, m.ButtonOrAxis, m.IsAxis, m.AxisDirection));
    Input.JoyConnectionChanged += _onJoyConnectionChanged;
    _subscribed = true;
  }

  private void _unsubscribe() {
    if (!_subscribed) {
      return;
    }
    _inputBinding?.Dispose();
    _inputBinding = null;
    Input.JoyConnectionChanged -= _onJoyConnectionChanged;
    _subscribed = false;
  }

  // The player picked up another device: swap every glyph over to it.
  private void _onLastUsedControllerChanged(ControllerType controllerType) => _refreshIfDeviceChanged();

  // Unplugging the pad sends the hints back to the keyboard, plugging one in
  // leaves them alone until the player actually presses something on it.
  private void _onJoyConnectionChanged(long device, bool connected) => _refreshIfDeviceChanged();

  private void _refreshIfDeviceChanged() {
    var type = InputUtils.GetEffectiveControllerType();
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
