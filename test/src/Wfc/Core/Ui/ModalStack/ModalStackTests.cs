namespace Wfc.Core.Ui.Test;

using System.Collections.Generic;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Ui;

public class ModalStackTests(Node testScene) : TestClass(testScene) {
  private SceneTree _tree = default!;
  private bool _pausedBeforeTest;
  private readonly List<Node> _owners = [];

  [Setup]
  public void Setup() {
    _tree = TestScene.GetTree();
    _pausedBeforeTest = _tree.Paused;
  }

  [Cleanup]
  public void Cleanup() {
    _tree.Paused = _pausedBeforeTest;
    foreach (var owner in _owners) {
      owner.Free();
    }
    _owners.Clear();
  }

  // Owners are never added to the tree: the stack only ever compares them.
  private Node NewOwner() {
    var owner = new Node();
    _owners.Add(owner);
    return owner;
  }

  [Test]
  public void EmptyStack_IsNotOpenAndBlocksNobody() {
    var stack = new ModalStack(_tree);

    stack.IsAnyOpen.ShouldBeFalse();
    stack.IsBlockedFor(NewOwner()).ShouldBeFalse();
  }

  [Test]
  public void Push_OpensTheStackAndPausesTheTree() {
    _tree.Paused = false;
    var stack = new ModalStack(_tree);

    stack.Push(NewOwner());

    stack.IsAnyOpen.ShouldBeTrue();
    _tree.Paused.ShouldBeTrue();
  }

  [Test]
  public void PopOfLastOwner_ClosesTheStackAndUnpausesTheTree() {
    _tree.Paused = false;
    var stack = new ModalStack(_tree);
    var owner = NewOwner();

    stack.Push(owner);
    stack.Pop(owner);

    stack.IsAnyOpen.ShouldBeFalse();
    _tree.Paused.ShouldBeFalse();
  }

  // The pause menu is already paused underneath its own overlays, so the stack has to
  // put the flag back rather than simply clear it.
  [Test]
  public void PopOfLastOwner_RestoresAPauseThatPredatedTheStack() {
    _tree.Paused = true;
    var stack = new ModalStack(_tree);
    var owner = NewOwner();

    stack.Push(owner);
    stack.Pop(owner);

    _tree.Paused.ShouldBeTrue();
  }

  [Test]
  public void NestedOwners_KeepTheTreePausedUntilTheLastPop() {
    _tree.Paused = false;
    var stack = new ModalStack(_tree);
    var first = NewOwner();
    var second = NewOwner();

    stack.Push(first);
    stack.Push(second);
    stack.Pop(second);

    stack.IsAnyOpen.ShouldBeTrue();
    _tree.Paused.ShouldBeTrue();

    stack.Pop(first);

    stack.IsAnyOpen.ShouldBeFalse();
    _tree.Paused.ShouldBeFalse();
  }

  [Test]
  public void IsBlockedFor_LetsOnlyTheTopmostOwnerThrough() {
    var stack = new ModalStack(_tree);
    var below = NewOwner();
    var top = NewOwner();

    stack.Push(below);
    stack.Push(top);

    stack.IsBlockedFor(top).ShouldBeFalse();
    stack.IsBlockedFor(below).ShouldBeTrue();
    stack.IsBlockedFor(NewOwner()).ShouldBeTrue();
  }

  [Test]
  public void RepeatedPush_DoesNotNeedAMatchingExtraPop() {
    _tree.Paused = false;
    var stack = new ModalStack(_tree);
    var owner = NewOwner();

    stack.Push(owner);
    stack.Push(owner);
    stack.Pop(owner);

    stack.IsAnyOpen.ShouldBeFalse();
    _tree.Paused.ShouldBeFalse();
  }

  // Teardown paths pop defensively, without knowing whether they ever pushed.
  [Test]
  public void PopOfAnOwnerThatNeverPushed_LeavesTheStackAlone() {
    _tree.Paused = false;
    var stack = new ModalStack(_tree);
    var owner = NewOwner();

    stack.Push(owner);
    stack.Pop(NewOwner());

    stack.IsAnyOpen.ShouldBeTrue();
    _tree.Paused.ShouldBeTrue();
  }
}
