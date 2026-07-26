namespace Wfc.Utils;

using System.Collections.Generic;
using Godot;
using Wfc.Core.Input;
using Wfc.Core.Input.Controllers;
using Wfc.Core.Settings;

public static class InputUtils {
  public static InputEventKey? GetFirstKeyKeyboardEventFromActionList(IEnumerable<InputEvent> actionList) {
    foreach (var el in actionList) {
      if (el is InputEventKey keyEvent) {
        return keyEvent;
      }
    }
    return null;
  }

  public static InputEventJoypadButton? GetFirstJoypadButtonEventFromActionList(IEnumerable<InputEvent> actionList) {
    foreach (var el in actionList) {
      if (el is InputEventJoypadButton joypadEvent) {
        return joypadEvent;
      }
    }
    return null;
  }

  public static InputEventJoypadMotion? GetFirstJoypadAxisEventFromActionList(IEnumerable<InputEvent> actionList) {
    foreach (var el in actionList) {
      if (el is InputEventJoypadMotion motionEvent) {
        return motionEvent;
      }
    }
    return null;
  }

  // Returns true if any gamepad is currently connected.
  // The device the hint bar, the glyphs and the binding rows should all speak for:
  // whatever the player last used, unless that was a gamepad and there is no longer
  // one plugged in. Three places carried their own copy of this rule.
  public static ControllerType GetEffectiveControllerType() {
    var lastUsed = GameSettings.LastUsedController;
    return lastUsed == ControllerType.Gamepad && !IsGamepadConnected()
      ? ControllerType.Keyboard
      : lastUsed;
  }

  public static bool IsGamepadConnected() {
    var connectedJoypads = Input.GetConnectedJoypads();
    return connectedJoypads.Count > 0;
  }

  // The pad whose art and name the UI should be showing: the one the player last
  // pressed something on, or, before they have touched any, the first one
  // plugged in. Returns -1 when no pad is connected.
  //
  // Which one matters as soon as two are plugged in: a PlayStation pad and an
  // Xbox pad draw their buttons with different art, and the player should see
  // the one in their hands rather than whichever the engine lists first.
  public static int GetActiveGamepadDevice() {
    var connectedJoypads = Input.GetConnectedJoypads();
    if (connectedJoypads.Count == 0) {
      return -1;
    }

    var active = InputDeviceDetector.ActiveGamepadDevice;
    return connectedJoypads.Contains(active) ? active : connectedJoypads[0];
  }

  // Gets the name of the gamepad in use, or null if none connected.
  public static string? GetConnectedGamepadName() {
    var device = GetActiveGamepadDevice();
    return device < 0 ? null : Input.GetJoyName(device);
  }

  // Converts a JoyButton to a human-readable string.
  public static string GetJoyButtonName(JoyButton button) => button switch {
    JoyButton.A => "A / Cross",
    JoyButton.B => "B / Circle",
    JoyButton.X => "X / Square",
    JoyButton.Y => "Y / Triangle",
    JoyButton.LeftShoulder => "LB / L1",
    JoyButton.RightShoulder => "RB / R1",
    JoyButton.LeftStick => "L3",
    JoyButton.RightStick => "R3",
    JoyButton.Back => "Back / Select",
    JoyButton.Start => "Start / Options",
    JoyButton.Guide => "Guide / Home",
    JoyButton.DpadUp => "D-Pad Up",
    JoyButton.DpadDown => "D-Pad Down",
    JoyButton.DpadLeft => "D-Pad Left",
    JoyButton.DpadRight => "D-Pad Right",
    JoyButton.Misc1 => "Misc",
    JoyButton.Paddle1 => "Paddle 1",
    JoyButton.Paddle2 => "Paddle 2",
    JoyButton.Paddle3 => "Paddle 3",
    JoyButton.Paddle4 => "Paddle 4",
    JoyButton.Touchpad => "Touchpad",
    _ => button.ToString()
  };

  // Converts a JoyAxis to a human-readable string including direction.
  public static string GetJoyAxisName(JoyAxis axis, float direction) {
    string axisName = axis switch {
      JoyAxis.LeftX => direction > 0 ? "Left Stick Right" : "Left Stick Left",
      JoyAxis.LeftY => direction > 0 ? "Left Stick Down" : "Left Stick Up",
      JoyAxis.RightX => direction > 0 ? "Right Stick Right" : "Right Stick Left",
      JoyAxis.RightY => direction > 0 ? "Right Stick Down" : "Right Stick Up",
      JoyAxis.TriggerLeft => "LT / L2",
      JoyAxis.TriggerRight => "RT / R2",
      _ => axis.ToString()
    };
    return axisName;
  }
}
