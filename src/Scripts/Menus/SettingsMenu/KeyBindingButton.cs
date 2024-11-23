using Godot;
using System.Linq;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class KeyBindingButton : Button {
  private const string DefaultText = "(EMPTY)";
  [Export] public string key { get; set; }
  private Key? value = null;

  private bool isListening = false;

  [Signal]
  public delegate void keyboard_action_boundEventHandler(string action, long key);

  private AnimationPlayer AnimationPlayer;

  public override void _Ready() {
    AnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
    var actionList = InputMap.ActionGetEvents(key).Cast<InputEvent>();
    var inputEvent = InputUtils.GetFirstKeyKeyboardEventFromActionList(actionList);
    if (inputEvent != null) {
      if (inputEvent is InputEventKey inputKeyEvent) {
        value = inputKeyEvent.Keycode;
        Text = OS.GetKeycodeString(value.Value);
      }
    }
    AnimationPlayer.Play("RESET");
  }

  private void Undo() {
    if (value != null) {
      Text = OS.GetKeycodeString(value.Value);
    }
    else {
      Text = DefaultText;
    }
  }

  public override void _Input(InputEvent ev) {
    bool handled = false;
    if (!isListening) {
      return;
    }
    if (ev is InputEventKey eventKey) {
      value = eventKey.Keycode;
      Text = OS.GetKeycodeString(value.Value);
      EmitSignal(nameof(keyboard_action_bound), key, (long)value);
      handled = true;
    }
    else if (ev is InputEventMouse eventMouse) {
      if (eventMouse.ButtonMask.HasFlag(MouseButtonMask.Left)) {
        Undo();
        handled = true;
      }
    }
    if (handled) {
      ButtonPressed = false;
      isListening = false;
      GetViewport().SetInputAsHandled();
      GetTree().Paused = false;
      AnimationPlayer.Play("RESET");
    }
  }

  public bool IsValid() {
    return Text == DefaultText;
  }

  private void _on_Control_on_action_bound_signal(string action, long key) {
    long val = this.value != null ? (long)this.value : -1L;
    if (action == this.key || key != (long)val) {
      return;
    }
    value = null;
    Text = DefaultText;
    EmitSignal(nameof(keyboard_action_bound), action, -1);
  }

  private void _on_KeyBindingButton_pressed() {
    if (ButtonPressed) {
      ButtonPressed = true;
      isListening = true;
      EventHandler.Instance.EmitKeyboardActionBiding();
      AnimationPlayer.Play("Blink");
      GetTree().Paused = true;
    }
  }

  private void _on_KeyBindingButton_mouse_entered() {
    if (!GetTree().Paused) {
      GrabFocus();
    }
  }
}
