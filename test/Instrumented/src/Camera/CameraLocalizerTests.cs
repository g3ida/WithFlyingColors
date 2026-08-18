namespace Wfc.test.instrumented.Camera;

using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Camera;
using Wfc.Utils;
using Wfc.Utils.Layers;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;

// A room is a rect the level author places, and the camera limits are read off it: where it lands
// once the localizer has been moved and scaled, which of its sides hold the camera in, and how much
// of it is kept when the room is asked to show exactly one screen. None of that shows up anywhere
// but in a playthrough, and a room framed a screen away from where it was drawn still looks like a
// framing that was authored on purpose.
public class CameraLocalizerTests(Node testScene) : TestClass(testScene) {
  private FakeGameLevelProvider _level = default!;
  private GameCamera _camera = default!;
  private CameraLocalizer _localizer = default!;

  [Setup]
  public async Task Setup() {
    _level = new FakeGameLevelProvider();
    TestScene.AddChild(_level);
    _camera = SceneHelpers.InstantiateNode<GameCamera>();
    _level.AddChild(_camera);
    _level.CameraNode = _camera;
    _localizer = SceneHelpers.InstantiateNode<CameraLocalizer>();
    _level.AddChild(_localizer);
    await PhysicsFrames.Frame(TestScene);
  }

  [Cleanup]
  public void Cleanup() => _level.QueueFree();

  // A room with no doorway authored on it is walked into, and the level author is not asked to
  // build the volume for that: it is the room.
  [Test]
  public async Task ALocalizerWithNoAuthoredWayInIsEnteredByWalkingIntoTheRoom() {
    // The way in is built as the localizer enters the tree, so the room has to be the room by then.
    var localizer = SceneHelpers.InstantiateNode<CameraLocalizer>();
    localizer.LimitRect = new Rect2(-300.0f, -200.0f, 600.0f, 400.0f);
    _level.AddChild(localizer);
    await PhysicsFrames.Frame(TestScene);

    var wayIn = localizer.GetChildren().OfType<Area2D>().ShouldHaveSingleItem();
    (wayIn.CollisionMask & PhysicsLayers.Player.Mask).ShouldBe(
      PhysicsLayers.Player.Mask,
      "the way in does not watch the layer the player is on, so it can never report them"
    );
    var shape = wayIn.GetChild<CollisionShape2D>(0);
    ((RectangleShape2D)shape.Shape).Size.ShouldBe(new Vector2(600.0f, 400.0f));
    shape.Position.ShouldBe(Vector2.Zero, "the way in does not cover the room it belongs to");
    localizer._GetConfigurationWarnings().ShouldBeEmpty("a localizer that needs nothing else still asks for something");
  }

  // A room's limits change the instant it is entered while its zoom eases in over its own beat, so
  // by default the camera pans and zooms at once. Held back, the zoom must not have started while
  // the pan is still being absorbed - and must still arrive once it has.
  [Test]
  public async Task AZoomHeldBackWaitsForTheMoveInsteadOfRunningWithIt() {
    _localizer.LimitRect = new Rect2(-960.0f, -540.0f, 1920.0f, 1080.0f);
    _localizer.Zoom = 0.5f;
    _localizer.ZoomAfterMoving = true;
    await PhysicsFrames.Frame(TestScene);
    var before = _camera.Zoom.X;

    _localizer.ApplyToCamera();
    _camera.TargetZoom.ShouldBe(0.5f, 0.001f, "the room's zoom is not the one the camera is headed for");

    await PhysicsFrames.Advance(TestScene, 10);
    _camera.Zoom.X.ShouldBe(before, 0.001f, "the held-back zoom set off alongside the move");

    var arrived = await PhysicsFrames.WaitFor(TestScene, () => Mathf.Abs(_camera.Zoom.X - 0.5f) < 0.001f, 20.0);
    arrived.ShouldBeTrue("the held-back zoom never arrived at all");
  }

