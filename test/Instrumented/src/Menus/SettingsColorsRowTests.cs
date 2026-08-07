namespace Wfc.test.instrumented.Menus;

using System;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Settings;
using Wfc.Entities.Ui;
using Wfc.Entities.Ui.SettingsUI.Grid;
using Wfc.Entities.Ui.SettingsUI.UISelect;
using Wfc.Screens;
using Wfc.Screens.MenuManager;
using Wfc.Skin;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// The palette is asked about once, on a first launch, so the settings menu is where it
// lives from then on. The row carries the colours themselves beside the name, for the
// same reason the first-run screen does: "Clear" tells a player nothing about whether
// they can separate the four.
public class SettingsColorsRowTests(Node testScene) : TestClass(testScene) {
  private const double SETTLE_TIMEOUT_SECONDS = 6.0;

  private FakeDependenciesProvider _provider = default!;
  private string _skinBeforeTest = default!;

  [Setup]
  public async Task Setup() {
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
  public async Task TheGeneralTabCarriesTheColoursRow() {
    var screen = await _openSettings();

    var row = _colorsRow(screen);
    row.ShouldNotBeNull("the general tab has no colours row");
    row.Visible.ShouldBeTrue();
    // The row is still the select's: focus has to land on something that can be
    // stepped, not on the swatches riding beside it.
    row.GetFocusableControl().ShouldBeOfType<UISelectButton>();
  }

  [Test]
  public async Task TheSwatchesFollowThePalettePickedOnTheRow() {
    var screen = await _openSettings();
    _swatchColorsOn(screen).ShouldBe(_basicColorsOf(SkinManager.DEFAULT_SKIN_NAME));

    screen.FindDescendants<SkinSelectDriver>().First()
      .onItemSelected(Variant.CreateFrom("clear"));
    await _idle();

    _swatchColorsOn(screen).ShouldBe(_basicColorsOf("clear"));
  }

  // The row it belongs to owns the space; the swatches must not push the value out of
  // the row or take the slack away from it.
  [Test]
  public async Task TheSwatchesSitInsideTheRow() {
    var screen = await _openSettings();
    var row = _colorsRow(screen)!;
    var swatches = screen.FindDescendants<SkinSwatches>().First();

    var rowRect = new Rect2(row.GlobalPosition, row.Size);
    var swatchRect = new Rect2(swatches.GlobalPosition, swatches.Size);
    rowRect.Encloses(swatchRect).ShouldBeTrue($"swatches at {swatchRect} escaped the row at {rowRect}");
  }

  private static UIGridRow? _colorsRow(Node screen) =>
    screen.FindDescendants<UIGridRow>()
      .FirstOrDefault(row => row.FindDescendants<SkinSelectDriver>().Any());

  private static string[] _swatchColorsOn(Node screen) =>
    screen.FindDescendants<SkinSwatches>().First().GetChildren()
      .OfType<ColorRect>()
      .Select(swatch => swatch.Color.ToHtml(false))
      .ToArray();

  private static string[] _basicColorsOf(string skinName) {
    var colors = SkinManager.Instance.GetSkin(skinName).GetColors(SkinColorIntensity.Basic);
    return new[] { SkinColor.TopFace, SkinColor.LeftFace, SkinColor.BottomFace, SkinColor.RightFace }
      .Select(face => colors[face].ToHtml(false))
      .ToArray();
  }

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
