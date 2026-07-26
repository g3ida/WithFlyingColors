namespace Wfc.Core.Ui;

using System.Collections.Generic;
using Godot;

public class ModalStack : IModalStack {
  private readonly SceneTree _tree;
  private readonly List<Node> _owners = [];

  // What the pause flag was before the first overlay went up. The pause menu is
  // already paused underneath its own overlays, so the stack restores rather than
  // clears.
  private bool _wasPausedBeforeFirstPush;

  public ModalStack(SceneTree tree) {
    _tree = tree;
  }

  public bool IsAnyOpen => _owners.Count > 0;

  public bool IsBlockedFor(Node owner) => _owners.Count > 0 && _owners[^1] != owner;

  public void Push(Node owner) {
    if (_owners.Contains(owner)) {
      return;
    }
    if (_owners.Count == 0) {
      _wasPausedBeforeFirstPush = _tree.Paused;
      _tree.Paused = true;
    }
    _owners.Add(owner);
  }

  public void Pop(Node owner) {
    if (!_owners.Remove(owner)) {
      return;
    }
    if (_owners.Count == 0) {
      _tree.Paused = _wasPausedBeforeFirstPush;
    }
  }
}
