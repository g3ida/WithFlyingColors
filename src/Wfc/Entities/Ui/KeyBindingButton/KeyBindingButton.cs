namespace Wfc.Entities.Ui;

using System;
using System.Linq;
using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class KeyBindingButton : Button {
  private const string DefaultText = "(EMPTY)";
  [Export] public string key { get; set; } = String.Empty;
  private Key? _value = null;

  private bool _isListening = false;

  [Signal]
  public delegate void keyboard_action_boundEventHandler(string action, long key);

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
      Text = DefaultText;
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
      EmitSignal(nameof(keyboard_action_bound), key, (long)_value);
      handled = true;
    }
    else if (@event is InputEventMouse eventMouse) {
      if (eventMouse.ButtonMask.HasFlag(MouseButtonMask.Left)) {
        Undo();
        handled = true;
      }
    }
    if (handled) {
      ButtonPressed = false;
      _isListening = false;
      GetViewport().SetInputAsHandled();
      GetTree().Paused = false;
      _animationPlayer.Play("RESET");
    }
  }

  public bool IsValid() {
    return Text == DefaultText;
  }

  private void _on_Control_on_action_bound_signal(string action, long key) {
    long val = this._value != null ? (long)this._value : -1L;
    if (action == this.key || key != (long)val) {
      return;
    }
    _value = null;
    Text = DefaultText;
    EmitSignal(nameof(keyboard_action_bound), action, -1);
  }

  private void _on_KeyBindingButton_pressed() {
    if (ButtonPressed) {
      ButtonPressed = true;
      _isListening = true;
      EventHandler.Instance.EmitKeyboardActionBiding();
      _animationPlayer.Play("Blink");
      GetTree().Paused = true;
    }
  }

  private void _on_KeyBindingButton_mouse_entered() {
    if (!GetTree().Paused) {
      GrabFocus();
    }
  }
}
