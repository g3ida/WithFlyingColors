using System.Collections.Generic;
using System.Diagnostics;
using Godot;

public partial class SolfegeBoard : Node2D
{
    private const float DURATION = 0.8f;
    private PackedScene NotesCursorScene = ResourceLoader.Load<PackedScene>("res://Assets/Scenes/Piano/NotesCursor.tscn");
    private Texture2D MusicPaperRectTexture = GD.Load<Texture2D>("res://Assets/Sprites/Piano/music-paper-rect.png");

    [Signal]
    public delegate void board_notes_playedEventHandler();

    [Signal]
    public delegate void expected_note_changedEventHandler(string newExpectedNote);

    [Signal]
    public delegate void wrong_note_playedEventHandler();

    Sprite2D MusicPaperRectNode;


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

    private Texture2D currentTexture;
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
        MusicPaperRectNode = GetNode<Sprite2D>("MusicPaperRect");
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
            EmitSignal(nameof(board_notes_playedEventHandler));
            _SetFlipPageShader(MusicPaperRectTexture);
            _InitState();
        }
        else
        {
            var solfegeTexture = solfegeNotesTextureGenerator.CreateFromNotes(PAGES[currentPage], MusicPaperRectNode.GetRect().Size);
            _SetFlipPageShader(solfegeTexture);
        }
    }

    private void _SetFlipPageShader(Texture2D nextTexture)
    {
        var paperShaderMaterial = (MusicPaperRectNode.Material as ShaderMaterial);
        paperShaderMaterial.SetShaderParameter("flip_left", true);
        paperShaderMaterial.SetShaderParameter("cylinder_direction", new Vector2(5.0f, 1.0f));
        paperShaderMaterial.SetShaderParameter("current_page", currentTexture);
        paperShaderMaterial.SetShaderParameter("next_page", nextTexture);
        time = 0.0f;
        isFlipping = true;
        Event.Instance().EmitPageFlipped();
    }

    private void _InitShader()
    {
        var paperShaderMaterial = (MusicPaperRectNode.Material as ShaderMaterial);
        paperShaderMaterial.SetShaderParameter("time", 0);
        paperShaderMaterial.SetShaderParameter("flip_duration", DURATION);
        paperShaderMaterial.SetShaderParameter("cylinder_ratio", 0.3f);
        paperShaderMaterial.SetShaderParameter("rect", MusicPaperRectNode.GetRect().Size);
    }

    public override void _Process(double delta)
    {
        if (isFlipping)
        {
            time += (float)delta;
            (MusicPaperRectNode.Material as ShaderMaterial).SetShaderParameter("time", time);
            if (time > DURATION)
            {
                isFlipping = false;
                MusicPaperRectNode.Texture = currentTexture;
            }
        }
    }

    public override void _EnterTree()
    {
        Event.Instance().Connect("piano_note_pressed", new Callable(this, "_OnNotePressed"));
        Event.Instance().Connect("checkpoint_loaded", new Callable(this, "Reset"));
        Event.Instance().Connect("checkpoint_reached", new Callable(this, "_OnCheckpointHit"));
    }

    public override void _ExitTree()
    {
        Event.Instance().Disconnect("piano_note_pressed", new Callable(this, "_OnNotePressed"));
        Event.Instance().Disconnect("checkpoint_loaded", new Callable(this, "Reset"));
        Event.Instance().Disconnect("checkpoint_reached", new Callable(this, "_OnCheckpointHit"));
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
            (MusicPaperRectNode.Material as ShaderMaterial).SetShaderParameter("current_page", currentTexture);
            if (notesCursor != null)
            {
                notesCursor.QueueFree();
                notesCursor = null;
            }
            notesCursor = NotesCursorScene.Instantiate<NotesCursor>();
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
            EmitSignal(nameof(wrong_note_playedEventHandler));
        }

        _SetNotesCursorPosition();
        EmitExpectedNoteChanged();
    }


    private void EmitExpectedNoteChanged()
    {
        if (currentState == BoardState.PLAYING)
        {
            EmitSignal(nameof(expected_note_changedEventHandler), PAGES[currentPage][currentNoteIndex]);
        }
    }

    private void EmitWrongNoteEvent()
    {
        Event.Instance().EmitWrongPianoNotePlayed();
        EmitSignal(nameof(wrong_note_playedEventHandler));
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
