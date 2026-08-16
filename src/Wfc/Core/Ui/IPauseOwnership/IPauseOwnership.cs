namespace Wfc.Core.Ui;

using Godot;

// Who is holding the game still.
//
// The tree's pause flag is a single boolean, and three unrelated things need the game held
// at once: the pause menu, an overlay that has taken the screen, and the orchestrator while
// it swaps a level behind the cover. Written directly, whichever touched it last won - so
// the orchestrator had to ask the pause menu whether it was paused before daring to let go,
// and the title card had to swallow the pause key so that nothing could take the flag while
// a swap was covering.
//
// Here each of them claims and releases in its own name, and the game runs again only once
// the last claim is gone. Nothing else writes SceneTree.Paused.
public interface IPauseOwnership {
  // True while anything is holding the game.
  bool IsHeld { get; }

  bool IsHeldBy(object owner);

  // Claiming twice in the same name is the same as claiming once: the pause menu re-pauses
  // itself whenever the window loses focus, whether or not it already had.
  void Claim(object owner);

  void Release(object owner);
}