  // The hold is not a fixed beat: it is however long this room's own pan takes, so a room that
  // follows slowly must wait longer before its zoom starts than one that follows quickly.
  [Test]
  public async Task ASlowerRoomHoldsItsZoomBackForLonger() {
    _localizer.LimitRect = new Rect2(-960.0f, -540.0f, 1920.0f, 1080.0f);
    _localizer.Zoom = 0.5f;
    _localizer.ZoomAfterMoving = true;
    _localizer.FollowSpeed = 1.0f;
    await PhysicsFrames.Frame(TestScene);
    var before = _camera.Zoom.X;

    _localizer.ApplyToCamera();
    _camera.PositionSmoothingSpeed.ShouldBe(1.0f, 0.001f, "the room's own follow speed never reached the camera");

    // Long enough that the same room at the level's speed would have started zooming by now.
    await PhysicsFrames.Advance(TestScene, 45);
    _camera.Zoom.X.ShouldBe(before, 0.001f, "the slow room's zoom did not wait for its own slower pan");
  }

  // The zoom runs on the same chase as the pan, so a room that follows slowly zooms slowly with it
  // rather than keeping a beat of its own that the slower pan is left behind by.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ASlowerRoomZoomsMoreSlowly() {
    _localizer.LimitRect = new Rect2(-960.0f, -540.0f, 1920.0f, 1080.0f);
    _localizer.Zoom = 0.5f;
    _localizer.FollowSpeed = 1.0f;
    await PhysicsFrames.Frame(TestScene);

    _localizer.ApplyToCamera();

    // Comfortably past the beat the same room zooms on at the level's speed, which this one is a
    // fraction of: arriving by here means the zoom kept that beat instead of taking the room's.
    await PhysicsFrames.Advance(TestScene, 90);
    Mathf.Abs(_camera.Zoom.X - 0.5f).ShouldBeGreaterThan(0.001f,
      "the slow room's zoom ran at the level's beat rather than its own");

