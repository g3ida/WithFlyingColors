namespace Wfc.test.instrumented.Menus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Settings;
using Wfc.Entities.Ui;
using Wfc.Entities.Ui.InputHint;
using Wfc.Entities.Ui.SettingsUI.UISelect;
using Wfc.Screens;
using Wfc.Screens.MenuManager;
using Wfc.Skin;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// The screen that asks which palette the player reads best. It is put as a preference
// and shows the colours themselves, so the answer needs nothing from the player beyond
// looking - and the screen must never say what it is really for.
public class SkinSelectMenuTests(Node testScene) : TestClass(testScene) {
  private const double SETTLE_TIMEOUT_SECONDS = 6.0;

  private FakeDependenciesProvider _provider = default!;
  private string _skinBeforeTest = default!;

  [Setup]
  public async Task Setup() {
    // Process wide, and the tests that run after this one are entitled to find it the
    // way they left it.
    _skinBeforeTest = GameSettings.Skin;
    GameSettings.Skin = SkinManager.DEFAULT_SKIN_NAME;
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await _idle();
  }

  [Cleanup]
  public void Cleanup() {
    GameSettings.Skin = _skinBeforeTest;
    _provider.QueueFree();
  }

  [Test]
  public async Task TheScreenOpensOnThePaletteAlreadyInUse() {
    GameSettings.Skin = "clear";

    var screen = await _openSkinSelect();

    _valueShownOn(screen).ShouldBe(SkinManager.DisplayName("clear"));
  }

  [Test]
  public async Task SteppingThePickerChangesThePalette() {
    var screen = await _openSkinSelect();

    _stepRight(screen);
    await _idle();

    GameSettings.Skin.ShouldNotBe(SkinManager.DEFAULT_SKIN_NAME);
    _valueShownOn(screen).ShouldBe(SkinManager.DisplayName(GameSettings.Skin));
  }

  // The swatches are the whole answer to the question the screen asks - a palette name
  // means nothing to someone deciding which four they can separate.
  [Test]
  public async Task TheSwatchesShowThePaletteBeingOffered() {
    var screen = await _openSkinSelect();
    _swatchColorsOf(screen).ShouldBe(_basicColorsOf(SkinManager.DEFAULT_SKIN_NAME));

    _pick(screen, "clear");
    await _idle();

    _swatchColorsOf(screen).ShouldBe(_basicColorsOf("clear"));
  }

  // Stepping through the options must not walk the arrows back and forth across the
  // screen: the value's box is kept as wide as the longest option, whichever one it is
  // currently showing. The names differ in length ("Neon" against "Classic"), so a box
  // that sized itself to the text would move them on every step.
  [Test]
  public async Task TheArrowsStayPutWhicheverOptionIsShowing() {
    var screen = await _openSkinSelect();
    var picker = _pickerOn(screen);
    var left = picker.GetNode<Button>("HBoxContainer/Left");
    var right = picker.GetNode<Button>("HBoxContainer/Right");
    var label = picker.FindDescendants<MarqueeLabel>().First();

    var seenPositions = new HashSet<(float, float)>();
    var seenValues = new HashSet<string>();
    for (var step = 0; step < SkinManager.SELECTABLE_SKINS.Length; step++) {
      await _idle();
      seenPositions.Add((left.GlobalPosition.X, right.GlobalPosition.X));
      seenValues.Add(label.Text);
      _stepRight(screen);
    }

    seenValues.Count.ShouldBe(SkinManager.SELECTABLE_SKINS.Length, "the picker never actually changed value");
    seenPositions.Count.ShouldBe(1,
      $"the arrows moved as the value changed: {string.Join(" ", seenPositions)}");
  }

  // Back is the one hint the screen must not offer: there is nothing behind it.
  [Test]
  public async Task BackIsNotOffered() {
    var screen = await _openSkinSelect();

    _captionsOf(screen).ShouldBe(["Navigate", "Select"]);
  }

