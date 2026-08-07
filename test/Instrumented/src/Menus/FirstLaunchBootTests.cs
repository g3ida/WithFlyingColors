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
using Wfc.Screens.MenuManager.Menus.MainMenu;
using Wfc.Utils;

// Which screen the game opens on. This is the one thing the fake provider cannot
// answer: the decision is made by the real one, off the settings file it has just
// read, before anything else has been shown.
public class FirstLaunchBootTests(Node testScene) : TestClass(testScene) {
  private const double SETTLE_TIMEOUT_SECONDS = 6.0;
  private const string SCRATCH_CONFIG_PATH = "user://test-boot-settings.ini";

  private static readonly string[] GAME_ACTIONS =
    ["move_left", "move_right", "jump", "rotate_left", "rotate_right", "pause", "dash", "down"];

  private readonly Dictionary<string, Godot.Collections.Array<InputEvent>> _savedEvents = new();
  private RootNode? _root;
  private string _configPathBeforeTest = default!;
  private Language _languageBeforeTest;

  // Booting the real provider loads the settings, which rebinds the InputMap process
  // wide - not something the tests that run after this one asked for.
  [Setup]
  public void Setup() {
    _savedEvents.Clear();
    foreach (var action in GAME_ACTIONS) {
      _savedEvents[action] = InputMap.ActionGetEvents(action);
    }
    _configPathBeforeTest = GameSettings.ConfigFilePath;
    _languageBeforeTest = GameSettings.Language;
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
  }

  [Test]
  public async Task AFirstLaunchAsksForALanguage() {
    _writeConfig(language: null);

    var screen = await _boot();

    screen.ShouldBeOfType<LanguageSelectMenu>();
  }

  [Test]
  public async Task ALaunchAfterOneOpensOnTheMainMenu() {
    _writeConfig(language: Language.Spanish);

    var screen = await _boot();

    screen.ShouldBeOfType<MainMenu>();
    GameSettings.Language.ShouldBe(Language.Spanish);
  }

  private async Task<GameMenu> _boot() {
    _root = SceneHelpers.InstantiateNode<RootNode>();
    TestScene.AddChild(_root);
    var screen = null as GameMenu;
    var tree = TestScene.GetTree();
    var elapsed = 0.0;
    while (elapsed < SETTLE_TIMEOUT_SECONDS && screen == null) {
      await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
      elapsed += 1.0 / 60.0;
      screen = _root.FindDescendants<GameMenu>().FirstOrDefault(candidate => !candidate.IsQueuedForDeletion());
    }
    screen.ShouldNotBeNull("booting the root node showed no screen at all");
    return screen;
  }

  // Everything the boot decision does not read is left out, so the file cannot apply
  // a window size or a binding on its way past.
  private static void _writeConfig(Language? language) {
    var configFile = new ConfigFile();
    configFile.SetValue("general", "last_controller", 0);
    if (language is { } chosen) {
      configFile.SetValue("general", "language", chosen.GetLanguageCode());
    }
    configFile.Save(SCRATCH_CONFIG_PATH).ShouldBe(Error.Ok);
  }
}
