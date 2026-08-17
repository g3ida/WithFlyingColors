namespace Wfc.test.instrumented.Enemies;

using Chickensoft.Sync.Primitives;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Event;
using Wfc.Entities.World.Enemies;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;
using Wfc.Utils.Colors;

// The rhythm is the hazard: the same beam on the same face must burn while it
// fires and must not while it rests or telegraphs.
public class TimingLazerTests(Node testScene) : TestClass(testScene) {
  private AutoChannel.Binding? _dyingBinding;

  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";

  // The beam fires along its own +X, so it lands on the cube's left face, and that face wears pink.
  private const string A_COLOR_THE_LEFT_FACE_REFUSES = ColorUtils.BLUE;
  private const float BACK_DOWN_THE_BEAM = 300.0f;
  private const int HELD_FRAMES = 10;

  // Long enough that the cycle never rolls over inside a ten-frame test.
  private const float OUTLASTS_THE_TEST = 100.0f;

  private FakeDependenciesProvider _provider = default!;
  private int _deaths;

  [Setup]
  public async Task Setup() {
    _deaths = 0;
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    _dyingBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.PlayerDying _) => _onPlayerDying());
    await _physicsFrame();
  }

  [Cleanup]
  public void Cleanup() {
    _dyingBinding?.Dispose();
    _dyingBinding = null;
    _provider.QueueFree();
  }

  [Test]
  public async Task AFiringLazerBurnsTheFaceOnce() {
    await _holdBeamOnLeftFace(fireDuration: OUTLASTS_THE_TEST, telegraphDuration: 0f);

    _deaths.ShouldBe(1);
  }

  [Test]
  public async Task ARestingLazerBurnsNothing() {
    await _holdBeamOnLeftFace(fireDuration: 0f, telegraphDuration: 0f);

    _deaths.ShouldBe(0);
  }

  [Test]
  public async Task ATelegraphingLazerBurnsNothing() {
    await _holdBeamOnLeftFace(fireDuration: 0f, telegraphDuration: OUTLASTS_THE_TEST);

    _deaths.ShouldBe(0);
  }

  private async Task _holdBeamOnLeftFace(float fireDuration, float telegraphDuration) {
    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    _provider.AddChild(player);
    await _physicsFrame();
    player.SetPhysicsProcess(false);

    var beam = SceneHelpers.InstantiateNode<TimingLazer>();
    beam.ColorGroup = A_COLOR_THE_LEFT_FACE_REFUSES;
    beam.FireDuration = fireDuration;
    beam.RestDuration = OUTLASTS_THE_TEST;
    beam.TelegraphDuration = telegraphDuration;
    _provider.AddChild(beam);
    beam.GlobalPosition = player.GlobalPosition - new Vector2(BACK_DOWN_THE_BEAM, 0f);
    _deaths = 0;

    for (var frame = 0; frame < HELD_FRAMES; frame++) {
      await _physicsFrame();
    }

    player.QueueFree();
    beam.QueueFree();
    await _physicsFrame();
  }

  private void _onPlayerDying() => _deaths++;

  private Task _physicsFrame() => PhysicsFrames.Frame(TestScene);
}
