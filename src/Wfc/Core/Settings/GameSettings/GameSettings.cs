namespace Wfc.Core.Settings;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input.Controllers;
using Wfc.Core.Localization;
using Wfc.Core.Logger;
using Wfc.Skin;
using Wfc.Utils;

public static class GameSettings {
  // Where the settings live. The instrumented tests point this at a scratch
  // file so a suite that saves can never overwrite the developer's real one.
  public static string ConfigFilePath { get; set; } = DEFAULT_CONFIG_PATH;

  // user:// rather than a bare filename: Godot resolves a relative path against the process
  // working directory, which is the project folder while developing but wherever the player
  // launched from in an installed build - often somewhere unwritable, and never per-user.
  public const string DEFAULT_CONFIG_PATH = "user://settings.ini";

  // Where they were kept before that. Read once, when there is nothing at the new path, so a
  // player who already has settings keeps them rather than being reset to defaults.
  private const string LEGACY_CONFIG_PATH = "settings.ini";
  private const float MaxVolume = 0f;
  private const float MinVolume = -50f;

  // Long enough for a swap chain rebuilt on a substituted V-Sync mode to have reported it.
  private const double VSYNC_FALLBACK_GRACE_SECONDS = 0.25;

  // Headroom over the display's own rate, for the frame limit that stands in for vsync. Enough
  // that turning vsync off still buys the latency it was turned off for, little enough that the
  // renderer cannot run away from the display engine.
  private const int UNCAPPED_REFRESH_MULTIPLE = 3;

  // What to pace against where the platform will not say what the display runs at.
  private const int ASSUMED_REFRESH_RATE = 60;

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
      GameEvents.Instance.OnLastUsedControllerChanged(value);
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

  /// <summary>
  /// Whether the settings file named a language when it was read. False marks a launch
  /// as a first one: the language in use otherwise came off the system, guessed rather
  /// than chosen.
  ///
  /// A snapshot of the file as loaded, deliberately not kept up to date by Save. The
  /// first-run screens save on their way out - the language screen's save writes a
  /// palette too - and a flag that followed the file would report the next question as
  /// already answered before it had been asked.
  /// </summary>
  public static bool HasStoredLanguage { get; private set; }

  /// <summary>
  /// Whether the settings file named a palette when it was read, on the same terms as
  /// <see cref="HasStoredLanguage"/>.
  /// </summary>
  public static bool HasStoredSkin { get; private set; }

