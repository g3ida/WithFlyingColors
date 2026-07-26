namespace Wfc.test.instrumented;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Input;
using Wfc.Core.Input.Controllers;
using Wfc.Core.Settings;
using Wfc.Utils;

// Covers the detector that lets the UI follow the player from the keyboard to a
// gamepad and back. It runs against the real autoloaded detector and pushes the
// events through Input.ParseInputEvent, the same road a physical device takes,
// so it also guards the assumption the detector is built on: that the root
// window's window_input signal carries gamepad events, not just keyboard ones.
public class InputDeviceDetectorTests(Node testScene) : TestClass(testScene) {
  private ControllerType _initial;

  [Setup]
  public void Setup() => _initial = GameSettings.LastUsedController;

  [Cleanup]
  public void Cleanup() => GameSettings.LastUsedController = _initial;

  [Test]
  public async Task ReportsGamepadWhenAPadButtonIsPressed() {
    GameSettings.LastUsedController = ControllerType.Keyboard;

    await _send(new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = true });

    GameSettings.LastUsedController.ShouldBe(ControllerType.Gamepad);
  }

  [Test]
  public async Task ReportsGamepadWhenAStickIsPushed() {
    GameSettings.LastUsedController = ControllerType.Keyboard;

    await _send(new InputEventJoypadMotion { Axis = JoyAxis.LeftX, AxisValue = 1f });

    GameSettings.LastUsedController.ShouldBe(ControllerType.Gamepad);
  }

  [Test]
  public async Task ReportsKeyboardWhenAKeyIsPressed() {
    GameSettings.LastUsedController = ControllerType.Gamepad;

    await _send(new InputEventKey { Keycode = Key.Space, Pressed = true });

    GameSettings.LastUsedController.ShouldBe(ControllerType.Keyboard);
  }

  // A key let go of after the player has already picked up the pad must not drag
  // the hints back to the keyboard.
  [Test]
  public async Task IgnoresKeyReleases() {
    GameSettings.LastUsedController = ControllerType.Gamepad;

    await _send(new InputEventKey { Keycode = Key.Space, Pressed = false });

    GameSettings.LastUsedController.ShouldBe(ControllerType.Gamepad);
  }

  // A pad resting on a table is not the player reaching for it.
  [Test]
  public async Task IgnoresStickDrift() {
    GameSettings.LastUsedController = ControllerType.Keyboard;

    await _send(new InputEventJoypadMotion { Axis = JoyAxis.LeftX, AxisValue = 0.1f });

    GameSettings.LastUsedController.ShouldBe(ControllerType.Keyboard);
  }

  // With a PlayStation pad and an Xbox pad both plugged in, the glyphs have to
  // follow the one being pressed rather than whichever the engine lists first.
  [Test]
  public async Task FollowsThePadThatWasPressed() {
    await _send(new InputEventJoypadButton { Device = 3, ButtonIndex = JoyButton.A, Pressed = true });

    InputDeviceDetector.ActiveGamepadDevice.ShouldBe(3);
  }

  // A key press says nothing about which pad is the interesting one.
  [Test]
  public async Task LeavesTheActivePadAloneOnKeyboardInput() {
    await _send(new InputEventJoypadButton { Device = 3, ButtonIndex = JoyButton.A, Pressed = true });

    await _send(new InputEventKey { Keycode = Key.Space, Pressed = true });

    InputDeviceDetector.ActiveGamepadDevice.ShouldBe(3);
  }

  // Unplugging the pad that was in use falls back to one that is still there,
  // rather than leaving the UI drawing a device that has gone.
  [Test]
  public async Task FallsBackToAConnectedPadWhenTheActiveOneIsGone() {
    await _send(new InputEventJoypadButton { Device = 99, ButtonIndex = JoyButton.A, Pressed = true });

    var connected = Input.GetConnectedJoypads();
    var expected = connected.Count > 0 ? connected[0] : -1;
    InputUtils.GetActiveGamepadDevice().ShouldBe(expected);
  }

  // The Xbox / PlayStation split the icons hang off, checked against the names
  // the pads actually report.
  [Test]
  public void MapsPadNamesToTheirHouseStyle() {
    GamepadIconHelper.IconTypeForJoyName("Sony DualSense Wireless Controller")
        .ShouldBe(GamepadIconHelper.ControllerIconType.PlayStation);
    GamepadIconHelper.IconTypeForJoyName("PS5 Controller")
        .ShouldBe(GamepadIconHelper.ControllerIconType.PlayStation);
    GamepadIconHelper.IconTypeForJoyName("Xbox 360 Controller")
        .ShouldBe(GamepadIconHelper.ControllerIconType.Xbox360);
    GamepadIconHelper.IconTypeForJoyName("Xbox Series X Controller")
        .ShouldBe(GamepadIconHelper.ControllerIconType.Xbox360);
  }

  // The two make their buttons out of different art; if they resolved to the
  // same texture the switch would be invisible however well it was detected.
  [Test]
  public void DrawsTheTwoHouseStylesWithDifferentArt() {
    var ps = GamepadIconHelper.GetButtonIcon(JoyButton.A, GamepadIconHelper.ControllerIconType.PlayStation);
    var xbox = GamepadIconHelper.GetButtonIcon(JoyButton.A, GamepadIconHelper.ControllerIconType.Xbox360);

    ps.ShouldNotBeNull();
    xbox.ShouldNotBeNull();
    ps.ResourcePath.ShouldNotBe(xbox.ResourcePath);
  }

  // Godot buffers parsed events and flushes them at the top of a frame, so the
  // answer is only in once a couple of frames have gone by.
  private async Task _send(InputEvent @event) {
    Input.ParseInputEvent(@event);
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
  }
}
