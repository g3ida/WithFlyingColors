namespace Wfc.Core.Settings;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Microsoft.VisualBasic;
using Wfc.Core.Input.Controllers;
using Wfc.Core.Localization;
using Wfc.Utils;
using EventHandler = Wfc.Core.Event.EventHandler;

public static class GameSettings {
  // Where the settings live. The instrumented tests point this at a scratch
  // file so a suite that saves can never overwrite the developer's real one.
  public static string ConfigFilePath { get; set; } = "settings.ini";
  private const float MaxVolume = 0f;
  private const float MinVolume = -50f;

  /// <summary>
  /// Tracks the last used controller type (keyboard or gamepad).
  /// This is updated automatically when input is detected (see InputDeviceDetector)
  /// and saved to settings.
  /// Assigning a different value raises Events.LastUsedControllerChanged, which is
  /// how the input hints and the controller settings follow the player from one
  /// device to the other.
  /// </summary>
  private static ControllerType _lastUsedController = ControllerType.Keyboard;
  public static ControllerType LastUsedController {
    get => _lastUsedController;
    set {
      if (_lastUsedController == value) {
        return;
      }
      _lastUsedController = value;
      // Null while the settings are loaded, before the autoloads are in the tree.
      EventHandler.Instance?.EmitLastUsedControllerChanged(value);
    }
  }

  /// <summary>
  /// Default gamepad bindings for the remappable game actions.
  /// </summary>
  public static readonly Dictionary<string, JoyButton> DefaultGamepadBindings = new() {
    { "jump", JoyButton.A },
    { "rotate_left", JoyButton.LeftShoulder },
    { "rotate_right", JoyButton.RightShoulder },
    { "dash", JoyButton.X },
    { "pause", JoyButton.Start },
  };

  // The directional actions are not gamepad-remappable: the D-Pad and the left
  // stick both always drive them, so aiming a dash never depends on which of the
  // two the player's thumb happens to be on. Kept out of the settings file for
  // the same reason.
  private static readonly (string action, JoyButton button, JoyAxis axis, float axisValue)[] _fixedGamepadDirections = {
    ("move_left", JoyButton.DpadLeft, JoyAxis.LeftX, -1f),
    ("move_right", JoyButton.DpadRight, JoyAxis.LeftX, 1f),
    ("down", JoyButton.DpadDown, JoyAxis.LeftY, 1f),
  };

  public static bool IsGamepadFixedDirectionAction(string action) =>
    _fixedGamepadDirections.Any(d => d.action == action);

  /// <summary>
  /// Whether this event belongs to the fixed direction set and so can never be
  /// captured as a binding for anything else.
  /// </summary>
  public static bool IsReservedGamepadInput(InputEvent @event) => @event switch {
    InputEventJoypadButton button => _fixedGamepadDirections.Any(d => d.button == button.ButtonIndex),
    InputEventJoypadMotion motion => _fixedGamepadDirections.Any(
      d => d.axis == motion.Axis && Mathf.Sign(d.axisValue) == Mathf.Sign(motion.AxisValue)),
    _ => false,
  };

  /// <summary>
  /// Rebinds the D-Pad and left stick to the directional actions, replacing
  /// whatever a settings file from before the directions were fixed put there.
  /// </summary>
  public static void ApplyFixedGamepadDirectionBindings() {
    foreach (var (action, button, axis, axisValue) in _fixedGamepadDirections) {
      UnbindActionGamepad(action);
      // Button first: everything that shows "the" binding for an action reads
      // the first event of its kind, and the D-Pad glyph is the readable one.
      InputMap.ActionAddEvent(action, new InputEventJoypadButton { ButtonIndex = button });
      InputMap.ActionAddEvent(action, new InputEventJoypadMotion { Axis = axis, AxisValue = axisValue });
    }
  }

  private static Language? _cachedLanguage;
  public static Language Language {
    get {
      var language = _cachedLanguage ??= _parseSystemLanguage();
      _cachedLanguage = language;
      return language;
    }
    set {
      if (_cachedLanguage == value)
        return;
      _cachedLanguage = value;
      TranslationServer.SetLocale(value.GetLanguageCode());
    }
  }

