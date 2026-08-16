namespace Wfc.Entities.World.ButtonGame;

using System.Collections.Generic;
using Godot;
using Wfc.Core.Logger;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

// Four buttons the cube steps on, and a melody it has to give back. Every round starts from the
// stand: it blinks until the player is on it, plays the round's melody from there - one button
// lighting up per note - and then listens. Nothing is ever played while the player is somewhere
// they cannot watch it from.
[ScenePath]
public partial class ButtonGame : Node2D, IPersistent {
  #region Constants
  // Each round is a run of button indices, and they lengthen as the room goes on.
  private static readonly int[][] ROUNDS = [
    [0, 2, 1],
    [3, 1, 2, 0],
    [1, 3, 0, 2, 1],
  ];

  // A beat between the player arriving on the stand and the first note, so the melody does not
  // start under the footfall that asked for it.
  private const float LEAD_IN = 0.45f;
  private const float SHOW_LIT = 0.4f;
  private const float SHOW_GAP = 0.14f;
  #endregion Constants

  #region Signals
  // Raised both for the win itself and for a restored room that had already been won, which is
  // what `immediate` separates: a door that opened before the player died is open when they come
  // back, not sliding open again.
  [Signal]
  public delegate void PuzzleWonEventHandler(bool immediate);

  // Raised once the round after the one just cleared is the current one.
  [Signal]
  public delegate void RoundClearedEventHandler();
  #endregion Signals

  // Stopped is the room between rounds as much as before the first one: the stand is blinking and
  // nothing happens until it is stood on.
  private enum GameState {
    Stopped,
    Showing,
    Listening,
    Won
  }

  #region Fields
  private GameState _state = GameState.Stopped;
  private int _currentRound;
  private int _inputIndex;
  // Counts two steps per note: the button lights on the even ones and goes dark on the odd.
  private int _showStep;
  private readonly List<GameButton> _buttons = [];

  private sealed record SaveData(GameState savedState = GameState.Stopped, int savedRound = 0);

  // What a death puts the room back to. Kept up to date as rounds are cleared, so dying does not
  // cost the ones already played.
  private SaveData _respawnData = new SaveData();

  // What a save file gets, which only a checkpoint writes. Rounds are not progress the player has
  // banked - the room has no checkpoint of its own inside it - so leaving the game partway through
  // costs the room, and they play it again from the top.
  private SaveData _checkpointData = new SaveData();
  #endregion Fields

  #region Nodes
  [NodePath("Buttons")]
  private Node2D _buttonsNode = default!;
  [NodePath("MelodyStand")]
  private MelodyStand _standNode = default!;
  [NodePath("StepTimer")]
  private Timer _stepTimerNode = default!;
  #endregion Nodes

  public override void _EnterTree() {
    base._EnterTree();
    this.WireNodes();
    _buttons.Clear();
    foreach (var child in _buttonsNode.GetChildren()) {
      if (child is GameButton button) {
        _buttons.Add(button);
        button.ButtonPressed += _onButtonPressed;
      }
    }
    _stepTimerNode.OneShot = true;
    _stepTimerNode.Timeout += _onStepTimerTimeout;
    _standNode.SteppedOn += StartRound;
    EventHandler.Instance.Events.CheckpointReached += _onCheckpointReached;
    EventHandler.Instance.Events.CheckpointLoaded += Reset;
  }

  public override void _Ready() {
    base._Ready();
    _refreshStand();
  }

  public override void _ExitTree() {
    foreach (var button in _buttons) {
      button.ButtonPressed -= _onButtonPressed;
    }
    _stepTimerNode.Timeout -= _onStepTimerTimeout;
    _standNode.SteppedOn -= StartRound;
    EventHandler.Instance.Events.CheckpointReached -= _onCheckpointReached;
    EventHandler.Instance.Events.CheckpointLoaded -= Reset;
    base._ExitTree();
  }

  public bool IsStopped() => _state == GameState.Stopped;

  public bool IsListening() => _state == GameState.Listening;

  public bool IsWon() => _state == GameState.Won;

  // The button the room is waiting on, for anything that wants to point at it. Null unless the
  // melody has been played and the room is listening, which is the only time there is one.
  public int? ExpectedButtonIndex =>
    _state == GameState.Listening ? ROUNDS[_currentRound][_inputIndex] : null;

  // What the stand asks for. Ignored unless the room is waiting to be asked, so a player who
  // wanders back onto the stand partway through a melody does not restart it.
  public void StartRound() {
    if (_state != GameState.Stopped) {
      return;
    }
    _state = GameState.Showing;
    _inputIndex = 0;
    _showStep = -1;
    // The row goes dark for the whole of the melody, so the one button lit at a time is the only
    // thing in the room with any light in it.
    _setAll(GameButton.Highlight.Dim);
    _refreshStand();
    _stepTimerNode.Start(LEAD_IN);
  }

  #region Rounds
  // Back to the stand, which starts blinking for the round now current. A round is never replayed
  // out from under the player: they ask for it again when they are standing where they can watch.
  private void _awaitStand() {
    _state = GameState.Stopped;
    _inputIndex = 0;
    _stepTimerNode.Stop();
    _setAll(GameButton.Highlight.Rest);
    _refreshStand();
  }

