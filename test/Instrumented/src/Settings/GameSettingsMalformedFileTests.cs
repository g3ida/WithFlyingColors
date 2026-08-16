namespace Wfc.test.instrumented.Settings;

using System.Collections.Generic;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Settings;

// The settings file is the one input the game reads before it has a screen to complain on:
// Load runs from DependenciesProvider.OnReady, which is also the call that performs the first
// navigation. An entry it cannot read has to be dropped, never thrown on and never applied as
// whatever it happens to convert to.
public class GameSettingsMalformedFileTests(Node testScene) : TestClass(testScene) {
  private const string SCRATCH_CONFIG_PATH = "user://test-malformed-settings.ini";

  private static readonly string[] GAME_ACTIONS =
    ["move_left", "move_right", "jump", "rotate_left", "rotate_right", "pause", "dash", "down"];

  private readonly Dictionary<string, Godot.Collections.Array<InputEvent>> _savedEvents = new();
  private string _configPathBeforeTest = default!;
  private bool _fullscreenBeforeTest;
  private bool _vsyncBeforeTest;

  // Loading rebinds the InputMap and touches the window, both of which are process wide and
  // neither of which the tests that run after this one asked for.
  [Setup]
  public void Setup() {
    _savedEvents.Clear();
    foreach (var action in GAME_ACTIONS) {
      _savedEvents[action] = InputMap.ActionGetEvents(action);
    }
    _configPathBeforeTest = GameSettings.ConfigFilePath;
    _fullscreenBeforeTest = GameSettings.Fullscreen;
    _vsyncBeforeTest = GameSettings.Vsync;
    GameSettings.ConfigFilePath = SCRATCH_CONFIG_PATH;
  }

  [Cleanup]
  public void Cleanup() {
    foreach (var (action, events) in _savedEvents) {
      InputMap.ActionEraseEvents(action);
      foreach (var @event in events) {
        InputMap.ActionAddEvent(action, @event);
      }
    }
    DirAccess.RemoveAbsolute(SCRATCH_CONFIG_PATH);
    GameSettings.ConfigFilePath = _configPathBeforeTest;
    GameSettings.Fullscreen = _fullscreenBeforeTest;
    GameSettings.Vsync = _vsyncBeforeTest;
  }

  // The regression this file exists for. Every one of these reached int.Parse or float.Parse
  // with no guard around it, and the exception came out of start-up.
  [Test]
  public void AButtonBindingWithNoNumberDoesNotBringDownTheLoadTest() {
    _writeConfig(("gamepad", "jump", "button:"));

    Should.NotThrow(GameSettings.Load);
  }

  [Test]
  public void AButtonBindingThatIsNotANumberDoesNotBringDownTheLoadTest() {
    _writeConfig(("gamepad", "jump", "button:sixteen"));

    Should.NotThrow(GameSettings.Load);
  }

  [Test]
  public void AnAxisBindingThatIsNotANumberDoesNotBringDownTheLoadTest() {
    _writeConfig(("gamepad", "dash", "axis:left:hard"));

    Should.NotThrow(GameSettings.Load);
  }

  [Test]
  public void ABindingInNoKnownShapeDoesNotBringDownTheLoadTest() {
    _writeConfig(("gamepad", "dash", "whatever"));

    Should.NotThrow(GameSettings.Load);
  }

  // Dropped rather than merely survived: the remappable actions are filled back in at the end
  // of Load, so a file with one bad entry still leaves the player able to press jump.
  [Test]
  public void AnUnreadableButtonBindingFallsBackToTheDefaultTest() {
    _writeConfig(("gamepad", "jump", "button:sixteen"));

    GameSettings.Load();

    var events = InputMap.ActionGetEvents("jump");
    var bound = false;
    foreach (var @event in events) {
      if (@event is InputEventJoypadButton button
          && button.ButtonIndex == GameSettings.DefaultGamepadBindings["jump"]) {
        bound = true;
      }
    }
    bound.ShouldBeTrue("jump should still answer to the default gamepad button");
  }

  // The silent twin of the crash: a keyboard entry that is not a keycode used to be converted
  // to Key.None and bound, which erases the working default and answers to nothing.
  [Test]
  public void AKeyboardEntryThatIsNotAKeycodeKeepsTheCurrentBindingTest() {
    GameSettings.BindActionToKeyboardKey("jump", (int)Key.Space);
    _writeConfig(("keyboard", "jump", "not_a_keycode"));

    GameSettings.Load();

    _keyboardKeyOf("jump").ShouldBe(Key.Space);
  }

  // The shapes that are not malformed still have to load, or the guard above has just broken
  // everybody's bindings instead.
  [Test]
  public void AWellFormedFileStillBindsEverythingTest() {
    _writeConfig(
      ("keyboard", "jump", Variant.From((int)Key.Q)),
      ("gamepad", "jump", "button:3"),
      ("gamepad", "dash", "axis:4:-1"));

    GameSettings.Load();

    _keyboardKeyOf("jump").ShouldBe(Key.Q);
    _gamepadButtonOf("jump").ShouldBe(JoyButton.Y);
    var axis = _gamepadAxisOf("dash");
    axis.ShouldNotBeNull();
    axis.Axis.ShouldBe(JoyAxis.TriggerLeft);
    axis.AxisValue.ShouldBe(-1f);
  }

  // An empty string is how Save records an action nobody has bound, and it has always meant
  // "leave it alone" rather than "bind nothing".
  [Test]
  public void AnEmptyBindingIsNotAnUnreadableOneTest() {
    GameSettings.BindActionToKeyboardKey("jump", (int)Key.Space);
    _writeConfig(("keyboard", "jump", ""), ("gamepad", "jump", ""));

    GameSettings.Load();

    _keyboardKeyOf("jump").ShouldBe(Key.Space);
  }

  private static Key _keyboardKeyOf(string action) {
    foreach (var @event in InputMap.ActionGetEvents(action)) {
      if (@event is InputEventKey key) {
        return key.Keycode;
      }
    }
    return Key.None;
  }

  private static JoyButton _gamepadButtonOf(string action) {
    foreach (var @event in InputMap.ActionGetEvents(action)) {
      if (@event is InputEventJoypadButton button) {
        return button.ButtonIndex;
      }
    }
    return JoyButton.Invalid;
  }

  private static InputEventJoypadMotion? _gamepadAxisOf(string action) {
    foreach (var @event in InputMap.ActionGetEvents(action)) {
      if (@event is InputEventJoypadMotion motion) {
        return motion;
      }
    }
    return null;
  }

  private static void _writeConfig(params (string section, string key, Variant value)[] entries) {
    var configFile = new ConfigFile();
    foreach (var (section, key, value) in entries) {
      configFile.SetValue(section, key, value);
    }
    configFile.Save(SCRATCH_CONFIG_PATH).ShouldBe(Error.Ok);
  }
}
