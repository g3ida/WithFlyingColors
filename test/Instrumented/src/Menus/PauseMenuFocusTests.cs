namespace Wfc.test.instrumented.Menus;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Screens;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// Builds the real pause menu and takes the window's focus away from underneath it.
//
// It hangs off the window's notification rather than the application's on purpose:
// the display server holds the application one back behind a debounce, so it lands
// late and is never sent at all to a player who alt-tabs straight back. Swapping the
// two here looks harmless and puts the bug back.
//
// Sent down this provider rather than from the root window, which is where the engine
// starts it: the viewport answers this notification by dropping whatever had mouse
// and GUI focus, and every other test in the suite shares that tree.
public class PauseMenuFocusTests(Node testScene) : TestClass(testScene) {
  private FakeDependenciesProvider _provider = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await _idle();
    _provider.InstantiateChildNode<PauseMenu>();
    await _idle();
  }

  [Cleanup]
  public void Cleanup() {
    TestScene.GetTree().Paused = false;
    _provider.QueueFree();
  }

  [Test]
  public async Task PausesWhenTheWindowLosesFocus() {
    TestScene.GetTree().Paused.ShouldBeFalse();

    await _loseWindowFocus();

    TestScene.GetTree().Paused.ShouldBeTrue();
  }

  // Coming back is the player's cue to unpause, not ours: dropping them straight
  // into a level they have not been looking at is how the pause earns its keep.
  [Test]
  public async Task StaysPausedWhenTheWindowGetsFocusBack() {
    await _loseWindowFocus();

    _provider.PropagateNotification((int)Node.NotificationWMWindowFocusIn);
    await _idle();

    TestScene.GetTree().Paused.ShouldBeTrue();
  }

  private async Task _loseWindowFocus() {
    _provider.PropagateNotification((int)Node.NotificationWMWindowFocusOut);
    await _idle();
  }

  private async Task _idle() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
  }
}