  // A player already on the pad when the room starts waiting would otherwise be blinked at until
  // they stepped off and back on.
  private void _refreshStand() {
    var waiting = _state == GameState.Stopped;
    _standNode.SetBlinking(waiting);
    if (waiting && _standNode.IsOccupied) {
      CallDeferred(nameof(StartRound));
    }
  }

  private void _onStepTimerTimeout() {
    if (_state != GameState.Showing) {
      return;
    }
    _showStep += 1;
    var sequence = ROUNDS[_currentRound];
    if (_showStep >= sequence.Length * 2) {
      _state = GameState.Listening;
      _setAll(GameButton.Highlight.Rest);
      return;
    }
    var button = _buttonOf(sequence[_showStep / 2]);
    if (button == null) {
      return;
    }
    var isLighting = _showStep % 2 == 0;
    button.SetHighlight(isLighting ? GameButton.Highlight.Lit : GameButton.Highlight.Dim);
    if (isLighting) {
      EventHandler.Instance.EmitButtonGameNotePlayed(button.NoteIndex);
    }
    _stepTimerNode.Start(isLighting ? SHOW_LIT : SHOW_GAP);
  }

  // The button sounds whatever the room is doing - a player wandering across a won room, or one
  // waiting at the stand, still gets a note out of it. What the press means to the round is what
  // the room's state decides.
  private void _onButtonPressed(int buttonIndex) {
    var button = _buttonOf(buttonIndex);
    if (button != null) {
      EventHandler.Instance.EmitButtonGameNotePlayed(button.NoteIndex);
    }

    // The melody is to be watched, not played along with: a button touched while it is still
    // being shown loses the round, so the player cannot feel their way through a sequence they
    // never actually listened to.
    if (_state == GameState.Showing) {
      _failRound();
      return;
    }

    if (_state != GameState.Listening) {
      return;
    }

    if (buttonIndex != ROUNDS[_currentRound][_inputIndex]) {
      _failRound();
      return;
    }

    _inputIndex += 1;
    if (_inputIndex < ROUNDS[_currentRound].Length) {
      return;
    }
    _currentRound += 1;
    if (_currentRound >= ROUNDS.Length) {
      _win();
    }
    else {
      _bankForRespawn();
      EmitSignal(SignalName.RoundCleared);
      _awaitStand();
    }
  }

  private void _failRound() {
    EventHandler.Instance.EmitButtonGameWrongNotePlayed();
    _awaitStand();
  }

  private void _win() {
    _state = GameState.Won;
    _currentRound = ROUNDS.Length - 1;
    _bankForRespawn();
    _stepTimerNode.Stop();
    _setAll(GameButton.Highlight.Rest);
    _refreshStand();
    EventHandler.Instance.EmitButtonGameWon();
    EmitSignal(SignalName.PuzzleWon, false);
  }

  private void _setAll(GameButton.Highlight highlight) {
    foreach (var button in _buttons) {
      button.SetHighlight(highlight);
    }
  }

  private GameButton? _buttonOf(int index) {
    foreach (var button in _buttons) {
      if (button.Index == index) {
        return button;
      }
    }
    Log.Error($"{Name} has no button with index {index}.");
    return null;
  }
  #endregion Rounds

  #region Checkpoints
  // A run that was under way comes back as one waiting to be asked for again: the round the
  // player had reached is kept, and its melody is theirs to call up from the stand rather than
  // something they are expected to have remembered across a death.
  private GameState _stateToSave() => _state switch {
    GameState.Showing or GameState.Listening => GameState.Stopped,
    _ => _state,
  };

  // Called as the room is played, so a respawn lands on the round reached. Deliberately not a
  // checkpoint: raising one here would write the round to disk, and the player has not passed
  // anything that ought to hold it if they put the game down.
  private void _bankForRespawn() => _respawnData = new SaveData(_stateToSave(), _currentRound);

  // A real checkpoint - the room's own, at the way in - is the only thing that commits the room to
  // a save file.
  private void _onCheckpointReached(Vector2 _position, string _colorGroup) {
    _bankForRespawn();
    _checkpointData = _respawnData;
  }

  public void Reset() {
    _stepTimerNode.Stop();
    _setAll(GameButton.Highlight.Rest);
    _currentRound = Mathf.Clamp(_respawnData.savedRound, 0, ROUNDS.Length - 1);
    _state = _respawnData.savedState;
    _inputIndex = 0;
    _refreshStand();
    if (_state == GameState.Won) {
      EmitSignal(SignalName.PuzzleWon, true);
    }
  }

  public string GetSaveId() => GetPath();
  public string Save(ISerializer serializer) => serializer.Serialize(_checkpointData);
  public void Load(ISerializer serializer, string data) {
    _checkpointData = serializer.Deserialize<SaveData>(data) ?? new SaveData();
    _respawnData = _checkpointData;
    Reset();
  }
  #endregion Checkpoints
}
