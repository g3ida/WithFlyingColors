namespace Wfc.Utils;

using System.Collections.Generic;
using Godot;

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
  public static bool IsGamepadConnected() {
    var connectedJoypads = Input.GetConnectedJoypads();
    return connectedJoypads.Count > 0;
  }

  // Gets the name of the first connected gamepad, or null if none connected.
  public static string? GetConnectedGamepadName() {
    var connectedJoypads = Input.GetConnectedJoypads();
    if (connectedJoypads.Count > 0) {
      return Input.GetJoyName(connectedJoypads[0]);
    }
    return null;
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
