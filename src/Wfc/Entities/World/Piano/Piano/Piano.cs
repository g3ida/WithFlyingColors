namespace Wfc.Entities.World.Piano;

using Chickensoft.Sync.Primitives;
using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using Wfc.Core.Event;
using Wfc.Screens.Levels;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class Piano : Node2D {
  private AutoChannel.Binding? _checkpointBinding;


  [NodePath("NotesContainer/PianoNote")]
  private PianoNote _pianoNote1 = null!;
  [NodePath("NotesContainer/PianoNote2")]
  private PianoNote _pianoNote2 = null!;
  [NodePath("NotesContainer/PianoNote3")]
  private PianoNote _pianoNote3 = null!;
  [NodePath("NotesContainer/PianoNote4")]
  private PianoNote _pianoNote4 = null!;
  [NodePath("NotesContainer/PianoNote5")]
  private PianoNote _pianoNote5 = null!;
  [NodePath("NotesContainer/PianoNote6")]
  private PianoNote _pianoNote6 = null!;
  [NodePath("NotesContainer/PianoNote7")]
  private PianoNote _pianoNote7 = null!;

  private Array<PianoNote> _pianoNotesNodes = [];

  [NodePath("SolfegeBoard")]
  private SolfegeBoard _solfegeBoardNode = null!;
  [NodePath("LettersContainer")]
  private Node _lettersContainerNode = null!;
  private NextNotePointer? _notesPointerNode = null;

  public override void _EnterTree() {
    base._EnterTree();
    this.WireNodes();
    _pianoNotesNodes = [
      _pianoNote1,
      _pianoNote2,
      _pianoNote3,
      _pianoNote4,
      _pianoNote5,
      _pianoNote6,
      _pianoNote7
    ];
    _checkpointBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.CheckpointLoaded _) => Reset());
    _solfegeBoardNode.ExpectedNoteChanged += _onSolfegeBoardExpectedNoteChanged;
    _solfegeBoardNode.BoardNotesPlayed += _onSolfegeBoardNotesPlayed;
    foreach (var note in _pianoNotesNodes) {
      note.OnNotePressed += _onPianoNotePressed;
      note.OnNoteReleased += _onPianoNoteReleased;
    }
  }

  public override void _ExitTree() {
    _checkpointBinding?.Dispose();
    _checkpointBinding = null;
    _solfegeBoardNode.ExpectedNoteChanged -= _onSolfegeBoardExpectedNoteChanged;
    _solfegeBoardNode.BoardNotesPlayed -= _onSolfegeBoardNotesPlayed;
    foreach (var note in _pianoNotesNodes) {
      note.OnNotePressed -= _onPianoNotePressed;
      note.OnNoteReleased -= _onPianoNoteReleased;
    }
    base._ExitTree();
  }

  public override void _Ready() {
    base._Ready();
  }

  private void _onPianoNotePressed(int noteIndex) {
    GameEvents.Instance.OnPianoNotePressed(noteIndex);
  }

  private void _onPianoNoteReleased(int noteIndex) {
    GameEvents.Instance.OnPianoNoteReleased(noteIndex);
  }

  private void _onSolfegeBoardNotesPlayed() {
    GameEvents.Instance.OnPianoPuzzleWon();
    _removePointerNode();
  }

  private void _removePointerNode() {
    if (_notesPointerNode != null) {
      _notesPointerNode.QueueFree();
      _notesPointerNode = null;
    }
  }

  public void StartGame() {
    if (_notesPointerNode != null && _notesPointerNode.IsInsideTree()) {
      _notesPointerNode.QueueFree();
    }
    _notesPointerNode = _instanceNotesPointer();
    _solfegeBoardNode.StartGame();
    MusicNote? expectedNote = _solfegeBoardNode.GetExpectedNote();
    _updateNotesPointerPosition(expectedNote);
  }

  private NextNotePointer _instanceNotesPointer() {
    var node = SceneHelpers.InstantiateNode<NextNotePointer>();
    _lettersContainerNode.AddChild(node);
    node.Owner = _lettersContainerNode;
    return node;
  }

  private void _onSolfegeBoardExpectedNoteChanged(int newExpectedNote) {
    _updateNotesPointerPosition(MusicNoteHelper.MusicNoteFromInt(newExpectedNote));
  }

  // Builds the pointer if it is missing rather than only repositioning an existing one.
  //
  // The board only reports an expected note while the puzzle is live, so being asked for one
  // is itself the signal that a pointer is wanted. That is what makes loading a checkpoint
  // mid-puzzle survivable: _EnterTree propagates parent-first, so Reset below runs before the
  // board's and cannot see the state it is about to restore - it would drop the pointer, the
  // board would come back Playing, and nothing would ever build another, since StartGame is
  // the only other construction site and it is gated on the board being stopped.
  private void _updateNotesPointerPosition(MusicNote? newExpectedNote) {
    var note = _getNoteNode(newExpectedNote);
    if (note == null) {
      return;
    }
    _notesPointerNode ??= _instanceNotesPointer();
    _notesPointerNode.Position = new Vector2(note.Position.X, 0);
  }

  private PianoNote? _getNoteNode(MusicNote? newExpectedNote) {
    if (newExpectedNote != null) {
      var index = (int)newExpectedNote;
      foreach (PianoNote note in _pianoNotesNodes) {
        if (note.Index == index) {
          return note;
        }
      }
    }
    return null;
  }

  // Drops the pointer belonging to the run that just ended. The board rebuilds it on the way
  // back up, through ExpectedNoteChanged - see _updateNotesPointerPosition.
  public void Reset() {
    _removePointerNode();
  }

  public bool IsStopped() {
    return _solfegeBoardNode.IsStopped();
  }
}
