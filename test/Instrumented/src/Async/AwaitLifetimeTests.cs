namespace Wfc.test.instrumented.Async;

using System;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;

// What an `await ToSignal(...)` does when the level under it is torn down. Every async method in
// the game rests on this, and it is not the same answer for the two things they wait on:
//
//   a node's own child - a Tween, an AnimationPlayer, a Timer - dies with it, and the wait
//   simply never comes back. Nothing after the await runs, and nothing is raised.
//
//   the SceneTree, and the timers it hands out, outlive every node. Those waits always come
//   back, on a node that may have left the tree in the meantime - where GetTree() and
//   GetWindow() answer null and the first use of either throws.
//
// Which is why a guard is worth writing for the second and pointless for the first. Pinned down
// here because nothing at any of the call sites says so, and the two read identically.
public class AwaitLifetimeTests(Node testScene) : TestClass(testScene) {
  // Long enough that it cannot finish on its own inside the test.
  private const float NEVER_FINISHES = 10.0f;
  private const int FRAMES_TO_SETTLE = 20;

  [Test]
  public async Task AWaitOnAFreedTweenNeverComesBackTest() {
    var node = new Node2D();
    TestScene.AddChild(node);
    await _idle();
    var tween = node.CreateTween();
    tween.TweenProperty(node, "position:x", 500f, NEVER_FINISHES);

    var wait = _watch(() => node.ToSignal(tween, Tween.SignalName.Finished));
    await _idle();
    node.QueueFree();
    await _settle();

    wait.Resumed.ShouldBeFalse("the tail of the method must not run");
    wait.Caught.ShouldBeNull("and nothing should be raised for anyone to catch");
  }

  [Test]
  public async Task AWaitOnAFreedAnimationPlayerNeverComesBackTest() {
    var host = new Node2D();
    var animation = new AnimationPlayer();
    host.AddChild(animation);
    TestScene.AddChild(host);
    await _idle();

    var wait = _watch(() => host.ToSignal(animation, AnimationPlayer.SignalName.AnimationFinished));
    await _idle();
    host.QueueFree();
    await _settle();

    wait.Resumed.ShouldBeFalse();
    wait.Caught.ShouldBeNull();
  }

  // The one that needs guarding, and the reason this file exists.
  [Test]
  public async Task AWaitOnTheTreeComesBackOnANodeThatHasLeftItTest() {
    var node = new Node2D();
    TestScene.AddChild(node);
    await _idle();
    var tree = TestScene.GetTree();

    TestScene.RemoveChild(node);
    var wait = _watch(() => tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame));
    await _settle();

    wait.Resumed.ShouldBeTrue("the tree outlives the node, so the wait always comes back");
    node.GetTree().ShouldBeNull("which is why the tail cannot ask the node for the tree again");
    node.GetWindow().ShouldBeNull();
    node.QueueFree();
  }

  private sealed class Watched {
    public bool Resumed;
    public Exception? Caught;
  }

  private static Watched _watch(Func<SignalAwaiter> beginWait) {
    var watched = new Watched();
    async void Run() {
      try {
        await beginWait();
        watched.Resumed = true;
      }
      catch (Exception error) {
        watched.Caught = error;
      }
    }
    Run();
    return watched;
  }

  private async Task _settle() {
    for (var frame = 0; frame < FRAMES_TO_SETTLE; frame++) {
      await _idle();
    }
  }

  private async Task _idle() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
  }
}
