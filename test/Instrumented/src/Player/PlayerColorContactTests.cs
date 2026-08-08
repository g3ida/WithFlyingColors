namespace Wfc.test.instrumented.Player;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Gems;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils.Colors;

// A gem is wide enough to reach two faces of the cube at once, and a dash covers enough ground in
// a single frame to arrive on both of them together. The face wearing the gem's color takes it;
// the other one reports the same contact, and reporting it as a wrong-color death had the gem
// collected and paid for at the same time.
public class PlayerColorContactTests(Node testScene) : TestClass(testScene) {
  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";
  private const string GEM_SCENE = "res://src/Wfc/Entities/World/Gems/Gem/Gem.tscn";

  // The cube in the scene wears these where the gem is put, and the corner between them answers
  // for both. Nothing else on the cube is near enough to the gem to have an opinion.
  private const string TOP_FACE_COLOR = ColorUtils.BLUE;
  private const string RIGHT_FACE_COLOR = ColorUtils.YELLOW;
  private const string NO_FACE_NEAR_THE_GEM = ColorUtils.PINK;

  private const float FLOOR_HALF_HEIGHT = 50f;
  private const float FLOOR_HALF_WIDTH = 1200f;
  private const float FLOOR_CENTER_Y = 400f;
  private const int A_LANDING = 30;
  private const int A_CONTACT_AND_THE_STATE_THAT_TAKES_IT = 3;

  private FakeDependenciesProvider _provider = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await _physicsFrame();
  }

  [Cleanup]
  public void Cleanup() => _provider.QueueFree();

  [Test]
  public async Task AGemOneFaceTakesDoesNotKillTheCubeWithAnother() {
    var player = await _addPlayerOnGround();
    var gem = _addGemAcrossTheTopRightCorner(player, RIGHT_FACE_COLOR);

    await PhysicsFrames.Advance(TestScene, A_CONTACT_AND_THE_STATE_THAT_TAKES_IT);

    gem.IsBeingCollected.ShouldBeTrue("the face wearing the gem's color never reached it");
    player.IsDying().ShouldBeFalse("the gem was collected and paid for at the same time");
  }

  // The rule the fix has to leave standing: a gem still asks for its own color, and a cube that
  // has none of it anywhere near the contact still dies on it.
  [Test]
  public async Task AGemNoNearbyFaceWearsStillKills() {
    var player = await _addPlayerOnGround();
    var gem = _addGemAcrossTheTopRightCorner(player, NO_FACE_NEAR_THE_GEM);

    await PhysicsFrames.Advance(TestScene, A_CONTACT_AND_THE_STATE_THAT_TAKES_IT);

    gem.IsBeingCollected.ShouldBeFalse("a gem no face wears was taken anyway");
    player.IsDying().ShouldBeTrue("a wrong-color gem stopped being lethal");
  }

  // A gem the level has already banked asks nothing of the player and any face may walk through it,
  // which it could not while every face was still judging it for its color.
  [Test]
  public async Task AGemTheLevelHasAlreadyGivenUpIsWalkedThroughByAnyFace() {
    var player = await _addPlayerOnGround();
    var gem = _addGemAcrossTheTopRightCorner(player, NO_FACE_NEAR_THE_GEM);
    gem.MarkAlreadyCollected();

    await PhysicsFrames.Advance(TestScene, A_CONTACT_AND_THE_STATE_THAT_TAKES_IT);

    player.IsDying().ShouldBeFalse("a ghost still charged the player for the color it no longer owes");
  }

  // Centered on the corner, where the gem's own width carries it across the top face and the right
  // face at once - which is where a dash puts it, a whole frame's travel at a time.
  private Gem _addGemAcrossTheTopRightCorner(Wfc.Entities.World.Player.Player player, string colorGroup) {
    var gem = GD.Load<PackedScene>(GEM_SCENE).Instantiate<Gem>();
    gem.GroupName = colorGroup;
    // A gem bobs about wherever it was standing when it entered the tree, so moving it afterwards
    // only lasts until its first physics frame.
    var half = player.GetCollisionHalfExtents();
    gem.Position = player.GlobalPosition + new Vector2(half.X, -half.Y);
    _provider.AddChild(gem);
    return gem;
  }

  private async Task<Wfc.Entities.World.Player.Player> _addPlayerOnGround() {
    var floor = new StaticBody2D();
    floor.AddChild(new CollisionShape2D {
      Shape = new RectangleShape2D { Size = new Vector2(FLOOR_HALF_WIDTH * 2f, FLOOR_HALF_HEIGHT * 2f) }
    });
    _provider.AddChild(floor);
    floor.GlobalPosition = new Vector2(0f, FLOOR_CENTER_Y);

    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    player.CollisionMask = 13;
    player.GlobalPosition = new Vector2(0f, FLOOR_CENTER_Y - FLOOR_HALF_HEIGHT - 60f);
    _provider.AddChild(player);

    await PhysicsFrames.Advance(TestScene, A_LANDING);
    player.IsOnFloor().ShouldBeTrue("the cube never landed on the test floor");
    return player;
  }

  private Task _physicsFrame() => PhysicsFrames.Frame(TestScene);
}
