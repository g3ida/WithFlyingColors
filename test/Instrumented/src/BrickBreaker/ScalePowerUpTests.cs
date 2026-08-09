namespace Wfc.test.instrumented.BrickBreaker;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.BrickBreaker.Powerups;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;

// The two size power-ups share one value on the player and take a while to reach the size they
// promise. Anything that takes one away before it gets there - another pickup, a death, leaving
// the room - has to leave the cube at the size the rest of the game is built around, since nothing
// downstream has any way of telling a power-up's size from a stranded one.
public class ScalePowerUpTests(Node testScene) : TestClass(testScene) {
  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";
  // Counted in physics ticks, which advance with real time, so this is seconds rather than a
  // number of frames - comfortably longer than the size change takes.
  private const int SETTLE_TICKS = 120;

  private FakeDependenciesProvider _provider = default!;
  private Wfc.Entities.World.Player.Player _player = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    _player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    _provider.AddChild(_player);
    await _idle();
    _player.SetPhysicsProcess(false);
  }

  [Cleanup]
  public void Cleanup() {
    _player.QueueFree();
    _provider.QueueFree();
  }

  [Test]
  public async Task ASizeChangeTakenAwayPartWayThroughIsStillHandedBack() {
    var powerUp = _pickUp<ScaleUpPowerUp>();
    await _idle();

    _player.Scale.X.ShouldBeGreaterThan(1.0f, "the cube should have started growing");
    _player.Scale.X.ShouldBeLessThan(powerUp.ScaleFactor, "the test needs the cube caught mid-change");

    powerUp.QueueFree();
    await _idle();

    _player.Scale.ShouldBe(Vector2.One);
  }

  [Test]
  public async Task TheOppositeSizeChangeTakesTheCubeOverFromWhereverItIs() {
    var grow = _pickUp<ScaleUpPowerUp>();
    await _idle();

    var shrink = _pickUp<ScaleDownPowerUp>();
    // What the handler does with the power-up its replacement has just taken over from.
    grow.QueueFree();
    await _settled(shrink.ScaleFactor);

    _player.Scale.X.ShouldBe(shrink.ScaleFactor, MathUtils.EPSILON, "the replacement owns the size now");

    shrink.QueueFree();
    await _idle();

    _player.Scale.ShouldBe(Vector2.One);
  }

  [Test]
  public async Task RespawningGivesTheCubeItsOwnSizeBack() {
    var powerUp = _pickUp<ScaleDownPowerUp>();
    await _settled(powerUp.ScaleFactor);

    _player.Reset();

    _player.Scale.ShouldBe(Vector2.One);

    powerUp.QueueFree();
    await _idle();
  }

  // Parented as the handler parents them, which is what starts the size change.
  private T _pickUp<T>() where T : PlayerScalePowerUp, new() {
    var powerUp = new T();
    _provider.AddChild(powerUp);
    return powerUp;
  }

  // Waited out on the physics clock, not in process frames. The size change is a Tween on a wall
  // clock while a headless run renders as fast as the machine allows, so a fixed count of process
  // frames buys whatever fraction of a second that machine happens to be worth - on CI it stopped
  // a hair short of the target and the size assertion missed by less than a percent.
  private async Task _settled(float scale) {
    for (var tick = 0; tick < SETTLE_TICKS; tick++) {
      if (Mathf.Abs(_player.Scale.X - scale) < MathUtils.EPSILON) {
        return;
      }
      await PhysicsFrames.Frame(TestScene);
    }
  }

  private async Task _idle() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
  }
}
