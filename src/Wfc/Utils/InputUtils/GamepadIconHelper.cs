namespace Wfc.Utils;

using Godot;

// Helper class for mapping gamepad buttons and axes to their icon textures.
// Supports Xbox 360 and PlayStation controllers.
//
// Every icon ships twice: the default art is drawn for a light surface (a dark
// button carrying a bright symbol) and the inverted set flips that, for hints
// painted straight onto a dark scene. Callers ask for the one that suits what
// the glyph will sit on rather than picking a folder themselves.
public static class GamepadIconHelper {
    private const string IconRoot = "res://Assets/Sprites/controller/";
    private const string InvertedIconRoot = "res://Assets/Sprites/controller/inverted/";
    private const string Xbox360IconDir = "x360/";
    private const string PlayStationIconDir = "playstation/";

    // Represents the type of controller being used for icon selection.
    public enum ControllerIconType {
        Xbox360,
        PlayStation
    }

    private static string IconDirectory(ControllerIconType iconType, bool onDarkBackground) =>
        (onDarkBackground ? InvertedIconRoot : IconRoot)
        + (iconType == ControllerIconType.PlayStation ? PlayStationIconDir : Xbox360IconDir);

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
    public static Texture2D? GetButtonIcon(JoyButton button, ControllerIconType? iconType = null, bool onDarkBackground = false) {
        var type = iconType ?? DetectControllerType();
        return LoadIcon(GetButtonIconName(button, type), type, onDarkBackground);
    }

    // Gets the icon texture for a gamepad axis with direction.
    public static Texture2D? GetAxisIcon(JoyAxis axis, float direction, ControllerIconType? iconType = null, bool onDarkBackground = false) {
        var type = iconType ?? DetectControllerType();
        return LoadIcon(GetAxisIconName(axis, direction, type), type, onDarkBackground);
    }

    // Gets the icon texture for the whole directional pad (used by hints that
    // stand for "any direction", like menu navigation).
    public static Texture2D? GetDirectionalPadIcon(ControllerIconType? iconType = null, bool onDarkBackground = false) {
        var type = iconType ?? DetectControllerType();
        var iconName = type == ControllerIconType.PlayStation ? "d-pad.png" : "hat.png";
        return LoadIcon(iconName, type, onDarkBackground);
    }

    private static Texture2D? LoadIcon(string iconName, ControllerIconType iconType, bool onDarkBackground) =>
        string.IsNullOrEmpty(iconName)
            ? null
            : GD.Load<Texture2D>(IconDirectory(iconType, onDarkBackground) + iconName);

    private static string GetButtonIconName(JoyButton button, ControllerIconType iconType) {
        if (iconType == ControllerIconType.PlayStation) {
            return button switch {
                JoyButton.A => "cross.png",
                JoyButton.B => "circle.png",
                JoyButton.X => "square.png",
                JoyButton.Y => "triangle.png",
                JoyButton.LeftShoulder => "l1.png",
                JoyButton.RightShoulder => "r1.png",
                JoyButton.LeftStick => "l-stick.png",
                JoyButton.RightStick => "r-stick.png",
                JoyButton.Back => "touchpad-press.png",
                JoyButton.Start => "options.png",
                JoyButton.DpadUp => "d-pad-up.png",
                JoyButton.DpadDown => "d-pad-down.png",
                JoyButton.DpadLeft => "d-pad-left.png",
                JoyButton.DpadRight => "d-pad-right.png",
                JoyButton.Touchpad => "touchpad-press.png",
                _ => string.Empty
            };
        }
        else {
            // Xbox 360 style icons
            return button switch {
                JoyButton.A => "a.png",
                JoyButton.B => "b.png",
                JoyButton.X => "x.png",
                JoyButton.Y => "y.png",
                JoyButton.LeftShoulder => "lb.png",
                JoyButton.RightShoulder => "rb.png",
                JoyButton.LeftStick => "l-stick-btn.png",
                JoyButton.RightStick => "r-stick-btn.png",
                JoyButton.Back => "back.png",
                JoyButton.Start => "start.png",
                JoyButton.DpadUp => "hat-up.png",
                JoyButton.DpadDown => "hat-bottom.png",
                JoyButton.DpadLeft => "hat-left.png",
                JoyButton.DpadRight => "hat-right.png",
                _ => string.Empty
            };
        }
    }

    private static string GetAxisIconName(JoyAxis axis, float direction, ControllerIconType iconType) {
        if (iconType == ControllerIconType.PlayStation) {
            return axis switch {
                JoyAxis.LeftX => direction > 0 ? "l-stick-right.png" : "l-stick-left.png",
                JoyAxis.LeftY => direction > 0 ? "l-stick-down.png" : "l-stick-up.png",
                JoyAxis.RightX => direction > 0 ? "r-stick-right.png" : "r-stick-left.png",
                JoyAxis.RightY => direction > 0 ? "r-stick-down.png" : "r-stick-up.png",
                JoyAxis.TriggerLeft => "l2.png",
                JoyAxis.TriggerRight => "r2.png",
                _ => string.Empty
            };
        }
        else {
            // Xbox 360 style icons
            return axis switch {
                JoyAxis.LeftX => direction > 0 ? "l-stick-right.png" : "l-stick-left.png",
                JoyAxis.LeftY => direction > 0 ? "l-stick-down.png" : "l-stick-up.png",
                JoyAxis.RightX => direction > 0 ? "r-stick-right.png" : "r-stick-left.png",
                JoyAxis.RightY => direction > 0 ? "r-stick-down.png" : "r-stick-up.png",
                JoyAxis.TriggerLeft => "lt.png",
                JoyAxis.TriggerRight => "rt.png",
                _ => string.Empty
            };
        }
    }
}
