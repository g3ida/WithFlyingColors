namespace Wfc.test.instrumented.Paint;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Paint;
using Wfc.Utils;
using Wfc.Utils.Colors;
using Wfc.Utils.Layers;
using Wfc.test.instrumented.Helpers;

// The paint left behind is a surface like any platform surface, and the level is authored around
// how much of one it claims. What it draws and what it claims are set from the same width, and
// nothing on screen says when the two have drifted - the cube simply dies short of the paint, or
// crosses the last of it and dies on white.
public class PaintSplatTests(Node testScene) : TestClass(testScene) {
  private const float WIDTH = 320f;

  private PaintSplat _splat = default!;

  [Cleanup]
  public void Cleanup() {
    if (GodotObject.IsInstanceValid(_splat)) {
      _splat.QueueFree();
    }
  }

  [Test]
  public async Task ItsColourAreaIsCutToThePaintItDraws() {
    await _spill(ColorUtils.PURPLE);

    var shape = _splat.GetNode<CollisionShape2D>("Area2D/ColorAreaShape");
    var rectangle = shape.Shape.ShouldBeOfType<RectangleShape2D>();

    rectangle.Size.X.ShouldBeLessThanOrEqualTo(WIDTH, "the paint claims more surface than it covers");
    rectangle.Size.X.ShouldBeGreaterThan(WIDTH * 0.8f, "the paint claims so little of itself it can be walked around");
    rectangle.Size.Y.ShouldBe(PaintSplat.POOL_DEPTH);
    // The node is placed on the surface the paint landed on, so the pool hangs below the origin
    // and its top edge is where the cube's downward face meets it.
    shape.Position.Y.ShouldBe(PaintSplat.POOL_DEPTH / 2f, 0.01f);
  }

  // A face is judged against the area's layer the same way it is against a platform's, and paint
  // on any other layer is a surface the cube walks through.
  [Test]
  public async Task ItSitsOnTheLayerFacesAreJudgedAgainst() {
    await _spill(ColorUtils.YELLOW);

    var area = _splat.GetNode<Area2D>("Area2D");
    area.CollisionLayer.ShouldBe(PhysicsLayers.Platform.Mask);
    area.IsInGroup(ColorUtils.YELLOW).ShouldBeTrue();
  }

  // Paint still crossing the air has landed on nothing. Opening the area with it also means a cube
  // stood where the bucket was aimed is caught by the paint arriving, rather than by paint that
  // was already a surface before the bucket broke.
  [Test]
  public async Task ThePaintIsNoSurfaceUntilItHasFinishedLanding() {
    await _spill(ColorUtils.PINK);
    var area = _splat.GetNode<Area2D>("Area2D");

    area.Monitorable.ShouldBeFalse("paint still in the air is already a surface");

    await _wallWait(0.6);

    area.Monitorable.ShouldBeTrue("the paint landed and never became a surface");
  }

  private async Task _spill(string group) {
    _splat = SceneHelpers.InstantiateNode<PaintSplat>();
    _splat.Setup(group, WIDTH);
    TestScene.AddChild(_splat);
    await PhysicsFrames.Frame(TestScene);
  }

  // Wall-clock rather than frame-counting: the throw is a tween, and a tween follows real time
  // however fast a headless run frames.
  private async Task _wallWait(double seconds) {
    var tree = TestScene.GetTree();
    var deadline = Time.GetTicksMsec() + (ulong)(seconds * 1000);
    while (Time.GetTicksMsec() < deadline) {
      await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
  }
}
