namespace Wfc.test.instrumented.Menus;

using System;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Input.Controllers;
using Wfc.Core.Settings;
using Wfc.Entities.Ui.SettingsUI;
using Wfc.Entities.Ui.SettingsUI.UISelect;
using Wfc.Screens;
using Wfc.Screens.MenuManager;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// The D-Pad sends one event per push. A stick sends them for as long as it is held, and
// every one of those carries the same direction - so a settings row stepped with the
// stick has to end up where the same row stepped with the D-Pad ends up.
public class SettingsStickNavigationTests(Node testScene) : TestClass(testScene) {
  private const double SETTLE_TIMEOUT_SECONDS = 6.0;

  private FakeDependenciesProvider _provider = default!;
  private ControllerType _controllerBeforeTest;
  private bool _accumulatedInputBeforeTest;

  [Setup]
  public async Task Setup() {
    _controllerBeforeTest = GameSettings.LastUsedController;
    _accumulatedInputBeforeTest = Input.UseAccumulatedInput;
    Input.UseAccumulatedInput = false;
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await _idle();
  }

  [Cleanup]
  public void Cleanup() {
    Input.ParseInputEvent(_stick(JoyAxis.LeftX, 0f));
    Input.ParseInputEvent(_stick(JoyAxis.LeftY, 0f));
    Input.UseAccumulatedInput = _accumulatedInputBeforeTest;
    GameSettings.LastUsedController = _controllerBeforeTest;
    _provider.QueueFree();
  }

  // The row the stick is pointed at is the row it steps. A push travels through every
  // strength on its way, and the ones short of the step have to be answered too.
  [Test]
  public async Task APushSidewaysLeavesTheFocusOnTheRow() {
    var screen = await _openSettings();
    var tabs = screen.FindDescendants<SettingsTabManager>().First();
    var select = screen.FindDescendants<UISelectButton>().First();
    _focusOwner(screen).ShouldBe(select, "the settings menu did not open on the first row");

    await _pushAndHold(JoyAxis.LeftX, 0.3f, 0.6f, 0.8f, 1f, 0.95f, 1f);

    _focusOwner(screen).ShouldBe(select, "the stick walked the focus off the row");
    tabs.CurrentPanelIndex.ShouldBe(0, "the stick switched tab from a row");
  }

  // The tab row is where sideways is meant to change tabs, so swallowing what does not
  // step the menu must not swallow what does.
  [Test]
  public async Task APushSidewaysOnTheTabRowStillSwitchesTabs() {
    var screen = await _openSettings();
    var tabs = screen.FindDescendants<SettingsTabManager>().First();

    await _pushAndHold(JoyAxis.LeftY, -1f);
    await _release(JoyAxis.LeftY);
    await _pushAndHold(JoyAxis.LeftX, 1f);

    tabs.CurrentPanelIndex.ShouldBe(1, "the stick no longer switches tabs from the tab row");
  }

  private static InputEventJoypadMotion _stick(JoyAxis axis, float value) =>
    new() { Axis = axis, AxisValue = value, Device = 0 };

  private async Task _pushAndHold(JoyAxis axis, params float[] values) {
    var tree = TestScene.GetTree();
    foreach (var value in values) {
      await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
      Input.ParseInputEvent(_stick(axis, value));
      await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
  }

  private async Task _release(JoyAxis axis) => await _pushAndHold(axis, 0f);

  private static Control? _focusOwner(Node screen) => screen.GetViewport().GuiGetFocusOwner();

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
