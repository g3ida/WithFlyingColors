namespace Wfc.Core.Ui.Test;

using System.Collections.Generic;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Ui;

// The game is held still by three unrelated things - the pause menu, an overlay holding the
// screen, and the orchestrator swapping a level behind the cover - and before this each of
// them wrote the tree's one pause flag directly, so the last to touch it won. What matters
// here is that letting go of one claim never lets go of another's.
public class PauseOwnershipTests(Node testScene) : TestClass(testScene) {
  private SceneTree _tree = default!;
  private bool _pausedBeforeTest;
  private readonly List<Node> _nodes = [];

  [Setup]
  public void Setup() {
    _tree = TestScene.GetTree();
    _pausedBeforeTest = _tree.Paused;
    _tree.Paused = false;
  }

  [Cleanup]
  public void Cleanup() {
    _tree.Paused = _pausedBeforeTest;
    foreach (var node in _nodes) {
      if (GodotObject.IsInstanceValid(node)) {
        node.Free();
      }
    }
    _nodes.Clear();
  }

  [Test]
  public void NothingHeldMeansTheGameRunsTest() {
    var pause = new PauseOwnership(_tree);

    pause.IsHeld.ShouldBeFalse();
    _tree.Paused.ShouldBeFalse();
  }

  [Test]
  public void AClaimHoldsTheGameAndReleasingLetsItRunTest() {
    var pause = new PauseOwnership(_tree);
    var owner = new object();

    pause.Claim(owner);
    _tree.Paused.ShouldBeTrue();
    pause.IsHeldBy(owner).ShouldBeTrue();

    pause.Release(owner);
    _tree.Paused.ShouldBeFalse();
    pause.IsHeldBy(owner).ShouldBeFalse();
  }

  // The whole point of the thing. This is the F10 hazard: the orchestrator finishing a level
  // swap used to write the flag false outright, which would have let go of an overlay's hold
  // as well as its own.
  [Test]
  public void ReleasingOneClaimLeavesAnotherHoldingTest() {
    var pause = new PauseOwnership(_tree);
    var orchestrator = new object();
    var overlay = new object();

    pause.Claim(overlay);
    pause.Claim(orchestrator);
    pause.Release(orchestrator);

    _tree.Paused.ShouldBeTrue("the overlay is still holding the game");

    pause.Release(overlay);
    _tree.Paused.ShouldBeFalse();
  }

  // The pause menu re-pauses itself every time the window loses focus, whether or not it had
  // already, and the orchestrator claims once per swap however many it runs.
  [Test]
  public void ClaimingTwiceNeedsOnlyOneReleaseTest() {
    var pause = new PauseOwnership(_tree);
    var owner = new object();

    pause.Claim(owner);
    pause.Claim(owner);
    pause.Release(owner);

    _tree.Paused.ShouldBeFalse();
  }

  // Teardown paths let go without knowing whether they ever took hold.
  [Test]
  public void ReleasingSomethingThatNeverClaimedChangesNothingTest() {
    var pause = new PauseOwnership(_tree);
    var owner = new object();
    pause.Claim(owner);

    pause.Release(new object());

    _tree.Paused.ShouldBeTrue();
  }

  // A level swapped out from under an overlay takes the overlay with it, and a claim held by
  // something that no longer exists has nobody left to release it.
  [Test]
  public void AClaimHeldByAFreedNodeStopsCountingTest() {
    var pause = new PauseOwnership(_tree);
    var doomed = new Node();
    _nodes.Add(doomed);
    var survivor = new object();

    pause.Claim(doomed);
    pause.Claim(survivor);
    doomed.Free();

    pause.Release(survivor);

    pause.IsHeld.ShouldBeFalse("a freed owner cannot go on holding the game");
    _tree.Paused.ShouldBeFalse();
  }
}
