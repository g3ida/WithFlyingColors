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

    GameEvents.Instance.OnCheckpointLoaded();
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

    GameEvents.Instance.OnCheckpointReached(anchor.Position, "blue");
    GameEvents.Instance.OnCheckpointLoaded();
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

    GameEvents.Instance.OnCheckpointLoaded();
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
    GameEvents.Instance.OnCheckpointReached(player.Position, "blue");
    await _frames(10);
    camera.GlobalPosition.X.ShouldBe(landmark.Position.X, 1f, "the room never took the camera off the player");

    GameEvents.Instance.OnCheckpointLoaded();
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

    GameEvents.Instance.OnCheckpointLoaded();
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

  // Smoothing arrives on its own schedule - most of the way there inside a fraction of the leg,
  // whatever the distance - which is exactly what an authored shot cannot use. An eased shot is
  // paced by its curve instead, so the leg's own time is what the travel takes.
  [Test]
  public async Task AnEasedShotIsPacedByItsCurveRatherThanBySmoothing() {
    var player = new Node2D { Position = new Vector2(1000f, 0f) };
    var landmark = new Node2D { Position = new Vector2(9000f, 0f) };
    var camera = _cameraFollowing(player);
    camera.LimitTop = -10000;
    camera.LimitBottom = 10000;
    camera.LimitLeft = -10000;
    camera.LimitRight = 24800;
    TestScene.AddChild(player);
    TestScene.AddChild(landmark);
    camera.MakeCurrent();
    await _frames(30);

    var from = camera.GetScreenCenterPosition().X;
    var token = camera.BeginFocusOverride(landmark, 1.0f, CameraEasing.Quad, Tween.EaseType.InOut);
    camera.PositionSmoothingEnabled.ShouldBeFalse("the curve is sharing the camera with smoothing");

    // Quad InOut is symmetric, so half the leg is about half the ground however far away the landmark
    // is. Banded rather than exact: the curve is steepest here, and which side of a physics tick the
    // sample falls on moves it much further than a tolerance worth asserting. Smoothing would be all
    // but arrived by now, which is the thing this has to tell apart.
    await _frames(30);
    var covered = (camera.GetScreenCenterPosition().X - from) / (landmark.Position.X - from);
    covered.ShouldBeInRange(0.35f, 0.65f,
      "the shot was not around half way through its leg, so its curve is not what is pacing it");

    await _frames(32);
    camera.GetScreenCenterPosition().X.ShouldBe(landmark.Position.X, 1f,
      "the shot did not land on what it was aimed at by the end of its leg");

    camera.EndFocusOverride(token);
    camera.PositionSmoothingEnabled.ShouldBeTrue("the shot kept smoothing suspended after handing back");

    player.QueueFree();
    landmark.QueueFree();
    await _frames(1);
  }

  // A room and a shot can be walked into on the same step - the room the shot is showing off is
  // usually the one framing it - and then the room's limits would clamp the travel out and its zoom
  // would run underneath it. The shot has the camera until it turns for home, and the way back is
  // the one travel there needs to be: the camera arrives already framed rather than settling on the
  // player and re-framing from there.
  [Test]
  public async Task ARoomIsTakenOnTheWayHomeAndShownOnceTheCameraHasStopped() {
    var player = new Node2D { Position = new Vector2(1000f, 0f) };
    var landmark = new Node2D { Position = new Vector2(9000f, 0f) };
    var roomTarget = new Node2D { Position = new Vector2(1200f, 0f) };
    var camera = _cameraFollowing(player);
    TestScene.AddChild(player);
    TestScene.AddChild(landmark);
    TestScene.AddChild(roomTarget);
    await _frames(1);

    var room = new FakeRoom(camera, 0.8f, () => camera.SetFollowNode(roomTarget));

    camera.ApplyRoomFraming(room);
    room.Taken.ShouldBe(1, "a room walked into with no shot running did not take the camera straight away");
    room.Shown.ShouldBe(1, "a room walked into with no shot running did not show its view straight away");

    // A shot opens on a beat it has not borrowed the camera for yet, and that is exactly the step
    // the room is walked into on.
    camera.BeginShot();
    camera.ApplyRoomFraming(room);
    room.Taken.ShouldBe(1, "the room took the camera during the shot's opening beat");
    room.Shown.ShouldBe(1, "the room changed the view during the shot's opening beat");

    var openingZoom = camera.TargetZoom;
    camera.SettleViewForShot(0.7f)
      .ShouldBeGreaterThan(0f, "the shot was given no beat to hold while the camera pulls back");
    camera.TargetZoom.ShouldBe(0.7f, 0.001f, "the camera was not pulled back to what the shot shows");
    room.Taken.ShouldBe(1, "the room clamped the shot's travel before it had even set off");

    var token = camera.BeginFocusOverride(landmark, 1.0f, CameraEasing.Quad, Tween.EaseType.InOut);
    await _frames(10);
    room.Taken.ShouldBe(1, "the room clamped the shot while it was still travelling out");
    camera.FollowNode.ShouldBe(landmark, "the room took the camera off what the shot was showing");

    camera.ReturnFocus(token, 1.0f, CameraEasing.Quad, Tween.EaseType.InOut);
    room.Taken.ShouldBe(2, "the way home was aimed before the room said where the camera may go");
    camera.FollowNode.ShouldBe(roomTarget,
      "the way back was aimed at what the shot borrowed the camera from rather than at the room's own target");
    room.Shown.ShouldBe(1, "the room changed the view under the way home, which drags the leg off its curve");
    camera.TargetZoom.ShouldBe(0.7f, 0.001f, "the way home did not travel at the view the shot opened on");

    // Held for with the stripes still in, so the framing change happens inside the shot.
    var settled = camera.SettleViewAfterShot(openingZoom);
    settled.ShouldBeGreaterThan(0f, "the shot was given no beat to hold its stripes in for the tighten");
    room.Taken.ShouldBe(2, "the room took the camera a second time once the leg had landed");
    room.Shown.ShouldBe(2, "the room never tightened onto its own view once the camera had stopped");
    room.PanWasStillToCome.ShouldBeFalse(
      "the room held its zoom back for a pan the way home had already absorbed");
    camera.TargetZoom.ShouldBe(0.8f, 0.001f, "the camera did not settle on the view the room shows");

    camera.EndFocusOverride(token);
    camera.EndShot();
    camera.FollowNode.ShouldBe(roomTarget, "the hand-back took the camera off what the room follows");

    player.QueueFree();
    landmark.QueueFree();
    roomTarget.QueueFree();
    await _frames(1);
  }

  // A shot with no view of its own opens on the room's, so a room and a shot walked into together
  // still pull back before anything moves. And a shot with no room to hand over to comes back to
  // the view it widened, rather than leaving the camera zoomed out for the rest of the level.
  [Test]
  public async Task AShotWithNoViewOfItsOwnOpensOnTheRoomsView() {
    var player = new Node2D { Position = new Vector2(1000f, 0f) };
    var camera = _cameraFollowing(player);
    TestScene.AddChild(player);
    await _frames(1);

    camera.BeginShot();
    camera.SettleViewForShot(0.0f).ShouldBe(0f, "a shot with no room to open on was held up all the same");

    camera.ApplyRoomFraming(new FakeRoom(camera, 0.6f, () => { }));
    camera.SettleViewForShot(0.0f).ShouldBeGreaterThan(0f, "the shot did not open on the room's view");
    camera.TargetZoom.ShouldBe(0.6f, 0.001f, "the camera was not pulled back to what the room shows");

    camera.SettleViewAfterShot(camera.TargetZoom);
    camera.EndShot();

    var opening = camera.TargetZoom;
    camera.BeginShot();
    camera.SettleViewForShot(0.4f);
    camera.SettleViewAfterShot(opening)
      .ShouldBeGreaterThan(0f, "the shot never came back off the view it had widened to");
    camera.TargetZoom.ShouldBe(opening, 0.001f, "the shot left the camera on the view it had widened to");
    camera.EndShot();

    player.QueueFree();
    await _frames(1);
  }

  // A room may have no view to change, and then there is no beat to hold the stripes in for. The
  // shot has to hear that from the room rather than reading back whatever the last zoom happened to
  // schedule, or it holds the player still for a change nobody made.
  [Test]
  public async Task ARoomThatChangesNoViewLeavesTheShotNothingToHoldFor() {
    var player = new Node2D { Position = new Vector2(1000f, 0f) };
    var camera = _cameraFollowing(player);
    TestScene.AddChild(player);
    await _frames(1);

    // A real zoom first, so a stale duration would be there to be read back.
    camera.BeginShot();
    camera.SettleViewForShot(0.5f).ShouldBeGreaterThan(0f, "the shot never pulled back at all");

    var viewlessRoom = new FakeRoom(camera, null, () => { });
    camera.ApplyRoomFraming(viewlessRoom);
    camera.SettleViewAfterShot(0.5f)
      .ShouldBe(0f, "the shot was held for a beat the room never asked for");
    viewlessRoom.Shown.ShouldBe(1, "the room was never shown");
    camera.EndShot();

    player.QueueFree();
    await _frames(1);
  }

  // The respawn has already restored the checkpoint's framing by the time the retired shot ends,
  // and a room that was waiting behind it would land on top of that.
  [Test]
  public async Task ARespawnDropsTheRoomFramingAShotWasHolding() {
    var player = new Node2D { Position = new Vector2(1000f, 0f) };
    var landmark = new Node2D { Position = new Vector2(9000f, 0f) };
    var camera = _cameraFollowing(player);
    TestScene.AddChild(player);
    TestScene.AddChild(landmark);
    await _frames(1);

    camera.BeginShot();
    var token = camera.BeginFocusOverride(landmark, 1.0f, CameraEasing.Quad, Tween.EaseType.InOut);
    var room = new FakeRoom(camera, 0.8f, () => { });
    camera.ApplyRoomFraming(room);

    GameEvents.Instance.OnCheckpointLoaded();
    await _frames(2);
    camera.ReturnFocus(token, 1.0f, CameraEasing.Quad, Tween.EaseType.InOut);
    camera.EndFocusOverride(token);
    camera.SettleViewAfterShot(camera.TargetZoom);
    camera.EndShot();
    room.Taken.ShouldBe(0, "the retired shot framed the camera for a room the respawn had dropped");
    room.Shown.ShouldBe(0, "the retired shot showed a room the respawn had dropped");

    player.QueueFree();
    landmark.QueueFree();
    await _frames(1);
  }

  // A room takes the camera and shows its view at two separate moments, and the zoom rides on the
  // second, which is what these count separately.
  private sealed class FakeRoom(GameCamera camera, float? zoom, Action onTaken) : ICameraRoom {
    public int Taken { get; private set; }
    public int Shown { get; private set; }
    public bool PanWasStillToCome { get; private set; } = true;

    public float? Zoom => zoom;

    public void TakeTheCamera() {
      Taken++;
      onTaken();
    }

    public float ShowTheRoom(bool aPanIsStillToCome) {
      Shown++;
      PanWasStillToCome = aPanIsStillToCome;
      return zoom is { } roomZoom ? camera.ZoomTo(roomZoom, aPanIsStillToCome) : 0.0f;
    }
  }

  // A leg aimed at something the limits will not let the camera reach used to hit the wall partway
  // through and then stand there for the rest of its time - on Level 1-1's way home, 1.2s of a 3s
  // leg - which reads as the shot hanging before it hands back. Aimed at the wall instead, its
  // motion and its clock run out together.
  [Test]
  public async Task ALegAimedPastTheLimitsIsStillArrivingWhenItsTimeRunsOut() {
    var player = new Node2D { Position = new Vector2(1000f, 0f) };
    var unreachable = new Node2D { Position = new Vector2(20000f, 0f) };
    var camera = _cameraFollowing(player);
    camera.LimitTop = -10000;
    camera.LimitBottom = 10000;
    camera.LimitLeft = 0;
    // The wall is far short of what the leg is aimed at, so most of the aim is unreachable.
    camera.LimitRight = 6000;
    TestScene.AddChild(player);
    TestScene.AddChild(unreachable);
    camera.MakeCurrent();
    await _frames(30);

    var wall = camera.LimitRight - (camera.GetViewportRect().Size.X * 0.5f);
    camera.BeginFocusOverride(unreachable, 1.0f, CameraEasing.Linear, Tween.EaseType.InOut);

    await _frames(45);
    var partWay = camera.GetScreenCenterPosition().X;
    await _frames(25);
    var landed = camera.GetScreenCenterPosition().X;

    landed.ShouldBeGreaterThan(partWay + 300.0f,
      "the leg had already parked against the wall and stood still for the rest of its time");
    landed.ShouldBe(wall, 5.0f, "the leg did not come to rest against the wall it was clamped by");

    player.QueueFree();
    unreachable.QueueFree();
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
