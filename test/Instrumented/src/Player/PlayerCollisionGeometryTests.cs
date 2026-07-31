namespace Wfc.test.instrumented.Player;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Player;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils.Colors;

// The cube used to be five hand-maintained descriptions of itself, disagreeing about how wide it
// is and about which color it shows where. Nothing reconciled them and nothing tested them, so
// each was free to drift on its own. These pin the ones that matter to the scene.
public class PlayerCollisionGeometryTests(Node testScene) : TestClass(testScene) {
  private const float JUMPING_SCALE = 4.5f;
  private const float ARENA_SCALE = 3.5f;
  private const float A_LITTLE_WAY_IN = 6.0f;

  private FakeDependenciesProvider _provider = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await _physicsFrame();
  }

  [Cleanup]
  public void Cleanup() => _provider.QueueFree();

  // One shape, so the half-extents are the surface rather than something derived from a hull and
  // eight plates standing proud of it.
  [Test]
  public async Task TheCubeIsOneCollisionShapeAndTheHalfExtentsAreIt() {
    var player = await _addPlayer();

    var shapes = 0;
    CollisionShape2D? only = null;
    foreach (var child in player.GetChildren()) {
      if (child is CollisionShape2D shape) {
        shapes++;
        only = shape;
      }
    }

    shapes.ShouldBe(1, "the cube's surface is one rectangle");
    var size = ((RectangleShape2D)only!.Shape).Size;
    player.GetCollisionHalfExtents().X.ShouldBe(size.X * 0.5f, 0.001f);
    player.GetCollisionHalfExtents().Y.ShouldBe(size.Y * 0.5f, 0.001f);
  }

  // The one that was silently orientation-dependent: the side faces never repositioned at all and
  // the top and bottom overcompensated, so which color you had rotated to the floor changed how
  // deep a landing had to be. The core verb of the game is rotating the cube, so this is a
  // difference the player feels without ever being able to name it.
  [Test]
  public async Task GrowingTheCornersLeavesEveryFaceWhereItWas() {
    var player = await _addPlayer();
    var before = _faceOuterEdges(player);

    player.ScaleCornersBy(JUMPING_SCALE);

    var after = _faceOuterEdges(player);
    for (var i = 0; i < before.Length; i++) {
      after[i].ShouldBe(before[i], 0.001f, "a face moved when the corners grew");
    }
  }

  [Test]
  public async Task EveryFaceSitsTheSameDistanceOut() {
    var player = await _addPlayer();
    player.ScaleCornersBy(JUMPING_SCALE);

    var edges = _faceOuterEdges(player);
    foreach (var edge in edges) {
      edge.ShouldBe(edges[0], 0.001f, "the cube's color band is lopsided");
    }
  }

  // Thickness is how deep a contact must be before it registers. Dragging it along with the corner
  // tolerance made flat-face contacts harder to detect exactly when corners were made easier -
  // and dropped the side faces behind the cube's own surface, where nothing could reach them.
  [Test]
  public async Task GrowingTheCornersLeavesTheFacesAsThickAsTheyWere() {
    var player = await _addPlayer();
    var before = _faceThickness(player);

    player.ScaleCornersBy(JUMPING_SCALE);

    _faceThickness(player).ShouldBe(before, 0.001f);
  }

  [Test]
  public async Task TheColorBandStandsProudOfTheCubesSurface() {
    var player = await _addPlayer();
    player.ScaleCornersBy(JUMPING_SCALE);

    foreach (var edge in _faceOuterEdges(player)) {
      edge.ShouldBeGreaterThan(
        player.GetCollisionHalfExtents().X,
        "a color band inside the surface can never be reached by a body resting against it"
      );
    }
  }

  // What the corner scaling exists to provide, now expressed as one number the analytic query and
  // the color areas both read. Bullets were judged against the unscaled body shapes and so never
  // saw any of it.
  [Test]
  public async Task AWiderSeamForgivesFurtherFromTheCorner() {
    var player = await _addPlayer();
    var half = player.GetCollisionHalfExtents();
    var nearTheBottomRight = player.GlobalPosition + new Vector2(half.X - A_LITTLE_WAY_IN, half.Y);

    player.AcceptsColorAt(nearTheBottomRight, ColorUtils.PURPLE)
      .ShouldBeTrue("the bottom face's own color is safe on the bottom face");
    player.AcceptsColorAt(nearTheBottomRight, ColorUtils.YELLOW)
      .ShouldBeFalse("at rest this is not near enough the corner to be a seam");

    player.ScaleCornersBy(JUMPING_SCALE);

    player.AcceptsColorAt(nearTheBottomRight, ColorUtils.YELLOW)
      .ShouldBeTrue("a widened seam has to reach this contact for anything to have been forgiven");
  }

  [Test]
  public async Task TheSeamWidensWithTheScaleFactor() {
    var player = await _addPlayer();
    var atRest = player.CornerSeam;

    player.ScaleCornersBy(JUMPING_SCALE);

    player.CornerSeam.ShouldBeGreaterThan(atRest);
  }

  // The brick breaker widens the corners for the whole minigame and says so while the player is
  // already standing in it. Recording the number is not applying it: the seam used to wait for the
  // next state change, and a paddle that only ever slides never has one.
  [Test]
  public async Task ANewDefaultFactorWidensTheSeamWithoutWaitingForAStateChange() {
    var player = await _addPlayer();
    var atRest = player.CornerSeam;

    player.CurrentDefaultCornerScaleFactor = ARENA_SCALE;

    player.CornerSeam.ShouldBeGreaterThan(atRest);
  }

  // A state that asked for its own tolerance keeps it. The new default is what it falls back to,
  // and it lands when that state lets go.
  [Test]
  public async Task ANewDefaultDoesNotNarrowASeamAStateIsHoldingOpen() {
    var player = await _addPlayer();
    player.ScaleCornersBy(JUMPING_SCALE);
    var airborne = player.CornerSeam;

    player.CurrentDefaultCornerScaleFactor = ARENA_SCALE;

    player.CornerSeam.ShouldBe(airborne, 0.001f);
  }

  // Reachable from save data, where the scale factor is persisted.
  [Test]
  public async Task AnAbsurdScaleFactorDoesNotInvertTheFaces() {
    var player = await _addPlayer();

    player.ScaleCornersBy(1000f);

    player.CornerSeam.ShouldBeLessThanOrEqualTo(player.CollisionHalfExtentsLocal.X);
    foreach (var face in _faces(player)) {
      _shapeOf(face).Size.X.ShouldBeGreaterThanOrEqualTo(0f, "an inverted face area collides with nothing");
    }
  }

  private static float[] _faceOuterEdges(Wfc.Entities.World.Player.Player player) {
    var faces = _faces(player);
    var edges = new float[faces.Length];
    for (var i = 0; i < faces.Length; i++) {
      edges[i] = faces[i].Position.Length() + (_shapeOf(faces[i]).Size.Y * 0.5f);
    }
    return edges;
  }

  private static float _faceThickness(Wfc.Entities.World.Player.Player player) =>
    _shapeOf(_faces(player)[0]).Size.Y;

  private static BoxFace[] _faces(Wfc.Entities.World.Player.Player player) => new[] {
    player.GetNode<BoxFace>("BottomFace"),
    player.GetNode<BoxFace>("TopFace"),
    player.GetNode<BoxFace>("LeftFace"),
    player.GetNode<BoxFace>("RightFace")
  };

  private static RectangleShape2D _shapeOf(Node2D face) =>
    (RectangleShape2D)face.GetNode<CollisionShape2D>("CollisionShape2D").Shape;

  private async Task<Wfc.Entities.World.Player.Player> _addPlayer() {
    var player = GD.Load<PackedScene>("res://src/Wfc/Entities/World/Player/Player/Player.tscn")
      .Instantiate<Wfc.Entities.World.Player.Player>();
    _provider.AddChild(player);
    await _physicsFrame();
    player.SetPhysicsProcess(false);
    return player;
  }

  private Task _physicsFrame() => PhysicsFrames.Frame(TestScene);
}
