using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class GameSettings : Node2D
{
    private const string ConfigFilePath = "settings.ini";
    private const float MaxVolume = 0f;
    private const float MinVolume = -50f;

    public bool Vsync
    {
        get => OS.VsyncEnabled;
        set => OS.VsyncEnabled = value;
    }

    public bool Fullscreen
    {
        get => OS.WindowFullscreen;
        set => OS.WindowFullscreen = value;
    }

    public Vector2 WindowSize
    {
        get => OS.WindowSize;
        set => OS.WindowSize = value;
    }

    public float SfxVolume
    {
        get => GetNormalizedAudioBusVolume("sfx");
        set => SetAudioBusVolume("sfx", value);
    }

    public float MusicVolume
    {
        get => GetNormalizedAudioBusVolume("music");
        set => SetAudioBusVolume("music", value);
    }

    private static GameSettings _instance = null;

    public static GameSettings Instance() {
      return _instance;
    }

    public override void _Ready()
    {
        _instance = GetTree().Root.GetNode<GameSettings>("GameSettings");
        SetProcess(false);
        LoadGameSettings();

    }

    private float GetVolumeInDb(float volume)
    {
        float vol = (MaxVolume - MinVolume) * volume + MinVolume;
        return Mathf.Clamp(vol, MinVolume, MaxVolume);
    }

    private float GetVolumeFromDb(float volumeDb)
    {
        float vol = -(volumeDb / MinVolume) + 1.0f;
        return Mathf.Clamp(vol, 0.0f, 1.0f);
    }

    private void SetAudioBusVolume(string busName, float volume)
    {
        float vol = GetVolumeInDb(volume);
        int musicBusIndex = AudioServer.GetBusIndex(busName);
        if (vol != MinVolume)
        {
            AudioServer.SetBusMute(musicBusIndex, false);
            AudioServer.SetBusVolumeDb(musicBusIndex, vol);
        }
        else
        {
            AudioServer.SetBusMute(musicBusIndex, true);
        }
    }

    private float GetNormalizedAudioBusVolume(string busName)
    {
        int musicBusIndex = AudioServer.GetBusIndex(busName);
        float volumeDb = AudioServer.GetBusVolumeDb(musicBusIndex);
        return GetVolumeFromDb(volumeDb);
    }

    public void BindActionToKeyboardKey(string action, int scancode)
    {
        // Erase the current action:
        var actionList = InputMap.GetActionList(action).Cast<InputEvent>();
        var inputEvent = InputUtils.GetFirstKeyKeyboardEventFromActionList(actionList);
        if (inputEvent != null)
        {
            var inputKeyEvent = inputEvent as InputEventKey;
            InputMap.ActionEraseEvent(action, inputKeyEvent);
        }

        // Add the new action:
        var newKey = new InputEventKey
        {
            Scancode = (uint)scancode
        };
        InputMap.ActionAddEvent(action, newKey);
    }

    public void UnbindActionKey(string action)
    {
        // Erase the current action:
        var actionList = InputMap.GetActionList(action).Cast<InputEvent>();
        var inputEvent = InputUtils.GetFirstKeyKeyboardEventFromActionList(actionList);
        if (inputEvent != null)
        {
            var inputKeyEvent = inputEvent as InputEventKey;
            InputMap.ActionEraseEvent(action, inputKeyEvent);
        }
    }

    private List<string> GetGameActions()
    {
        var actions = InputMap.GetActions().Cast<string>();
        var gameActions = new List<string>();
        foreach (var action in actions)
        {
            if (action.Find("ui_") == -1)
            {
                gameActions.Add(action);
            }
        }
        return gameActions;
    }

    public bool AreActionKeysValid()
    {
        var gameActions = GetGameActions();
        foreach (var action in gameActions)
        {
            var actionList = InputMap.GetActionList(action).Cast<InputEvent>();
            if (InputUtils.GetFirstKeyKeyboardEventFromActionList(actionList) == null)
            {
                return false;
            }
        }
        return true;
    }

    public void SaveGameSettings()
    {
        // Save game actions:
        var configFile = new ConfigFile();

        var gameActions = GetGameActions();
        foreach (var action in gameActions)
        {
            var key = action;
            var actionList = InputMap.GetActionList(key).Cast<InputEvent>();
            var keyValue = InputUtils.GetFirstKeyKeyboardEventFromActionList(actionList);
            if (keyValue != null)
            {
                configFile.SetValue("keyboard", key, keyValue.Scancode);
            }
            else
            {
                configFile.SetValue("keyboard", key, "");
            }
        }

        // Display settings:
        configFile.SetValue("display", "fullscreen", Fullscreen);
        configFile.SetValue("display", "vsync", Vsync);
        configFile.SetValue("display", "resolution", $"{WindowSize.x}x{WindowSize.y}");

        // Audio settings:
        configFile.SetValue("audio", "sfx_volume", SfxVolume);
        configFile.SetValue("audio", "music_volume", MusicVolume);

        configFile.Save(ConfigFilePath);
    }

    private void LoadGameSettings()
    {
        var configFile = new ConfigFile();
        if (configFile.Load(ConfigFilePath) == Error.Ok)
        {
            foreach (string key in configFile.GetSectionKeys("keyboard"))
            {
                var keyValue = configFile.GetValue("keyboard", key);
                if (keyValue.ToString() != "")
                {
                    BindActionToKeyboardKey(key, (int)keyValue);
                }
            }

            foreach (string key in configFile.GetSectionKeys("display"))
            {
                var keyValue = configFile.GetValue("display", key);
                if (key == "fullscreen")
                {
                    Fullscreen = Convert.ToBoolean(keyValue);
                }
                else if (key == "vsync")
                {
                    Vsync = Convert.ToBoolean(keyValue);
                }
                else if (key == "resolution")
                {
                    var values = keyValue.ToString().Split('x');
                    if (values.Length == 2)
                    {
                        WindowSize = new Vector2(float.Parse(values[0]), float.Parse(values[1]));
                    }
                }
            }

            // Audio settings:
            foreach (string key in configFile.GetSectionKeys("audio"))
            {
                var keyValue = configFile.GetValue("audio", key);
                if (key == "sfx_volume")
                {
                    SfxVolume = float.Parse(keyValue.ToString());
                }
                else if (key == "music_volume")
                {
                    MusicVolume = float.Parse(keyValue.ToString());
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
