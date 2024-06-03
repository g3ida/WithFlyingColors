using Godot;
using System;
using System.Collections.Generic;

public class Piano : Node2D
{
    private PackedScene NotesPointerScene = (PackedScene)ResourceLoader.Load("res://Assets/Scenes/Piano/NextNotePointer.tscn");
    private List<string> notes = new List<string> { "do", "re", "mi", "fa", "sol", "la", "si" };

    private Godot.Collections.Array PianoNotesNodes;
    private SolfegeBoard SolfegeBoardNode;
    private Node lettersContainerNode;

    private NextNotePointer NotesPointerNode = null;

    public override void _Ready()
    {
        PianoNotesNodes = GetNode("NotesContainer").GetChildren();
        SolfegeBoardNode = GetNode<SolfegeBoard>("SolfegeBoard");
        lettersContainerNode = GetNode("LettersContainer");
    }

    private void _on_piano_note_pressed(PianoNote note)
    {
        int index = note.index - 1;
        Event.Instance().EmitPianoNotePressed(notes[index]);
    }

    private void _on_piano_note_released(PianoNote note)
    {
        int index = note.index - 1;
        Event.Instance().EmitPianoNoteReleased(notes[index]);
    }

    private void _on_SolfegeBoard_board_notes_played()
    {
        Event.Instance().EmitPianoPuzzleWon();
        _RemovePointerNode();
    }

    private void _RemovePointerNode()
    {
        if (NotesPointerNode != null)
        {
            NotesPointerNode.QueueFree();
            NotesPointerNode = null;
        }
    }

    public void StartGame()
    {
        if (NotesPointerNode != null && NotesPointerNode.IsInsideTree())
        {
            NotesPointerNode.QueueFree();
        }
        NotesPointerNode = _InstanceNotesPointer();
        SolfegeBoardNode.StartGame();
        string expectedNote = SolfegeBoardNode.GetExpectedNote();
        _UpdateNotesPointerPosition(expectedNote);
    }

    private NextNotePointer _InstanceNotesPointer()
    {
        var node = NotesPointerScene.Instance();
        lettersContainerNode.AddChild(node);
        node.Owner = lettersContainerNode;
        return node as NextNotePointer;
    }

    private void _on_SolfegeBoard_expected_note_changed(string newExpectedNote)
    {
        _UpdateNotesPointerPosition(newExpectedNote);
    }

    private void _UpdateNotesPointerPosition(string newExpectedNote)
    {
        if (NotesPointerNode != null)
        {
            var note = _GetNoteNode(newExpectedNote);
            if (note != null)
            {
                NotesPointerNode.Position = new Vector2(note.Position.x, 0);
            }
        }
    }

    private void _on_SolfegeBoard_wrong_note_played()
    {
        // Replace with function body.
    }

    private PianoNote _GetNoteNode(string newExpectedNote)
    {
        int idx = notes.IndexOf(newExpectedNote);
        if (idx != -1)
        {
            foreach (PianoNote note in PianoNotesNodes)
            {
                if (note.index == idx + 1)
                {
                    return note;
                }
            }
        }
        return null;
    }

    public void Reset()
    {
        _RemovePointerNode();
    }

    private void _SetupNotePointerPosition()
    {
        _UpdateNotesPointerPosition(SolfegeBoardNode.GetExpectedNote());
    }


    // FIXME logic after migration should change
    public override void _EnterTree()
    {
        Event.Instance().Connect("checkpoint_loaded", this, "Reset");
    }

    public override void _ExitTree()
    {
        Event.Instance().Disconnect("checkpoint_loaded", this, "Reset");
    }

    public bool IsStopped()
    {
        return SolfegeBoardNode.IsStopped();
    }
}
