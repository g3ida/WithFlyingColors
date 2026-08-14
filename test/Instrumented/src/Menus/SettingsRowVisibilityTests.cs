namespace Wfc.test.instrumented.Menus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Input.Controllers;
using Wfc.Core.Settings;
using Wfc.Entities.Ui.SettingsUI;
using Wfc.Entities.Ui.SettingsUI.Grid;
using Wfc.Screens;
using Wfc.Screens.MenuManager;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// A settings row can come and go while its panel is open - the resizable window row
// is only there in windowed mode - so what the focus can move to is asked as it
// walks rather than settled when the panel was built.
//
// The fullscreen box that drives it is not touched here: ticking it changes the real
// window mode, which stalls a frame and upsets the suites that measure against the
// clock. The row is hidden directly instead, which is all that box does.
public class SettingsRowVisibilityTests(Node testScene) : TestClass(testScene) {
  private const int VIDEO_PANEL_INDEX = 1;
  private const string RESIZABLE_ROW = "Resizable";
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
    Input.ParseInputEvent(_stick(0f));
    Input.UseAccumulatedInput = _accumulatedInputBeforeTest;
    GameSettings.LastUsedController = _controllerBeforeTest;
    _provider.QueueFree();
  }

  [Test]
  public async Task TheResizableRowIsThereInWindowedMode() {
    var screen = await _openVideoTab();
    GameSettings.Fullscreen.ShouldBeFalse("the test run is not windowed, so there is nothing to check");

    _row(screen, RESIZABLE_ROW).Visible
        .ShouldBeTrue("a window with edges to drag offered no way to allow it");
  }

  [Test]
  public async Task AHiddenRowIsWalkedPast() {
    var screen = await _openVideoTab();
    (await _walk(screen, 5)).ShouldContain(RESIZABLE_ROW, "the row was not in the walk to begin with");

    _row(screen, RESIZABLE_ROW).Visible = false;
    await _idle();

    (await _walk(screen, 5)).ShouldNotContain(RESIZABLE_ROW, "the focus landed on a row that is not there");
  }

  // The row used to be filtered out when the panel was built, so one that came back
  // could never be reached again without leaving the tab and returning.
  [Test]
  public async Task ARowPutBackIsReachableAgain() {
    var screen = await _openVideoTab();
    var row = _row(screen, RESIZABLE_ROW);

    row.Visible = false;
    await _idle();
    await _walk(screen, 3);
    row.Visible = true;
    await _idle();

    (await _walk(screen, 5)).ShouldContain(RESIZABLE_ROW, "the row came back with no way to reach it");
  }

  private static UIGridRow _row(Node screen, string name) =>
    screen.FindDescendants<UIGridRow>().First(row => row.Name.ToString() == name);

  // The rows the focus visits over that many steps down, named by the row each
  // landed control belongs to.
  private async Task<List<string>> _walk(Node screen, int steps) {
    var visited = new List<string>();
    for (var i = 0; i < steps; i++) {
      await _pressDown();
      var owner = screen.GetViewport().GuiGetFocusOwner();
      visited.Add(owner == null ? "<none>" : _rowNameOf(owner));
    }
    return visited;
  }

  private static string _rowNameOf(Node node) {
    for (var walked = node; walked != null; walked = walked.GetParent()) {
      if (walked is UIGridRow row) {
        return row.Name.ToString();
      }
    }
    return node.Name.ToString();
  }

  private async Task _pressDown() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    Input.ParseInputEvent(_stick(1f));
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    Input.ParseInputEvent(_stick(0f));
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
  }

  private static InputEventJoypadMotion _stick(float value) =>
    new() { Axis = JoyAxis.LeftY, AxisValue = value, Device = 0 };

  private async Task<GameMenu> _openVideoTab() {
    var screen = await _openSettings();
    screen.FindDescendants<SettingsTabManager>().First().SwitchToPanel(VIDEO_PANEL_INDEX);
    await _idle();
    return screen;
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