  // The screen asks about legibility, never about the player. Anything naming a
  // condition would turn a preference into a diagnosis at the door.
  [Test]
  public async Task NothingOnTheScreenNamesAConditionOrTheWordColourBlind() {
    var screen = await _openSkinSelect();

    var words = string.Join(" ", _allTextOf(screen)).ToLowerInvariant();
    foreach (var forbidden in new[] {
      "colour blind", "color blind", "colourblind", "colorblind", "deuteran", "protan",
      "tritan", "accessib", "impair", "deficien", "disab"
    }) {
      words.ShouldNotContain(forbidden);
    }
  }

  [Test]
  public async Task ConfirmingWritesThePaletteAndLeavesForTheMainMenu() {
    var screen = await _openSkinSelect();
    _pick(screen, "clear");

    _confirm(screen);

    (await _waitUntil(() => _provider.MenuManager.GetCurrentMenu() == GameMenus.MAIN_MENU))
      .ShouldBeTrue("the colour screen never handed over to the main menu");
    _savedSkinName().ShouldBe("clear");
  }

  private static void _stepRight(GameMenu screen) =>
    _pickerOn(screen).GetNode<Button>("HBoxContainer/Right").EmitSignal(BaseButton.SignalName.Pressed);

  private static void _confirm(GameMenu screen) =>
    _pickerOn(screen).EmitSignal(BaseButton.SignalName.Pressed);

  private static void _pick(GameMenu screen, string skin) {
    var driver = screen.FindDescendants<SkinSelectDriver>().FirstOrDefault();
    driver.ShouldNotBeNull("the colour screen has no palette select");
    driver.onItemSelected(Variant.CreateFrom(skin));
  }

  private static UISelectButton _pickerOn(GameMenu screen) {
    var picker = screen.FindDescendants<UISelectButton>().FirstOrDefault();
    picker.ShouldNotBeNull("the colour screen has no picker");
    return picker;
  }

  private static string _valueShownOn(GameMenu screen) =>
    _pickerOn(screen).FindDescendants<MarqueeLabel>().First().Text;

  private static string[] _swatchColorsOf(Node screen) =>
    screen.GetNode<HBoxContainer>("Picker/VBox/Swatches").GetChildren()
      .OfType<ColorRect>()
      .Select(swatch => swatch.Color.ToHtml(false))
      .ToArray();

  private static string[] _basicColorsOf(string skinName) {
    var colors = SkinManager.Instance.GetSkin(skinName).GetColors(SkinColorIntensity.Basic);
    return new[] { SkinColor.TopFace, SkinColor.LeftFace, SkinColor.BottomFace, SkinColor.RightFace }
      .Select(face => colors[face].ToHtml(false))
      .ToArray();
  }

  private static string[] _captionsOf(Node screen) =>
    screen.FindDescendants<InputHintCard>()
      .Select(card => card.GetNode<Label>("HBox/CaptionBox/Caption").Text)
      .ToArray();

  private static string[] _allTextOf(Node screen) =>
    screen.FindDescendants<Control>()
      .Select(control => control switch {
        Label label => label.Text,
        Button button => button.Text,
        MarqueeLabel marquee => marquee.Text,
        _ => string.Empty
      })
      .Where(text => !string.IsNullOrEmpty(text))
      .ToArray();

  private static string _savedSkinName() {
    var configFile = new ConfigFile();
    configFile.Load(GameSettings.ConfigFilePath).ShouldBe(Error.Ok, "confirming wrote no settings file");
    return configFile.GetValue("general", "skin").As<string>();
  }

  private async Task<GameMenu> _openSkinSelect() {
    _provider.MenuManager.GoToMenu(GameMenus.SKIN_SELECT).ShouldBeTrue();
    await _idle();
    var screen = _currentScreen();
    screen.ShouldNotBeNull("the colour menu produced no screen");
    screen.ShouldBeOfType<SkinSelectMenu>();
    (await _waitUntil(() => !screen.IsInTransitionState())).ShouldBeTrue("the colour menu never finished entering");
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
