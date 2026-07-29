namespace Wfc.test.instrumented.Enemies;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Enemies;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;
using Wfc.Utils.Colors;
using EventHandler = Wfc.Core.Event.EventHandler;

// The beam reports by looking rather than by being touched, which makes it the one hazard that can
// go on reporting the same contact for as long as the cube stands in it.
public class LazerBeamTests(Node testScene) : TestClass(testScene) {
  private const string PLAYER_SCENE = "res://src/Wfc/Entities/World/Player/Player/Player.tscn";

  // The beam fires along its own +X, so it lands on the cube's left face, and that face wears pink.
  private const string A_COLOR_THE_LEFT_FACE_REFUSES = ColorUtils.BLUE;
  private const string THE_LEFT_FACE_COLOR = ColorUtils.PINK;
  private const float BACK_DOWN_THE_BEAM = 300.0f;
  private const int HELD_FRAMES = 10;

  private FakeDependenciesProvider _provider = default!;
  private int _deaths;

  [Setup]
  public async Task Setup() {
    _deaths = 0;
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    EventHandler.Instance.Events.PlayerDying += _onPlayerDying;
    await _physicsFrame();
  }

  [Cleanup]
  public void Cleanup() {
    EventHandler.Instance.Events.PlayerDying -= _onPlayerDying;
    _provider.QueueFree();
  }

  [Test]
  public async Task ABeamHeldOnAFaceItBurnsReportsOneDeath() {
    await _holdBeamOnLeftFace(A_COLOR_THE_LEFT_FACE_REFUSES);

    _deaths.ShouldBe(1, "the beam reported the same crossing once a frame");
  }

  [Test]
  public async Task ABeamOfTheFacesOwnColorReportsNothing() {
    await _holdBeamOnLeftFace(THE_LEFT_FACE_COLOR);

    _deaths.ShouldBe(0);
  }

  // The cube stands in the beam with its own physics stopped, so nothing turns the first report into
  // a dying state: what is counted here is what the beam says, not what the cube makes of it.
  private async Task _holdBeamOnLeftFace(string beamColor) {
    var player = GD.Load<PackedScene>(PLAYER_SCENE).Instantiate<Wfc.Entities.World.Player.Player>();
    _provider.AddChild(player);
    await _physicsFrame();
    player.SetPhysicsProcess(false);

    var beam = SceneHelpers.InstantiateNode<LazerBeam>();
    beam.ColorGroup = beamColor;
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

  private void _onPlayerDying(Node? area, Vector2 position, int entityType) => _deaths++;

  private async Task _physicsFrame() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
  }
}
