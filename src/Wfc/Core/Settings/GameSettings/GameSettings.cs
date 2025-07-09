namespace Wfc.Core.Settings;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Microsoft.VisualBasic;

public static class GameSettings {
  private const string ConfigFilePath = "settings.ini";
  private const float MaxVolume = 0f;
  private const float MinVolume = -50f;

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
    set => DisplayServer.WindowSetSize(value);
  }

  public static float SfxVolume {
    get => GetNormalizedAudioBusVolume("sfx");
    set => SetAudioBusVolume("sfx", value);
  }

  public static float MusicVolume {
    get => GetNormalizedAudioBusVolume("music");
    set => SetAudioBusVolume("music", value);
  }

  private static float GetVolumeInDb(float volume) {
    float vol = (MaxVolume - MinVolume) * volume + MinVolume;
    return Mathf.Clamp(vol, MinVolume, MaxVolume);
  }

  private static float GetVolumeFromDb(float volumeDb) {
    float vol = -(volumeDb / MinVolume) + 1.0f;
    return Mathf.Clamp(vol, 0.0f, 1.0f);
  }

  private static void SetAudioBusVolume(string busName, float volume) {
    float vol = GetVolumeInDb(volume);
    int musicBusIndex = AudioServer.GetBusIndex(busName);
    if (vol != MinVolume) {
      AudioServer.SetBusMute(musicBusIndex, false);
      AudioServer.SetBusVolumeDb(musicBusIndex, vol);
    }
    else {
      AudioServer.SetBusMute(musicBusIndex, true);
    }
  }

  private static float GetNormalizedAudioBusVolume(string busName) {
    int musicBusIndex = AudioServer.GetBusIndex(busName);
    float volumeDb = AudioServer.GetBusVolumeDb(musicBusIndex);
    return GetVolumeFromDb(volumeDb);
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

  private static List<string> GetGameActions() {
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
    var gameActions = GetGameActions();
    foreach (var action in gameActions) {
      var actionList = InputMap.ActionGetEvents(action).Cast<InputEvent>();
      if (InputUtils.GetFirstKeyKeyboardEventFromActionList(actionList) == null) {
        return false;
      }
    }
    return true;
  }

  public static void Save() {
    // Save game actions:
    var configFile = new ConfigFile();

    var gameActions = GetGameActions();
    foreach (var action in gameActions) {
      var key = action;
      var actionList = InputMap.ActionGetEvents(key).Cast<InputEvent>();
      var keyValue = InputUtils.GetFirstKeyKeyboardEventFromActionList(actionList);
      if (keyValue != null) {
        configFile.SetValue("keyboard", key, Variant.From<int>((int)keyValue.Keycode));
      }
      else {
        configFile.SetValue("keyboard", key, "");
      }
    }

    // Display settings:
    configFile.SetValue("display", "fullscreen", Fullscreen);
    configFile.SetValue("display", "vsync", Vsync);
    configFile.SetValue("display", "resolution", $"{WindowSize.X}x{WindowSize.Y}");

    // Audio settings:
    configFile.SetValue("audio", "sfx_volume", SfxVolume);
    configFile.SetValue("audio", "music_volume", MusicVolume);

    configFile.Save(ConfigFilePath);
  }

  public static void Load() {
    var configFile = new ConfigFile();
    if (configFile.Load(ConfigFilePath) == Error.Ok) {
      foreach (string key in configFile.GetSectionKeys("keyboard")) {
        var keyValue = configFile.GetValue("keyboard", key);
        if ((keyValue.VariantType != Variant.Type.String) || keyValue.As<string>() != "") {
          BindActionToKeyboardKey(key, keyValue.As<int>());
        }
      }

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
          if (values.Length == 2) {
            WindowSize = new Vector2I(int.Parse(values[0]), int.Parse(values[1]));
          }
        }
      }

      // Audio settings:
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
    else // Default settings if settings file does not exist:
    {
      Fullscreen = true;
      Vsync = true;
    }
  }
}
