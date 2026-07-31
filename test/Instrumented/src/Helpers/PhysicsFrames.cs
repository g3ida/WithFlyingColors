namespace Wfc.test.instrumented.Helpers;

using System;
using System.Threading.Tasks;
using Godot;

// A headless run has no wall clock worth waiting on: physics frames are the only unit that
// reproduces. Every suite drives the tree by hand through these.
public static class PhysicsFrames {
  public static async Task Frame(Node node) {
    var tree = node.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
  }

  public static async Task Advance(Node node, int count) {
    for (var frame = 0; frame < count; frame++) {
      await Frame(node);
    }
  }

  // True as soon as the condition holds, false if it never does within the timeout.
  public static async Task<bool> WaitFor(Node node, Func<bool> until, double timeoutSeconds) {
    var deadline = timeoutSeconds * Engine.PhysicsTicksPerSecond;
    for (var frame = 0; frame < deadline; frame++) {
      if (until()) {
        return true;
      }
      await Frame(node);
    }
    return false;
  }
}
