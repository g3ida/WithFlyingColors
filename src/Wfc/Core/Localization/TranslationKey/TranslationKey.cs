namespace Wfc.Core.Localization;

using System;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;
using Wfc.Core.Exceptions;
using Wfc.Utils;

public enum TranslationKey {
  menu_button_play,
  menu_button_continue,
  menu_button_newGame,
  menu_button_quit,
  menu_button_selectedSlot,
  menu_button_settings,
  menu_button_stats,
  menu_header_mainMenu,
  menu_header_gameSettings,
  menu_header_gameStats,
  menu_button_options,
  menu_button_selectSlot,
  menu_button_saveSlot,
  menu_button_deleteSlot,
  menu_button_removeSlot,
  menu_button_resumeGame,
  game_settings_category_display,
  game_settings_screenResolution,
  game_settings_fullscreen,
  game_settings_category_controller,
  game_settings_controllerType,
  game_settings_keyboard,
  game_settings_joystick,
  game_command_jump,
  game_command_moveLeft,
  game_command_moveRight,
  game_command_rotateLeft,
  game_command_rotateRight,
  game_command_dash,
  game_command_down,
  game_command_pause,
  game_settings_category_audio,
  game_settings_audio_sfxVolume,
  game_settings_audio_musicVolume,
  menu_label_currentSlot,
  game_level_title_tutorial,
  game_level_title_darkGames,
}

public static partial class TranslationKeyExtensions {

  public static string ToTranslationKeyString(this TranslationKey key) {
    var name = Enum.GetName<TranslationKey>(key)
        ?? throw new GameExceptions.InvalidArgumentException("Invalid key: " + key);
    var snakeParts = name.Split('_').Select(StringUtils.ToSnakeCase);
    return string.Join(".", snakeParts);
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
