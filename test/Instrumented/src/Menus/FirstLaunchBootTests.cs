namespace Wfc.test.instrumented.Menus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Base;
using Wfc.Core.Localization;
using Wfc.Core.Settings;
using Wfc.Screens;
using Wfc.Screens.MenuManager;
using Wfc.Screens.MenuManager.Menus.MainMenu;
using Wfc.Utils;

// Which screen the game opens on, and what it asks before it lets the player in. This
// is the one thing the fake provider cannot answer: the decision is made by the real
// one, off the settings file it has just read, before anything has been shown.
//
// The questions are asked once each and never again, so the case that matters most is
// the existing player's: a file that answers the language but has never heard of a
// palette must ask only the new question.
public class FirstLaunchBootTests(Node testScene) : TestClass(testScene) {
  private const double SETTLE_TIMEOUT_SECONDS = 6.0;
  private const string SCRATCH_CONFIG_PATH = "user://test-boot-settings.ini";

  private static readonly string[] GAME_ACTIONS =
    ["move_left", "move_right", "jump", "rotate_left", "rotate_right", "pause", "dash", "down"];

  private readonly Dictionary<string, Godot.Collections.Array<InputEvent>> _savedEvents = new();
  private RootNode? _root;
  private string _configPathBeforeTest = default!;
  private Language _languageBeforeTest;
  private string _skinBeforeTest = default!;

  // Booting the real provider loads the settings, which rebinds the InputMap and
  // repaints the game, both of which are process wide and neither of which the tests
  // that run after this one asked for.
  [Setup]
  public void Setup() {
    _savedEvents.Clear();
    foreach (var action in GAME_ACTIONS) {
      _savedEvents[action] = InputMap.ActionGetEvents(action);
    }
    _configPathBeforeTest = GameSettings.ConfigFilePath;
    _languageBeforeTest = GameSettings.Language;
    _skinBeforeTest = GameSettings.Skin;
    GameSettings.ConfigFilePath = SCRATCH_CONFIG_PATH;
  }

  [Cleanup]
  public void Cleanup() {
    _root?.QueueFree();
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
  }

  [Test]
  public async Task AFirstLaunchAsksForALanguageFirst() {
    _writeConfig(language: null, skin: null);

    var screen = await _boot();

    screen.ShouldBeOfType<LanguageSelectMenu>();
  }

  // The existing player: they have a language on file and have never been asked which
  // colours they read best.
  [Test]
  public async Task ALaunchThatOnlyOwesThePaletteGoesStraightToIt() {
    _writeConfig(language: Language.Spanish, skin: null);

    var screen = await _boot();

    screen.ShouldBeOfType<SkinSelectMenu>();
    GameSettings.Language.ShouldBe(Language.Spanish);
  }

  [Test]
  public async Task ALaunchThatOwesNothingOpensOnTheMainMenu() {
    _writeConfig(language: Language.Spanish, skin: "clear");

    var screen = await _boot();

    screen.ShouldBeOfType<MainMenu>();
    GameSettings.Language.ShouldBe(Language.Spanish);
    GameSettings.Skin.ShouldBe("clear");
  }

  // Both questions on a fresh install, in order, with no way to get stuck between them.
  [Test]
  public async Task AFirstLaunchAsksBothQuestionsThenPlays() {
    _writeConfig(language: null, skin: null);
    var language = await _boot();
    language.ShouldBeOfType<LanguageSelectMenu>();

    _confirm(language);
    var colors = await _waitForScreen<SkinSelectMenu>();

    _confirm(colors);
    await _waitForScreen<MainMenu>();
  }

  // Nobody is asked a question twice: a player who answers the language on a file that
  // already names a palette is let straight through.
  [Test]
  public async Task ThePaletteIsNotAskedForWhenTheFileAlreadyNamesOne() {
    _writeConfig(language: null, skin: "clear");
    var language = await _boot();
    language.ShouldBeOfType<LanguageSelectMenu>();

    _confirm(language);

    await _waitForScreen<MainMenu>();
  }

  private static void _confirm(GameMenu screen) =>
    screen.FindDescendants<Godot.Button>()
      .First(button => button is Wfc.Entities.Ui.SettingsUI.UISelect.UISelectButton)
      .EmitSignal(BaseButton.SignalName.Pressed);

  private async Task<GameMenu> _waitForScreen<T>() where T : GameMenu {
    (await _waitUntil(() => _currentScreen() is T))
      .ShouldBeTrue($"never reached {typeof(T).Name}, stopped at {_currentScreen()?.GetType().Name ?? "nothing"}");
    return _currentScreen()!;
  }

  private async Task<GameMenu> _boot() {
    _root = SceneHelpers.InstantiateNode<RootNode>();
    TestScene.AddChild(_root);
    (await _waitUntil(() => _currentScreen() != null)).ShouldBeTrue("booting the root node showed no screen at all");
    return _currentScreen()!;
  }

  private GameMenu? _currentScreen() =>
    _root?.FindDescendants<GameMenu>().FirstOrDefault(screen => !screen.IsQueuedForDeletion());

  private async Task<bool> _waitUntil(Func<bool> condition) {
    var tree = TestScene.GetTree();
    var elapsed = 0.0;
    while (elapsed < SETTLE_TIMEOUT_SECONDS) {
      if (condition()) {
        return true;
      }
      await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
      elapsed += 1.0 / 60.0;
    }
    return condition();
  }

  // Everything the boot decision does not read is left out, so the file cannot apply a
  // window size or a binding on its way past.
  private static void _writeConfig(Language? language, string? skin) {
    var configFile = new ConfigFile();
    configFile.SetValue("general", "last_controller", 0);
    if (language is { } chosen) {
      configFile.SetValue("general", "language", chosen.GetLanguageCode());
    }
    if (skin != null) {
      configFile.SetValue("general", "skin", skin);
    }
    configFile.Save(SCRATCH_CONFIG_PATH).ShouldBe(Error.Ok);
  }
}
