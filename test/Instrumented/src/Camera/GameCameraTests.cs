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

// A respawn is a cut: whatever camera work the death left in flight - the squash's zoom
// punch is the only death effect that touches zoom - must be discarded outright, not eased
// back from. A punch whose release is disturbed by the reload otherwise leaves the camera
// slightly zoomed out, which a bottom-limited room shows as a view shifted up.
public class GameCameraTests(Node testScene) : TestClass(testScene) {
  private const double PUNCH_TIMEOUT = 2.0;

  [Test]
  public async Task ACheckpointReloadDiscardsTheZoomPunchInFlight() {
    var anchor = new Node2D();
    var camera = SceneHelpers.InstantiateNode<GameCamera>();
    camera.FollowPath = new NodePath("..");
    anchor.AddChild(camera);
    TestScene.AddChild(anchor);
    await _frames(1);

    // Deep, so a punch left mid-release is unmistakable against the restored zoom.
    EventHandler.Instance.EmitCameraZoomPunchRequest(0.5f);
    var punched = await _waitFor(() => camera.Zoom.X < 0.999f);
    punched.ShouldBeTrue("the punch never moved the zoom, so there was nothing to interrupt");

    EventHandler.Instance.EmitCheckpointLoaded();
    camera.Zoom.X.ShouldBe(1f, 0.001f, "the reload eased the zoom back instead of cutting to it");
    camera.Offset.ShouldBe(Vector2.Zero, "the reload left an offset on the camera");

    // And it stays: the punch's tween must be dead, not still releasing underneath.
    await _frames(10);
    camera.Zoom.X.ShouldBe(1f, 0.001f, "a zoom tween kept running after the reload");

    anchor.QueueFree();
    await _frames(1);
  }

  // A frozen-camera room (the pool, the brick breaker) collapses its limits to exactly one
  // legal view, so the framing is re-decided by stateless clamping every frame. Whatever
  // the death's camera work does - the squash's zoom pulse shoving the view against a
  // limit, the respawn re-aiming at the player - the same framing must come back on its
  // own, with no camera state worth carrying across.
  [Test]
  public async Task ACollapsedLimitRoomAlwaysSettlesOnItsOneLegalFraming() {
    var anchor = new Node2D { Position = new Vector2(1000f, 700f) };
    var camera = SceneHelpers.InstantiateNode<GameCamera>();
    camera.FollowPath = new NodePath("..");
    anchor.AddChild(camera);
    TestScene.AddChild(anchor);
    camera.MakeCurrent();
    await _frames(2);

    // The freezing localizer's shape: a drag box the size of the screen, and limits
    // collapsed to the view around a centre the followed node does not sit on.
    camera.SetDragMarginTop(1);
    camera.SetDragMarginBottom(1);
    camera.SetDragMarginLeft(1);
    camera.SetDragMarginRight(1);
    var halfView = camera.GetViewportRect().Size * 0.5f;
    var centre = new Vector2(1000f, 400f);
    camera.LimitLeft = (int)(centre.X - halfView.X);
    camera.LimitRight = (int)(centre.X + halfView.X);
    camera.LimitTop = (int)(centre.Y - halfView.Y);
    camera.LimitBottom = (int)(centre.Y + halfView.Y);
    await _frames(120);
    camera.GetScreenCenterPosition().Y.ShouldBe(centre.Y, 3f,
      "the collapsed limits do not decide the framing on their own");

    EventHandler.Instance.EmitCameraZoomPunchRequest(0.2f);
    await _frames(180);
    camera.GetScreenCenterPosition().Y.ShouldBe(centre.Y, 3f,
      "the zoom pulse walked the camera to a different resting place");

    EventHandler.Instance.EmitCheckpointReached(anchor.Position, "blue");
    EventHandler.Instance.EmitCheckpointLoaded();
    await _frames(120);
    camera.GetScreenCenterPosition().Y.ShouldBe(centre.Y, 3f,
      "the respawn left the camera off the room's one legal framing");

    anchor.QueueFree();
    await _frames(1);
  }

  private Task<bool> _waitFor(Func<bool> until) => PhysicsFrames.WaitFor(TestScene, until, PUNCH_TIMEOUT);

  private Task _frames(int count) => PhysicsFrames.Advance(TestScene, count);
}
