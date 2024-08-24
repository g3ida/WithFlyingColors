using Godot;
using System;
using System.Collections.Generic;
using Wfc.Core.Event;

public partial class AudioManager : Node {
  [Signal]
  public delegate void PlaySfxEventHandler(string sfxName);

  private readonly PackedScene _musicTrackManagerScene = ResourceLoader.Load<PackedScene>("res://Assets/Scenes/Audio/MusicTrackManager.tscn");

  private const string BASE_PATH = "res://Assets/sfx/";

  private Dictionary<string, Dictionary<string, object>> _sfxData = new()
    {
        {"brick", new Dictionary<string, object> {{"path", BASE_PATH + "brick.ogg"}, {"volume", -4}}},
        {"bricksSlide", new Dictionary<string, object> {{"path", BASE_PATH + "bricks_slide.ogg"}, {"volume", 0}}},
        {"gemCollect", new Dictionary<string, object> {{"path", BASE_PATH + "gem.ogg"}, {"volume", -15}}},
        {"GemEngine", new Dictionary<string, object> {{"path", BASE_PATH + "gems/gem-engine.ogg"}, {"volume", 8}}},
        {"GemPut", new Dictionary<string, object> {{"path", BASE_PATH + "gems/temple_put_gem.ogg"}, {"volume", -5}}},
        {"jump", new Dictionary<string, object> {{"path", BASE_PATH + "jumping.ogg"}, {"volume", -5}}},
        {"land", new Dictionary<string, object> {{"path", BASE_PATH + "stand.ogg"}, {"volume", -8}}},
        {"menuFocus", new Dictionary<string, object> {{"path", BASE_PATH + "click2.ogg"}}},
        {"menuMove", new Dictionary<string, object> {{"path", BASE_PATH + "menu_move.ogg"}, {"volume", 0}}},
        {"menuSelect", new Dictionary<string, object> {{"path", BASE_PATH + "menu_select.ogg"}, {"volume", 0}}},
        {"menuValueChange", new Dictionary<string, object> {{"path", BASE_PATH + "click.ogg"}, {"volume", 0}}},
        {"pageFlip", new Dictionary<string, object> {{"path", BASE_PATH + "piano/page-flip.ogg"}, {"volume", 5}}},
        {"piano_do", new Dictionary<string, object> {{"path", BASE_PATH + "piano/do.ogg"}, {"volume", -3}}},
        {"piano_re", new Dictionary<string, object> {{"path", BASE_PATH + "piano/re.ogg"}, {"volume", -3}}},
        {"piano_mi", new Dictionary<string, object> {{"path", BASE_PATH + "piano/mi.ogg"}, {"volume", -3}}},
        {"piano_fa", new Dictionary<string, object> {{"path", BASE_PATH + "piano/fa.ogg"}, {"volume", -3}}},
        {"piano_sol", new Dictionary<string, object> {{"path", BASE_PATH + "piano/sol.ogg"}, {"volume", -3}}},
        {"piano_la", new Dictionary<string, object> {{"path", BASE_PATH + "piano/la.ogg"}, {"volume", -3}}},
        {"piano_si", new Dictionary<string, object> {{"path", BASE_PATH + "piano/si.ogg"}, {"volume", -3}}},
        {"pickup", new Dictionary<string, object> {{"path", BASE_PATH + "pickup.ogg"}, {"volume", -4}}},
        {"playerExplode", new Dictionary<string, object> {{"path", BASE_PATH + "die.ogg"}, {"volume", -10}}},
        {"playerFalling", new Dictionary<string, object> {{"path", BASE_PATH + "falling.ogg"}, {"volume", -10}}},
        {"rotateLeft", new Dictionary<string, object> {{"path", BASE_PATH + "rotate-box.ogg"}, {"volume", -20}, {"pitch_scale", 0.9}}},
        {"rotateRight", new Dictionary<string, object> {{"path", BASE_PATH + "rotate-box.ogg"}, {"volume", -20}}},
        {"shine", new Dictionary<string, object> {{"path", BASE_PATH + "shine.ogg"}, {"volume", -5}}},
        {"success", new Dictionary<string, object> {{"path", BASE_PATH + "success.ogg"}, {"volume", -1}}},
        {"tetrisLine", new Dictionary<string, object> {{"path", BASE_PATH + "tetris_line.ogg"}, {"volume", -7}}},
        {"winMiniGame", new Dictionary<string, object> {{"path", BASE_PATH + "win_mini_game.ogg"}, {"volume", 1}}},
        {"wrongAnswer", new Dictionary<string, object> {{"path", BASE_PATH + "piano/wrong-answer.ogg"}, {"volume", 10}}}
    };

