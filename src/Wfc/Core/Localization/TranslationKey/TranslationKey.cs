namespace Wfc.Core.Localization;

using System;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;
using Wfc.Core.Exceptions;
using Wfc.Utils;

// Note: Please only add entries at the end to preserve existing enum values declared in scene files.
public enum TranslationKey {
  menu_button_play = 0,
  menu_button_continue = 1,
  menu_button_newGame = 2,
  menu_button_quit = 3,
  menu_button_selectedSlot = 4,
  menu_button_settings = 5,
  menu_button_stats = 6,
  menu_header_mainMenu = 7,
  menu_header_gameSettings = 8,
  menu_header_gameStats = 9,
  menu_button_options = 10,
  menu_button_selectSlot = 11,
  menu_button_saveSlot = 12,
  menu_button_deleteSlot = 13,
  menu_button_removeSlot = 14,
  menu_button_resumeGame = 15,
  game_settings_category_general = 16,
  game_settings_category_display = 17,
  game_settings_screenResolution = 18,
  game_settings_fullscreen = 19,
  game_settings_category_controller = 20,
  game_settings_controllerType = 21,
  game_settings_keyboard = 22,
  game_settings_joystick = 23,
  game_command_jump = 24,
  game_command_moveLeft = 25,
  game_command_moveRight = 26,
  game_command_rotateLeft = 27,
  game_command_rotateRight = 28,
  game_command_dash = 29,
  game_command_down = 30,
  game_command_pause = 31,
  game_settings_category_audio = 32,
  game_settings_audio_sfxVolume = 33,
  game_settings_audio_musicVolume = 34,
  game_level_title_tutorial = 36,
  game_level_title_darkGames = 37,
  game_settings_general_language = 38,
  game_settings_display_resolution = 39,
  game_settings_display_fullscreen = 40,
  game_settings_display_vsync = 41,
  game_settings_display_resolutionAuto = 42,
  controller_type_keyboard = 43,
  controller_type_gamepad = 44,
  controller_label_controls = 45,
  game_command_empty = 46,
  menu_hint_select = 47,
  menu_hint_back = 48,
  menu_hint_switchTab = 49,
  menu_hint_navigate = 50,
  menu_header_selectSlot = 51,
  menu_header_selectLevel = 52,
  menu_header_levelCleared = 53,
  menu_button_levelSelection = 54,
  menu_button_mainMenu = 55,
  menu_button_confirm = 57,
  menu_button_cancel = 58,
  menu_dialog_bindAllKeys = 61,
  menu_button_ok = 62,
  menu_label_emptySlot = 63,
  menu_label_slotIndex = 65,
  game_level_title_fourColors = 66,
  game_level_title_letsPlayWithColors = 67,
  menu_button_loadGame = 68,
  menu_dialog_overwriteSlot = 69,
  menu_header_loadGame = 70,
  menu_header_newGame = 71,
  menu_label_slotCompletion = 74,
  menu_label_selectSlotNewGame = 75,
  menu_label_selectSlotLoad = 76,
  menu_hint_load = 77,
  menu_label_slotCreated = 78,
  menu_label_slotLastPlayed = 79,
  game_level_title_hub = 80,
  game_hint_enterDoor = 81,
  menu_button_returnToHub = 82,
  menu_button_restartLevel = 83,
  menu_button_restartCheckpoint = 84,
  game_notification_checkpointReached = 85,
  game_notification_levelCleared = 86,
  menu_header_pickColors = 87,
  game_settings_general_colors = 88,
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
