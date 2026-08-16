namespace Wfc.Core.Ui;

using System.Collections.Generic;
using Godot;

public class ModalStack : IModalStack {
  private readonly IPauseOwnership _pause;
  private readonly List<Node> _owners = [];

  public ModalStack(IPauseOwnership pause) {
    _pause = pause;
  }

  public bool IsAnyOpen => _owners.Count > 0;

  public bool IsBlockedFor(Node owner) => _owners.Count > 0 && _owners[^1] != owner;

  public void Push(Node owner) {
    if (_owners.Contains(owner)) {
      return;
    }
    _owners.Add(owner);
    // One claim for the stack rather than one per overlay: an overlay opened over another
    // is still the same single reason the game is being held.
    _pause.Claim(this);
  }

  public void Pop(Node owner) {
    if (!_owners.Remove(owner)) {
      return;
    }
    if (_owners.Count == 0) {
      // Nothing is restored here. Whatever else was holding the game - the pause menu the
      // overlay was opened on top of - is still holding it in its own name.
      _pause.Release(this);
    }
  }
}
