namespace Wfc.test.instrumented.Settings;

using System.Collections.Generic;
using System.Linq;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Settings;

// The gamepad's directions are not the player's to rebind: the D-Pad and the left
// stick always drive them, a capture can never take those inputs for another
// action, and a mapping where one input drives two actions is caught as broken.
public class GameSettingsBindingTests(Node testScene) : TestClass(testScene) {
  private static readonly string[] GAME_ACTIONS =
    ["move_left", "move_right", "jump", "rotate_left", "rotate_right", "pause", "dash", "down"];

  private readonly Dictionary<string, Godot.Collections.Array<InputEvent>> _savedEvents = new();

  [Setup]
  public void Setup() {
    _savedEvents.Clear();
    foreach (var action in GAME_ACTIONS) {
      _savedEvents[action] = InputMap.ActionGetEvents(action);
    }
  }

  [Cleanup]
  public void Cleanup() {
    foreach (var (action, events) in _savedEvents) {
      InputMap.ActionEraseEvents(action);
      foreach (var @event in events) {
        InputMap.ActionAddEvent(action, @event);
      }
    }
  }

  [Test]
  public void FixedDirectionsCarryBothDpadAndStick() {
    GameSettings.ApplyFixedGamepadDirectionBindings();

    _assertFixedDirection("move_left", JoyButton.DpadLeft, JoyAxis.LeftX, -1);
    _assertFixedDirection("move_right", JoyButton.DpadRight, JoyAxis.LeftX, 1);
    _assertFixedDirection("down", JoyButton.DpadDown, JoyAxis.LeftY, 1);
  }

  [Test]
  public void FixedDirectionsReplaceWhateverAStaleFileLeft() {
    GameSettings.BindActionToGamepadAxis("move_right", JoyAxis.RightX, 1f);

    GameSettings.ApplyFixedGamepadDirectionBindings();

    var motions = InputMap.ActionGetEvents("move_right").OfType<InputEventJoypadMotion>();
    motions.ShouldAllBe(motion => motion.Axis == JoyAxis.LeftX);
  }

  [Test]
  public void AKeyOnTwoActionsIsADuplicate() {
    GameSettings.BindActionToKeyboardKey("dash", (int)Key.J);
    GameSettings.BindActionToKeyboardKey("jump", (int)Key.K);
    GameSettings.HasDuplicateKeyboardBindings().ShouldBeFalse("distinct keys were read as colliding");

    GameSettings.BindActionToKeyboardKey("jump", (int)Key.J);
    GameSettings.HasDuplicateKeyboardBindings().ShouldBeTrue("one key on two actions went unnoticed");
  }

  [Test]
  public void AButtonOnTwoActionsIsADuplicate() {
    GameSettings.ApplyFixedGamepadDirectionBindings();
    GameSettings.BindActionToGamepadButton("dash", JoyButton.Y);
    GameSettings.BindActionToGamepadButton("jump", JoyButton.A);
    GameSettings.HasDuplicateGamepadBindings().ShouldBeFalse("distinct buttons were read as colliding");

    GameSettings.BindActionToGamepadButton("jump", JoyButton.Y);
    GameSettings.HasDuplicateGamepadBindings().ShouldBeTrue("one button on two actions went unnoticed");
  }

  [Test]
  public void OppositeDirectionsOfOneAxisAreNotDuplicates() {
    GameSettings.ApplyFixedGamepadDirectionBindings();
    GameSettings.BindActionToGamepadAxis("rotate_left", JoyAxis.RightX, -1f);
    GameSettings.BindActionToGamepadAxis("rotate_right", JoyAxis.RightX, 1f);

    GameSettings.HasDuplicateGamepadBindings().ShouldBeFalse();
  }

  [Test]
  public void OnlyTheDirectionInputsAreReserved() {
    GameSettings.IsReservedGamepadInput(new InputEventJoypadButton { ButtonIndex = JoyButton.DpadLeft }).ShouldBeTrue();
    GameSettings.IsReservedGamepadInput(new InputEventJoypadMotion { Axis = JoyAxis.LeftY, AxisValue = 0.9f }).ShouldBeTrue();
    // Stick up belongs to nobody: there is no up action, a dash never gains height.
    GameSettings.IsReservedGamepadInput(new InputEventJoypadMotion { Axis = JoyAxis.LeftY, AxisValue = -0.9f }).ShouldBeFalse();
    GameSettings.IsReservedGamepadInput(new InputEventJoypadButton { ButtonIndex = JoyButton.DpadUp }).ShouldBeFalse();
    GameSettings.IsReservedGamepadInput(new InputEventJoypadButton { ButtonIndex = JoyButton.Y }).ShouldBeFalse();

    GameSettings.IsGamepadFixedDirectionAction("down").ShouldBeTrue();
    GameSettings.IsGamepadFixedDirectionAction("dash").ShouldBeFalse();
  }

  private static void _assertFixedDirection(string action, JoyButton button, JoyAxis axis, int axisSign) {
    var events = InputMap.ActionGetEvents(action);
    var buttons = events.OfType<InputEventJoypadButton>().ToList();
    var motions = events.OfType<InputEventJoypadMotion>().ToList();

    buttons.Count.ShouldBe(1, $"{action} should hold exactly one D-Pad binding");
    buttons[0].ButtonIndex.ShouldBe(button);
    motions.Count.ShouldBe(1, $"{action} should hold exactly one stick binding");
    motions[0].Axis.ShouldBe(axis);
    Mathf.Sign(motions[0].AxisValue).ShouldBe(axisSign);
  }
}
