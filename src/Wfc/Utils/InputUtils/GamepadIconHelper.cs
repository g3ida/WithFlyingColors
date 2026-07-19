namespace Wfc.Utils;

using Godot;

// Helper class for mapping gamepad buttons and axes to their icon textures.
// Supports Xbox 360 and PlayStation controllers.
public static class GamepadIconHelper {
    private const string Xbox360IconPath = "res://Assets/Sprites/controller/x360/";
    private const string PlayStationIconPath = "res://Assets/Sprites/controller/playstation/";

    // Represents the type of controller being used for icon selection.
    public enum ControllerIconType {
        Xbox360,
        PlayStation
    }

    // Detects the controller type based on the connected gamepad name.
    public static ControllerIconType DetectControllerType() {
        var connectedJoypads = Input.GetConnectedJoypads();
        if (connectedJoypads.Count == 0) {
            return ControllerIconType.Xbox360; // Default to Xbox icons
        }

        var joyName = Input.GetJoyName(connectedJoypads[0]).ToLower();

        // Check for PlayStation controllers
        if (joyName.Contains("playstation") ||
            joyName.Contains("ps3") ||
            joyName.Contains("ps4") ||
            joyName.Contains("ps5") ||
            joyName.Contains("dualsense") ||
            joyName.Contains("dualshock") ||
            joyName.Contains("sony")) {
            return ControllerIconType.PlayStation;
        }

        // Default to Xbox 360 style icons
        return ControllerIconType.Xbox360;
    }

    // Gets the icon texture for a gamepad button.
    public static Texture2D? GetButtonIcon(JoyButton button, ControllerIconType? iconType = null) {
        var type = iconType ?? DetectControllerType();
        var iconPath = GetButtonIconPath(button, type);

        if (string.IsNullOrEmpty(iconPath)) {
            return null;
        }

        return GD.Load<Texture2D>(iconPath);
    }

    // Gets the icon texture for a gamepad axis with direction.
    public static Texture2D? GetAxisIcon(JoyAxis axis, float direction, ControllerIconType? iconType = null) {
        var type = iconType ?? DetectControllerType();
        var iconPath = GetAxisIconPath(axis, direction, type);

        if (string.IsNullOrEmpty(iconPath)) {
            return null;
        }

        return GD.Load<Texture2D>(iconPath);
    }

    private static string GetButtonIconPath(JoyButton button, ControllerIconType iconType) {
        if (iconType == ControllerIconType.PlayStation) {
            return button switch {
                JoyButton.A => PlayStationIconPath + "cross.png",
                JoyButton.B => PlayStationIconPath + "circle.png",
                JoyButton.X => PlayStationIconPath + "square.png",
                JoyButton.Y => PlayStationIconPath + "triangle.png",
                JoyButton.LeftShoulder => PlayStationIconPath + "l1.png",
                JoyButton.RightShoulder => PlayStationIconPath + "r1.png",
                JoyButton.LeftStick => PlayStationIconPath + "l-stick.png",
                JoyButton.RightStick => PlayStationIconPath + "r-stick.png",
                JoyButton.Back => PlayStationIconPath + "touchpad-press.png",
                JoyButton.Start => PlayStationIconPath + "options.png",
                JoyButton.DpadUp => PlayStationIconPath + "d-pad-up.png",
                JoyButton.DpadDown => PlayStationIconPath + "d-pad-down.png",
                JoyButton.DpadLeft => PlayStationIconPath + "d-pad-left.png",
                JoyButton.DpadRight => PlayStationIconPath + "d-pad-right.png",
                JoyButton.Touchpad => PlayStationIconPath + "touchpad-press.png",
                _ => string.Empty
            };
        }
        else {
            // Xbox 360 style icons
            return button switch {
                JoyButton.A => Xbox360IconPath + "a.png",
                JoyButton.B => Xbox360IconPath + "b.png",
                JoyButton.X => Xbox360IconPath + "x.png",
                JoyButton.Y => Xbox360IconPath + "y.png",
                JoyButton.LeftShoulder => Xbox360IconPath + "lb.png",
                JoyButton.RightShoulder => Xbox360IconPath + "rb.png",
                JoyButton.LeftStick => Xbox360IconPath + "l-stick-btn.png",
                JoyButton.RightStick => Xbox360IconPath + "r-stick-btn.png",
                JoyButton.Back => Xbox360IconPath + "back.png",
                JoyButton.Start => Xbox360IconPath + "start.png",
                JoyButton.DpadUp => Xbox360IconPath + "hat-up.png",
                JoyButton.DpadDown => Xbox360IconPath + "hat-bottom.png",
                JoyButton.DpadLeft => Xbox360IconPath + "hat-left.png",
                JoyButton.DpadRight => Xbox360IconPath + "hat-right.png",
                _ => string.Empty
            };
        }
    }

    private static string GetAxisIconPath(JoyAxis axis, float direction, ControllerIconType iconType) {
        if (iconType == ControllerIconType.PlayStation) {
            return axis switch {
                JoyAxis.LeftX => direction > 0 ? PlayStationIconPath + "l-stick-right.png" : PlayStationIconPath + "l-stick-left.png",
                JoyAxis.LeftY => direction > 0 ? PlayStationIconPath + "l-stick-down.png" : PlayStationIconPath + "l-stick-up.png",
                JoyAxis.RightX => direction > 0 ? PlayStationIconPath + "r-stick-right.png" : PlayStationIconPath + "r-stick-left.png",
                JoyAxis.RightY => direction > 0 ? PlayStationIconPath + "r-stick-down.png" : PlayStationIconPath + "r-stick-up.png",
                JoyAxis.TriggerLeft => PlayStationIconPath + "l2.png",
                JoyAxis.TriggerRight => PlayStationIconPath + "r2.png",
                _ => string.Empty
            };
        }
        else {
            // Xbox 360 style icons
            return axis switch {
                JoyAxis.LeftX => direction > 0 ? Xbox360IconPath + "l-stick-right.png" : Xbox360IconPath + "l-stick-left.png",
                JoyAxis.LeftY => direction > 0 ? Xbox360IconPath + "l-stick-down.png" : Xbox360IconPath + "l-stick-up.png",
                JoyAxis.RightX => direction > 0 ? Xbox360IconPath + "r-stick-right.png" : Xbox360IconPath + "r-stick-left.png",
                JoyAxis.RightY => direction > 0 ? Xbox360IconPath + "r-stick-down.png" : Xbox360IconPath + "r-stick-up.png",
                JoyAxis.TriggerLeft => Xbox360IconPath + "lt.png",
                JoyAxis.TriggerRight => Xbox360IconPath + "rt.png",
                _ => string.Empty
            };
        }
    }
}
