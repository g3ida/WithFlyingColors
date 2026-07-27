namespace Wfc.test.instrumented;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Utils.Layers;

// Which layers the cube answers to, on the two nodes that answer for different things: the body,
// which decides what the cube cannot walk through, and the color faces, which decide what kills
// it. A mask edit that breaks one of these is invisible until someone plays the level it broke.
//
// Nothing is added to the tree - masks are scene data, and staying out keeps _Ready and the
// dependencies it would want out of it.
public class PlayerLayersTests(Node testScene) : TestClass(testScene) {
  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";
  private const string POWER_UP_SCENE = "res://src/Wfc/Entities/World/BrickBreaker/Powerups/PowerUp/PowerUp.tscn";

  private static readonly string[] FACE_NODES = {
    "TopFace", "BottomFace", "LeftFace", "RightFace",
    "FaceSeparatorTL", "FaceSeparatorTR", "FaceSeparatorBL", "FaceSeparatorBR"
  };

  // Everything the cube is allowed to be killed or landed on by. Each of these carries its color
  // as a group and is judged by whichever face reaches it.
  private static readonly (string Name, LayerInfo Layer)[] COLORED_PARTNERS = {
    ("platforms", PhysicsLayers.Platform),
    ("fall zones", PhysicsLayers.FallZone),
    ("gems", PhysicsLayers.Gems),
    ("bullets", PhysicsLayers.Bullets),
    ("tetris blocks", PhysicsLayers.Tetris),
    ("bricks", PhysicsLayers.Bricks),
  };

  [Test]
  public void EveryColorFaceSeesEveryColoredThingItCanTouch() {
    using var player = _load(PLAYER_SCENE);

    foreach (var face in FACE_NODES) {
      var area = player.GetNode<Area2D>(face);
      area.CollisionLayer.ShouldBe(
        PhysicsLayers.BoxFace.Mask,
        $"{face} is off the face layer, so nothing that looks for a face finds it"
      );
      foreach (var (name, layer) in COLORED_PARTNERS) {
        (area.CollisionMask & layer.Mask).ShouldBe(
          layer.Mask,
          $"{face} cannot see {name}, so touching one is neither fatal nor a landing"
        );
      }
    }
  }

  // The lazer casts against the face layer to find out what color the cube is showing it, so a
  // face off that layer leaves the beam shining through the player.
  [Test]
  public void TheFacesAreOnTheLayerTheLazerCastsAgainst() {
    using var player = _load(PLAYER_SCENE);

    foreach (var face in FACE_NODES) {
      (player.GetNode<Area2D>(face).CollisionLayer & PhysicsLayers.BoxFace.Mask)
        .ShouldBe(PhysicsLayers.BoxFace.Mask);
    }
  }

  // A power-up is caught, not walked into. It used to carry a static body the cube alone could
  // collide with, so an uncollected one - or one that arrived while the player was dying, which
  // skipped the free - stood in the arena as an invisible wall.
  [Test]
  public void APowerUpIsNotSomethingTheCubeCanStandOn() {
    using var player = _load(PLAYER_SCENE);
    using var powerUp = _load<Node2D>(POWER_UP_SCENE);

    (player.CollisionMask & PhysicsLayers.PowerUp.Mask)
      .ShouldBe(0u, "a solid power-up is an obstacle in an arena that has no room for one");

    foreach (var child in powerUp.GetChildren()) {
      child.ShouldNotBeOfType<StaticBody2D>("nothing is left for the cube to collide with");
    }
  }

  // And it is still caught: the pickup is the power-up's own area noticing the cube's body.
  [Test]
  public void APowerUpStillSeesTheCubeThatCatchesIt() {
    using var player = _load(PLAYER_SCENE);
    using var powerUp = _load<Node2D>(POWER_UP_SCENE);

    (powerUp.GetNode<Area2D>("Area2D").CollisionMask & player.CollisionLayer)
      .ShouldBe(player.CollisionLayer, "a power-up that cannot see the player can never be picked up");
  }

  // Fall zones and faces exist only as areas, so a body mask bit for either detects nothing and
  // says nothing. Carrying them read as though the body took part in those contacts, which is the
  // same misreading that left the cube solid against falling power-ups.
  [Test]
  public void TheCubesBodyDoesNotMaskLayersOnlyAreasLiveOn() {
    using var player = _load(PLAYER_SCENE);
    var areaOnly = PhysicsLayers.FallZone.Mask | PhysicsLayers.BoxFace.Mask;

    (player.CollisionMask & areaOnly)
      .ShouldBe(0u, "the color faces detect these; a body mask bit cannot detect anything");
  }

  // A bullet does have a body, and briefly stopped the cube dead on contact - it frees itself on
  // touching the player, but freeing is deferred and the body is solid for the rest of the frame.
  // Whether a bullet kills is the faces' business; it is never a wall.
  [Test]
  public void ABulletDoesNotBlockTheCube() {
    using var player = _load(PLAYER_SCENE);

    (player.CollisionMask & PhysicsLayers.Bullets.Mask)
      .ShouldBe(0u, "a bullet the cube cannot walk through is a moving wall");
  }

  // Tetris blocks, by contrast, are bodies the cube is meant to stand on.
  [Test]
  public void TheCubeStillStandsOnTetrisBlocks() {
    using var player = _load(PLAYER_SCENE);

    (player.CollisionMask & PhysicsLayers.Tetris.Mask)
      .ShouldBe(PhysicsLayers.Tetris.Mask, "the cube rides the falling blocks");
  }

  // The cube still has to stand on the world.
  [Test]
  public void TheCubeStillCollidesWithTheGround() {
    using var player = _load(PLAYER_SCENE);
    var ground = PhysicsLayers.Default.Mask | PhysicsLayers.Platform.Mask;

    (player.CollisionMask & ground).ShouldBe(ground);
  }

  private static T _load<T>(string path) where T : Node => GD.Load<PackedScene>(path).Instantiate<T>();

  private static CharacterBody2D _load(string path) => _load<CharacterBody2D>(path);
}
