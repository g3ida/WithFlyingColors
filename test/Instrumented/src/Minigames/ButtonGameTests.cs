namespace Wfc.test.instrumented.Minigames;

using System.Collections.Generic;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Serialization;
using Wfc.Entities.World.ButtonGame;
using Wfc.Utils;
using Wfc.Utils.Colors;
using EventHandler = Wfc.Core.Event.EventHandler;

// The room plays a melody and then listens for it back. Everything it does that the player can
// see - a button lighting up, a note sounding, a door opening - is downstream of a round counter
// and an input cursor that nothing on screen spells out, and a death in the middle of it has to
// leave the player on the round they had reached rather than back at the first.
public class ButtonGameTests(Node testScene) : TestClass(testScene) {
  private ButtonGame _game = default!;

  [Setup]
  public async Task Setup() {
    _game = await _room();
  }

  [Cleanup]
  public void Cleanup() => _discard(_game);

  [Test]
  public void TheRoomIsIdleUntilTheStandIsSteppedOn() {
    _game.IsStopped().ShouldBeTrue();
    _game.ExpectedButtonIndex.ShouldBeNull("an idle room is not waiting on anything");
    _stand().IsOccupied.ShouldBeFalse();
  }

  // The stand is the only way in. Walking into the room is not enough, and neither is walking
  // past the buttons: the melody is always played to a player standing where they can watch it.
  [Test]
  public void SteppingOnToTheStandStartsTheRound() {
    _stepOntoStand();

    _game.IsStopped().ShouldBeFalse("stepping onto the stand should have started the round");
  }

  [Test]
  public void SomethingThatIsNotThePlayerDoesNotStartTheRound() {
    var body = new RigidBody2D();
    _stand().GetNode<Area2D>("DetectionArea").EmitSignal(Area2D.SignalName.BodyEntered, body);
    body.QueueFree();

    _game.IsStopped().ShouldBeTrue();
  }

  // It blinks for exactly as long as it is worth stepping on, so a room in the middle of a melody
  // or already won is not still inviting the player back onto the pad.
  [Test]
  public void TheStandBlinksOnlyWhileTheRoomIsWaitingToBeAsked() {
    _stand().IsBlinking.ShouldBeTrue("an idle room should be inviting the player onto the stand");

    _stepOntoStand();
    _stand().IsBlinking.ShouldBeFalse("the stand should go quiet once the melody is playing");

    _playThrough();
    _stand().IsBlinking.ShouldBeFalse("a won room should not still be asking to be played");
  }

  // The melody is the instruction, so a room that took input while it was still playing would be
  // answerable before it had asked.
  [Test]
  public void TheRoomListensOnlyOnceItHasPlayedTheMelody() {
    _game.StartRound();

    _game.IsListening().ShouldBeFalse();
    _game.ExpectedButtonIndex.ShouldBeNull();

    _playOutMelody();

    _game.IsListening().ShouldBeTrue();
    _game.ExpectedButtonIndex.ShouldNotBeNull();
  }

  // The melody is the only instruction the player gets, so a note has to be unmissable: the row
  // drops dark and the one button being sounded is the only lit thing in the room. A note shown
  // as one shade of its own colour against three others in theirs is what this replaced, and it
  // was not visible at all.
  [Test]
  public void OnlyTheButtonBeingSoundedIsLitAndTheRestGoDark() {
    _game.StartRound();
    var timer = _game.GetNode<Timer>("StepTimer");

    timer.EmitSignal(Timer.SignalName.Timeout);

    _highlights(GameButton.Highlight.Lit).ShouldBe(1, "exactly one button carries the note");
    _highlights(GameButton.Highlight.Dim).ShouldBe(_buttons().Count - 1, "the rest of the row should be dark");
  }

  // And the room hands the buttons back at full colour to be played, so the dark is read as the
  // room speaking rather than as the buttons being switched off.
  [Test]
  public void TheRowComesBackToFullColourToBePlayed() {
    _game.StartRound();
    _playOutMelody();

    _game.IsListening().ShouldBeTrue();
    _highlights(GameButton.Highlight.Rest).ShouldBe(_buttons().Count);
  }

  // Wrong, and the room goes back to the stand rather than replaying the melody at a player who
  // is standing over on the buttons with their back to it.
  [Test]
  public void AWrongButtonSendsThePlayerBackToTheStandForTheSameRound() {
    var cleared = 0;
    _game.RoundCleared += () => cleared += 1;
    _game.StartRound();
    _playOutMelody();
    var expected = _game.ExpectedButtonIndex!.Value;

    _press(_anyButtonOtherThan(expected));

    _game.IsListening().ShouldBeFalse("a wrong button should stop the room listening");
    _game.IsStopped().ShouldBeTrue("the room should be waiting on the stand again");
    _game.IsWon().ShouldBeFalse();
    cleared.ShouldBe(0);

    _game.StartRound();
    _playOutMelody();
    _game.ExpectedButtonIndex.ShouldBe(expected, "the round replayed should be the one that was missed");
  }

