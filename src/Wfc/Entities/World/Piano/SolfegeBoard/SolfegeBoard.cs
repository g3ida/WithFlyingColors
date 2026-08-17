namespace Wfc.Entities.World.Piano;

using Chickensoft.Sync.Primitives;
using System.Collections.Generic;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using Wfc.Screens.Levels;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

[ScenePath]
public partial class SolfegeBoard : Node2D, IPersistent {
  private AutoChannel.Binding? _pianoBinding;

  private const float DURATION = 0.8f;
  private static readonly Vector2 FLIP_CYLINDER_DIRECTION = new Vector2(5.0f, 1.0f);
  private Texture2D MusicPaperRectTexture = GD.Load<Texture2D>("res://Assets/Sprites/Piano/music-paper-rect.png");
  private Texture2D BookCoverTexture = GD.Load<Texture2D>("res://Assets/Sprites/Piano/book-cover.png");

  [Signal]
  public delegate void BoardNotesPlayedEventHandler();

  [Signal]
  public delegate void ExpectedNoteChangedEventHandler(int newExpectedNote);

  [Signal]
  public delegate void WrongNotePlayedEventHandler();

  [NodePath("MusicPaperRect")]
  Sprite2D MusicPaperRectNode = null!;


  private readonly MusicNote[][] PAGES = new MusicNote[][] {
        new MusicNote[] { MusicNote.Do, MusicNote.Do, MusicNote.Sol, MusicNote.Sol, MusicNote.La, MusicNote.La, MusicNote.Sol },
        new MusicNote[] { MusicNote.Sol, MusicNote.Sol, MusicNote.Mi, MusicNote.Mi, MusicNote.Re, MusicNote.Re, MusicNote.Do },
        new MusicNote[] { MusicNote.Sol, MusicNote.Sol, MusicNote.Fa, MusicNote.Fa, MusicNote.Mi, MusicNote.Mi, MusicNote.Re },
        new MusicNote[] { MusicNote.Sol, MusicNote.Sol, MusicNote.Fa, MusicNote.Fa, MusicNote.Mi, MusicNote.Mi, MusicNote.Re },
        new MusicNote[] { MusicNote.Do, MusicNote.Do, MusicNote.Sol, MusicNote.Sol, MusicNote.La, MusicNote.La, MusicNote.Sol },
        new MusicNote[] { MusicNote.Sol, MusicNote.Sol, MusicNote.Mi, MusicNote.Mi, MusicNote.Re, MusicNote.Re, MusicNote.Do }
    };

  private int _numPages;

  private enum BoardState {
    Stopped,
    Playing,
    Finished
  }

  private Texture2D? _currentTexture;
  private BoardState _currentState = BoardState.Stopped;
  private bool _isFlipping = false;
  private int _currentPage = 0;
  private int _currentNoteIndex = 0;
  private NotesCursor? _notesCursor = null;
  private float _time = 0;

  private sealed record SaveData(BoardState savedState = BoardState.Stopped);
  private SaveData _saveData = new SaveData();

  private SolfegeNotesTextureGenerator solfegeNotesTextureGenerator = new SolfegeNotesTextureGenerator();

