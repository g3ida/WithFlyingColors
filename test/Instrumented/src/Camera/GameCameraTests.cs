namespace Wfc.test.instrumented.Camera;

using System;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Event;
using Wfc.Entities.World.Camera;
using Wfc.Screens.Levels;
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
    GameEvents.Instance.RequestCameraZoomPunch(0.5f);
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

    GameEvents.Instance.RequestCameraZoomPunch(0.2f);
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

  // Nothing has been saved yet before the first checkpoint, and a death there is the most
  // likely death in any level. What comes back has to be the level as authored: a fallback
  // built out of a record's own defaults describes no room in the game, and a player who
  // falls in the opening seconds is handed a view of somewhere they have never been.
  [Test]
  public async Task ADeathBeforeAnyCheckpointRestoresTheLevelAsAuthored() {
    var anchor = new Node2D { Position = new Vector2(600f, -200f) };
    var camera = _cameraFollowing(anchor);
    camera.LimitTop = -1000;
    camera.LimitBottom = 620;
    camera.LimitLeft = -350;
    camera.LimitRight = 24800;
    camera.SetDragMarginTop(0.05f);
    TestScene.AddChild(anchor);
    camera.MakeCurrent();
    await _frames(120);

    var framedAt = camera.GetScreenCenterPosition();

    EventHandler.Instance.EmitCheckpointLoaded();
    await _frames(120);

    camera.LimitTop.ShouldBe(-1000, "the respawn replaced the level's own top limit");
    camera.LimitBottom.ShouldBe(620, "the respawn replaced the level's own bottom limit");
    camera.LimitLeft.ShouldBe(-350, "the respawn replaced the level's own left limit");
    camera.LimitRight.ShouldBe(24800, "the respawn replaced the level's own right limit");
    camera.DragTopMargin.ShouldBe(0.05f, 0.001f, "the respawn replaced the level's own drag margin");
    camera.GetScreenCenterPosition().Y.ShouldBe(framedAt.Y, 3f,
      "the respawn dropped the view to somewhere the player had never seen");

    anchor.QueueFree();
    await _frames(1);
  }

  // A room may aim the camera at something that is not the player - the level exit pins the
  // frame the player walks out of - and that aim has no end trigger of its own. A respawn is that
  // end: gameplay follows the player, so a death anywhere leaves the camera on the player,
  // however the run had aimed it beforehand.
  [Test]
  public async Task ARespawnTakesTheCameraOffWhateverTheRoomHadAimedItAt() {
    var player = new Node2D { Position = new Vector2(1000f, 0f) };
    var landmark = new Node2D { Position = new Vector2(4000f, 0f) };
    var camera = _cameraFollowing(player);
    TestScene.AddChild(player);
    TestScene.AddChild(landmark);
    await _frames(1);

    camera.SetFollowNode(landmark);
    EventHandler.Instance.EmitCheckpointReached(player.Position, "blue");
    await _frames(10);
    camera.GlobalPosition.X.ShouldBe(landmark.Position.X, 1f, "the room never took the camera off the player");

    EventHandler.Instance.EmitCheckpointLoaded();
    await _frames(10);
    camera.FollowNode.ShouldBe(player, "the respawn left the camera on the room's landmark");
    camera.GlobalPosition.X.ShouldBe(player.Position.X, 1f, "the camera stopped following the player");

    player.QueueFree();
    landmark.QueueFree();
    await _frames(1);
  }

  // Following the player leaves the camera's drag box a margin off them, and a shot that
  // inherits that slack comes to rest that far off its own subject. The level exit aims at
  // the frame the player is walking out of, so a shot that lands beside what it was given
  // is a pinned frame sliding out from under them.
  [Test]
  public async Task AShotComesToRestOnWhatItWasAimedAt() {
    var player = new Node2D();
    var landmark = new Node2D { Position = new Vector2(1200f, 0f) };
    var camera = _cameraFollowing(player);
    camera.LimitTop = -10000;
    camera.LimitBottom = 10000;
    camera.LimitLeft = -10000;
    camera.LimitRight = 24800;
    TestScene.AddChild(player);
    TestScene.AddChild(landmark);
    camera.MakeCurrent();
    await _frames(1);

    // Away from the landmark and far enough that the chase drags the box out to its margin.
    player.Position = new Vector2(4000f, 0f);
    await _frames(60);

    camera.BeginFocusOverride(landmark, camera.PositionSmoothingSpeed);
    await _frames(120);
    camera.GetScreenCenterPosition().X.ShouldBe(landmark.Position.X, 1f,
      "the shot came to rest beside the node it was aimed at");

    player.QueueFree();
    landmark.QueueFree();
    await _frames(1);
  }

  // A shot borrows the camera, and a respawn revokes the borrow. What the shot does afterwards
  // - hand back a stale target, restore a travel speed the reload has already set - must reach
  // nothing, or the restore is undone a beat after it happened.
  [Test]
  public async Task AShotThatOutlivesTheRespawnCannotWriteToTheCamera() {
    var player = new Node2D { Position = new Vector2(1000f, 0f) };
    var subject = new Node2D { Position = new Vector2(4000f, 0f) };
    var camera = _cameraFollowing(player);
    TestScene.AddChild(player);
    TestScene.AddChild(subject);
    await _frames(1);

    var authoredSpeed = camera.PositionSmoothingSpeed;
    var token = camera.BeginFocusOverride(subject, authoredSpeed * 0.5f);
    await _frames(2);
    camera.FollowNode.ShouldBe(subject, "the shot never got the camera");

    EventHandler.Instance.EmitCheckpointLoaded();
    await _frames(2);
    camera.FollowNode.ShouldBe(player, "the respawn did not take the camera back off the shot");
    camera.PositionSmoothingSpeed.ShouldBe(authoredSpeed, 0.001f,
      "the respawn kept the shot's travel speed");

    // The shot runs on unaware, exactly as an awaiting cutscene does.
    camera.ReturnFocus(token);
    camera.EndFocusOverride(token);
    await _frames(2);
    camera.FollowNode.ShouldBe(player, "the retired shot handed the camera to its own target");
    camera.PositionSmoothingSpeed.ShouldBe(authoredSpeed, 0.001f,
      "the retired shot restored a travel speed of its own");

    player.QueueFree();
    subject.QueueFree();
    await _frames(1);
  }

  // The camera is given extra room while the player is off the ground and takes it back on
  // landing. Jumping again before landing must not make the widened margin the one there is
  // to come back to, or the camera keeps the jump's slack for the rest of the level.
  [Test]
  public async Task JumpingAgainBeforeLandingDoesNotKeepTheJumpsSlack() {
    var anchor = new Node2D();
    var camera = _cameraFollowing(anchor);
    TestScene.AddChild(anchor);
    await _frames(1);

    camera.SetDragMarginTop(0.05f);
    camera.SetDragMarginBottom(0.05f);

    GameEvents.Instance.OnPlayerJumped();
    GameEvents.Instance.OnPlayerJumped();
    camera.DragTopMargin.ShouldBe(GameCamera.CAMERA_DRAG_JUMP, 0.001f,
      "the jump never widened the margin");

    GameEvents.Instance.OnPlayerLanded();
    camera.DragTopMargin.ShouldBe(0.05f, 0.001f, "landing kept the jump's widened top margin");
    camera.DragBottomMargin.ShouldBe(0.05f, 0.001f, "landing kept the jump's widened bottom margin");

    anchor.QueueFree();
    await _frames(1);
  }

  private static GameCamera _cameraFollowing(Node2D target) {
    var camera = SceneHelpers.InstantiateNode<GameCamera>();
    camera.FollowPath = new NodePath("..");
    target.AddChild(camera);
    return camera;
  }

  private Task<bool> _waitFor(Func<bool> until) => PhysicsFrames.WaitFor(TestScene, until, PUNCH_TIMEOUT);

  private Task _frames(int count) => PhysicsFrames.Advance(TestScene, count);
}