  // Playing along with the melody instead of watching it loses the round outright. Otherwise the
  // sequence could be felt out a button at a time while it was still being shown, which is the
  // one thing the room is asking the player not to do.
  [Test]
  public void TouchingAButtonDuringTheMelodyLosesTheRound() {
    _game.StartRound();
    var timer = _game.GetNode<Timer>("StepTimer");
    timer.EmitSignal(Timer.SignalName.Timeout);
    var lit = _buttons().Find(button => button.CurrentHighlight == GameButton.Highlight.Lit)!;

    _press(lit.Index);

    _game.IsStopped().ShouldBeTrue("the round should have been lost back to the stand");
    _game.IsListening().ShouldBeFalse();
    _highlights(GameButton.Highlight.Rest).ShouldBe(_buttons().Count, "the row was left mid-melody");
  }

  // Even the button that was about to be the right answer: while the melody is playing there is
  // no right answer to give yet.
  [Test]
  public void TheMelodyStopsWhenItIsInterrupted() {
    _game.StartRound();
    var timer = _game.GetNode<Timer>("StepTimer");
    timer.EmitSignal(Timer.SignalName.Timeout);

    _press(_buttons()[0].Index);
    _playOutMelody();

    _game.IsListening().ShouldBeFalse("the interrupted melody carried on playing to its end");
    _game.IsStopped().ShouldBeTrue();
  }

  // But not once the room is done asking: noodling on the buttons while the stand blinks, or in a
  // room already won, is just a noise the player is allowed to make.
  [Test]
  public void TouchingAButtonWhileTheStandIsBlinkingCostsNothing() {
    _press(_buttons()[0].Index);

    _game.IsStopped().ShouldBeTrue();
    _stand().IsBlinking.ShouldBeTrue();
  }

  [Test]
  public void ClearingEveryRoundWinsTheRoom() {
    var cleared = 0;
    var won = 0;
    var wonImmediately = true;
    _game.RoundCleared += () => cleared += 1;
    _game.PuzzleWon += immediate => {
      won += 1;
      wonImmediately = immediate;
    };

    var rounds = _playThrough();

    _game.IsWon().ShouldBeTrue();
    won.ShouldBe(1);
    wonImmediately.ShouldBeFalse("winning it here and now is not a room being restored");
    cleared.ShouldBe(rounds.Count - 1, "every round but the last reports itself cleared");
    _game.ExpectedButtonIndex.ShouldBeNull("a won room is not waiting on anything");
  }

  // Rounds lengthen, which is the whole shape of the puzzle: a room whose rounds were all the same
  // length would be three repetitions rather than three steps.
  [Test]
  public void EachRoundIsLongerThanTheOneBefore() {
    var rounds = _playThrough();

    rounds.Count.ShouldBeGreaterThan(1);
    for (var i = 1; i < rounds.Count; i++) {
      rounds[i].Count.ShouldBeGreaterThan(rounds[i - 1].Count);
    }
  }

  // Dying is not meant to cost the rounds already cleared - only the one in hand, whose melody is
  // played again rather than expected to be remembered across the respawn.
  [Test]
  public void ARespawnComesBackOnTheRoundThePlayerHadReached() {
    _game.StartRound();
    _playOutMelody();
    var firstRound = _playRound();
    _game.StartRound();
    _playOutMelody();
    var secondRound = _game.ExpectedButtonIndex;

    EventHandler.Instance.EmitCheckpointLoaded();

    _game.IsStopped().ShouldBeTrue("a respawn puts the player back on the stand, not mid-melody");
    _game.StartRound();
    _playOutMelody();
    _game.ExpectedButtonIndex.ShouldBe(secondRound, "the respawn dropped the player back to an earlier round");
    _playRound().Count.ShouldBeGreaterThan(firstRound.Count, "the round restored was not the one reached");
  }

  // A room reached before it was ever started is a room the player has not played, and a respawn
  // has to leave it that way rather than starting it for them.
  [Test]
  public void ARespawnBeforeTheRoomWasStartedLeavesItIdle() {
    EventHandler.Instance.EmitCheckpointReached(Vector2.Zero, ColorUtils.PURPLE);

    EventHandler.Instance.EmitCheckpointLoaded();

    _game.IsStopped().ShouldBeTrue();
  }

  [Test]
  public void AWonRoomIsStillWonAfterARespawn() {
    _playThrough();
    EventHandler.Instance.EmitCheckpointReached(Vector2.Zero, ColorUtils.PURPLE);
    var won = 0;
    var wonImmediately = false;
    _game.PuzzleWon += immediate => {
      won += 1;
      wonImmediately = immediate;
    };

    EventHandler.Instance.EmitCheckpointLoaded();

    _game.IsWon().ShouldBeTrue();
    won.ShouldBe(1);
    wonImmediately.ShouldBeTrue("a room that was already won does not open its door a second time");
  }

