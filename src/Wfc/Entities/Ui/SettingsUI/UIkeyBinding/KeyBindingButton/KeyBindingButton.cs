namespace Wfc.Entities.Ui;

using Chickensoft.Sync.Primitives;
using System;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input.Controllers;
using Wfc.Core.Input;
using Wfc.Core.Localization;
using Wfc.Core.Settings;
using Wfc.Core.Ui;
using Wfc.Entities.Ui.SettingsUI;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[Meta(typeof(IAutoNode))]
public partial class KeyBindingButton : Button, IEditableControl, IDarkBackgroundAware {
  private AutoChannel.Binding? _controllerBinding;


  #region Dependencies
  // The "empty" caption is a translated string kept in a field, so it has to be
  // read again for an unbound action to follow a language change.
  public override void _Notification(int what) {
    this.Notify(what);
    if (what == NotificationTranslationChanged && IsNodeReady()) {
      _applyLocalizedText();
    }
  }

  [Dependency]
  public ILocalizationService LocalizationService => this.DependOn<ILocalizationService>();
  [Dependency]
  public IModalStack ModalStack => this.DependOn<IModalStack>();
  #endregion Dependencies

  #region Exports
  [Export]
  public string key { get; set; } = String.Empty;
  #endregion Exports

  // The gamepad glyphs come in a light-surface and a dark-surface set; which one
  // this button shows follows the surface it was told it sits on, which the row
  // flips under it whenever it takes focus.
  public bool OnDarkBackground {
    get => _onDarkBackground;
    set {
      if (_onDarkBackground == value) {
        return;
      }
      _onDarkBackground = value;
      _reloadArt();
    }
  }

  private bool _onDarkBackground;
  private string _defaultText = "(EMPTY)";

  public enum BindingType { Keyboard, Gamepad }
  private BindingType _type = BindingType.Keyboard;
  public BindingType Type {
    get => _type;
    set {
      if (_type != value) {
        _type = value;
        // On the gamepad panel the direction rows only display their fixed
        // D-Pad/stick mapping; editing them is a keyboard-panel affair.
        Disabled = _isLockedGamepadDirection();
        _loadCurrentBinding();
      }
    }
  }

  private bool _isLockedGamepadDirection() =>
    _type == BindingType.Gamepad && GameSettings.IsGamepadFixedDirectionAction(key);
  // Stores either a Key, JoyButton or JoyAxis binding information.
  private Key? _value = null;
  private JoyButton? _buttonValue = null;
  private JoyAxis? _axisValue = null;
  private float _axisDirection = 0f;

  private bool _isListening = false;
  private bool _isSubscribed;

  [Signal]
  public delegate void onkeyboardActionBoundEventHandler(string action, long key);
  [Signal]
  public delegate void OnGamepadActionBoundEventHandler(string action, int buttonOrAxis, bool isAxis, float axisDirection);
  [Signal]
  public delegate void SelectionChangedEventHandler(bool isEdit);

