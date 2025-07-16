namespace Wfc.Entities.World.Piano;

using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using Wfc.Core.Event;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

[ScenePath]
public partial class Piano : Node2D {

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
    EventHandler.Instance.Events.CheckpointLoaded += Reset;
    _solfegeBoardNode.WrongNotePlayed += _onSolfegeBoardWrongNotePlayed;
    _solfegeBoardNode.ExpectedNoteChanged += _onSolfegeBoardExpectedNoteChanged;
    _solfegeBoardNode.BoardNotesPlayed += _onSolfegeBoardNotesPlayed;
    foreach (var note in _pianoNotesNodes) {
      note.OnNotePressed += _onPianoNotePressed;
      note.OnNoteReleased += _onPianoNoteReleased;
    }
  }

  public override void _ExitTree() {
    EventHandler.Instance.Events.CheckpointLoaded -= Reset;
    _solfegeBoardNode.WrongNotePlayed -= _onSolfegeBoardWrongNotePlayed;
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
    EventHandler.Instance.EmitPianoNotePressed(noteIndex);
  }

  private void _onPianoNoteReleased(int noteIndex) {
    EventHandler.Instance.EmitPianoNoteReleased(noteIndex);
  }

  private void _onSolfegeBoardNotesPlayed() {
    EventHandler.Instance.EmitPianoPuzzleWon();
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
    return node as NextNotePointer;
  }

  private void _onSolfegeBoardExpectedNoteChanged(int newExpectedNote) {
    _updateNotesPointerPosition(MusicNoteHelper.MusicNoteFromInt(newExpectedNote));
  }

  private void _updateNotesPointerPosition(MusicNote? newExpectedNote) {
    if (_notesPointerNode != null) {
      var note = _getNoteNode(newExpectedNote);
      if (note != null) {
        _notesPointerNode.Position = new Vector2(note.Position.X, 0);
      }
    }
  }

  private void _onSolfegeBoardWrongNotePlayed() {
    // Replace with function body.
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

  public void Reset() {
    _removePointerNode();
  }

  private void _setupNotePointerPosition() {
    _updateNotesPointerPosition(_solfegeBoardNode.GetExpectedNote());
  }

  public bool IsStopped() {
    return _solfegeBoardNode.IsStopped();
  }
}
