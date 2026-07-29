namespace Wfc.Core.Input;

using Godot;

// Lets go of every action the player was still holding when the game lost focus.
//
// The OS stops delivering input to a window that is not focused, so a key let go
// of while the player is in another application never arrives as a release and
// the action stays down: they come back to a cube that walks on by itself, and to
// menus that swallow their next press, since Godot only refreshes just-pressed
// when an action changes state.
//
// Created by AutoloadManager, so it lives for the whole run and covers the menus
// as well as the levels.
public partial class InputFocusGuard : Node {
  public override void _Notification(int what) {
    base._Notification(what);

    // The window's notifications rather than the application's: the display server
    // holds those back behind a debounce so it can tell one of our own windows
    // taking focus from another application taking it, which makes them both late
    // and, for a player who alt-tabs straight back, never sent at all.
    //
    // Both edges, because the compositor hands the keys it believes are held back
    // to the window when focus returns to it.
    if (what == NotificationWMWindowFocusOut || what == NotificationWMWindowFocusIn) {
      ReleaseHeldActions();
    }
  }

  // Every action rather than the ones in InputManager.Actions: the engine's own
  // ui_* bindings latch the same way, and the menus are built on them.
  //
  // No FlushBufferedEvents here, tempting as it is against a press still sitting in
  // the queue: it pumps the input queue from inside a notification, which means
  // _input runs in the middle of a propagation. Sweeping both edges of focus covers
  // that press anyway, on the way back in.
  public static void ReleaseHeldActions() {
    foreach (var action in InputMap.GetActions()) {
      Godot.Input.ActionRelease(action);
    }
  }
}
