namespace Wfc.Core.Ui;

using Godot;

// Which overlays currently hold the screen.
//
// Menus, their widgets and the settings focus manager all watch for the same
// UICancel, and which one answered used to come down to tree order plus whoever
// called SetInputAsHandled first. Overlays register here while they are up, and
// anything that can be spoken over checks IsAnyOpen before acting on a global
// action.
//
// Pushing pauses the scene tree, so an overlay that has to keep working while it is
// up needs ProcessMode.Always. The pause is restored to whatever it was before the
// first overlay, rather than simply cleared, so an overlay over the pause menu
// leaves the game paused behind it.
public interface IModalStack {
  // True while any overlay holds the screen.
  bool IsAnyOpen { get; }

  // True when an overlay other than this one holds the screen. Overlays ask this
  // before handling input so only the topmost one answers.
  bool IsBlockedFor(Node owner);

  void Push(Node owner);

  void Pop(Node owner);
}
