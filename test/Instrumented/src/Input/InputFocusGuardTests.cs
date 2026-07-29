namespace Wfc.test.instrumented;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Autoload;
using Wfc.Core.Input;

// Covers the guard that keeps an action held when the game lost focus from staying
// held. It runs against the real autoloaded guard and hands it the notifications the
// OS sends, so the assumption it is built on - that they reach a node this far down -
// is checked here rather than by a player alt-tabbing back to a cube that walked off
// on its own.
//
// The window's notifications, not the application's: the display server holds those
// back behind a debounce, so a player who alt-tabs straight back never gets them at
// all. Swapping the two here looks harmless and puts the bug back.
//
// Handed to the guard alone rather than propagated from the root window. The engine
// sends this one to everything under the window, and the viewport answers it by
// dropping whatever had mouse and GUI focus - which, in a suite that shares one tree
// with every other test, is somebody else's furniture.
public class InputFocusGuardTests(Node testScene) : TestClass(testScene) {
  private const string MOVE_RIGHT = "move_right";

  [Cleanup]
  public void Cleanup() => InputFocusGuard.ReleaseHeldActions();

  // The release of a key let go of in another application never arrives, so without
  // this the cube is still walking when the player comes back.
  [Test]
  public void ReleasesAHeldActionWhenTheGameLosesFocus() {
    Input.ActionPress(MOVE_RIGHT);
    Input.IsActionPressed(MOVE_RIGHT).ShouldBeTrue();

    _loseFocus();

    Input.IsActionPressed(MOVE_RIGHT).ShouldBeFalse();
  }

  // The menus run on the engine's own ui_* actions, which are not all in
  // InputManager.Actions and latch just the same.
  [Test]
  public void ReleasesEveryActionTheInputMapKnowsAbout() {
    foreach (var action in InputMap.GetActions()) {
      Input.ActionPress(action);
    }

    _loseFocus();

    foreach (var action in InputMap.GetActions()) {
      Input.IsActionPressed(action)
        .ShouldBeFalse($"Action '{action}' was left held after the game lost focus");
    }
  }

  // The compositor tells the window which keys it believes are held the moment focus
  // returns to it, so the way back has to be swept as well as the way out.
  [Test]
  public void ReleasesAHeldActionWhenTheGameGetsFocusBack() {
    Input.ActionPress(MOVE_RIGHT);

    _guard().Notification((int)Node.NotificationWMWindowFocusIn);

    Input.IsActionPressed(MOVE_RIGHT).ShouldBeFalse();
  }

  private static void _loseFocus() => _guard().Notification((int)Node.NotificationWMWindowFocusOut);

  private static InputFocusGuard _guard() => AutoloadManager.Instance.InputFocusGuard;
}