  // A save file is written when a checkpoint is passed, and the room has none inside it. Rounds
  // are worth keeping across a death but not worth banking: a player who puts the game down
  // partway through the room comes back to play it from the top.
  [Test]
  public async Task LeavingTheGamePartwayThroughLosesTheRoundsPlayed() {
    _game.StartRound();
    _playOutMelody();
    _playRound();
    var serializer = new SimpleJsonSerializer();
    var saved = _game.Save(serializer);

    var reloaded = await _room();
    reloaded.Load(serializer, saved);

    reloaded.IsStopped().ShouldBeTrue();
    reloaded.StartRound();
    _playOutMelodyOf(reloaded);
    var expected = reloaded.ExpectedButtonIndex;
    _discard(reloaded);

    var fresh = await _room();
    fresh.StartRound();
    _playOutMelodyOf(fresh);
    expected.ShouldBe(fresh.ExpectedButtonIndex, "the reloaded room did not start from the first round");
    _discard(fresh);
  }

  // The same room left after a checkpoint keeps what the checkpoint saw.
  [Test]
  public async Task AWonRoomIsStillWonAfterASaveAndLoad() {
    _playThrough();
    EventHandler.Instance.EmitCheckpointReached(Vector2.Zero, ColorUtils.PURPLE);
    var serializer = new SimpleJsonSerializer();
    var saved = _game.Save(serializer);

    var reloaded = await _room();
    reloaded.Load(serializer, saved);

    reloaded.IsWon().ShouldBeTrue();
    _discard(reloaded);
  }

  [Test]
  public async Task ARoomWithNoSavedDataOfItsOwnLoadsIdle() {
    var reloaded = await _room();

    reloaded.Load(new SimpleJsonSerializer(), "null");

    reloaded.IsStopped().ShouldBeTrue();
    _discard(reloaded);
  }

  #region Helpers
  private async Task<ButtonGame> _room() {
    var game = SceneHelpers.InstantiateNode<ButtonGame>();
    TestScene.AddChild(game);
    await _idle();
    return game;
  }

  // Removed before it is freed so that its subscriptions to the global events come off now rather
  // than at the end of the frame, where the next test's room is already listening alongside it.
  private void _discard(ButtonGame game) {
    TestScene.RemoveChild(game);
    game.QueueFree();
  }

  // Runs the demonstration out without waiting it out. The room advances one step per timeout, and
  // the ones left over after it has finished playing are ignored.
  private void _playOutMelody() => _playOutMelodyOf(_game);

  private static void _playOutMelodyOf(ButtonGame game) {
    var timer = game.GetNode<Timer>("StepTimer");
    for (var i = 0; i < 32; i++) {
      timer.EmitSignal(Timer.SignalName.Timeout);
    }
  }

  private List<int> _playRound() {
    var pressed = new List<int>();
    while (_game.ExpectedButtonIndex is int expected) {
      pressed.Add(expected);
      _press(expected);
    }
    return pressed;
  }

  private List<List<int>> _playThrough() {
    var rounds = new List<List<int>>();
    while (!_game.IsWon() && rounds.Count < 16) {
      _game.StartRound();
      _playOutMelody();
      rounds.Add(_playRound());
    }
    return rounds;
  }

  private MelodyStand _stand() => _game.GetNode<MelodyStand>("MelodyStand");

  private void _stepOntoStand() {
    var player = new Wfc.Entities.World.Player.Player();
    _stand().GetNode<Area2D>("DetectionArea").EmitSignal(Area2D.SignalName.BodyEntered, player);
    player.QueueFree();
  }

  private List<GameButton> _buttons() {
    var buttons = new List<GameButton>();
    foreach (var child in _game.GetNode("Buttons").GetChildren()) {
      if (child is GameButton button) {
        buttons.Add(button);
      }
    }
    return buttons;
  }

  private int _highlights(GameButton.Highlight highlight) =>
    _buttons().FindAll(button => button.CurrentHighlight == highlight).Count;

  private void _press(int index) {
    foreach (var child in _game.GetNode("Buttons").GetChildren()) {
      if (child is GameButton button && button.Index == index) {
        button.EmitSignal(GameButton.SignalName.ButtonPressed, index);
        return;
      }
    }
    throw new KeyNotFoundException($"The room has no button with index {index}");
  }

  private int _anyButtonOtherThan(int index) {
    foreach (var child in _game.GetNode("Buttons").GetChildren()) {
      if (child is GameButton button && button.Index != index) {
        return button.Index;
      }
    }
    throw new KeyNotFoundException("The room has only one button");
  }

  private async Task _idle() {
    var tree = TestScene.GetTree();
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
  }
  #endregion Helpers
}
