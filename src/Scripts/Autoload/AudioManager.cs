using Godot;
using System;
using System.Collections.Generic;
using Wfc.Core.Event;
using EventHandler = Wfc.Core.Event.EventHandler;

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
    EventHandler.Instance.Connect(EventType.PlayerJumped, new Callable(this, nameof(OnPlayerJumped)));
    EventHandler.Instance.Connect(EventType.PlayerRotate, new Callable(this, nameof(OnPlayerRotate)));
    EventHandler.Instance.Connect(EventType.PlayerLand, new Callable(this, nameof(OnPlayerLand)));
    EventHandler.Instance.Connect(EventType.GemCollected, new Callable(this, nameof(OnGemCollected)));
    EventHandler.Instance.Connect(EventType.FullscreenToggled, new Callable(this, nameof(OnButtonToggle)));
    EventHandler.Instance.Connect(EventType.VsyncToggled, new Callable(this, nameof(OnButtonToggle)));
    EventHandler.Instance.Connect(EventType.ScreenSizeChanged, new Callable(this, nameof(OnButtonToggle)));
    EventHandler.Instance.Connect(EventType.OnActionBound, new Callable(this, nameof(OnKeyBound)));
    EventHandler.Instance.Connect(EventType.TabChanged, new Callable(this, nameof(OnTabChanged)));
    EventHandler.Instance.Connect(EventType.FocusChanged, new Callable(this, nameof(OnFocusChanged)));
    EventHandler.Instance.Connect(EventType.MenuBoxRotated, new Callable(this, nameof(OnMenuBoxRotated)));
    EventHandler.Instance.Connect(EventType.KeyboardActionBinding, new Callable(this, nameof(OnKeyboardActionBinding)));
    EventHandler.Instance.Connect(EventType.PlayerExplode, new Callable(this, nameof(OnPlayerExplode)));
    EventHandler.Instance.Connect(EventType.PauseMenuEnter, new Callable(this, nameof(OnPauseMenuEnter)));
    EventHandler.Instance.Connect(EventType.PauseMenuExit, new Callable(this, nameof(OnPauseMenuExit)));
    EventHandler.Instance.Connect(EventType.PlayerFall, new Callable(this, nameof(OnPlayerFalling)));
    EventHandler.Instance.Connect(EventType.TetrisLinesRemoved, new Callable(this, nameof(OnTetrisLinesRemoved)));
    EventHandler.Instance.Connect(EventType.PickedPowerUp, new Callable(this, nameof(OnPickedPowerup)));
    EventHandler.Instance.Connect(EventType.BrickBroken, new Callable(this, nameof(OnBrickBroken)));
    EventHandler.Instance.Connect(EventType.BreakBreakerWin, new Callable(this, nameof(OnWinMiniGame)));
    EventHandler.Instance.Connect(EventType.BrickBreakerStart, new Callable(this, nameof(OnBrickBreakerStart)));
    EventHandler.Instance.Connect(EventType.MenuButtonPressed, new Callable(this, nameof(OnMenuButtonPressed)));
    EventHandler.Instance.Connect(EventType.SfxVolumeChanged, new Callable(this, nameof(OnButtonToggle)));
    EventHandler.Instance.Connect(EventType.MusicVolumeChanged, new Callable(this, nameof(OnButtonToggle)));
    EventHandler.Instance.Connect(EventType.PianoNotePressed, new Callable(this, nameof(OnPianoNotePressed)));
    EventHandler.Instance.Connect(EventType.PianoNotePressed, new Callable(this, nameof(OnPianoNoteReleased)));
    EventHandler.Instance.Connect(EventType.PageFlipped, new Callable(this, nameof(OnPageFlipped)));
    EventHandler.Instance.Connect(EventType.WrongPianoNotePlayed, new Callable(this, nameof(OnWrongPianoNotePlayed)));
    EventHandler.Instance.Connect(EventType.PianoPuzzleWon, new Callable(this, nameof(OnPianoPuzzleWon)));
    EventHandler.Instance.Connect(EventType.GemTempleTriggered, new Callable(this, nameof(OnGemTempleTriggered)));
    EventHandler.Instance.Connect(EventType.GemEngineStarted, new Callable(this, nameof(OnGemEngineStarted)));
    EventHandler.Instance.Connect(EventType.GemPutInTemple, new Callable(this, nameof(OnGemPutInTemple)));
  }

  private void DisconnectSignals() {
    Disconnect(nameof(PlaySfx), new Callable(this, nameof(OnPlaySfx)));
    EventHandler.Instance.Disconnect(EventType.PlayerJumped, new Callable(this, nameof(OnPlayerJumped)));
    EventHandler.Instance.Disconnect(EventType.PlayerRotate, new Callable(this, nameof(OnPlayerRotate)));
    EventHandler.Instance.Disconnect(EventType.PlayerLand, new Callable(this, nameof(OnPlayerLand)));
    EventHandler.Instance.Disconnect(EventType.GemCollected, new Callable(this, nameof(OnGemCollected)));
    EventHandler.Instance.Disconnect(EventType.FullscreenToggled, new Callable(this, nameof(OnButtonToggle)));
    EventHandler.Instance.Disconnect(EventType.VsyncToggled, new Callable(this, nameof(OnButtonToggle)));
    EventHandler.Instance.Disconnect(EventType.ScreenSizeChanged, new Callable(this, nameof(OnButtonToggle)));
    EventHandler.Instance.Disconnect(EventType.OnActionBound, new Callable(this, nameof(OnKeyBound)));
    EventHandler.Instance.Disconnect(EventType.TabChanged, new Callable(this, nameof(OnTabChanged)));
    EventHandler.Instance.Disconnect(EventType.FocusChanged, new Callable(this, nameof(OnFocusChanged)));
    EventHandler.Instance.Disconnect(EventType.MenuBoxRotated, new Callable(this, nameof(OnMenuBoxRotated)));
    EventHandler.Instance.Disconnect(EventType.KeyboardActionBinding, new Callable(this, nameof(OnKeyboardActionBinding)));
    EventHandler.Instance.Disconnect(EventType.PlayerExplode, new Callable(this, nameof(OnPlayerExplode)));
    EventHandler.Instance.Disconnect(EventType.PauseMenuEnter, new Callable(this, nameof(OnPauseMenuEnter)));
    EventHandler.Instance.Disconnect(EventType.PauseMenuExit, new Callable(this, nameof(OnPauseMenuExit)));
    EventHandler.Instance.Disconnect(EventType.PlayerFall, new Callable(this, nameof(OnPlayerFalling)));
    EventHandler.Instance.Disconnect(EventType.TetrisLinesRemoved, new Callable(this, nameof(OnTetrisLinesRemoved)));
    EventHandler.Instance.Disconnect(EventType.PickedPowerUp, new Callable(this, nameof(OnPickedPowerup)));
    EventHandler.Instance.Disconnect(EventType.BrickBroken, new Callable(this, nameof(OnBrickBroken)));
    EventHandler.Instance.Disconnect(EventType.BreakBreakerWin, new Callable(this, nameof(OnWinMiniGame)));
    EventHandler.Instance.Disconnect(EventType.BrickBreakerStart, new Callable(this, nameof(OnBrickBreakerStart)));
    EventHandler.Instance.Disconnect(EventType.MenuButtonPressed, new Callable(this, nameof(OnMenuButtonPressed)));
    EventHandler.Instance.Disconnect(EventType.SfxVolumeChanged, new Callable(this, nameof(OnButtonToggle)));
    EventHandler.Instance.Disconnect(EventType.MusicVolumeChanged, new Callable(this, nameof(OnButtonToggle)));
    EventHandler.Instance.Disconnect(EventType.PianoNotePressed, new Callable(this, nameof(OnPianoNotePressed)));
    EventHandler.Instance.Disconnect(EventType.PianoNoteReleased, new Callable(this, nameof(OnPianoNoteReleased)));
    EventHandler.Instance.Disconnect(EventType.PageFlipped, new Callable(this, nameof(OnPageFlipped)));
    EventHandler.Instance.Disconnect(EventType.WrongPianoNotePlayed, new Callable(this, nameof(OnWrongPianoNotePlayed)));
    EventHandler.Instance.Disconnect(EventType.PianoPuzzleWon, new Callable(this, nameof(OnPianoPuzzleWon)));
    EventHandler.Instance.Disconnect(EventType.GemTempleTriggered, new Callable(this, nameof(OnGemTempleTriggered)));
    EventHandler.Instance.Disconnect(EventType.GemEngineStarted, new Callable(this, nameof(OnGemEngineStarted)));
    EventHandler.Instance.Disconnect(EventType.GemPutInTemple, new Callable(this, nameof(OnGemPutInTemple)));
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
