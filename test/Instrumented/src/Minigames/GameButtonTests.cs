namespace Wfc.test.instrumented.Minigames;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.ButtonGame;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;
using Wfc.Utils.Colors;

// A button is a coloured surface the cube is meant to stand on, which puts its colour area within
// a couple of pixels of the faces that are not the one standing there. Get that wrong by so much
// as a pixel and stepping on the right button kills the player, which reads as the puzzle being
// broken rather than as a collision shape being a few pixels off.
public class GameButtonTests(Node testScene) : TestClass(testScene) {
  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";
  // Default, platform and fall zone: what the cube collides with in a level, minus the layers
  // this test has nothing on.
  private const uint PLAYER_MASK = 13;
  // Where a button places the surface the cube lands on, above the button's own origin.
  private const float CAP_TOP = -81f;

  private FakeDependenciesProvider _services = default!;
  private FakeGameLevelProvider _level = default!;

  [Setup]
  public async Task Setup() {
    _services = new FakeDependenciesProvider();
    TestScene.AddChild(_services);
    _level = new FakeGameLevelProvider();
    _services.AddChild(_level);
    await PhysicsFrames.Frame(TestScene);
  }

  [Cleanup]
  public void Cleanup() => _services.QueueFree();

  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task StandingOnAButtonOfTheColourFacingDownIsSafe() {
    // The cube starts with its purple face down, so a purple button is the one it may stand on.
    _button(ColorUtils.PURPLE);
    var player = _playerAbove();

    await PhysicsFrames.Advance(TestScene, 60);

    player.IsDying().ShouldBeFalse("standing on a button of the cube's own colour killed it");
  }

  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task StandingOnAButtonOfAnotherColourStillKills() {
    _button(ColorUtils.YELLOW);
    var player = _playerAbove();

    await PhysicsFrames.Advance(TestScene, 60);

    player.IsDying().ShouldBeTrue("the button's colour stopped being lethal at all");
  }

  // The press is what the room listens for, and it only reports once the cap has finished
  // travelling - a button that reported on contact would fire before it looked pressed.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task StandingOnAButtonPressesItAndReportsIt() {
    var button = _button(ColorUtils.PURPLE);
    var cap = button.GetNode<AnimatableBody2D>("Cap");
    var restedAt = cap.Position.Y;
    var pressed = -1;
    button.ButtonPressed += index => pressed = index;

    _playerAbove();
    var deepest = restedAt;
    for (var frame = 0; frame < 90; frame++) {
      await PhysicsFrames.Frame(TestScene);
      deepest = Mathf.Max(deepest, cap.Position.Y);
    }
    deepest.ShouldBeGreaterThan(restedAt + 1f, "the cap did not travel into its base");
    pressed.ShouldBe(button.Index, "the button never reported the press");
    cap.Position.Y.ShouldBeGreaterThan(restedAt + 1f, "the cap came back up under a cube still standing on it");
  }

  #region Helpers
  private GameButton _button(string colorGroup) {
    var button = SceneHelpers.InstantiateNode<GameButton>();
    button.ColorGroup = colorGroup;
    _level.AddChild(button);
    return button;
  }

  // Dropped from just above the cap rather than placed on it: landing is what runs the colour
  // check, and a cube that started already touching would never enter the area at all.
  private Wfc.Entities.World.Player.Player _playerAbove() {
    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    player.CollisionMask = PLAYER_MASK;
    player.Position = new Vector2(0f, CAP_TOP - 120f);
    _level.AddChild(player);
    _level.PlayerNode = player;
    return player;
  }
  #endregion Helpers
}
