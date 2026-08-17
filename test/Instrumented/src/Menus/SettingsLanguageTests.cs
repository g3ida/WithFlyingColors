namespace Wfc.test.instrumented.Menus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Chickensoft.Sync.Primitives;
using Godot;
using Shouldly;
using Wfc.Core.Localization;
using Wfc.Core.Settings;
using Wfc.Entities.Ui;
using Wfc.Entities.Ui.SettingsUI.UISelect;
using Wfc.Screens;
using Wfc.Screens.MenuManager;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;
using EventHandler = Wfc.Core.Event.EventHandler;

// The language is picked in the settings, so the settings screen is the one screen
// that has to follow the change while the player is still looking at it. Everything
// on it holds a string that was translated once, when it was built, which leaves the
// engine's own auto-translation nothing to redo.
public class SettingsLanguageTests(Node testScene) : TestClass(testScene) {
  private const double SETTLE_TIMEOUT_SECONDS = 6.0;

  private static readonly string[] FRENCH_TITLE_WORDS = ["Options", "du", "jeu"];

  private FakeDependenciesProvider _provider = default!;
  private Language _languageBeforeTest;

  [Setup]
  public async Task Setup() {
    // Process wide, and the tests that run after this one are entitled to find it
    // the way they left it.
    _languageBeforeTest = GameSettings.Language;
    GameSettings.Language = Language.English;
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await _idle();
  }

  [Cleanup]
  public void Cleanup() {
    GameSettings.Language = _languageBeforeTest;
    _provider.QueueFree();
  }

  // The bug this covers: picking a language changed the locale and nothing else, so
  // the screen kept its English captions until it was built again.
  [Test]
  public async Task PickingALanguageRewritesTheScreenItWasPickedOn() {
    var screen = await _openSettings();
    _textsOf(screen).ShouldContain("General", "the tab captions did not start out in English");

    _pick(screen, Language.French);
    await _idle();

    var texts = _textsOf(screen);
    // The tab captions, and the label of the row the language select sits on.
    texts.ShouldContain("Général");
    texts.ShouldContain("Langue");
    texts.ShouldNotContain("General");
  }

  // The title is one label per word and languages disagree on how many words that is,
  // so it is the one thing on the screen that has to be built again rather than
  // rewritten: "Game Settings" is two words, "Options du jeu" is three.
  [Test]
  public async Task TheScreenTitleIsBuiltAgainForTheNewLanguage() {
    var screen = await _openSettings();
    screen.FindDescendants<TitleLabel>().Count().ShouldBe(2);

    _pick(screen, Language.French);
    await _idle();

    var words = screen.FindDescendants<TitleLabel>().Select(label => label.content).ToList();
    words.ShouldBe(FRENCH_TITLE_WORDS);
  }

  // Rebuilding the title throws away the transitions that went with it, and the screen
  // waits on every transition it knows about before it hands over to the next one. Left
  // holding the freed ones it would never leave.
  [Test]
  public async Task TheScreenStillLeavesAfterItsTitleWasBuiltAgain() {
    var screen = await _openSettings();

    _pick(screen, Language.French);
    await _idle();
    screen.NavigateToScreen(GameMenus.MAIN_MENU);

    (await _waitUntil(() => _provider.MenuManager.GetCurrentMenu() == GameMenus.MAIN_MENU))
      .ShouldBeTrue("the settings screen never finished leaving");
  }

  // The sound that goes with the change hangs off this event, and the select reports
  // its selection whenever it draws itself - opening the screen is not a change.
  [Test]
  public async Task OnlyARealChangeIsAnnounced() {
    var screen = await _openSettings();
    var driver = screen.FindDescendants<LanguageSelectDriver>().FirstOrDefault();
    driver.ShouldNotBeNull("the general tab has no language select");

    var announced = new List<Language>();
    using var binding = SettingsRepo.Instance.Channel.Bind()
      .On((in ISettingsRepo.LanguageChanged message) => announced.Add(message.Language));

    driver.onItemSelected(Variant.CreateFrom(Language.English.GetLanguageCode()));
    announced.ShouldBeEmpty("picking the language already in use was announced as a change");

    driver.onItemSelected(Variant.CreateFrom(Language.French.GetLanguageCode()));
    announced.ShouldBe([Language.French]);
    GameSettings.Language.ShouldBe(Language.French);
  }

  private static void _pick(GameMenu screen, Language language) {
    var driver = screen.FindDescendants<LanguageSelectDriver>().FirstOrDefault();
    driver.ShouldNotBeNull("the general tab has no language select");
    driver.onItemSelected(Variant.CreateFrom(language.GetLanguageCode()));
  }

  // Every caption on the screen, whatever kind of control it is drawn by.
  private static List<string> _textsOf(Node screen) =>
    screen.FindDescendants<Control>()
      .Select(control => control switch {
        Label label => label.Text,
        Button button => button.Text,
        _ => string.Empty
      })
      .Where(text => !string.IsNullOrEmpty(text))
      .ToList();

  private async Task<GameMenu> _openSettings() {
    _provider.MenuManager.GoToMenu(GameMenus.MAIN_MENU);
    await _idle();
    var mainMenu = _currentScreen();
    if (mainMenu != null) {
      await _waitUntil(() => !mainMenu.IsInTransitionState());
    }

    _provider.MenuManager.GoToMenu(GameMenus.SETTINGS_MENU);
    await _idle();
    var screen = _currentScreen();
    screen.ShouldNotBeNull("the settings menu produced no screen");
    (await _waitUntil(() => !screen.IsInTransitionState())).ShouldBeTrue("the settings menu never finished entering");
    return screen;
  }

  private GameMenu? _currentScreen() =>
    _provider.FindDescendants<GameMenu>().FirstOrDefault(screen => !screen.IsQueuedForDeletion());

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

  private async Task _idle() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
  }
}
