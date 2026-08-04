namespace Wfc.test.instrumented.Menus;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Input.Controllers;
using Wfc.Core.Settings;
using Wfc.Screens;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// The pause menu's Settings entry swaps the buttons for the settings view inside
// the same overlay: the game has to stay paused underneath the whole time, and
// closing the view has to bring the buttons back rather than resume play.
public class PauseSettingsMenuTests(Node testScene) : TestClass(testScene) {
  private FakeDependenciesProvider _provider = default!;
  private PauseMenu _pauseMenu = default!;
  private ControllerType _savedController;

  [Setup]
  public async Task Setup() {
    // Closing validates the bindings of whatever device was used last. Earlier
    // suites leave that on the gamepad, and with a pad plugged into the test
    // machine the default InputMap has no pad bindings to satisfy it - so the
    // device is pinned to the keyboard, whose default bindings are complete.
    _savedController = GameSettings.LastUsedController;
    GameSettings.LastUsedController = ControllerType.Keyboard;
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await _idle();
    _pauseMenu = _provider.InstantiateChildNode<PauseMenu>();
    await _idle();
  }

  [Cleanup]
  public void Cleanup() {
    GameSettings.LastUsedController = _savedController;
    TestScene.GetTree().Paused = false;
    _provider.QueueFree();
  }

  [Test]
  public async Task SettingsOpensInsideThePauseOverlayAndKeepsTheGamePaused() {
    await _pause();
    var settingsView = _settingsView();
    settingsView.IsOpen.ShouldBeFalse();

    _settingsButton().EmitSignal(BaseButton.SignalName.Pressed);
    await _idle();

    settingsView.IsOpen.ShouldBeTrue();
    settingsView.Visible.ShouldBeTrue();
    TestScene.GetTree().Paused.ShouldBeTrue();
  }

  [Test]
  public async Task ClosingTheSettingsReturnsToThePauseButtonsStillPaused() {
    await _pause();
    _settingsButton().EmitSignal(BaseButton.SignalName.Pressed);
    await _idle();
    var settingsView = _settingsView();

    settingsView.TryClose().ShouldBeTrue();
    await _idle();

    settingsView.IsOpen.ShouldBeFalse();
    settingsView.Visible.ShouldBeFalse();
    TestScene.GetTree().Paused.ShouldBeTrue();
  }

  private async Task _pause() {
    _provider.PropagateNotification((int)Node.NotificationWMWindowFocusOut);
    await _idle();
    TestScene.GetTree().Paused.ShouldBeTrue();
  }

  private Button _settingsButton() =>
    _pauseMenu.GetNode<Button>("PauseMenuImpl/CenterContainer/VBoxContainer/SettingsButton");

  private PauseSettingsMenu _settingsView() =>
    _pauseMenu.GetNode<PauseSettingsMenu>("PauseSettingsMenu");

  private async Task _idle() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
  }
}
