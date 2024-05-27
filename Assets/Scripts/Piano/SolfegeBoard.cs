using System.Collections.Generic;
using System.Diagnostics;
using Godot;

public class SolfegeBoard : Node2D
{
    private const float DURATION = 0.8f;
    private PackedScene NotesCursorScene = GD.Load<PackedScene>("res://Assets/Scenes/Piano/NotesCursor.tscn");
    private Texture MusicPaperRectTexture = GD.Load<Texture>("res://Assets/Sprites/Piano/music-paper-rect.png");

    [Signal]
    public delegate void board_notes_played();

    [Signal]
    public delegate void expected_note_changed(string newExpectedNote);

    [Signal]
    public delegate void wrong_note_played();

    Sprite MusicPaperRectNode;


    private readonly string[][] PAGES = new string[][] {
        new string[] { "do", "do", "sol", "sol", "la", "la", "sol" },
        new string[] { "sol", "sol", "mi", "mi", "re", "re", "do" },
        new string[] { "sol", "sol", "fa", "fa", "mi", "mi", "re" },
        new string[] { "sol", "sol", "fa", "fa", "mi", "mi", "re" },
        new string[] { "do", "do", "sol", "sol", "la", "la", "sol" },
        new string[] { "sol", "sol", "mi", "mi", "re", "re", "do" }
    };

    private int NUM_PAGES;

    private enum BoardState
    {
        STOPPED,
        PLAYING,
        FINISHED
    }

    private Texture currentTexture;
    private BoardState currentState = BoardState.STOPPED;
    private bool isFlipping = false;
    private int currentPage = 0;
    private int currentNoteIndex = 0;
    private NotesCursor notesCursor = null;
    private float time = 0;

    private Dictionary<string, BoardState> saveData = new Dictionary<string, BoardState>
    {
        { "current_state", BoardState.STOPPED }
    };

    private SolfegeNotesTextureGenerator solfegeNotesTextureGenerator;

    public override void _Ready()
    {
        MusicPaperRectNode = GetNode<Sprite>("MusicPaperRect");
        NUM_PAGES = PAGES.Length;
        solfegeNotesTextureGenerator = new SolfegeNotesTextureGenerator();
        _InitShader();
        _InitState();
    }

    public void StartGame()
    {
        currentState = BoardState.PLAYING;
        _InitState();
    }

    public void FlipNextPage()
    {
        currentPage += 1;
        if (currentPage >= NUM_PAGES)
        {
            currentState = BoardState.FINISHED;
            EmitSignal(nameof(board_notes_played));
            _SetFlipPageShader(MusicPaperRectTexture);
            _InitState();
        }
        else
        {
            var solfegeTexture = solfegeNotesTextureGenerator.CreateFromNotes(PAGES[currentPage], MusicPaperRectNode.GetRect().Size);
            _SetFlipPageShader(solfegeTexture);
        }
    }

    private void _SetFlipPageShader(Texture nextTexture)
    {
        var paperShaderMaterial = (MusicPaperRectNode.Material as ShaderMaterial);
        paperShaderMaterial.SetShaderParam("flip_left", true);
        paperShaderMaterial.SetShaderParam("cylinder_direction", new Vector2(5.0f, 1.0f));
        paperShaderMaterial.SetShaderParam("current_page", currentTexture);
        paperShaderMaterial.SetShaderParam("next_page", nextTexture);
        time = 0.0f;
        isFlipping = true;
        Event.Instance().EmitPageFlipped();
    }

    private void _InitShader()
    {
        var paperShaderMaterial = (MusicPaperRectNode.Material as ShaderMaterial);
        paperShaderMaterial.SetShaderParam("time", 0);
        paperShaderMaterial.SetShaderParam("flip_duration", DURATION);
        paperShaderMaterial.SetShaderParam("cylinder_ratio", 0.3f);
        paperShaderMaterial.SetShaderParam("rect", MusicPaperRectNode.GetRect().Size);
    }

    public override void _Process(float delta)
    {
        if (isFlipping)
        {
            time += delta;
            (MusicPaperRectNode.Material as ShaderMaterial).SetShaderParam("time", time);
            if (time > DURATION)
            {
                isFlipping = false;
                MusicPaperRectNode.Texture = currentTexture;
            }
        }
    }

