namespace Wfc.test.instrumented.Audio;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Audio;
using Wfc.Utils;

// The music player owns two things the rest of the game cannot see: an AudioStreamPlayer per
// track it has loaded, and the effects it hangs on the music bus. Both outlive anything that
// merely drops a reference, so both are worth pinning down.
public class MusicTrackManagerTests(Node testScene) : TestClass(testScene) {
  private const string TRACK = "fight";
  private const string BUS = "music";

  private MusicTrackManager _manager = default!;
  private int _effectsBeforeTest;

  // Adding a bus effect is process-wide and nothing takes it off again, so the manager built
  // here leaves two behind on the bus the real one is already using. They are counted going in
  // and taken off again on the way out, or every suite after this one runs against a music bus
  // with a growing stack of duplicates on it.
  [Setup]
  public async Task Setup() {
    _effectsBeforeTest = AudioServer.GetBusEffectCount(AudioServer.GetBusIndex(BUS));
    _manager = SceneHelpers.InstantiateNode<MusicTrackManager>();
    TestScene.AddChild(_manager);
    await _idle();
  }

  [Cleanup]
  public void Cleanup() {
    _manager.QueueFree();
    var busIndex = AudioServer.GetBusIndex(BUS);
    while (AudioServer.GetBusEffectCount(busIndex) > _effectsBeforeTest) {
      AudioServer.RemoveBusEffect(busIndex, 0);
    }
  }

  // The effects are placed by name, so rearranging the bus layout cannot silently send them to
  // whichever bus happens to sit at that position.
  [Test]
  public void ItHangsItsEffectsOnTheMusicBusTest() {
    var busIndex = AudioServer.GetBusIndex(BUS);

    AudioServer.GetBusEffectCount(busIndex).ShouldBe(_effectsBeforeTest + 2);
    AudioServer.GetBusName(busIndex).ShouldBe(BUS);
  }

  [Test]
  public async Task LoadingATrackGivesItAPlayerOnTheMusicBusTest() {
    _manager.LoadTrack(TRACK);
    await _idle();

    var player = _playerOf(_manager).ShouldNotBeNull();
    player.Bus.ToString().ShouldBe(BUS);
  }

  // The regression this file exists for: the player was detached and the pool entry dropped,
  // which left nothing able to reach it and nothing to free it.
  [Test]
  public async Task RemovingATrackFreesItsPlayerTest() {
    _manager.LoadTrack(TRACK);
    await _idle();
    var player = _playerOf(_manager).ShouldNotBeNull();

    _manager.RemoveTrack(TRACK);
    await _idle();

    GodotObject.IsInstanceValid(player).ShouldBeFalse("the stream player should have been freed");
    _playerOf(_manager).ShouldBeNull();
  }

  private static AudioStreamPlayer? _playerOf(Node manager) {
    foreach (var child in manager.GetChildren()) {
      if (child is AudioStreamPlayer player && !player.IsQueuedForDeletion()) {
        return player;
      }
    }
    return null;
  }

  private async Task _idle() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
  }
}