  public static bool Vsync {
    get {
      switch (DisplayServer.WindowGetVsyncMode()) {
        case DisplayServer.VSyncMode.Disabled:
          return false;
        case DisplayServer.VSyncMode.Enabled:
        case DisplayServer.VSyncMode.Mailbox:
        case DisplayServer.VSyncMode.Adaptive:
          return true;
      }
      return false;

    }
    set => DisplayServer.WindowSetVsyncMode(value ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
  }

  public static bool Fullscreen {
    get => DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen;
    set => DisplayServer.WindowSetMode(value ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
  }

  public static Vector2I WindowSize {
    get => DisplayServer.WindowGetSize();
    // fixme: github issue: https://github.com/godotengine/godot/issues/105597
    set {
      // A settings file written on a larger monitor would otherwise ask for a
      // window that does not fit on this one.
      var screenSize = DisplayServer.ScreenGetSize(DisplayServer.WindowGetCurrentScreen());
      DisplayServer.WindowSetSize(value.Clamp(Vector2I.One, screenSize));
      _centerWindowOnScreen();
    }
  }

  // Resizing leaves the window pinned to the bottom right of the screen on Linux,
  // so every size change has to put it back in the middle. That includes the one
  // done while the settings are read, which is why the game came up in the corner
  // on a cold start. Does what Window.MoveToCenter does, without needing the
  // Window and the scene tree that the settings do not have.
  private static void _centerWindowOnScreen() {
    if (DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Windowed) {
      return;
    }
    var screen = DisplayServer.ScreenGetUsableRect(DisplayServer.WindowGetCurrentScreen());
    if (screen.Size == Vector2I.Zero) {
      return;
    }
    var windowSize = DisplayServer.WindowGetSizeWithDecorations();
    DisplayServer.WindowSetPosition(screen.Position + ((screen.Size - windowSize) / 2));
  }

  public static float SfxVolume {
    get => _getNormalizedAudioBusVolume("sfx");
    set => _setAudioBusVolume("sfx", value);
  }

  public static float MusicVolume {
    get => _getNormalizedAudioBusVolume("music");
    set => _setAudioBusVolume("music", value);
  }

  private static float _getVolumeInDb(float volume) {
    float vol = (MaxVolume - MinVolume) * volume + MinVolume;
    return Mathf.Clamp(vol, MinVolume, MaxVolume);
  }

  private static float _getVolumeFromDb(float volumeDb) {
    float vol = -(volumeDb / MinVolume) + 1.0f;
    return Mathf.Clamp(vol, 0.0f, 1.0f);
  }

  private static void _setAudioBusVolume(string busName, float volume) {
    float vol = _getVolumeInDb(volume);
    int musicBusIndex = AudioServer.GetBusIndex(busName);
    if (vol != MinVolume) {
      AudioServer.SetBusMute(musicBusIndex, false);
      AudioServer.SetBusVolumeDb(musicBusIndex, vol);
    }
    else {
      AudioServer.SetBusMute(musicBusIndex, true);
    }
  }

  private static float _getNormalizedAudioBusVolume(string busName) {
    int musicBusIndex = AudioServer.GetBusIndex(busName);
    float volumeDb = AudioServer.GetBusVolumeDb(musicBusIndex);
    return _getVolumeFromDb(volumeDb);
  }

  private static Language _parseSystemLanguage() {
    var locale = TranslationServer.GetLocale();
    var languageCodeStr = locale?.Split('-')[0] ?? Language.English.GetLanguageCode();
    return languageCodeStr.LanguageCodeToLanguage();
  }

  public static void BindActionToKeyboardKey(string action, int scanCode) {
    // Erase the current action:
    var actionList = InputMap.ActionGetEvents(action).Cast<InputEvent>();
    var inputEvent = InputUtils.GetFirstKeyKeyboardEventFromActionList(actionList);
    if (inputEvent != null) {
      var inputKeyEvent = inputEvent as InputEventKey;
      InputMap.ActionEraseEvent(action, inputKeyEvent);
    }

    // Add the new action:
    var newKey = new InputEventKey {
      Keycode = (Godot.Key)scanCode
    };
    InputMap.ActionAddEvent(action, newKey);
  }

  public static void UnbindActionKey(string action) {
    // Erase the current action:
    var actionList = InputMap.ActionGetEvents(action).Cast<InputEvent>();
    var inputEvent = InputUtils.GetFirstKeyKeyboardEventFromActionList(actionList);
    if (inputEvent != null) {
      var inputKeyEvent = inputEvent as InputEventKey;
      InputMap.ActionEraseEvent(action, inputKeyEvent);
    }
  }

  /// <summary>
  /// Binds a gamepad button to an action.
  /// </summary>
  public static void BindActionToGamepadButton(string action, JoyButton button) {
    // Erase the current gamepad button action:
    var actionList = InputMap.ActionGetEvents(action).Cast<InputEvent>();
    var inputEvent = InputUtils.GetFirstJoypadButtonEventFromActionList(actionList);
    if (inputEvent != null) {
      InputMap.ActionEraseEvent(action, inputEvent);
    }
    // Also erase any axis binding for this action
    var axisEvent = InputUtils.GetFirstJoypadAxisEventFromActionList(actionList);
    if (axisEvent != null) {
      InputMap.ActionEraseEvent(action, axisEvent);
    }

    // Add the new gamepad button action:
    var newButton = new InputEventJoypadButton {
      ButtonIndex = button
    };
    InputMap.ActionAddEvent(action, newButton);
  }

  /// <summary>
  /// Binds a gamepad axis (with direction) to an action.
  /// </summary>
  public static void BindActionToGamepadAxis(string action, JoyAxis axis, float axisValue) {
    // Erase the current gamepad button action:
    var actionList = InputMap.ActionGetEvents(action).Cast<InputEvent>();
    var inputEvent = InputUtils.GetFirstJoypadButtonEventFromActionList(actionList);
    if (inputEvent != null) {
      InputMap.ActionEraseEvent(action, inputEvent);
    }
    // Also erase any existing axis binding
    var axisEvent = InputUtils.GetFirstJoypadAxisEventFromActionList(actionList);
    if (axisEvent != null) {
      InputMap.ActionEraseEvent(action, axisEvent);
    }

    // Add the new gamepad axis action:
    var newAxis = new InputEventJoypadMotion {
      Axis = axis,
      AxisValue = axisValue
    };
    InputMap.ActionAddEvent(action, newAxis);
  }

  /// <summary>
  /// Unbinds any gamepad binding from an action.
  /// </summary>
  public static void UnbindActionGamepad(string action) {
    var actionList = InputMap.ActionGetEvents(action).Cast<InputEvent>();
    var buttonEvent = InputUtils.GetFirstJoypadButtonEventFromActionList(actionList);
    if (buttonEvent != null) {
      InputMap.ActionEraseEvent(action, buttonEvent);
    }
    var axisEvent = InputUtils.GetFirstJoypadAxisEventFromActionList(actionList);
    if (axisEvent != null) {
      InputMap.ActionEraseEvent(action, axisEvent);
    }
  }

  private static List<string> _getGameActions() {
    var actions = InputMap.GetActions().Cast<StringName>();
    var gameActions = new List<string>();
    foreach (var action in actions) {
      if (action.ToString().Find("ui_") == -1) {
        gameActions.Add(action);
      }
    }
    return gameActions;
  }

  public static bool AreActionKeysValid() {
    var gameActions = _getGameActions();
    foreach (var action in gameActions) {
      var actionList = InputMap.ActionGetEvents(action).Cast<InputEvent>();
      if (InputUtils.GetFirstKeyKeyboardEventFromActionList(actionList) == null) {
        return false;
      }
    }
    return true;
  }

  /// <summary>
  /// Checks if all game actions have valid gamepad bindings (button or axis).
  /// </summary>
  public static bool AreGamepadBindingsValid() {
    var gameActions = _getGameActions();
    foreach (var action in gameActions) {
      var actionList = InputMap.ActionGetEvents(action).Cast<InputEvent>();
      var hasButton = InputUtils.GetFirstJoypadButtonEventFromActionList(actionList) != null;
      var hasAxis = InputUtils.GetFirstJoypadAxisEventFromActionList(actionList) != null;
      if (!hasButton && !hasAxis) {
        return false;
      }
    }
    return true;
  }

  /// <summary>
  /// Checks that no key drives two game actions. A mapping can be fully bound and
  /// still broken this way, so this is validated separately from the checks above.
  /// </summary>
  public static bool HasDuplicateKeyboardBindings() {
    var seen = new HashSet<Key>();
    foreach (var action in _getGameActions()) {
      var actionList = InputMap.ActionGetEvents(action).Cast<InputEvent>();
      var keyEvent = InputUtils.GetFirstKeyKeyboardEventFromActionList(actionList);
      if (keyEvent != null && !seen.Add(keyEvent.Keycode)) {
        return true;
      }
    }
    return false;
  }

  /// <summary>
  /// Checks that no button or axis direction drives two game actions.
  /// </summary>
  public static bool HasDuplicateGamepadBindings() {
    var seen = new HashSet<string>();
    foreach (var action in _getGameActions()) {
      foreach (var @event in InputMap.ActionGetEvents(action).Cast<InputEvent>()) {
        var signature = @event switch {
          InputEventJoypadButton button => $"button:{(int)button.ButtonIndex}",
          InputEventJoypadMotion motion => $"axis:{(int)motion.Axis}:{Mathf.Sign(motion.AxisValue)}",
          _ => null,
        };
        if (signature != null && !seen.Add(signature)) {
          return true;
        }
      }
    }
    return false;
  }

  public static void Save() {
    // Save game actions:
    var configFile = new ConfigFile();

    var gameActions = _getGameActions();
    foreach (var action in gameActions) {
      var key = action;
      var actionList = InputMap.ActionGetEvents(key).Cast<InputEvent>();

      // Save keyboard bindings
      var keyValue = InputUtils.GetFirstKeyKeyboardEventFromActionList(actionList);
      if (keyValue != null) {
        configFile.SetValue("keyboard", key, Variant.From<int>((int)keyValue.Keycode));
      }
      else {
        configFile.SetValue("keyboard", key, "");
      }

      // The fixed directions never reach the file: what a stale entry would say
      // is applied over anyway.
      if (IsGamepadFixedDirectionAction(key)) {
        continue;
      }

      // Save gamepad button bindings
      var gamepadButton = InputUtils.GetFirstJoypadButtonEventFromActionList(actionList);
      if (gamepadButton != null) {
        configFile.SetValue("gamepad", key, $"button:{(int)gamepadButton.ButtonIndex}");
      }
      else {
        // Check for axis binding
        var gamepadAxis = InputUtils.GetFirstJoypadAxisEventFromActionList(actionList);
        if (gamepadAxis != null) {
          configFile.SetValue("gamepad", key, $"axis:{(int)gamepadAxis.Axis}:{gamepadAxis.AxisValue}");
        }
        else {
          configFile.SetValue("gamepad", key, "");
        }
      }
    }

    // Display settings:
    configFile.SetValue("display", "fullscreen", Fullscreen);
    configFile.SetValue("display", "vsync", Vsync);
    configFile.SetValue("display", "resolution", $"{WindowSize.X}x{WindowSize.Y}");

    // Audio settings:
    configFile.SetValue("audio", "sfx_volume", SfxVolume);
    configFile.SetValue("audio", "music_volume", MusicVolume);

    // General settings:
    configFile.SetValue("general", "language", Language.GetLanguageCode());
    configFile.SetValue("general", "last_controller", (int)LastUsedController);

    configFile.Save(ConfigFilePath);
  }

  /// <summary>
  /// Applies default gamepad bindings for actions that don't have gamepad bindings yet.
  /// </summary>
  public static void ApplyDefaultGamepadBindings() {
    foreach (var (action, button) in DefaultGamepadBindings) {
      var actionList = InputMap.ActionGetEvents(action).Cast<InputEvent>();
      var existingButton = InputUtils.GetFirstJoypadButtonEventFromActionList(actionList);
      var existingAxis = InputUtils.GetFirstJoypadAxisEventFromActionList(actionList);

      // Only apply default if no gamepad binding exists
      if (existingButton == null && existingAxis == null) {
        BindActionToGamepadButton(action, button);
      }
    }
  }

  public static void Load() {
    var configFile = new ConfigFile();
    if (configFile.Load(ConfigFilePath) == Error.Ok) {
      // Keyboard settings:
      if (configFile.HasSection("keyboard")) {
        foreach (string key in configFile.GetSectionKeys("keyboard")) {
          var keyValue = configFile.GetValue("keyboard", key);
          if ((keyValue.VariantType != Variant.Type.String) || keyValue.As<string>() != "") {
            BindActionToKeyboardKey(key, keyValue.As<int>());
          }
        }
      }
      // Gamepad settings:
      if (configFile.HasSection("gamepad")) {
        foreach (string action in configFile.GetSectionKeys("gamepad")) {
          // Files written before the directions were fixed carry entries for them.
          if (IsGamepadFixedDirectionAction(action)) {
            continue;
          }
          var bindingValue = configFile.GetValue("gamepad", action).As<string>();
          if (!string.IsNullOrEmpty(bindingValue)) {
            if (bindingValue.StartsWith("button:")) {
              var buttonIndex = int.Parse(bindingValue.Substring(7));
              BindActionToGamepadButton(action, (JoyButton)buttonIndex);
            }
            else if (bindingValue.StartsWith("axis:")) {
              var parts = bindingValue.Substring(5).Split(':');
              if (parts.Length == 2) {
                var axisIndex = int.Parse(parts[0]);
                var axisValue = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                BindActionToGamepadAxis(action, (JoyAxis)axisIndex, axisValue);
              }
            }
          }
        }
      }
      // Display settings:
      if (configFile.HasSection("display")) {
        foreach (string key in configFile.GetSectionKeys("display")) {
          var keyValue = configFile.GetValue("display", key);
          if (key == "fullscreen") {
            Fullscreen = keyValue.As<bool>();
          }
          else if (key == "vsync") {
            Vsync = keyValue.As<bool>();
          }
          else if (key == "resolution") {
            var values = keyValue.As<string>().Split('x');
            // A degenerate size is a corrupt entry (a save made without a real
            // window writes the window's zero size); applying it would leave
            // the game with no window to see.
            if (values.Length == 2
                && int.TryParse(values[0], out var width) && width > 0
                && int.TryParse(values[1], out var height) && height > 0) {
              WindowSize = new Vector2I(width, height);
            }
          }
        }
      }
      // Audio settings:
      if (configFile.HasSection("audio")) {
        foreach (string key in configFile.GetSectionKeys("audio")) {
          var keyValue = configFile.GetValue("audio", key);
          if (key == "sfx_volume") {
            SfxVolume = keyValue.As<float>();
          }
          else if (key == "music_volume") {
            MusicVolume = keyValue.As<float>();
          }
        }
      }
      // General settings:
      if (configFile.HasSection("general")) {
        foreach (string key in configFile.GetSectionKeys("general")) {
          var keyValue = configFile.GetValue("general", key);
          if (key == "language") {
            Language = keyValue.As<string>().LanguageCodeToLanguage();
          }
          else if (key == "last_controller") {
            LastUsedController = (ControllerType)keyValue.As<int>();
          }
        }
      }

      // Apply default gamepad bindings if not already set
      ApplyDefaultGamepadBindings();
      ApplyFixedGamepadDirectionBindings();
    }
    else // Default settings if settings file does not exist:
    {
      Fullscreen = true;
      Vsync = true;
      // Apply default gamepad bindings for new installations
      ApplyDefaultGamepadBindings();
      ApplyFixedGamepadDirectionBindings();
    }
  }
}