  private readonly Dictionary<string, AudioStreamPlayer> _sfxPool = [];
  public required MusicTrackManager MusicTrackManager;

  private static AudioManager _instance = null!;

  public static AudioManager Instance() {
    return _instance;
  }

  public override void _EnterTree() {
    base._EnterTree();
    _instance = GetTree().Root.GetNode<AudioManager>("AudioManagerCS");
    MusicTrackManager = _musicTrackManagerScene.Instantiate<MusicTrackManager>();
    ConnectSignals();
  }

  public override void _Ready() {
    ProcessMode = ProcessModeEnum.Always;
    SetProcess(false);
    /// FIXME: make this an extension function and use all over any node CreateChild<NodeType>()
    AddChild(MusicTrackManager);
    MusicTrackManager.Owner = this;
    ///
    FillSfxPool();
  }

  private void FillSfxPool() {
    foreach (var key in _sfxData.Keys) {
      var stream = GD.Load<AudioStream>(_sfxData[key]["path"].ToString());
      var audioPlayer = new AudioStreamPlayer();
      Helpers.SetLooping(stream, false);
      audioPlayer.Stream = stream;

      if (_sfxData[key].TryGetValue("volume", out var volume)) {
        audioPlayer.VolumeDb = Convert.ToSingle(volume);
      }

      if (_sfxData[key].TryGetValue("pitch_scale", out var pitchScale)) {
        audioPlayer.PitchScale = Convert.ToSingle(pitchScale);
      }

      var bus = "sfx";
      if (_sfxData[key].TryGetValue("bus", out var busValue)) {
        bus = busValue.ToString();
      }

      audioPlayer.Bus = bus;
      _sfxPool[key] = audioPlayer;
      AddChild(audioPlayer);
      audioPlayer.Owner = this;
    }
  }

  private void ConnectSignals() {
    PlaySfx += OnPlaySfx;
    Event.Instance.Connect(EventType.PlayerJumped, new Callable(this, nameof(OnPlayerJumped)));
    Event.Instance.Connect(EventType.PlayerRotate, new Callable(this, nameof(OnPlayerRotate)));
    Event.Instance.Connect(EventType.PlayerLand, new Callable(this, nameof(OnPlayerLand)));
    Event.Instance.Connect(EventType.GemCollected, new Callable(this, nameof(OnGemCollected)));
    Event.Instance.Connect(EventType.FullscreenToggled, new Callable(this, nameof(OnButtonToggle)));
    Event.Instance.Connect(EventType.VsyncToggled, new Callable(this, nameof(OnButtonToggle)));
    Event.Instance.Connect(EventType.ScreenSizeChanged, new Callable(this, nameof(OnButtonToggle)));
    Event.Instance.Connect(EventType.OnActionBound, new Callable(this, nameof(OnKeyBound)));
    Event.Instance.Connect(EventType.TabChanged, new Callable(this, nameof(OnTabChanged)));
    Event.Instance.Connect(EventType.FocusChanged, new Callable(this, nameof(OnFocusChanged)));
    Event.Instance.Connect(EventType.MenuBoxRotated, new Callable(this, nameof(OnMenuBoxRotated)));
    Event.Instance.Connect(EventType.KeyboardActionBinding, new Callable(this, nameof(OnKeyboardActionBinding)));
    Event.Instance.Connect(EventType.PlayerExplode, new Callable(this, nameof(OnPlayerExplode)));
    Event.Instance.Connect(EventType.PauseMenuEnter, new Callable(this, nameof(OnPauseMenuEnter)));
    Event.Instance.Connect(EventType.PauseMenuExit, new Callable(this, nameof(OnPauseMenuExit)));
    Event.Instance.Connect(EventType.PlayerFall, new Callable(this, nameof(OnPlayerFalling)));
    Event.Instance.Connect(EventType.TetrisLinesRemoved, new Callable(this, nameof(OnTetrisLinesRemoved)));
    Event.Instance.Connect(EventType.PickedPowerUp, new Callable(this, nameof(OnPickedPowerup)));
    Event.Instance.Connect(EventType.BrickBroken, new Callable(this, nameof(OnBrickBroken)));
    Event.Instance.Connect(EventType.BreakBreakerWin, new Callable(this, nameof(OnWinMiniGame)));
    Event.Instance.Connect(EventType.BrickBreakerStart, new Callable(this, nameof(OnBrickBreakerStart)));
    Event.Instance.Connect(EventType.MenuButtonPressed, new Callable(this, nameof(OnMenuButtonPressed)));
    Event.Instance.Connect(EventType.SfxVolumeChanged, new Callable(this, nameof(OnButtonToggle)));
    Event.Instance.Connect(EventType.MusicVolumeChanged, new Callable(this, nameof(OnButtonToggle)));
    Event.Instance.Connect(EventType.PianoNotePressed, new Callable(this, nameof(OnPianoNotePressed)));
    Event.Instance.Connect(EventType.PianoNotePressed, new Callable(this, nameof(OnPianoNoteReleased)));
    Event.Instance.Connect(EventType.PageFlipped, new Callable(this, nameof(OnPageFlipped)));
    Event.Instance.Connect(EventType.WrongPianoNotePlayed, new Callable(this, nameof(OnWrongPianoNotePlayed)));
    Event.Instance.Connect(EventType.PianoPuzzleWon, new Callable(this, nameof(OnPianoPuzzleWon)));
    Event.Instance.Connect(EventType.GemTempleTriggered, new Callable(this, nameof(OnGemTempleTriggered)));
    Event.Instance.Connect(EventType.GemEngineStarted, new Callable(this, nameof(OnGemEngineStarted)));
    Event.Instance.Connect(EventType.GemPutInTemple, new Callable(this, nameof(OnGemPutInTemple)));
  }

