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

    // Detects which art to draw from the name of the pad the player is using.
    // Follows them from one pad to another: press a button on a PlayStation pad
    // while an Xbox one is also plugged in and the icons become PlayStation ones.
    public static ControllerIconType DetectControllerType() {
        var device = InputUtils.GetActiveGamepadDevice();
        return device < 0
            ? ControllerIconType.Xbox360 // Default to Xbox icons
            : IconTypeForJoyName(Input.GetJoyName(device));
    }

    // The art a pad reporting this name should be drawn with. Anything that
    // isn't recognisably a PlayStation pad is drawn Xbox style, which covers
    // the Xbox pads themselves and the many third party pads that copy them.
    public static ControllerIconType IconTypeForJoyName(string joyName) {
        // Invariant: these are device names, matched against ASCII keywords.
        var name = joyName.ToLowerInvariant();

        // Check for PlayStation controllers
        if (name.Contains("playstation") ||
            name.Contains("ps3") ||
            name.Contains("ps4") ||
            name.Contains("ps5") ||
            name.Contains("dualsense") ||
            name.Contains("dualshock") ||
            name.Contains("sony")) {
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

    // Gets the icon texture for the whole directional pad (used by hints that
    // stand for "any direction", like menu navigation).
    public static Texture2D? GetDirectionalPadIcon(ControllerIconType? iconType = null) {
        var type = iconType ?? DetectControllerType();
        var iconPath = type == ControllerIconType.PlayStation
            ? PlayStationIconPath + "d-pad.png"
            : Xbox360IconPath + "hat.png";

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
