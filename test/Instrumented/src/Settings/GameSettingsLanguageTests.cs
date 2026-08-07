namespace Wfc.test.instrumented.Settings;

using System.Collections.Generic;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Localization;
using Wfc.Core.Settings;

// A launch counts as the first one when the settings file names no language: the one
// the game would otherwise draw itself in was read off the system, guessed rather than
// chosen. That is the whole of what sends the player to the language screen, so it has
// to hold for a file written before the game ever asked, not only for a missing one.
public class GameSettingsLanguageTests(Node testScene) : TestClass(testScene) {
  private const string SCRATCH_CONFIG_PATH = "user://test-language-settings.ini";

  private static readonly string[] GAME_ACTIONS =
    ["move_left", "move_right", "jump", "rotate_left", "rotate_right", "pause", "dash", "down"];

  private readonly Dictionary<string, Godot.Collections.Array<InputEvent>> _savedEvents = new();
  private string _configPathBeforeTest = default!;
  private Language _languageBeforeTest;
  private bool _fullscreenBeforeTest;
  private bool _vsyncBeforeTest;

  // Loading rebinds the InputMap and touches the window, both of which are process
  // wide and neither of which the tests that run after this one asked for.
  [Setup]
  public void Setup() {
    _savedEvents.Clear();
    foreach (var action in GAME_ACTIONS) {
      _savedEvents[action] = InputMap.ActionGetEvents(action);
    }
    _configPathBeforeTest = GameSettings.ConfigFilePath;
    _languageBeforeTest = GameSettings.Language;
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
    GameSettings.Language = _languageBeforeTest;
    GameSettings.Fullscreen = _fullscreenBeforeTest;
    GameSettings.Vsync = _vsyncBeforeTest;
  }

  [Test]
  public void AFileThatNamesALanguageIsNotAFirstLaunch() {
    _writeConfig(("general", "language", "it"));

    GameSettings.Load();

    GameSettings.HasStoredLanguage.ShouldBeTrue();
    GameSettings.Language.ShouldBe(Language.Italian);
  }

  // The upgrade path: a settings file written before the game ever asked.
  [Test]
  public void AFileWithNoLanguageInItIsAFirstLaunch() {
    _writeConfig(("general", "last_controller", 0));

    GameSettings.Load();

    GameSettings.HasStoredLanguage.ShouldBeFalse();
  }

  [Test]
  public void AnEmptyLanguageIsNoLanguage() {
    _writeConfig(("general", "language", ""));

    GameSettings.Load();

    GameSettings.HasStoredLanguage.ShouldBeFalse();
  }

  [Test]
  public void NoFileAtAllIsAFirstLaunch() {
    DirAccess.RemoveAbsolute(SCRATCH_CONFIG_PATH);

    GameSettings.Load();

    GameSettings.HasStoredLanguage.ShouldBeFalse();
  }

  // Writing the file is what the language screen does to stop itself coming back.
  [Test]
  public void SavingRecordsThatTheLanguageWasChosen() {
    _writeConfig(("general", "last_controller", 0));
    GameSettings.Load();
    GameSettings.HasStoredLanguage.ShouldBeFalse();

    GameSettings.Language = Language.Dutch;
    GameSettings.Save();

    GameSettings.HasStoredLanguage.ShouldBeTrue();
    GameSettings.Load();
    GameSettings.HasStoredLanguage.ShouldBeTrue();
    GameSettings.Language.ShouldBe(Language.Dutch);
  }

  private static void _writeConfig(params (string section, string key, Variant value)[] entries) {
    var configFile = new ConfigFile();
    foreach (var (section, key, value) in entries) {
      configFile.SetValue(section, key, value);
    }
    configFile.Save(SCRATCH_CONFIG_PATH).ShouldBe(Error.Ok);
  }
}