    public override void _EnterTree()
    {
        Event.GdInstance().Connect("piano_note_pressed", this, "_OnNotePressed");
        Event.GdInstance().Connect("checkpoint_loaded", this, "Reset");
        Event.GdInstance().Connect("checkpoint_reached", this, "_OnCheckpointHit");
    }

    public override void _ExitTree()
    {
        Event.GdInstance().Disconnect("piano_note_pressed", this, "_OnNotePressed");
        Event.GdInstance().Disconnect("checkpoint_loaded", this, "Reset");
        Event.GdInstance().Disconnect("checkpoint_reached", this, "_OnCheckpointHit");
    }

    private void _InitState()
    {
        if (currentState == BoardState.PLAYING)
        {
            currentNoteIndex = 0;
            currentPage = 0;
            MusicPaperRectNode.Visible = true;
            currentTexture = MusicPaperRectNode.Texture;
            var resetTexture = solfegeNotesTextureGenerator.CreateFromNotes(PAGES[currentPage], MusicPaperRectNode.GetRect().Size);
            _SetFlipPageShader(resetTexture);
            (MusicPaperRectNode.Material as ShaderMaterial).SetShaderParam("current_page", currentTexture);
            if (notesCursor != null)
            {
                notesCursor.QueueFree();
                notesCursor = null;
            }
            notesCursor = (NotesCursor)NotesCursorScene.Instance();
            MusicPaperRectNode.AddChild(notesCursor);
            notesCursor.Owner = MusicPaperRectNode;
            _SetNotesCursorPosition();
        }
        else if (currentState == BoardState.FINISHED || currentState == BoardState.STOPPED)
        {
            if (notesCursor != null)
            {
                notesCursor.QueueFree();
                notesCursor = null;
                MusicPaperRectNode.Texture = MusicPaperRectTexture;
                MusicPaperRectNode.Visible = false;
            }
        }
    }

    private Vector2 _GetNotePositionFromIndex(int noteIndex)
    {
        var gen = solfegeNotesTextureGenerator;
        var x = SolfegeNotesTextureGenerator.SOLFEGE_KEY_OFFSET + noteIndex * SolfegeNotesTextureGenerator.NOTE_SPRITE_WIDTH;
        var y = 0;
        return new Vector2(x, y);
    }

    private void _SetNotesCursorPosition()
    {
        if (notesCursor != null)
        {
            var pos = _GetNotePositionFromIndex(currentNoteIndex);
            notesCursor.MoveToPosition(pos);
        }
    }

    public void Reset()
    {
        currentState = (BoardState)saveData["current_state"];
        _InitState();
        EmitExpectedNoteChanged();
    }

    private void _OnNotePressed(string note)
    {
        if (currentState != BoardState.PLAYING)
            return;

        if (note == PAGES[currentPage][currentNoteIndex])
        {
            currentNoteIndex += 1;
            if (currentNoteIndex >= PAGES[currentPage].Length)
            {
                currentNoteIndex = 0;
                FlipNextPage();
            }
        }
        else
        {
            currentNoteIndex = 0;
            EmitSignal(nameof(wrong_note_played));
        }

        _SetNotesCursorPosition();
        EmitExpectedNoteChanged();
    }


    private void EmitExpectedNoteChanged()
    {
        if (currentState == BoardState.PLAYING)
        {
            EmitSignal(nameof(expected_note_changed), PAGES[currentPage][currentNoteIndex]);
        }
    }

    private void EmitWrongNoteEvent()
    {
        Event.Instance().EmitWrongPianoNotePlayed();
        EmitSignal(nameof(wrong_note_played));
    }

    public string GetExpectedNote()
    {
        if (currentState == BoardState.PLAYING)
        {
            return PAGES[currentPage][currentNoteIndex];
        }
        else
        {
            return null;
        }
    }

    private void _OnCheckpointHit(Node checkpoint)
    {
        saveData["current_state"] = currentState;
    }

    public bool IsStopped()
    {
        return currentState == BoardState.STOPPED;
    }
}
