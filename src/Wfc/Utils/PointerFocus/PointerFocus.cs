namespace Wfc.Utils;

using Godot;

// Whether the cursor sitting over a widget is the player pointing at it.
//
// The engine works out what the cursor is over again whenever the interface moves,
// so a screen that relays itself out under a still cursor reports the mouse entering
// whatever slid into that spot. Focus follows the mouse here, and a settings row
// arriving under an untouched cursor would take the selection away from the row the
// player is on.
//
// The cursor cannot be asked whether it moved: changing the window mode remaps the
// coordinates it is reported in and makes the engine announce a move of its own.
// Whatever moves the interface says so instead, and pointing is taken as meant again
// once the view has stopped moving.
public static class PointerFocus {
  // Long enough to cover a window changing mode, being resized and being centred
  // again, each of which lands its own round of layout.
  private const ulong SETTLING_MSEC = 600;

  private static ulong _settledAt;

  public static bool IsPlayerPointing => Time.GetTicksMsec() >= _settledAt;

  public static void SuspendWhileTheViewSettles() => _settledAt = Time.GetTicksMsec() + SETTLING_MSEC;
}
