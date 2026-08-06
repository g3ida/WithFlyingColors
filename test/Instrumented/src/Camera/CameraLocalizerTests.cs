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