  private void DisconnectSignals() {
    Disconnect(nameof(PlaySfx), new Callable(this, nameof(OnPlaySfx)));
    Event.Instance.Disconnect(EventType.PlayerJumped, new Callable(this, nameof(OnPlayerJumped)));
    Event.Instance.Disconnect(EventType.PlayerRotate, new Callable(this, nameof(OnPlayerRotate)));
    Event.Instance.Disconnect(EventType.PlayerLand, new Callable(this, nameof(OnPlayerLand)));
    Event.Instance.Disconnect(EventType.GemCollected, new Callable(this, nameof(OnGemCollected)));
    Event.Instance.Disconnect(EventType.FullscreenToggled, new Callable(this, nameof(OnButtonToggle)));
    Event.Instance.Disconnect(EventType.VsyncToggled, new Callable(this, nameof(OnButtonToggle)));
    Event.Instance.Disconnect(EventType.ScreenSizeChanged, new Callable(this, nameof(OnButtonToggle)));
    Event.Instance.Disconnect(EventType.OnActionBound, new Callable(this, nameof(OnKeyBound)));
    Event.Instance.Disconnect(EventType.TabChanged, new Callable(this, nameof(OnTabChanged)));
    Event.Instance.Disconnect(EventType.FocusChanged, new Callable(this, nameof(OnFocusChanged)));
    Event.Instance.Disconnect(EventType.MenuBoxRotated, new Callable(this, nameof(OnMenuBoxRotated)));
    Event.Instance.Disconnect(EventType.KeyboardActionBinding, new Callable(this, nameof(OnKeyboardActionBinding)));
    Event.Instance.Disconnect(EventType.PlayerExplode, new Callable(this, nameof(OnPlayerExplode)));
    Event.Instance.Disconnect(EventType.PauseMenuEnter, new Callable(this, nameof(OnPauseMenuEnter)));
    Event.Instance.Disconnect(EventType.PauseMenuExit, new Callable(this, nameof(OnPauseMenuExit)));
    Event.Instance.Disconnect(EventType.PlayerFall, new Callable(this, nameof(OnPlayerFalling)));
    Event.Instance.Disconnect(EventType.TetrisLinesRemoved, new Callable(this, nameof(OnTetrisLinesRemoved)));
    Event.Instance.Disconnect(EventType.PickedPowerUp, new Callable(this, nameof(OnPickedPowerup)));
    Event.Instance.Disconnect(EventType.BrickBroken, new Callable(this, nameof(OnBrickBroken)));
    Event.Instance.Disconnect(EventType.BreakBreakerWin, new Callable(this, nameof(OnWinMiniGame)));
    Event.Instance.Disconnect(EventType.BrickBreakerStart, new Callable(this, nameof(OnBrickBreakerStart)));
    Event.Instance.Disconnect(EventType.MenuButtonPressed, new Callable(this, nameof(OnMenuButtonPressed)));
    Event.Instance.Disconnect(EventType.SfxVolumeChanged, new Callable(this, nameof(OnButtonToggle)));
    Event.Instance.Disconnect(EventType.MusicVolumeChanged, new Callable(this, nameof(OnButtonToggle)));
    Event.Instance.Disconnect(EventType.PianoNotePressed, new Callable(this, nameof(OnPianoNotePressed)));
    Event.Instance.Disconnect(EventType.PianoNoteReleased, new Callable(this, nameof(OnPianoNoteReleased)));
    Event.Instance.Disconnect(EventType.PageFlipped, new Callable(this, nameof(OnPageFlipped)));
    Event.Instance.Disconnect(EventType.WrongPianoNotePlayed, new Callable(this, nameof(OnWrongPianoNotePlayed)));
    Event.Instance.Disconnect(EventType.PianoPuzzleWon, new Callable(this, nameof(OnPianoPuzzleWon)));
    Event.Instance.Disconnect(EventType.GemTempleTriggered, new Callable(this, nameof(OnGemTempleTriggered)));
    Event.Instance.Disconnect(EventType.GemEngineStarted, new Callable(this, nameof(OnGemEngineStarted)));
    Event.Instance.Disconnect(EventType.GemPutInTemple, new Callable(this, nameof(OnGemPutInTemple)));
  }

