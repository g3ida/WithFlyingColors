namespace Wfc.test.instrumented.Minigames;

using Chickensoft.Sync.Primitives;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Event;
using Wfc.Core.Localization;
using Wfc.Entities.World.ButtonGame;
using Wfc.Entities.World.Platforms;
using Wfc.test.instrumented.Helpers;
using Wfc.test.instrumented.Helpers.Fakes;
using Wfc.Utils;
using Wfc.Utils.Colors;

// The door is the room's reward and the only thing in it that makes a continuous noise. Both ends
// of that matter: it has to open when the puzzle is won, and it has to fall silent when it gets
// there rather than humming for the rest of the level.
public class ButtonGameDoorTests(Node testScene) : TestClass(testScene) {
  private AutoChannel.Binding? _notifiedBinding;

  private FakeDependenciesProvider _services = default!;
  private FakeGameLevelProvider _level = default!;
  private ButtonGameScene _room = default!;

  [Setup]
  public async Task Setup() {
    _services = new FakeDependenciesProvider();
    TestScene.AddChild(_services);
    _level = new FakeGameLevelProvider();
    _services.AddChild(_level);
    _room = SceneHelpers.InstantiateNode<ButtonGameScene>();
    _level.AddChild(_room);
    await PhysicsFrames.Frame(TestScene);
  }

  [Cleanup]
  public void Cleanup() => _services.QueueFree();

  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task TheDoorOpensOnTheWinAndGoesSilentOnceItHasArrived() {
    _win();

    (await PhysicsFrames.WaitFor(TestScene, () => !_slider().IsPhysicsProcessing(), 6.0))
      .ShouldBeTrue("the door never finished its run");

    _door().Position.Y.ShouldBe(360f, 1f, "the door did not travel its full run");
    _sound().Playing.ShouldBeFalse("the door kept its slide sound going after it had parked");

    // And stays silent: nothing restarts it a second later.
    await PhysicsFrames.Advance(TestScene, 60);
    _sound().Playing.ShouldBeFalse("the door's slide sound came back after it had stopped");
  }

  // A won room is one the player has already opened. Coming back into it after a death has to find
  // the door open and quiet, not slam it shut and run the whole opening - noise and all - again.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task ARespawnIntoAWonRoomFindsTheDoorAlreadyOpen() {
    _win();
    (await PhysicsFrames.WaitFor(TestScene, () => !_slider().IsPhysicsProcessing(), 6.0)).ShouldBeTrue();

    GameEvents.Instance.OnCheckpointLoaded();
    await PhysicsFrames.Advance(TestScene, 30);

    _door().Position.Y.ShouldBe(360f, 1f, "the respawn shut the door on a room that was already won");
    _sound().Playing.ShouldBeFalse("the respawn set the door sliding, and sounding, all over again");
  }

  // Winning is the room's second checkpoint, and it has to announce itself the way the campfire at
  // the stand does - the player is being told the same thing, that what they just did is kept.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task WinningTakesACheckpointAndReportsIt() {
    var checkpoints = 0;
    var notifications = 0;
    var takenAt = Vector2.Zero;
    void OnReached(Vector2 position, string _group) {
      checkpoints += 1;
      takenAt = position;
    }
    void OnNotified(TranslationKey key) => notifications += 1;
    _notifiedBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.NotificationRaised m) => OnNotified(m.Key))
      .On((in IGameEvents.CheckpointReached m) => OnReached(m.Position, m.ColorGroup));

    _win();
    await PhysicsFrames.Advance(TestScene, 4);

    _notifiedBinding?.Dispose();
    _notifiedBinding = null;

    checkpoints.ShouldBe(1, "winning should take exactly one checkpoint, and the rounds none");
    notifications.ShouldBe(1, "the checkpoint the win takes was never reported to the player");
    takenAt.X.ShouldBe(_room.GetNode<Marker2D>("WinCheckpoint").GlobalPosition.X, 1f);
  }

  // And winning is worth banking, unlike the rounds on the way to it: this is the checkpoint that
  // makes the room survive the player putting the game down.
  [Test]
  [Timeout(SlowTest.TIMEOUT_MILLISECONDS)]
  public async Task TheWinSurvivesLeavingTheGame() {
    _win();
    await PhysicsFrames.Advance(TestScene, 4);
    var serializer = new Wfc.Core.Serialization.SimpleJsonSerializer();

    var saved = _game().Save(serializer);
    var reloaded = SceneHelpers.InstantiateNode<ButtonGame>();
    _level.AddChild(reloaded);
    await PhysicsFrames.Frame(TestScene);
    reloaded.Load(serializer, saved);

    reloaded.IsWon().ShouldBeTrue("the win was not written to the save");
    reloaded.QueueFree();
  }

  #region Helpers
  private ButtonGame _game() => _room.GetNode<ButtonGame>("ButtonGame");
  private AnimatableBody2D _door() => _room.GetNode<AnimatableBody2D>("SlidingDoor");
  private PlatformSlider _slider() => _room.GetNode<PlatformSlider>("SlidingDoor/Slider");
  private AudioStreamPlayer2D _sound() => _room.GetNode<AudioStreamPlayer2D>("SlidingDoor/Slider/Slide");

  // Plays the room out round by round without waiting any of the melodies: the timer is pumped
  // straight through, and every button is pressed in the order the room is asking for.
  private void _win() {
    var game = _game();
    var timer = game.GetNode<Timer>("StepTimer");
    for (var round = 0; round < 16 && !game.IsWon(); round++) {
      game.StartRound();
      for (var step = 0; step < 32; step++) {
        timer.EmitSignal(Timer.SignalName.Timeout);
      }
      while (game.ExpectedButtonIndex is int expected) {
        foreach (var child in game.GetNode("Buttons").GetChildren()) {
          if (child is GameButton button && button.Index == expected) {
            button.EmitSignal(GameButton.SignalName.ButtonPressed, expected);
            break;
          }
        }
      }
    }
    game.IsWon().ShouldBeTrue("the room could not be played to a win");
  }
  #endregion Helpers
}
