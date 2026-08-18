namespace Wfc.Entities.World.Platforms.Test;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Platforms;
using Wfc.test;

// The splash is drawn against SCREEN_UV, so what reaches the shader has to be where on screen
// the cube touched down rather than where in the level it did. Handed the world position, the
// contact point sits far outside the unit square the shader searches, and the circle - which
// only ever grows to a few UV units before it has finished brightening - can never arrive at
// it. The surface then plays a splash nobody can see, which is indistinguishable from the
// landing never having been heard.
public class PlatformSplashTests(Node testScene) : TestClass(testScene) {
  private const float TOLERANCE = 0.001f;

  private Camera2D _cameraNode = default!;
  private ShaderMaterial _material = default!;

  [SetupAll]
  public async Task Setup() {
    _cameraNode = new Camera2D();
    TestScene.AddChild(_cameraNode);
    _cameraNode.MakeCurrent();
    // A camera reports the centre it is framing rather than the position it was just given.
    await TestScene.GetTree().ToSignal(TestScene.GetTree(), SceneTree.SignalName.ProcessFrame);

    _material = new ShaderMaterial {
      Shader = GD.Load<Shader>("res://Assets/Shaders/ColorSplash.tres"),
    };
  }

  [CleanupAll]
  public void Cleanup() {
    TestScene.RemoveChild(_cameraNode);
    _cameraNode.QueueFree();
  }

  [Test]
  public void ALandingAtTheCentreOfTheScreenIsWrittenAtTheCentreOfTheScreen() {
    _write(_centre());

    _contactPos().X.ShouldBeCloseTo(0.5f, TOLERANCE);
    _contactPos().Y.ShouldBeCloseTo(0.5f, TOLERANCE);
  }

  // Screen coordinates run down the way UV does, so a landing below the camera has to be
  // written below the middle rather than flipped above it.
  [Test]
  public void ALandingOffCentreIsWrittenAsTheFractionOfTheScreenItIsAt() {
    var resolution = _cameraNode.GetViewportRect().Size;

    _write(_centre() + (resolution / 4f));

    _contactPos().X.ShouldBeCloseTo(0.75f, TOLERANCE);
    _contactPos().Y.ShouldBeCloseTo(0.75f, TOLERANCE);
  }

  // The regression itself: a level is thousands of pixels wide, and the contact position is a
  // world position. Anywhere the camera happens to be framing has to come out inside the unit
  // square.
  [Test]
  public void ALandingDeepIntoALevelIsStillWrittenInsideTheScreen() {
    _cameraNode.Position = new Vector2(12000f, 640f);
    _write(_centre() + new Vector2(120f, -40f));

    var written = _contactPos();
    written.X.ShouldBeInRange(0f, 1f);
    written.Y.ShouldBeInRange(0f, 1f);
  }

  // A room framed closer puts the same world point somewhere else on screen, so the splash
  // has to follow the zoom rather than sit where an unzoomed camera would have put it.
  [Test]
  public void AZoomedCameraWritesTheLandingWhereTheZoomPutsIt() {
    var resolution = _cameraNode.GetViewportRect().Size;
    _cameraNode.Zoom = new Vector2(2f, 2f);

    _write(_centre() + new Vector2(resolution.X / 4f, 0f));

    _contactPos().X.ShouldBeCloseTo(1f, TOLERANCE);
    _cameraNode.Zoom = Vector2.One;
  }

  // Read off the camera rather than assumed: where a camera settles is its own business, and
  // this is asking what a landing at that point is written as, not where the point is.
  private Vector2 _centre() => _cameraNode.GetScreenCenterPosition();

  private void _write(Vector2 contact) =>
    PlatformSplash.Write(_material, _cameraNode, contact, 0f);

  private Vector2 _contactPos() =>
    _material.GetShaderParameter(PlatformSplash.ContactPosParam).AsVector2();
}
