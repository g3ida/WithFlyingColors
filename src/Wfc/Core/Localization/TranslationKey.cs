namespace Wfc.Core.Localization;

using System;
using Godot;
using Wfc.Core.Exceptions;

public enum TranslationKey {
  Play,
  Continue,
  NewGame,
  Quit,
  SelectedSlot,
  Settings,
  GameSettings,
  Stats,
  GameStats,
  Options,
  SelectSlot,
  SaveSlot,
  DeleteSlot,
  RemoveSlot,
  ResumeGame,
  Display,
  ScreenResolution,
  Fullscreen,
  Controller,
  ControllerType,
  Keyboard,
  Joystick,
  Jump,
  MoveLeft,
  MoveRight,
  RotateLeft,
  RotateRight,
  Dash,
  Down,
  Pause,
  Audio,
  SfxVolume,
  MusicVolume,
  LevelTutorial,
  LevelDarkGames,
}

public static class TranslationKeyExtensions {
  public static string ToTranslationKeyString(this TranslationKey key) {
    return Enum.GetName(typeof(TranslationKey), key)
      ?? throw new GameExceptions.InvalidArgumentException("Invalid key: " + key);
  }


  public static string ToTranslationKeyStringSafe(this TranslationKey key) {
    try {
      return key.ToTranslationKeyString();
    }
    catch (Exception ex) {
      GD.PrintErr($"Failed to convert TranslationKey '{key}' to string: {ex.Message}");
      return string.Empty;
    }
  }
}
