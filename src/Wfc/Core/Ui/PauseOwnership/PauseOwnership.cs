namespace Wfc.Core.Ui;

using System.Collections.Generic;
using Godot;

public class PauseOwnership : IPauseOwnership {
  private readonly SceneTree _tree;
  private readonly List<object> _owners = [];

  public PauseOwnership(SceneTree tree) {
    _tree = tree;
  }

  public bool IsHeld {
    get {
      _forgetFreedOwners();
      return _owners.Count > 0;
    }
  }

  public bool IsHeldBy(object owner) => _owners.Contains(owner);

  public void Claim(object owner) {
    if (!_owners.Contains(owner)) {
      _owners.Add(owner);
    }
    _apply();
  }

  public void Release(object owner) {
    _owners.Remove(owner);
    _apply();
  }

  // A claim held by a node that has since been freed - a level swapped out from under an
  // overlay that was still up - has nobody left to release it, and would hold the game for
  // the rest of the run. The owners that hold claims release them as they leave the tree;
  // this is the backstop for the ones that cannot.
  private void _forgetFreedOwners() =>
    _owners.RemoveAll(owner => owner is GodotObject godotOwner && !GodotObject.IsInstanceValid(godotOwner));

  private void _apply() {
    _forgetFreedOwners();
    _tree.Paused = _owners.Count > 0;
  }
}
