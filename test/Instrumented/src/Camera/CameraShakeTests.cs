namespace Wfc.test.instrumented.Camera;

using System;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Camera;
using Wfc.test.instrumented.Helpers;
using Wfc.Utils;
using EventHandler = Wfc.Core.Event.EventHandler;

// The shake writes the camera's offset through timers and tweens of its own, and a respawn
// can land in the middle of one - the burst that kills the cube is barely done shaking when
// the reload comes. The offset it was holding must not ride onto the restored camera.
public class CameraShakeTests(Node testScene) : TestClass(testScene) {
  private const double SHAKE_TIMEOUT = 2.0;

  [Test]
  public async Task ACheckpointReloadCallsOffTheShakeInFlight() {
    var camera = new Camera2D();
    var shake = SceneHelpers.LoadScene<CameraShake>().Instantiate<CameraShake>();
    camera.AddChild(shake);
    TestScene.AddChild(camera);
    await _frames(1);

    // Long and wide, so the reload below is guaranteed to land mid-shake.
    shake.Start(duration: 10f, frequency: 30f, amplitude: 500f);
    var shaken = await _waitFor(() => camera.Offset != Vector2.Zero);
    shaken.ShouldBeTrue("the shake never moved the camera, so there was nothing to interrupt");

    EventHandler.Instance.EmitCheckpointLoaded();
    camera.Offset.ShouldBe(Vector2.Zero, "the reload left the shake's offset on the camera");

    // And it stays still: the timers and the tween in flight are called off with the offset.
    await _frames(10);
    camera.Offset.ShouldBe(Vector2.Zero, "the shake kept running after the reload");

    camera.QueueFree();
    await _frames(1);
  }

  private Task<bool> _waitFor(Func<bool> until) => PhysicsFrames.WaitFor(TestScene, until, SHAKE_TIMEOUT);

  private Task _frames(int count) => PhysicsFrames.Advance(TestScene, count);
}