  public override void _ExitTree() {
    DisconnectSignals();
    base._ExitTree();
  }

  private void OnPlaySfx(string sfx) {
    if (_sfxPool.TryGetValue(sfx, out var value)) {
      value.Play();
    }
  }

  public void StopAllSfx() {
    foreach (var sfx in _sfxPool.Values) {
      sfx.Stop();
    }
  }

  public void StopAllExcept(string[] sfxList) {
    foreach (var sfx in _sfxPool) {
      if (!Array.Exists(sfxList, element => element == sfx.Key)) {
        sfx.Value.Stop();
      }
    }
  }

  public void EmitPlaySfx(string sfxName) {
    EmitSignal(nameof(PlaySfx), sfxName);
  }

  public void PauseAllSfx() {
    foreach (var sfx in _sfxPool.Values) {
      if (sfx.Playing) {
        sfx.StreamPaused = true;
      }
    }
  }

  public void ResumeAllSfx() {
    foreach (var sfx in _sfxPool.Values) {
      if (sfx.Playing) {
        sfx.StreamPaused = false;
      }
    }
  }

  private void OnPlayerJumped() {
    OnPlaySfx("jump");
  }

  private void OnPlayerRotate(int dir) {
    OnPlaySfx(dir == -1 ? "rotateLeft" : "rotateRight");
  }

  private void OnPlayerLand() {
    OnPlaySfx("land");
  }

  private void OnMenuButtonPressed(int menuButton) {
    OnPlaySfx("menuSelect");
  }

  private void OnGemCollected(string color, Vector2 position, SpriteFrames x) {
    OnPlaySfx("gemCollect");
  }

  private void OnButtonToggle(bool value) {
    OnPlaySfx("menuValueChange");
  }

  private void OnKeyBound(bool value, bool value2) {
    OnPlaySfx("menuValueChange");
  }

  private void OnTabChanged() {
    OnPlaySfx("menuFocus");
  }

  private void OnFocusChanged() {
    OnPlaySfx("menuFocus");
  }

  private void OnMenuBoxRotated() {
    OnPlaySfx("rotateRight");
  }

  private void OnKeyboardActionBinding() {
    OnPlaySfx("menuValueChange");
  }

  private void OnPlayerExplode() {
    OnPlaySfx("playerExplode");
  }

  private void OnPlayerFalling() {
    OnPlaySfx("playerFalling");
  }

  private void OnTetrisLinesRemoved() {
    OnPlaySfx("tetrisLine");
  }

  private void OnPickedPowerup() {
    OnPlaySfx("pickup");
  }

  private void OnBrickBroken(string color, Vector2 position) {
    OnPlaySfx("brick");
  }

  private void OnWinMiniGame() {
    OnPlaySfx("winMiniGame");
  }

  private void OnBrickBreakerStart() {
    OnPlaySfx("bricksSlide");
  }

  private void OnPianoNotePressed(string note) {
    OnPlaySfx("piano_" + note);
  }

  private void OnPianoNoteReleased(string note) {
    // do nothing
  }

  private void OnPageFlipped() {
    OnPlaySfx("pageFlip");
  }

  private void OnWrongPianoNotePlayed() {
    OnPlaySfx("wrongAnswer");
  }

  private void OnPianoPuzzleWon() {
    OnPlaySfx("success");
  }

  private void OnGemTempleTriggered() {
    OnPlaySfx("shine");
  }

  private void OnGemEngineStarted() {
    OnPlaySfx("GemEngine");
  }

  private void OnGemPutInTemple() {
    OnPlaySfx("GemPut");
  }

  private void OnPauseMenuEnter() {
    OnPlaySfx("menuSelect");
  }

  private void OnPauseMenuExit() {
    OnPlaySfx("menuSelect");
  }
}
