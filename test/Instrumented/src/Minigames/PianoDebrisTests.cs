namespace Wfc.test.instrumented.Minigames;

using Chickensoft.Sync.Primitives;
using System.Collections.Generic;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Event;
using Wfc.Entities.World.Explosion;
using Wfc.Entities.World.Piano;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;
using Wfc.Utils.Layers;

// A death on the keyboard scatters the cube over the keys, and the keys answer it. The sound is
// the point, but so is what it must not do: the debris share the player's collision layer, so
// the board would read the whole shower as answers to the sheet the player is working through.
public class PianoDebrisTests(Node testScene) : TestClass(testScene) {
  private AutoChannel.Binding? _pianoBinding;
  private readonly List<int> _struck = [];
  private readonly List<int> _pressed = [];

  private FakeDependenciesProvider _services = default!;
  private PianoNote _note = default!;

  [Setup]
  public async Task Setup() {
    _struck.Clear();
    _pressed.Clear();
    _services = new FakeDependenciesProvider();
    TestScene.AddChild(_services);
    _note = SceneHelpers.InstantiateNode<PianoNote>();
    _services.AddChild(_note);
    _pianoBinding = GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.PianoNoteStruck m) => _struck.Add(m.NoteIndex))
      .On((in IGameEvents.PianoNotePressed m) => _pressed.Add(m.NoteIndex));
    await PhysicsFrames.Frame(TestScene);
  }

  [Cleanup]
  public void Cleanup() {
    _pianoBinding?.Dispose();
    _pianoBinding = null;
    _services.QueueFree();
  }

  [Test]
  public async Task ADebrisLandingOnAKeySoundsItWithoutAnsweringTheSheet() {
    _land((await _debris(1, fallSpeed: 400.0f))[0]);

    _struck.ShouldBe([_note.Index]);
    _pressed.ShouldBeEmpty("the sheet must not read debris as an answer");
  }

  // The blast is born inside the keys' detection areas and travels upward out of them, so entry
  // alone says nothing. Only something that has come down onto a key has struck it.
  [Test]
  public async Task DebrisLeavingTheKeysOnTheWayUpDoesNotSoundThem() {
    _land((await _debris(1, fallSpeed: -400.0f))[0]);

    _struck.ShouldBeEmpty("debris flying up off the keyboard should be silent");
  }

  // The cube breaks into dozens of blocks and a key is wide enough to catch a good many of them
  // at once. Sounding every one would be a burst of noise rather than a note.
  [Test]
  public async Task AKeyCaughtByTheWholeCubeAtOnceSoundsOnce() {
    foreach (var block in await _debris(12, fallSpeed: 400.0f)) {
      _land(block);
    }

    _struck.Count.ShouldBe(1);
  }

  // The guard tests above raise the signal by hand and cannot see the one thing the whole effect
  // hangs on: whether a key is ever told about a shard at all, and whether anything about the
  // shard still says "fell hard" by the time it is. It is not the landing frame that reports the
  // overlap, so every one of them passed while the keyboard stayed silent.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ARealShardDroppedOnARealKeySoundsIt() {
    var scene = SceneHelpers.InstantiateNode<PianoScene>();
    _services.AddChild(scene);
    // The room reads a camera off the level it expects to sit in, and there is none here.
    scene.PropagateCall(Node.MethodName.SetProcess, new Godot.Collections.Array { false });
    await PhysicsFrames.Frame(TestScene);

    var key = (PianoNote)scene.GetNode<Node>("Piano/NotesContainer").GetChild(2);
    var shard = (await _debris(1, fallSpeed: 0.0f))[0];
    shard.SetColliderShape(new RectangleShape2D { Size = new Vector2(12, 12) });
    shard.CollisionLayer = PhysicsLayers.Player.Mask;
    shard.GlobalPosition = key.GlobalPosition + new Vector2(0, -200);
    shard.GravityScale = 3;

    (await PhysicsFrames.WaitFor(TestScene, () => _struck.Count > 0, 3.0))
      .ShouldBeTrue("a shard falling onto a key never sounded it");
    _struck.ShouldBe([key.Index]);
  }

  // The signal is wired to the handler in the .tscn, so raising it exercises the connection and
  // the guard together, without asking the physics server to report an overlap it would need a
  // real fall to produce.
  private void _land(ExplosionElement debris) =>
    _note.GetNode<Area2D>("Area2D").EmitSignal(Area2D.SignalName.BodyEntered, debris);

  // How hard a shard came down is taken while it is still coming down, so a shard that has never
  // been stepped has no fall to its name however its velocity reads. Weightless, or the drop over
  // the frame they are given would be a fall of its own.
  private async Task<List<ExplosionElement>> _debris(int count, float fallSpeed) {
    var blocks = new List<ExplosionElement>();
    for (var block = 0; block < count; block++) {
      var debris = SceneHelpers.InstantiateNode<ExplosionElement>();
      _services.AddChild(debris);
      debris.GravityScale = 0.0f;
      debris.LinearVelocity = new Vector2(0.0f, fallSpeed);
      blocks.Add(debris);
    }
    await PhysicsFrames.Frame(TestScene);
    return blocks;
  }
}
