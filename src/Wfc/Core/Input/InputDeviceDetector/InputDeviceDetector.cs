namespace Wfc.Core.Input;

using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input.Controllers;
using Wfc.Core.Settings;
using Wfc.Utils;

// Watches every input event the game window receives and records which kind of
// device produced it, so the UI can follow the player from the keyboard to a
// gamepad and back without them ever changing a setting. The answer is written
// to GameSettings.LastUsedController, which announces the change to whoever
// draws device specific UI (the input hint bar, the controller settings).
//
// It listens on the root window's window_input signal rather than through
// _Input: menus mark their navigation input as handled (SettingsFocusManager
// does, on every arrow press), which stops _input propagation before it reaches
// an autoload sitting above them in the tree. window_input fires before the
// event is pushed into the tree at all, so nothing can swallow it first.
//
// Created by AutoloadManager, so it lives for the whole run.
public partial class InputDeviceDetector : Node {
  // How far a stick has to travel before it counts as a deliberate push rather
  // than the drift of a pad resting on a table.
  private const float AXIS_DEADZONE = 0.5f;

  private static InputDeviceDetector? _instance;
  public static InputDeviceDetector? Instance => _instance;

  // The pad the player last pressed something on, or -1 before they have touched
  // one. Read through InputUtils.GetActiveGamepadDevice(), which falls back to a
  // connected pad when this one has been unplugged.
  public static int ActiveGamepadDevice { get; private set; } = -1;

  // Turned off while a key binding button captures its next input: the keys and
  // buttons pressed there are being bound, not being played with, and letting
  // them through would swap the settings panel to the other device halfway
  // through the capture.
  public bool Enabled { get; set; } = true;

  private Window? _window;

  public override void _EnterTree() {
    base._EnterTree();
    _instance = this;

    // Detection has to keep working while the tree is paused: the pause menu
    // and the key binding capture both pause it.
    ProcessMode = ProcessModeEnum.Always;
    SetProcess(false);
    SetPhysicsProcess(false);
    SetProcessInput(false);

    _window = GetTree().Root;
    _window.WindowInput += _onWindowInput;
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (_window != null) {
      _window.WindowInput -= _onWindowInput;
      _window = null;
    }
    if (_instance == this) {
      _instance = null;
    }
  }

  private void _onWindowInput(InputEvent @event) {
    if (!Enabled) {
      return;
    }

    var detected = _deviceOf(@event);
    if (detected == null) {
      return;
    }

    // Note the pad before announcing anything, so that a player moving from the
    // keyboard to a pad gets a single announcement with the right art already
    // resolved behind it.
    var artChanged = detected == ControllerType.Gamepad && _setActiveGamepad(@event.Device);
    var deviceKindChanged = GameSettings.LastUsedController != detected.Value;

    // Announces the change of device kind, if that is what this is.
    GameSettings.LastUsedController = detected.Value;

    // Swapping a PlayStation pad for an Xbox one (or the other way round) leaves
    // the kind of device alone, so nothing above would have said a word, yet
    // every glyph on screen is now drawn in the wrong house style.
    if (artChanged && !deviceKindChanged) {
      GameEvents.Instance.OnLastUsedControllerChanged(detected.Value);
    }
  }

  // Records the pad in the player's hands. Returns whether the art the UI should
  // be drawing changed as a result: two Xbox pads look alike, an Xbox pad and a
  // PlayStation pad do not.
  private static bool _setActiveGamepad(int device) {
    if (ActiveGamepadDevice == device) {
      return false;
    }

    var before = GamepadIconHelper.DetectControllerType();
    ActiveGamepadDevice = device;
    return GamepadIconHelper.DetectControllerType() != before;
  }

  // The device behind an event, or null when the event says nothing about which
  // one the player is holding.
  //
  // Only presses count: a key released after the player has picked up the pad
  // would otherwise flip the hints straight back to the keyboard. Mouse motion
  // is ignored for the same reason, a nudged mouse is not a change of device.
  private static ControllerType? _deviceOf(InputEvent @event) => @event switch {
    InputEventKey key when key.Pressed && !key.IsEcho() => ControllerType.Keyboard,
    InputEventMouseButton mouse when mouse.Pressed => ControllerType.Keyboard,
    InputEventJoypadButton pad when pad.Pressed => ControllerType.Gamepad,
    InputEventJoypadMotion motion when Mathf.Abs(motion.AxisValue) > AXIS_DEADZONE => ControllerType.Gamepad,
    _ => null,
  };
}
