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
using Wfc.Entities.Ui.SettingsUI.Grid;
using Wfc.Screens;
using Wfc.Screens.MenuManager;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// Pointing at a settings row focuses it, and the engine reports the pointer entering
// a row whenever the layout moves under it - which is what changing the window mode
// does. The row that arrives under a cursor nobody touched must not take the
// selection away from the row the player walked to.
//
// The window mode itself is not changed here: it stalls a frame and upsets the
// suites that measure against the clock. The settling the panel announces is what
// the rows answer, so that is what is driven.
public class SettingsPointerFocusTests(Node testScene) : TestClass(testScene) {
  private const int VIDEO_PANEL_INDEX = 1;
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
  public async Task ARowArrivingUnderTheCursorDoesNotTakeTheSelection() {
    var screen = await _openVideoTab();
    var walkedTo = await _walkToSecondRow(screen);

    PointerFocus.SuspendWhileTheViewSettles();
    _otherRow(screen, walkedTo).EmitSignal(Control.SignalName.MouseEntered);
    await _idle();

    _focusedRowName(screen).ShouldBe(walkedTo, "a row that slid under the cursor took the selection");
  }

  [Test]
  public async Task PointingAtARowStillFocusesIt() {
    var screen = await _openVideoTab();
    var walkedTo = await _walkToSecondRow(screen);
    (await _waitUntil(() => PointerFocus.IsPlayerPointing)).ShouldBeTrue("the view never finished settling");

    var pointedAt = _otherRow(screen, walkedTo);
    pointedAt.EmitSignal(Control.SignalName.MouseEntered);
    await _idle();

    _focusedRowName(screen).ShouldBe(pointedAt.Name.ToString(), "pointing at a row no longer focuses it");
  }

  // Two steps in, so there is a row above and below to be wrongly pulled to.
  private async Task<string> _walkToSecondRow(GameMenu screen) {
    await _pressDown();
    await _pressDown();
    var name = _focusedRowName(screen);
    name.ShouldNotBe("<none>", "walking the panel focused nothing");
    return name;
  }

  private static UIGridRow _otherRow(Node screen, string focusedRowName) =>
    screen.FindDescendants<UIGridRow>()
      .First(row => row.IsVisibleInTree() && row.Name.ToString() != focusedRowName);

  private static string _focusedRowName(Node screen) {
    for (Node? walked = screen.GetViewport().GuiGetFocusOwner(); walked != null; walked = walked.GetParent()) {
      if (walked is UIGridRow row) {
        return row.Name.ToString();
      }
    }
    return "<none>";
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
    screen.FindDescendants<SettingsTabManager>().First().SwitchToPanel(VIDEO_PANEL_INDEX);
    await _idle();
    return screen;
  }

  private GameMenu? _currentScreen() =>
    _provider.FindDescendants<GameMenu>().FirstOrDefault(screen => !screen.IsQueuedForDeletion());

  private async Task<bool> _waitUntil(Func<bool> condition) {
    var tree = TestScene.GetTree();
    var start = Time.GetTicksMsec();
    while ((Time.GetTicksMsec() - start) < SETTLE_TIMEOUT_SECONDS * 1000) {
      if (condition()) {
        return true;
      }
      await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
    return condition();
  }

  private async Task _idle() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
  }
}
