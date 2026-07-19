namespace Wfc.Core.Input;

using Godot;
using Wfc.Core.Input.Controllers;
using Wfc.Core.Settings;
using EventHandler = Wfc.Core.Event.EventHandler;

// Monitors input events to detect which input device (keyboard or gamepad) was last used.
// This is used to automatically switch the displayed controller type in the settings UI.
public partial class InputDeviceDetector : Node {
    private static InputDeviceDetector? _instance;
    public static InputDeviceDetector Instance => _instance!;

    // Signal emitted when the last used controller type changes.
    [Signal]
    public delegate void LastUsedControllerChangedEventHandler(int controllerType);

    public override void _EnterTree() {
        base._EnterTree();
        _instance = this;
    }

    public override void _Input(InputEvent @event) {
        base._Input(@event);

        ControllerType? detectedType = null;

        if (@event is InputEventKey || @event is InputEventMouseButton) {
            detectedType = ControllerType.Keyboard;
        }
        else if (@event is InputEventJoypadButton || @event is InputEventJoypadMotion motion && Mathf.Abs(motion.AxisValue) > 0.5f) {
            detectedType = ControllerType.Gamepad;
        }

        if (detectedType != null && GameSettings.LastUsedController != detectedType.Value) {
            GameSettings.LastUsedController = detectedType.Value;
            EmitSignal(SignalName.LastUsedControllerChanged, (int)detectedType.Value);
        }
    }
}