  public override void _EnterTree() {
    this.WireNodes();
    _pianoBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.PianoNotePressed message) => _OnNotePressed(message.NoteIndex));
    EventHandler.Instance.Events.CheckpointLoaded += Reset;
    EventHandler.Instance.Events.CheckpointReached += _OnCheckpointHit;
  }

  public override void _ExitTree() {
    _pianoBinding?.Dispose();
    _pianoBinding = null;
    EventHandler.Instance.Events.CheckpointLoaded -= Reset;
    EventHandler.Instance.Events.CheckpointReached -= _OnCheckpointHit;
  }

  public override void _Ready() {
    _numPages = PAGES.Length;
    _InitShader();
    _InitState();
  }

  public void StartGame() {
    _currentState = BoardState.Playing;
    _InitState();
  }

  public void FlipNextPage() {
    _currentPage += 1;
    if (_currentPage >= _numPages) {
      _currentState = BoardState.Finished;
      EmitSignal(nameof(BoardNotesPlayed));
      _SetFlipPageShader(MusicPaperRectTexture);
      _InitState();
    }
    else {
      var solfegeTexture = solfegeNotesTextureGenerator.CreateFromNotes(PAGES[_currentPage], MusicPaperRectNode.GetRect().Size);
      _SetFlipPageShader(solfegeTexture);
    }
  }

  // The sheet the flip peels away is the sprite's own texture, except on the opening
  // turn, where it is the cover of the closed book the player walked up to.
  private void _SetFlipPageShader(Texture2D nextTexture, Texture2D? pageBeingTurned = null) {
    var paperShaderMaterial = MusicPaperRectNode.Material as ShaderMaterial;
    if (paperShaderMaterial != null) {
      paperShaderMaterial.SetShaderParameter("flip_left", true);
      paperShaderMaterial.SetShaderParameter("cylinder_direction", FLIP_CYLINDER_DIRECTION);
      paperShaderMaterial.SetShaderParameter("next_page", nextTexture);
      var frontTexture = pageBeingTurned ?? _currentTexture;
      if (frontTexture != null) {
        paperShaderMaterial.SetShaderParameter("current_page", frontTexture);
      }
    }
    _time = 0.0f;
    _isFlipping = true;
    GameEvents.Instance.OnPageFlipped();
  }

  private void _InitShader() {
    var paperShaderMaterial = (MusicPaperRectNode.Material as ShaderMaterial);
    paperShaderMaterial?.SetShaderParameter("time", 0);
    paperShaderMaterial?.SetShaderParameter("flip_duration", DURATION);
    paperShaderMaterial?.SetShaderParameter("cylinder_ratio", 0.3f);
    paperShaderMaterial?.SetShaderParameter("rect", MusicPaperRectNode.GetRect().Size);
    // The scene ships this at (0, 0) and only the flip sets it, so before the first page turn
    // the shader was normalizing a zero vector.
    paperShaderMaterial?.SetShaderParameter("cylinder_direction", FLIP_CYLINDER_DIRECTION);
  }

  public override void _Process(double delta) {
    if (_isFlipping) {
      _time += (float)delta;
      (MusicPaperRectNode.Material as ShaderMaterial)?.SetShaderParameter("time", _time);
      if (_time > DURATION) {
        _isFlipping = false;
        MusicPaperRectNode.Texture = _currentTexture;
      }
    }
  }

  private void _InitState() {
    if (_currentState == BoardState.Playing) {
      _currentNoteIndex = 0;
      _currentPage = 0;
      MusicPaperRectNode.Visible = true;
      _currentTexture = MusicPaperRectNode.Texture;
      var resetTexture = solfegeNotesTextureGenerator.CreateFromNotes(PAGES[_currentPage], MusicPaperRectNode.GetRect().Size);
      _SetFlipPageShader(resetTexture, BookCoverTexture);
      if (_notesCursor != null) {
        _notesCursor.QueueFree();
        _notesCursor = null;
      }
      _notesCursor = SceneHelpers.InstantiateNode<NotesCursor>();
      MusicPaperRectNode.AddChild(_notesCursor);
      _notesCursor.Owner = MusicPaperRectNode;
      _SetNotesCursorPosition();
    }
    else if (_currentState is BoardState.Finished or BoardState.Stopped) {
      if (_notesCursor != null) {
        _notesCursor.QueueFree();
        _notesCursor = null;
      }
      // Hiding the sheet is not conditional on there being a cursor to remove. On the very
      // first _InitState - the one from _Ready - there is no cursor yet, so this was skipped
      // and the shader-driven sprite stayed visible with its page samplers unassigned, which
      // Godot binds as white: a blank rectangle sitting over the piano for the whole level.
      MusicPaperRectNode.Texture = MusicPaperRectTexture;
      MusicPaperRectNode.Visible = false;
    }
  }

  private Vector2 _GetNotePositionFromIndex(int noteIndex) {
    var x = SolfegeNotesTextureGenerator.SOLFEGE_KEY_OFFSET + noteIndex * SolfegeNotesTextureGenerator.NOTE_SPRITE_WIDTH;
    var y = 0;
    return new Vector2(x, y);
  }

  private void _SetNotesCursorPosition() {
    if (_notesCursor != null) {
      var pos = _GetNotePositionFromIndex(_currentNoteIndex);
      _notesCursor.MoveToPosition(pos);
    }
  }

  public void Reset() {
    _currentState = _saveData.savedState;
    _InitState();
    EmitExpectedNoteChanged();
  }

  private void _OnNotePressed(int note) {
    if (_currentState != BoardState.Playing)
      return;
    var parsedNote = MusicNoteHelper.MusicNoteFromInt(note);
    if (parsedNote == PAGES[_currentPage][_currentNoteIndex]) {
      _currentNoteIndex += 1;
      if (_currentNoteIndex >= PAGES[_currentPage].Length) {
        _currentNoteIndex = 0;
        FlipNextPage();
      }
    }
    else {
      _currentNoteIndex = 0;
      EmitWrongNoteEvent();
    }

    _SetNotesCursorPosition();
    EmitExpectedNoteChanged();
  }

  private void EmitExpectedNoteChanged() {
    if (_currentState == BoardState.Playing) {
      EmitSignal(nameof(ExpectedNoteChanged), (int)PAGES[_currentPage][_currentNoteIndex]);
    }
  }

  // The only way to report a wrong note. The wrong-note branch used to emit the local signal
  // by itself and this method had no callers at all, so the global event - the one SfxManager
  // answers with an actual sound - was never raised: the player's only cue that they had
  // missed was the cursor snapping back over ~40 ms, on a board mounted well above the keys,
  // while the note they played sounded exactly as if it had been right.
  private void EmitWrongNoteEvent() {
    GameEvents.Instance.OnWrongPianoNotePlayed();
    EmitSignal(nameof(WrongNotePlayed));
  }

  public MusicNote? GetExpectedNote() {
    if (_currentState == BoardState.Playing) {
      return PAGES[_currentPage][_currentNoteIndex];
    }
    return null;
  }

  private void _OnCheckpointHit(Vector2 _position, string _colorGroup) {
    _saveData = new SaveData(_currentState);
  }

  public bool IsStopped() {
    return _currentState == BoardState.Stopped;
  }

  public string GetSaveId() => this.GetPath();
  public string Save(ISerializer serializer) => serializer.Serialize(_saveData);
  public void Load(ISerializer serializer, string data) {
    var deserializedData = serializer.Deserialize<SaveData>(data);
    this._saveData = deserializedData ?? new SaveData();
    Reset();
  }
}
