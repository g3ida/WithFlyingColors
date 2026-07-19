namespace Wfc.Entities.Ui;

using System;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Localization;
using Wfc.Entities.Ui.SettingsUI;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class KeyBindingButton : Button, IEditableControl {

  #region Dependencies
  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public ILocalizationService LocalizationService => this.DependOn<ILocalizationService>();
  #endregion Dependencies

  #region Exports
  [Export]
  public string key { get; set; } = String.Empty;
  #endregion Exports

  private string _defaultText = "(EMPTY)";

  public enum BindingType { Keyboard, Gamepad }
  private BindingType _type = BindingType.Keyboard;
  public BindingType Type {
    get => _type;
    set {
      if (_type != value) {
        _type = value;
        _loadCurrentBinding();
      }
    }
  }
  // Stores either a Key, JoyButton or JoyAxis binding information.
  private Key? _value = null;
  private JoyButton? _buttonValue = null;
  private JoyAxis? _axisValue = null;
  private float _axisDirection = 0f;

  private bool _isListening = false;

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
      icon = GamepadIconHelper.GetButtonIcon(_buttonValue.Value);
      fallbackText = InputUtils.GetJoyButtonName(_buttonValue.Value);
    }
    else if (_axisValue != null) {
      icon = GamepadIconHelper.GetAxisIcon(_axisValue.Value, _axisDirection);
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
      _buttonValue = joypadButton.ButtonIndex;
      _axisValue = null;
      _updateIconDisplay();
      EmitSignal(nameof(OnGamepadActionBound), key, (int)_buttonValue, false, 0f);
      handled = true;
    }
    else if (@event is InputEventJoypadMotion joypadMotion) {
      // Only register axis movement if it's significant (deadzone)
      if (Mathf.Abs(joypadMotion.AxisValue) > 0.5f) {
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
    if (isEditing) {
      _animationPlayer.Play("Blink");
      EventHandler.Instance.EmitKeyboardActionBiding();
    }
    else {
      _animationPlayer.Play("RESET");
    }
    GetTree().Paused = isEditing;
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

  public void _onGamepadActionBoundSignal(string action, int buttonOrAxis, bool isAxis, float axisDirection) {
    // If another action was bound with the same button/axis, clear our binding
    if (action == this.key) {
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
    if (ButtonPressed) {
      setEditing(true);
    }
  }

  private void _onKeyBindingButtonMouseEntered() {
    if (!GetTree().Paused) {
      GrabFocus();
    }
  }

  public void OnResolved() {
    _defaultText = LocalizationService.GetLocalizedString(TranslationKey.game_command_empty);
    // Update display now that we have the localized empty string
    _loadCurrentBinding();
  }

  public bool IsInEditMode() => _isListening;

  private void _emitSelectionChangedSignal() {
    EmitSignal(nameof(SelectionChanged), _isListening);
  }
}
