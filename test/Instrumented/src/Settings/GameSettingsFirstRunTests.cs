namespace Wfc.test.instrumented.Settings;

using System.Collections.Generic;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Localization;
using Wfc.Core.Settings;
using Wfc.Skin;

// What the settings file records about the questions the game only asks once. A launch
// counts as a first one for a question the file does not answer - the language in use
// otherwise came off the system and the palette is whatever the game shipped, guessed
// rather than chosen. That has to hold for a file written before the game ever asked,
// not only for a missing one, since that is every existing player's next launch.
public class GameSettingsFirstRunTests(Node testScene) : TestClass(testScene) {
  private const string SCRATCH_CONFIG_PATH = "user://test-language-settings.ini";

  private static readonly string[] GAME_ACTIONS =
    ["move_left", "move_right", "jump", "rotate_left", "rotate_right", "pause", "dash", "down"];

  private readonly Dictionary<string, Godot.Collections.Array<InputEvent>> _savedEvents = new();
  private string _configPathBeforeTest = default!;
  private Language _languageBeforeTest;
  private string _skinBeforeTest = default!;
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
    _skinBeforeTest = GameSettings.Skin;
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
    GameSettings.Skin = _skinBeforeTest;
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

  // Writing the file is what the first-run screens do to stop themselves coming back.
  // The flags describe the file as it was read, so it is the next load that has to
  // report the questions as answered - not the save itself.
  [Test]
  public void ALaunchAfterSavingIsNotAFirstLaunch() {
    _writeConfig(("general", "last_controller", 0));
    GameSettings.Load();
    GameSettings.HasStoredLanguage.ShouldBeFalse();
    GameSettings.HasStoredSkin.ShouldBeFalse();

    GameSettings.Language = Language.Dutch;
    GameSettings.Save();
    GameSettings.Load();

    GameSettings.HasStoredLanguage.ShouldBeTrue();
    GameSettings.HasStoredSkin.ShouldBeTrue();
    GameSettings.Language.ShouldBe(Language.Dutch);
  }

  // The language screen saves on its way out, and that save writes a palette too. A
  // flag that followed the file rather than the load would count the colour question
  // as answered before the player had been shown it.
  [Test]
  public void SavingDoesNotCountAsHavingBeenAsked() {
    _writeConfig(("general", "last_controller", 0));
    GameSettings.Load();

    GameSettings.Save();

    GameSettings.HasStoredSkin.ShouldBeFalse();
    GameSettings.HasStoredLanguage.ShouldBeFalse();
  }

  [Test]
  public void AFileThatNamesAPaletteIsNotAFirstLaunch() {
    _writeConfig(("general", "language", "en"), ("general", "skin", "clear"));

    GameSettings.Load();

    GameSettings.HasStoredSkin.ShouldBeTrue();
    GameSettings.Skin.ShouldBe("clear");
  }

  // A file naming a palette the game no longer has is no answer at all: the default
  // stays, and the player is asked rather than played on in colours they never picked.
  [Test]
  public void APaletteThatNoLongerExistsIsNoAnswer() {
    GameSettings.Skin = SkinManager.DEFAULT_SKIN_NAME;
    _writeConfig(("general", "language", "en"), ("general", "skin", "not_a_palette"));

    GameSettings.Load();

    GameSettings.HasStoredSkin.ShouldBeFalse();
    GameSettings.Skin.ShouldBe(SkinManager.DEFAULT_SKIN_NAME);
  }

  private static void _writeConfig(params (string section, string key, Variant value)[] entries) {
    var configFile = new ConfigFile();
    foreach (var (section, key, value) in entries) {
      configFile.SetValue(section, key, value);
    }
    configFile.Save(SCRATCH_CONFIG_PATH).ShouldBe(Error.Ok);
  }
}
