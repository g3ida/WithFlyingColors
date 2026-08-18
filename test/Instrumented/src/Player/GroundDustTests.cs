namespace Wfc.test.instrumented.Player;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Input;
using Wfc.Entities.World.Player;
using Wfc.Skin;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils.Colors;
using Wfc.Utils.Layers;

// The dust behind a walking cube is drawn in the colour of the ground it is walking on, which is
// the one thing on screen the player has to keep reading. Where that colour comes from is the
// whole of the effect: a neutral surface wears every colour group rather than none, so the first
// group found on one is an arbitrary colour it looks nothing like.
public class GroundDustTests(Node testScene) : TestClass(testScene) {
  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";

  private const float FLOOR_HALF_HEIGHT = 50f;
  private const float FLOOR_HALF_WIDTH = 1200f;
  private const float FLOOR_CENTER_Y = 400f;
  private const int A_LANDING = 30;
  private const int A_WALK_UP_TO_SPEED = 10;
  private const double A_LANDING_AT_MOST_SECONDS = 2.0;
  // Comfortably over, and comfortably under, the fall a burst needs.
  private const float A_FALL_WORTH_RAISING_DUST = 200f;
  private const float A_STEP_DOWN = 8f;

  private FakeDependenciesProvider _provider = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    await PhysicsFrames.Frame(TestScene);
  }

  [Cleanup]
  public void Cleanup() {
    _provider.Input.ReleaseAll();
    _provider.QueueFree();
  }

  [Test]
  public async Task WalkingOnAColoredSurfaceRaisesDustInItsColor() {
    var dust = await _walkOnGroundWearing(ColorUtils.PURPLE);

    dust.Emitting.ShouldBeTrue("a cube walking at speed left no dust behind it");
    _rgbOf(dust).ShouldBe(_skinColorOf(ColorUtils.PURPLE), "the dust is not the colour of the floor");
  }

  // The trap the effect is built around: reading a colour name off a neutral surface picks one of
  // the four at random, and the cube would kick up purple dust off a white platform.
  [Test]
  public async Task WalkingOnANeutralSurfaceRaisesColorlessDust() {
    var dust = await _walkOnGroundWearing(ColorUtils.COLOR_GROUPS);

    _rgbOf(dust).ShouldBe(Colors.White, "a surface every face is safe on lent the dust a colour");
  }

  [Test]
  public async Task AStandingCubeRaisesNoDust() {
    var player = await _addPlayerOnGroundWearing(ColorUtils.PURPLE);
    await PhysicsFrames.Advance(TestScene, A_WALK_UP_TO_SPEED);

    _trailOf(player).Emitting.ShouldBeFalse("a cube that never moved kicked up dust");
  }

  [Test]
  public async Task LandingOnAColoredSurfaceRaisesDustInItsColor() {
    var player = await _addPlayerOnGroundWearing(ColorUtils.PURPLE);
    var landing = _landingOf(player);
    landing.Emitting.ShouldBeFalse("the burst went off before the cube had landed on anything");

    await _dropFrom(player, A_FALL_WORTH_RAISING_DUST);

    landing.Emitting.ShouldBeTrue("landing raised no dust");
    _rgbOf(landing).ShouldBe(_skinColorOf(ColorUtils.PURPLE), "the burst is not the colour of the floor");
  }

  // The burst is placed by hand on the tick the floor comes back, off an emitter that has been
  // sitting wherever the last landing left it - so where it ends up is worth stating.
  [Test]
  public async Task LandingAfterAJumpBurstsUnderTheCube() {
    var player = await _addPlayerOnGroundWearing(ColorUtils.PURPLE);
    var landing = _landingOf(player);
    _provider.Input.Press(IInputManager.Action.MoveRight);
    await PhysicsFrames.Advance(TestScene, A_WALK_UP_TO_SPEED);
    _provider.Input.Press(IInputManager.Action.Jump);
    await PhysicsFrames.Frame(TestScene);
    _provider.Input.Release(IInputManager.Action.Jump);
    var airborne = await PhysicsFrames.WaitFor(TestScene, () => !player.IsOnFloor(), 2.0);
    airborne.ShouldBeTrue("never left the ground");
    var landed = await PhysicsFrames.WaitFor(TestScene, () => player.IsOnFloor(), 3.0);
    landed.ShouldBeTrue("never came down");

    landing.Emitting.ShouldBeTrue("a cube that jumped and came down raised no dust");
    landing.GlobalPosition.X.ShouldBe(player.GlobalPosition.X, 1f,
      "the burst went off somewhere other than under the cube");
    landing.GlobalPosition.Y.ShouldBeGreaterThan(player.GlobalPosition.Y,
      "the burst went off above the cube rather than at its underside");
  }

  // A cube that steps down off a lip has not come down on anything, and puffing there turns every
  // nudge along a broken surface into an event.
  [Test]
  public async Task SteppingDownRaisesNoDust() {
    var player = await _addPlayerOnGroundWearing(ColorUtils.PURPLE);

    await _dropFrom(player, A_STEP_DOWN);

    _landingOf(player).Emitting.ShouldBeFalse("a step down was drawn as a landing");
  }

  // Lifted straight up and let go, so what lands is a fall of a known height rather than whatever
  // a jump happens to be worth.
  private async Task _dropFrom(Wfc.Entities.World.Player.Player player, float height) {
    player.GlobalPosition -= new Vector2(0f, height);
    // A body lifted off the ground still reports standing on it until it is next moved, so the
    // fall has to be waited out from the far side of the cube actually leaving.
    var airborne = await PhysicsFrames.WaitFor(
      TestScene, () => !player.IsOnFloor(), A_LANDING_AT_MOST_SECONDS);
    airborne.ShouldBeTrue("the cube never left the floor it was lifted off");
    var landed = await PhysicsFrames.WaitFor(
      TestScene, () => player.IsOnFloor(), A_LANDING_AT_MOST_SECONDS);
    landed.ShouldBeTrue("the cube never came back down");
  }

  private async Task<CpuParticles2D> _walkOnGroundWearing(params string[] colorGroups) {
    var player = await _addPlayerOnGroundWearing(colorGroups);
    _provider.Input.Press(IInputManager.Action.MoveRight);
    await PhysicsFrames.Advance(TestScene, A_WALK_UP_TO_SPEED);
    return _trailOf(player);
  }

  private async Task<Wfc.Entities.World.Player.Player> _addPlayerOnGroundWearing(params string[] colorGroups) {
    var floor = new StaticBody2D();
    floor.AddChild(new CollisionShape2D {
      Shape = new RectangleShape2D { Size = new Vector2(FLOOR_HALF_WIDTH * 2f, FLOOR_HALF_HEIGHT * 2f) }
    });
    // The colour rides on an area over the body, the way every coloured surface in the game
    // carries its colour, with its top edge on the surface the cube stands on.
    var colorArea = new Area2D { CollisionLayer = PhysicsLayers.Platform.Mask, Monitoring = false };
    colorArea.AddChild(new CollisionShape2D {
      Shape = new RectangleShape2D { Size = new Vector2(FLOOR_HALF_WIDTH * 2f, FLOOR_HALF_HEIGHT * 2f) }
    });
    foreach (var group in colorGroups) {
      colorArea.AddToGroup(group);
    }
    floor.AddChild(colorArea);
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

  private static CpuParticles2D _trailOf(Wfc.Entities.World.Player.Player player) =>
    player.GetNode<CpuParticles2D>("GroundDust/Walk");

  private static CpuParticles2D _landingOf(Wfc.Entities.World.Player.Player player) =>
    player.GetNode<CpuParticles2D>("GroundDust/Landing");

  private static Color _rgbOf(CpuParticles2D dust) => new(dust.Color, 1f);

  private static Color _skinColorOf(string colorGroup) => SkinManager.Instance.CurrentSkin.GetColor(
    GameSkin.ColorGroupToSkinColor(colorGroup), SkinColorIntensity.Basic);
}