  /// <summary>
  /// The palette the game draws itself in. Held by SkinManager, which every drawing
  /// node already reads; this is only the way in and out of the settings file.
  /// </summary>
  public static string Skin {
    get => SkinManager.Instance.CurrentSkinName;
    set => SkinManager.Instance.SetCurrentSkin(value);
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

  // The display server reports the mode the driver settled on, not the one it was handed, so
  // asking it what the player chose gives the wrong answer wherever a request was substituted.
  private static bool _vsync = true;

  // Turning vsync off asks for latency, not for tearing. Mailbox renders uncapped the way
  // Disabled does but hands the display the newest finished frame at each refresh, which is
  // the only way to have both; where the driver has no Mailbox to give it is Disabled, and
  // the tearing comes back.
  //
  // Either way the frame rate is capped. Presenting with no limit at all does not merely waste
  // the frames the display never shows - it stalls on the display engine for whole frames at a
  // time, which is the one thing the player turned vsync off to avoid.
  public static bool Vsync {
    get => _vsync;
    set {
      _vsync = value;
      Engine.MaxFps = value ? 0 : _uncappedFrameLimit();
      DisplayServer.WindowSetVsyncMode(
        value ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Mailbox);
      if (!value) {
        _reconcileVsyncFallback();
      }
    }
  }

  // Not every platform knows what its display runs at; those report nothing positive.
  private static int _uncappedFrameLimit() {
    var refreshRate = DisplayServer.ScreenGetRefreshRate(DisplayServer.WindowGetCurrentScreen());
    var refresh = refreshRate > 0.0f ? Mathf.RoundToInt(refreshRate) : ASSUMED_REFRESH_RATE;
    return refresh * UNCAPPED_REFRESH_MULTIPLE;
  }

  // A driver that cannot honour Mailbox falls back to Enabled, and only says so once it has
  // rebuilt its swap chain a couple of frames later. Left alone that reads as the player
  // having asked for vsync, so the request is checked back once the fallback would be visible
  // and Disabled applied instead - which is what turning vsync off asked for.
  private static void _reconcileVsyncFallback() {
    if (Engine.GetMainLoop() is not SceneTree tree) {
      return;
    }
    var timer = tree.CreateTimer(VSYNC_FALLBACK_GRACE_SECONDS, processAlways: true, processInPhysics: false, ignoreTimeScale: true);
    timer.Timeout += () => {
      if (!_vsync && DisplayServer.WindowGetVsyncMode() != DisplayServer.VSyncMode.Mailbox) {
        DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
      }
    };
  }

  public static bool Fullscreen {
    get => DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen;
    set => DisplayServer.WindowSetMode(value ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
  }

  /// <summary>
  /// Whether the player may drag the window's edges. Says nothing in fullscreen,
  /// which has no edges to take hold of, but is remembered for the return to
  /// windowed. What a drag is allowed to end at is WindowAspectGuard's to say.
  /// </summary>
  public static bool Resizable {
    get => !DisplayServer.WindowGetFlag(DisplayServer.WindowFlags.ResizeDisabled);
    set => DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.ResizeDisabled, !value);
  }

  /// <summary>
  /// Whether the frame timing overlay is drawn over the game. The overlay belongs to the
  /// game screen, so this says nothing about the menus.
  ///
  /// Assigning announces IGameEvents.PerformanceOverlayToggled, which is how the overlay
  /// follows a player who ticks the box from the pause menu, with a level already on screen.
  /// </summary>
  private static bool _performanceOverlay;
  public static bool PerformanceOverlay {
    get => _performanceOverlay;
    set {
      if (_performanceOverlay == value) {
        return;
      }
      _performanceOverlay = value;
      GameEvents.Instance.OnPerformanceOverlayToggled(value);
    }
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

  // The setting lives on the bus in two parts, and both are written every time. The bottom of
  // the range is quiet rather than silent, so turning a slider all the way down has to mute as
  // well - and a bus muted without its volume following it down keeps whatever it was set to
  // before, which is what the getter below would then read back.
  private static void _setAudioBusVolume(string busName, float volume) {
    var busIndex = AudioServer.GetBusIndex(busName);
    AudioServer.SetBusVolumeDb(busIndex, _getVolumeInDb(volume));
    AudioServer.SetBusMute(busIndex, volume <= 0f);
  }

  // Muted is the bottom of the range rather than a state beside it: a slider has one position
  // to show, and Save reads the setting back out through here.
  private static float _getNormalizedAudioBusVolume(string busName) {
    var busIndex = AudioServer.GetBusIndex(busName);
    if (AudioServer.IsBusMute(busIndex)) {
      return 0f;
    }
    return _getVolumeFromDb(AudioServer.GetBusVolumeDb(busIndex));
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
    configFile.SetValue("display", "resizable", Resizable);
    configFile.SetValue("display", "performance_overlay", PerformanceOverlay);

    // Audio settings:
    configFile.SetValue("audio", "sfx_volume", SfxVolume);
    configFile.SetValue("audio", "music_volume", MusicVolume);

    // General settings:
    configFile.SetValue("general", "language", Language.GetLanguageCode());
    configFile.SetValue("general", "skin", Skin);
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

  // Load runs before the first screen exists, so neither of these throws on an entry it cannot
  // read: a dropped binding falls back to whatever the action already carries, while an
  // exception here takes start-up with it and leaves nowhere to correct the file from.
  private static void _bindKeyboardFromFile(string action, Variant value) {
    // A keycode is written as an integer and an unbound action as an empty string. Anything
    // else converts to Key.None, which binds and passes validation while answering to no key.
    if (value.VariantType == Variant.Type.Int) {
      BindActionToKeyboardKey(action, value.As<int>());
      return;
    }
    if (value.As<string>() != "") {
      Log.Warning($"Ignoring unreadable keyboard binding for '{action}'; keeping the current one.");
    }
  }

  private static void _bindGamepadFromFile(string action, string binding) {
    if (string.IsNullOrEmpty(binding)) {
      return;
    }

    if (binding.StartsWith("button:")) {
      if (int.TryParse(binding[7..], out var buttonIndex)) {
        BindActionToGamepadButton(action, (JoyButton)buttonIndex);
        return;
      }
    }
    else if (binding.StartsWith("axis:")) {
      var parts = binding[5..].Split(':');
      if (parts.Length == 2
          && int.TryParse(parts[0], out var axisIndex)
          && float.TryParse(parts[1], System.Globalization.CultureInfo.InvariantCulture, out var axisValue)) {
        BindActionToGamepadAxis(action, (JoyAxis)axisIndex, axisValue);
        return;
      }
    }

    Log.Warning($"Ignoring unreadable gamepad binding for '{action}': '{binding}'.");
  }

  // Falls back to the pre-user:// location, and only ever for the real settings path: the
  // tests point ConfigFilePath at a scratch file, and a fallback there would quietly read the
  // developer's own settings into a test that asked for a launch with no file at all.
  private static Error _loadConfigFile(ConfigFile configFile) {
    var error = configFile.Load(ConfigFilePath);
    if (error == Error.Ok || ConfigFilePath != DEFAULT_CONFIG_PATH) {
      return error;
    }
    return configFile.Load(LEGACY_CONFIG_PATH);
  }

  public static void Load() {
    var configFile = new ConfigFile();
    HasStoredLanguage = false;
    HasStoredSkin = false;
    if (_loadConfigFile(configFile) == Error.Ok) {
      // Keyboard settings:
      if (configFile.HasSection("keyboard")) {
        foreach (string key in configFile.GetSectionKeys("keyboard")) {
          _bindKeyboardFromFile(key, configFile.GetValue("keyboard", key));
        }
      }
      // Gamepad settings:
      if (configFile.HasSection("gamepad")) {
        foreach (string action in configFile.GetSectionKeys("gamepad")) {
          // Files written before the directions were fixed carry entries for them.
          if (IsGamepadFixedDirectionAction(action)) {
            continue;
          }
          _bindGamepadFromFile(action, configFile.GetValue("gamepad", action).As<string>());
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
          else if (key == "performance_overlay") {
            PerformanceOverlay = keyValue.As<bool>();
          }
          else if (key == "resizable") {
            Resizable = keyValue.As<bool>();
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
            var code = keyValue.As<string>();
            if (!string.IsNullOrEmpty(code)) {
              Language = code.LanguageCodeToLanguage();
              HasStoredLanguage = true;
            }
          }
          else if (key == "skin") {
            // A name no longer in the game leaves the default in place, and counts as
            // never having been asked, so the player is asked again rather than
            // playing on in a palette they did not pick.
            HasStoredSkin = SkinManager.Instance.SetCurrentSkin(keyValue.As<string>());
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