  #region Nodes
  [NodePath("HBoxContainer/IconTexture")]
  private TextureRect _iconTexture = default!;
  [NodePath("HBoxContainer/Label")]
  private Label _label = default!;
  [NodePath("AnimationPlayer")]
  private AnimationPlayer _animationPlayer = default!;
  #endregion Nodes

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _loadCurrentBinding();
    _animationPlayer.Play("RESET");
    // This button processes Always so it can read a binding while the tree is paused,
    // which also means it hears the mouse under an overlay. Declining then is what
    // stops a stray hover moving focus out of a capture in progress.
    //
    // Subscribing in _Ready is deliberate: it happens once, where OnResolved can run
    // again if the button is moved. ModalStack is only touched when the closure runs,
    // on a hover, long after dependencies have resolved.
    this.GrabFocusOnHover(canFocus: () => !ModalStack.IsAnyOpen);
  }

  // Subscribed from _EnterTree, not _Ready: UIGridRow reparents this button into
  // its row while the settings screen builds itself, and _Ready never runs twice.
  public override void _EnterTree() {
    base._EnterTree();
    if (!_isSubscribed) {
    _controllerBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.LastUsedControllerChanged m) => _onLastUsedControllerChanged(m.Controller));
      Input.JoyConnectionChanged += _onJoyConnectionChanged;
      _isSubscribed = true;
    }
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (_isSubscribed) {
    _controllerBinding?.Dispose();
    _controllerBinding = null;
      Input.JoyConnectionChanged -= _onJoyConnectionChanged;
      _isSubscribed = false;
    }
    // Leaving while still listening (the screen was torn down mid capture) would
    // otherwise strand device detection in the off state, and the tree paused, for
    // the rest of the run. Guarded on _isListening because UIGridRow reparents this
    // button during setup, long before its dependencies resolve.
    if (_isListening) {
      _isListening = false;
      _setDetectionEnabled(true);
      ModalStack.Pop(this);
    }
  }

  // A pad of another make draws its buttons with different art, so the icon has
  // to be resolved again even though the binding behind it hasn't moved. The
  // keyboard/gamepad switch arrives separately, through Type.
  private void _onLastUsedControllerChanged(ControllerType controllerType) => _reloadArt();

  // Unplugging the pad in use hands the icons over to whichever is left.
  private void _onJoyConnectionChanged(long device, bool connected) => _reloadArt();

  private void _reloadArt() {
    if (IsNodeReady()) {
      _loadCurrentBinding();
    }
  }

  private void _loadCurrentKeyboardBinding() {
    var actionList = InputMap.ActionGetEvents(key).Cast<InputEvent>();
    var inputEvent = InputUtils.GetFirstKeyKeyboardEventFromActionList(actionList);
    if (inputEvent != null) {
      if (inputEvent is InputEventKey inputKeyEvent) {
        _setKeyboardEventKey(inputKeyEvent);
        return;
      }
    }

    // No binding found
    _showEmptyState();
  }


  private void _loadCurrentGamepadBinding() {
    var actionList = InputMap.ActionGetEvents(key).Cast<InputEvent>();

    // Check for button binding first
    var buttonEvent = InputUtils.GetFirstJoypadButtonEventFromActionList(actionList);
    if (buttonEvent != null) {
      _buttonValue = buttonEvent.ButtonIndex;
      _axisValue = null;
      _updateIconDisplay();
      return;
    }

    // Check for axis binding
    var axisEvent = InputUtils.GetFirstJoypadAxisEventFromActionList(actionList);
    if (axisEvent != null) {
      _axisValue = axisEvent.Axis;
      _axisDirection = axisEvent.AxisValue;
      _buttonValue = null;
      _updateIconDisplay();
      return;
    }

    // No binding found
    _showEmptyState();
  }

  private void _loadCurrentBinding() {
    switch (Type) {
      case BindingType.Keyboard:
        _loadCurrentKeyboardBinding();
        break;
      case BindingType.Gamepad:
        _loadCurrentGamepadBinding();
        break;
    }

  }

  private void _showEmptyState() {
    _iconTexture.Visible = false;
    _label.Visible = true;
    _label.Text = _defaultText;
  }

  private void _updateIconDisplay() {
    Texture2D? icon = null;
    string fallbackText = _defaultText;

    if (_buttonValue != null) {
      icon = GamepadIconHelper.GetButtonIcon(_buttonValue.Value, onDarkBackground: _onDarkBackground);
      fallbackText = InputUtils.GetJoyButtonName(_buttonValue.Value);
    }
    else if (_axisValue != null) {
      icon = GamepadIconHelper.GetAxisIcon(_axisValue.Value, _axisDirection, onDarkBackground: _onDarkBackground);
      fallbackText = InputUtils.GetJoyAxisName(_axisValue.Value, _axisDirection);
    }

    if (icon != null) {
      _iconTexture.Texture = icon;
      _iconTexture.Visible = true;
      _label.Visible = false;
    }
    else {
      _iconTexture.Visible = false;
      _label.Visible = true;
      _label.Text = fallbackText;
    }
  }

  private void Undo() {
    if (_buttonValue != null || _axisValue != null) {
      _updateIconDisplay();
    }
    else {
      _showEmptyState();
    }
  }

  public override void _Input(InputEvent @event) {
    bool handled = false;
    if (!_isListening) {
      return;
    }

    switch (Type) {
      case BindingType.Keyboard:
        _handleKeyboardInput(@event, ref handled);
        break;
      case BindingType.Gamepad:
        _handleGamepadInput(@event, ref handled);
        break;
    }

    if (handled) {
      setEditing(false);
      GetViewport().SetInputAsHandled();
    }
  }

  private void _handleGamepadInput(InputEvent @event, ref bool handled) {
    if (@event is InputEventJoypadButton joypadButton && joypadButton.Pressed) {
      // The D-Pad and the stick belong to the directional actions on every pad;
      // a capture ignores them so they cannot be stolen onto anything else.
      if (GameSettings.IsReservedGamepadInput(@event)) {
        return;
      }
      _buttonValue = joypadButton.ButtonIndex;
      _axisValue = null;
      _updateIconDisplay();
      EmitSignal(nameof(OnGamepadActionBound), key, (int)_buttonValue, false, 0f);
      handled = true;
    }
    else if (@event is InputEventJoypadMotion joypadMotion) {
      // Only register axis movement if it's significant (deadzone)
      if (Mathf.Abs(joypadMotion.AxisValue) > 0.5f) {
        if (GameSettings.IsReservedGamepadInput(@event)) {
          return;
        }
        _axisValue = joypadMotion.Axis;
        _axisDirection = joypadMotion.AxisValue > 0 ? 1f : -1f;
        _buttonValue = null;
        _updateIconDisplay();
        EmitSignal(nameof(OnGamepadActionBound), key, (int)_axisValue, true, _axisDirection);
        handled = true;
      }
    }
    else if (@event is InputEventMouse eventMouse) {
      if (eventMouse.ButtonMask.HasFlag(MouseButtonMask.Left)) {
        Undo();
        handled = true;
      }
    }
    else if (@event is InputEventKey eventKey && eventKey.Pressed) {
      // Allow canceling with keyboard Escape key
      if (eventKey.Keycode == Key.Escape) {
        Undo();
        handled = true;
      }
    }
  }

  private void _handleKeyboardInput(InputEvent @event, ref bool handled) {
    if (@event is InputEventKey eventKey) {
      var keycode = _setKeyboardEventKey(eventKey);
      EmitSignal(nameof(onkeyboardActionBound), key, (long)keycode);
      handled = true;
    }
    else if (@event is InputEventMouse eventMouse) {
      if (eventMouse.ButtonMask.HasFlag(MouseButtonMask.Left)) {
        Undo();
        handled = true;
      }
    }
  }

  private Key _setKeyboardEventKey(InputEventKey inputKeyEvent) {
    _value = inputKeyEvent.Keycode;
    _iconTexture.Visible = false;
    _label.Visible = true;
    _label.Text = OS.GetKeycodeString(_value.Value);
    return _value ?? Key.Unknown;
  }

  public bool IsValid() {
    return (Type == BindingType.Keyboard && _value != null) ||
      (Type == BindingType.Gamepad && (_buttonValue != null || _axisValue != null));
  }

  public void setEditing(bool isEditing) {
    if (_isListening == isEditing) {
      return;
    }
    _isListening = isEditing;
    ButtonPressed = isEditing;
    _setDetectionEnabled(!isEditing);
    // Registering as a modal is what pauses the tree, so the settings screen behind
    // stops navigating while the next press is being read as a binding. This button
    // processes Always (set in its scene), so it still hears that press itself.
    if (isEditing) {
      ModalStack.Push(this);
      _animationPlayer.Play("Blink");
      GameEvents.Instance.OnKeyboardActionBinding();
    }
    else {
      ModalStack.Pop(this);
      _animationPlayer.Play("RESET");
    }
    _emitSelectionChangedSignal();
  }

  private void _onKeyboardActionBoundSignal(string action, long key) {
    long val = this._value != null ? (long)this._value : -1L;
    if (action == this.key || key != (long)val) {
      return;
    }
    _value = null;
    _showEmptyState();
    EmitSignal(nameof(onkeyboardActionBound), action, -1);
  }

  // Another row took this button or axis: ours goes empty rather than letting one
  // press drive two actions. Called by the KeyBindingController for every row; the
  // keyboard equivalent above is wired row-to-row in the scene instead.
  public void HandleGamepadActionBound(string action, int buttonOrAxis, bool isAxis, float axisDirection) {
    if (action == key || _isLockedGamepadDirection()) {
      return;
    }

    bool shouldClear = false;
    if (!isAxis && _buttonValue != null && (int)_buttonValue == buttonOrAxis) {
      shouldClear = true;
    }
    else if (isAxis && _axisValue != null && (int)_axisValue == buttonOrAxis && _axisDirection == axisDirection) {
      shouldClear = true;
    }

    if (shouldClear) {
      _buttonValue = null;
      _axisValue = null;
      _showEmptyState();
      EmitSignal(nameof(OnGamepadActionBound), key, -1, false, 0f);
    }
  }

  private void _onKeyBindingButtonPressed() {
    // Disabled already swallows presses; this covers a press that arrived through
    // code or a focus quirk while the row is locked.
    if (_isLockedGamepadDirection()) {
      ButtonPressed = false;
      return;
    }
    if (ButtonPressed) {
      setEditing(true);
    }
  }

  public void OnResolved() => _applyLocalizedText();

  private void _applyLocalizedText() {
    _defaultText = LocalizationService.GetLocalizedString(TranslationKey.game_command_empty);
    // Update display now that we have the localized empty string
    _loadCurrentBinding();
  }

  public bool IsInEditMode() => _isListening;

  // While this button is capturing, what gets pressed is a binding rather than
  // the player reaching for another device: the automatic keyboard/gamepad
  // switch has to stay out of it, or binding a gamepad button would swap the
  // panel over to the keyboard halfway through the capture.
  private static void _setDetectionEnabled(bool enabled) {
    if (InputDeviceDetector.Instance != null) {
      InputDeviceDetector.Instance.Enabled = enabled;
    }
  }

  private void _emitSelectionChangedSignal() {
    EmitSignal(nameof(SelectionChanged), _isListening);
  }
}
