namespace Wfc.test.instrumented.Menus;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Input;
using Wfc.Screens;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// Back is the second way out of the pause menu, so a device with nothing bound to
// pause is not left with the buttons as its only exit. It goes one way only: it
// closes the menu, and leaves a level that is running alone.
public class PauseMenuCloseTests(Node testScene) : TestClass(testScene) {
  private FakeDependenciesProvider _provider = default!;
  private PauseMenu _pauseMenu = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await _idle();
    _pauseMenu = _provider.InstantiateChildNode<PauseMenu>();
    await _idle();
  }

  [Cleanup]
  public void Cleanup() {
    TestScene.GetTree().Paused = false;
    _provider.QueueFree();
  }

  [Test]
  public async Task BackClosesThePauseMenu() {
    await _pause();
    TestScene.GetTree().Paused.ShouldBeTrue();

    _pressBack();

    TestScene.GetTree().Paused.ShouldBeFalse("back left the game paused");
  }

  [Test]
  public void BackLeavesARunningGameAlone() {
    _pressBack();

    TestScene.GetTree().Paused.ShouldBeFalse("back paused a game nobody asked to pause");
  }

  private async Task _pause() {
    _provider.PropagateNotification((int)Node.NotificationWMWindowFocusOut);
    await _idle();
  }

  // The menu reads the action rather than the event, so the event only has to exist.
  private void _pressBack() {
    _provider.Input.Press(IInputManager.Action.UICancel);
    _pauseMenu._Input(new InputEventAction());
    _provider.Input.ReleaseAll();
  }

  private async Task _idle() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
  }
}
