using Godot;
using System;
using System.Collections.Generic;
using Wfc.Core.Event;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class Piano : Node2D {
  private PackedScene NotesPointerScene = (PackedScene)ResourceLoader.Load("res://Assets/Scenes/Piano/NextNotePointer.tscn");
  private List<string> notes = new List<string> { "do", "re", "mi", "fa", "sol", "la", "si" };

  private Godot.Collections.Array<Node> PianoNotesNodes;
  private SolfegeBoard SolfegeBoardNode;
  private Node lettersContainerNode;

  private NextNotePointer NotesPointerNode = null;

  public override void _Ready() {
    PianoNotesNodes = GetNode("NotesContainer").GetChildren();
    SolfegeBoardNode = GetNode<SolfegeBoard>("SolfegeBoard");
    lettersContainerNode = GetNode("LettersContainer");
  }

  private void _on_piano_note_pressed(PianoNote note) {
    int index = note.index - 1;
    EventHandler.Instance.EmitPianoNotePressed(notes[index]);
  }

  private void _on_piano_note_released(PianoNote note) {
    int index = note.index - 1;
    EventHandler.Instance.EmitPianoNoteReleased(notes[index]);
  }

  private void _on_SolfegeBoard_board_notes_played() {
    EventHandler.Instance.EmitPianoPuzzleWon();
    _RemovePointerNode();
  }

  private void _RemovePointerNode() {
    if (NotesPointerNode != null) {
      NotesPointerNode.QueueFree();
      NotesPointerNode = null;
    }
  }

  public void StartGame() {
    if (NotesPointerNode != null && NotesPointerNode.IsInsideTree()) {
      NotesPointerNode.QueueFree();
    }
    NotesPointerNode = _InstanceNotesPointer();
    SolfegeBoardNode.StartGame();
    string expectedNote = SolfegeBoardNode.GetExpectedNote();
    _UpdateNotesPointerPosition(expectedNote);
  }

  private NextNotePointer _InstanceNotesPointer() {
    var node = NotesPointerScene.Instantiate();
    lettersContainerNode.AddChild(node);
    node.Owner = lettersContainerNode;
    return node as NextNotePointer;
  }

  private void _on_SolfegeBoard_expected_note_changed(string newExpectedNote) {
    _UpdateNotesPointerPosition(newExpectedNote);
  }

  private void _UpdateNotesPointerPosition(string newExpectedNote) {
    if (NotesPointerNode != null) {
      var note = _GetNoteNode(newExpectedNote);
      if (note != null) {
        NotesPointerNode.Position = new Vector2(note.Position.X, 0);
      }
    }
  }

  private void _on_SolfegeBoard_wrong_note_played() {
    // Replace with function body.
  }

  private PianoNote _GetNoteNode(string newExpectedNote) {
    int idx = notes.IndexOf(newExpectedNote);
    if (idx != -1) {
      foreach (PianoNote note in PianoNotesNodes) {
        if (note.index == idx + 1) {
          return note;
        }
      }
    }
    return null;
  }

  public void Reset() {
    _RemovePointerNode();
  }

  private void _SetupNotePointerPosition() {
    _UpdateNotesPointerPosition(SolfegeBoardNode.GetExpectedNote());
  }


  // FIXME logic after migration should change
  public override void _EnterTree() {
    EventHandler.Instance.Connect(EventType.CheckpointLoaded, new Callable(this, "Reset"));
  }

  public override void _ExitTree() {
    EventHandler.Instance.Disconnect(EventType.CheckpointLoaded, new Callable(this, "Reset"));
  }

  public bool IsStopped() {
    return SolfegeBoardNode.IsStopped();
  }
}
