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

  private Key? _value = null;

  private bool _isListening = false;

  [Signal]
  public delegate void onkeyboardActionBoundEventHandler(string action, long key);
  [Signal]
  public delegate void SelectionChangedEventHandler(bool isEdit);

  [NodePath("AnimationPlayer")]
  private AnimationPlayer _animationPlayer = default!;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    var actionList = InputMap.ActionGetEvents(key).Cast<InputEvent>();
    var inputEvent = InputUtils.GetFirstKeyKeyboardEventFromActionList(actionList);
    if (inputEvent != null) {
      if (inputEvent is InputEventKey inputKeyEvent) {
        _value = inputKeyEvent.Keycode;
        Text = OS.GetKeycodeString(_value.Value);
      }
    }
    _animationPlayer.Play("RESET");
  }

  private void Undo() {
    if (_value != null) {
      Text = OS.GetKeycodeString(_value.Value);
    }
    else {
      Text = _defaultText;
    }
  }

  public override void _Input(InputEvent @event) {
    bool handled = false;
    if (!_isListening) {
      return;
    }
    if (@event is InputEventKey eventKey) {
      _value = eventKey.Keycode;
      Text = OS.GetKeycodeString(_value.Value);
      EmitSignal(nameof(onkeyboardActionBound), key, (long)_value);
      handled = true;
    }
    else if (@event is InputEventMouse eventMouse) {
      if (eventMouse.ButtonMask.HasFlag(MouseButtonMask.Left)) {
        Undo();
        handled = true;
      }
    }
    if (handled) {
      setEditing(false);
      GetViewport().SetInputAsHandled();
    }
  }

  public bool IsValid() {
    return Text == _defaultText;
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

  private void _onActionBoundSignal(string action, long key) {
    long val = this._value != null ? (long)this._value : -1L;
    if (action == this.key || key != (long)val) {
      return;
    }
    _value = null;
    Text = _defaultText;
    EmitSignal(nameof(onkeyboardActionBound), action, -1);
  }

  private void _onKeyBindingButtonPressed() {
    if (ButtonPressed) {
      setEditing(true);
    }
  }

  private void _onKeyBindingButtonMmouseEentered() {
    if (!GetTree().Paused) {
      GrabFocus();
    }
  }

  public void OnResolved() {
    _defaultText = LocalizationService.GetLocalizedString(TranslationKey.game_command_empty);
  }

  public bool IsInEditMode() => _isListening;

  private void _emitSelectionChangedSignal() {
    EmitSignal(nameof(SelectionChanged), _isListening);
  }
}
