namespace Wfc.test.instrumented.Doors;

using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Door;
using Wfc.Screens.Levels;
using Wfc.test.Helpers.Fakes;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;
using Wfc.Utils.Colors;

// What a door says about the level behind it. The gems in the arch count them one by one;
// the keystone comet is what the four of them make, so it is carved stone until the last one
// is home. And a clear the player has just walked out of is watched rather than found: the
// door holds the gems it has been warned about until the ceremony puts them in.
public class DoorCeremonyTests(Node testScene) : TestClass(testScene) {
  private FakeDependenciesProvider _provider = default!;
  private Door _door = default!;

  [Setup]
  public async Task Setup() {
    _provider = new FakeDependenciesProvider();
    TestScene.AddChild(_provider);
    _door = SceneHelpers.InstantiateNode<Door>();
    _door.TargetLevel = LevelId.Tutorial;
    _provider.AddChild(_door);
    await _frames(2);
  }

  [Cleanup]
  public void Cleanup() => _provider.QueueFree();

  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task TheKeystoneStaysCarvedUntilEveryGemIsHome() {
    _provider.Save = new FakeSaveManager(selectedSlot: 0).WithFilledSlot(0, progress: 0);
    _provider.Save.RecordLevelCleared(TestScene.GetTree(), LevelId.Tutorial, LevelId.Hub, _allGemsButOne());
    await _frames(1);

    _keystone().IsComplete.ShouldBeFalse("three gems lit the comet that only four can make");

    _provider.Save.RecordLevelCleared(TestScene.GetTree(), LevelId.Tutorial, LevelId.Hub, ColorUtils.COLOR_GROUPS);
    await _frames(1);

    _keystone().IsComplete.ShouldBeTrue("every gem is home and the comet is still carved stone");
  }

  // The clear is banked while the swap cover is still down. A door that took it there and then
  // would have finished celebrating before the player ever saw the hub.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task AWarnedDoorHoldsItsGemsBackUntilTheCeremonyRuns() {
    _provider.Save = new FakeSaveManager(selectedSlot: 0).WithFilledSlot(0, progress: 0);
    await _frames(1);

    _door.ExpectCelebration();
    _provider.Save.RecordLevelCleared(TestScene.GetTree(), LevelId.Tutorial, LevelId.Hub, ColorUtils.COLOR_GROUPS);
    await _frames(2);

    _archGems().Any(gem => gem.IsCollected)
      .ShouldBeFalse("the door showed the gems while the screen was still covered");

    _door.Celebrate();

    (await _waitUntil(() => _archGems().All(gem => gem.IsCollected)))
      .ShouldBeTrue("the ceremony never put the gems into the arch");
    (await _waitUntil(() => _keystone().IsComplete))
      .ShouldBeTrue("the gems all arrived but the comet never formed");
  }

  // Nothing was warned, so nothing is owed: a door that queued a ceremony it was never told
  // to expect would sit on the gems until something asked for them.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task AnUnwarnedDoorShowsWhatTheSlotSaysAtOnce() {
    _provider.Save = new FakeSaveManager(selectedSlot: 0).WithFilledSlot(0, progress: 0);
    _provider.Save.RecordLevelCleared(TestScene.GetTree(), LevelId.Tutorial, LevelId.Hub, ColorUtils.COLOR_GROUPS);
    await _frames(2);

    _archGems().All(gem => gem.IsCollected).ShouldBeTrue("the door held gems nobody was going to celebrate");
    _keystone().IsComplete.ShouldBeTrue();
  }

  private static string[] _allGemsButOne() => [.. ColorUtils.COLOR_GROUPS.Take(ColorUtils.COLOR_GROUPS.Length - 1)];

  private DoorGem _keystone() => _door.FindDescendants<DoorGem>().First();

  private DoorArchGem[] _archGems() => [.. _door.FindDescendants<DoorArchGem>()];

  private async Task _frames(int count) => await PhysicsFrames.Advance(TestScene, count);

  private async Task<bool> _waitUntil(System.Func<bool> until) =>
    await PhysicsFrames.WaitFor(TestScene, until, 15.0);
}