    var arrived = await PhysicsFrames.WaitFor(TestScene, () => Mathf.Abs(_camera.Zoom.X - 0.5f) < 0.001f, 60.0);
    arrived.ShouldBeTrue("the slow room's zoom never arrived at all");
  }

  // A room's follow speed belongs to that room. A later room that has no opinion about it must get
  // the level's back rather than inheriting whatever the last room that did have one left behind,
  // or one room's pace quietly becomes the rest of the level's - and a checkpoint bakes it in.
  [Test]
  public async Task ARoomWithNoSpeedOfItsOwnPutsTheLevelsBack() {
    var authored = _camera.AuthoredFollowSpeed;
    authored.ShouldBeGreaterThan(0.0f, "the level opened with no follow speed to put back");

    _localizer.FollowSpeed = authored * 4.0f;
    _localizer.ApplyToCamera();
    _camera.PositionSmoothingSpeed.ShouldBe(authored * 4.0f, 0.001f, "the room never took its own pace");

    var plainRoom = SceneHelpers.InstantiateNode<CameraLocalizer>();
    _level.AddChild(plainRoom);
    await PhysicsFrames.Frame(TestScene);
    plainRoom.ApplyToCamera();
    _camera.PositionSmoothingSpeed.ShouldBe(authored, 0.001f,
      "a room with no speed of its own kept the previous room's");
  }

  // The same room without the hold: the zoom is under way while the move still is.
  [Test]
  public async Task AZoomThatIsNotHeldBackRunsWithTheMove() {
    _localizer.LimitRect = new Rect2(-960.0f, -540.0f, 1920.0f, 1080.0f);
    _localizer.Zoom = 0.5f;
    await PhysicsFrames.Frame(TestScene);
    var before = _camera.Zoom.X;

    _localizer.ApplyToCamera();

    var moved = await PhysicsFrames.WaitFor(TestScene, () => Mathf.Abs(_camera.Zoom.X - before) > 0.001f, 2.0);
    moved.ShouldBeTrue("the zoom never started, so holding it back would mean nothing");
  }

  [Test]
  public async Task TheRoomIsFramedWhereTheLocalizerStandsAndAnOpenEdgeIsLetGo() {
    _localizer.Position = new Vector2(1000.0f, 500.0f);
    _localizer.LimitRect = new Rect2(-200.0f, -100.0f, 400.0f, 200.0f);
    _localizer.LimitedEdges = CameraEdges.Left | CameraEdges.Bottom;
    await PhysicsFrames.Frame(TestScene);

    _localizer.ApplyLimitsToCamera();

    _camera.LimitLeft.ShouldBe(800, "the room is not clamping where the localizer stands");
    _camera.LimitBottom.ShouldBe(600, "the room is not clamping where the localizer stands");
    _camera.LimitRight.ShouldBe(Constants.DEFAULT_CAMERA_LIMIT_RIGHT, "an open edge held the camera in");
    _camera.LimitTop.ShouldBe(Constants.DEFAULT_CAMERA_LIMIT_TOP, "an open edge held the camera in");
  }

  // The pool is placed at a scale, so the rect it carries is a room three times taller than it is
  // drawn in the localizer's own space.
  [Test]
  public async Task AScaledLocalizerCarriesTheRoomWithIt() {
    _localizer.Position = new Vector2(1000.0f, 500.0f);
    _localizer.Scale = new Vector2(2.0f, 3.0f);
    _localizer.LimitRect = new Rect2(-200.0f, -100.0f, 400.0f, 200.0f);
    _localizer.LimitedEdges = CameraLocalizer.ALL_EDGES;
    await PhysicsFrames.Frame(TestScene);

    _localizer.ApplyLimitsToCamera();

    _camera.LimitLeft.ShouldBe(600);
    _camera.LimitRight.ShouldBe(1400);
    _camera.LimitTop.ShouldBe(200);
    _camera.LimitBottom.ShouldBe(800);
  }

  // Walking out of a room is not the same as walking into one that clamps nothing: a level is
  // authored with limits and margins of its own, and opening them right up would let the camera
  // travel off the end of it.
  [Test]
  public async Task ARoomThatHandsTheCameraBackRestoresWhatTheLevelWasAuthoredWith() {
    var (left, top, right, bottom) = (_camera.LimitLeft, _camera.LimitTop, _camera.LimitRight, _camera.LimitBottom);
    var drag = _camera.DragTopMargin;
    _localizer.Position = new Vector2(4000.0f, 4000.0f);
    _localizer.LimitRect = new Rect2(-100.0f, -100.0f, 200.0f, 200.0f);
    _localizer.FreezeCamera = true;
    await PhysicsFrames.Frame(TestScene);
    _localizer.ApplyToCamera();
    _camera.LimitLeft.ShouldNotBe(left, "the room this walks out of never took the camera in the first place");

    var handBack = SceneHelpers.InstantiateNode<CameraLocalizer>();
    handBack.RestoreLevelFraming = true;
    _level.AddChild(handBack);
    await PhysicsFrames.Frame(TestScene);

    handBack.ApplyToCamera();

    _camera.LimitLeft.ShouldBe(left);
    _camera.LimitTop.ShouldBe(top);
    _camera.LimitRight.ShouldBe(right);
    _camera.LimitBottom.ShouldBe(bottom);
    _camera.DragTopMargin.ShouldBe(drag, "the camera was handed back still frozen behind the room's drag box");
  }

  [Test]
  public async Task AFittedAxisCollapsesTheRoomToOneScreenfulAndLeavesAnOpenAxisAlone() {
    var view = TestScene.GetViewport().GetVisibleRect().Size;
    _localizer.Position = Vector2.Zero;
    _localizer.LimitRect = new Rect2(-2000.0f, -1000.0f, 4000.0f, 2000.0f);
    _localizer.LimitedEdges = CameraEdges.Left | CameraEdges.Right | CameraEdges.Bottom;
    _localizer.FitWidthToView = true;
    _localizer.FitHeightToView = true;
    await PhysicsFrames.Frame(TestScene);

    _localizer.ApplyLimitsToCamera();

    (_camera.LimitRight - _camera.LimitLeft).ShouldBe(
      Mathf.RoundToInt(view.X),
      "the fitted axis left the camera room to travel, so the room does not decide the framing"
    );
    Mathf.Abs(_camera.LimitLeft + _camera.LimitRight).ShouldBeLessThanOrEqualTo(
      1,
      "the fitted axis drifted off the room's centre"
    );
    _camera.LimitBottom.ShouldBe(1000, "an axis with an open edge has no band to fit and was squeezed anyway");
  }
}
