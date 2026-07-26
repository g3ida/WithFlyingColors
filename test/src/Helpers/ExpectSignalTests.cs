namespace Wfc.test.Helpers;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.test;

// The helper every other async test now depends on to fail rather than hang, so its
// timeout branch is worth proving rather than assuming.
public class ExpectSignalTests(Node testScene) : TestClass(testScene) {
  [Test]
  public async Task ReportsTheSignalWhenItArrives() {
    var timer = new Timer { WaitTime = 0.05, OneShot = true, Autostart = true };
    TestScene.AddChild(timer);

    var fired = await TestScene.GetTree().ExpectSignal(timer, Timer.SignalName.Timeout);

    fired.ShouldBeTrue();
    timer.QueueFree();
  }

  // The case that matters: without a deadline this await would never resume and the
  // whole run would sit here until the CI job was killed hours later.
  [Test]
  public async Task GivesUpOnASignalThatNeverArrives() {
    var silent = new Timer { WaitTime = 3600, OneShot = true };
    TestScene.AddChild(silent);

    var fired = await TestScene.GetTree().ExpectSignal(silent, Timer.SignalName.Timeout, 0.2);

    fired.ShouldBeFalse();
    silent.QueueFree();
  }
}
