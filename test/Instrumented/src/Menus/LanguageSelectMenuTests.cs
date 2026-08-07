namespace Wfc.test.instrumented.Menus;

using System;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Localization;
using Wfc.Core.Settings;
using Wfc.Entities.Ui;
using Wfc.Entities.Ui.InputHint;
using Wfc.Entities.Ui.SettingsUI.UISelect;
using Wfc.Screens;
using Wfc.Screens.MenuManager;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// The screen the game opens on its very first launch, to be told which language to
// draw itself in. Everything about it is one-shot: it leads to the main menu and
// nowhere else, it has nothing behind it to go back to, and confirming writes the
// choice to the settings file so the next launch does not ask again.
public class LanguageSelectMenuTests(Node testScene) : TestClass(testScene) {
  private const double SETTLE_TIMEOUT_SECONDS = 6.0;

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

  [Test]
  public async Task TheScreenOpensOnTheLanguageAlreadyInUse() {
    GameSettings.Language = Language.Italian;

    var screen = await _openLanguageSelect();

    _valueShownOn(screen).ShouldBe(Language.Italian.GetLanguageNativeName());
  }

  // The picker is the whole screen, so the arrows have to reach it: nothing else here
  // owns left and right, and the engine would otherwise spend them moving the focus
  // onto one of the two arrows the picker is drawn from.
  [Test]
  public async Task SteppingThePickerChangesTheLanguage() {
    var screen = await _openLanguageSelect();

    _stepRight(screen);
    await _idle();

    GameSettings.Language.ShouldNotBe(Language.English);
    _valueShownOn(screen).ShouldBe(GameSettings.Language.GetLanguageNativeName());
  }

  // The hints are the only words on the screen, and they are picked at the moment the
  // player is choosing which language to read them in.
  [Test]
  public async Task TheHintsFollowTheLanguageBeingPicked() {
    var screen = await _openLanguageSelect();
    _captionsOf(screen).ShouldContain("Select");

    _pick(screen, Language.French);
    await _idle();

    _captionsOf(screen).ShouldContain("Valider");
  }

  // Back is the one hint the screen must not offer: there is nothing behind it.
  [Test]
  public async Task BackIsNotOffered() {
    var screen = await _openLanguageSelect();

    _captionsOf(screen).ShouldBe(["Navigate", "Select"]);
  }

  // Which screen it hands over to depends on what else the settings file still owes an
  // answer for, and that routing is covered against a controlled file in
  // FirstLaunchBootTests. What matters here is that confirming writes the choice and
  // the screen does not stay up.
  [Test]
  public async Task ConfirmingWritesTheLanguageAndLeaves() {
    var screen = await _openLanguageSelect();
    _pick(screen, Language.German);

    _confirm(screen);

    (await _waitUntil(() => _provider.MenuManager.GetCurrentMenu() != GameMenus.LANGUAGE_SELECT))
      .ShouldBeTrue("the language screen never handed over");
    _savedLanguageCode().ShouldBe(Language.German.GetLanguageCode());
  }

  private static void _stepRight(GameMenu screen) =>
    _pickerOn(screen).GetNode<Button>("HBoxContainer/Right").EmitSignal(BaseButton.SignalName.Pressed);

  private static void _confirm(GameMenu screen) =>
    _pickerOn(screen).EmitSignal(BaseButton.SignalName.Pressed);

  private static void _pick(GameMenu screen, Language language) {
    var driver = screen.FindDescendants<LanguageSelectDriver>().FirstOrDefault();
    driver.ShouldNotBeNull("the language screen has no language select");
    driver.onItemSelected(Variant.CreateFrom(language.GetLanguageCode()));
  }

  private static UISelectButton _pickerOn(GameMenu screen) {
    var picker = screen.FindDescendants<UISelectButton>().FirstOrDefault();
    picker.ShouldNotBeNull("the language screen has no picker");
    return picker;
  }

  private static string _valueShownOn(GameMenu screen) =>
    _pickerOn(screen).FindDescendants<MarqueeLabel>().First().Text;

  // The words a card writes, not the glyphs beside them: a key cap keeps the text of
  // whatever it last drew even while it is hidden.
  private static string[] _captionsOf(Node screen) =>
    screen.FindDescendants<InputHintCard>()
      .Select(card => card.GetNode<Label>("HBox/CaptionBox/Caption").Text)
      .ToArray();

  private static string _savedLanguageCode() {
    var configFile = new ConfigFile();
    configFile.Load(GameSettings.ConfigFilePath).ShouldBe(Error.Ok, "confirming wrote no settings file");
    return configFile.GetValue("general", "language").As<string>();
  }

  private async Task<GameMenu> _openLanguageSelect() {
    _provider.MenuManager.GoToMenu(GameMenus.LANGUAGE_SELECT).ShouldBeTrue();
    await _idle();
    var screen = _currentScreen();
    screen.ShouldNotBeNull("the language menu produced no screen");
    screen.ShouldBeOfType<LanguageSelectMenu>();
    (await _waitUntil(() => !screen.IsInTransitionState())).ShouldBeTrue("the language menu never finished entering");
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
