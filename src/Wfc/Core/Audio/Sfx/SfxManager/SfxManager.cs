namespace Wfc.Core.Audio;

using System;
using System.Collections.Generic;
using Godot;
using Wfc.Core.Event;
using Wfc.Entities.World.Piano;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

[ScenePath]
public partial class SfxManager : Node2D, ISfxManager {
  [Signal]
  public delegate void PlaySfxEventHandler(string sfxName);

  private readonly Dictionary<string, AudioStreamPlayer> _sfxPool = [];

  public override void _EnterTree() {
    base._EnterTree();
    ConnectSignals();
  }

  public override void _Ready() {
    ProcessMode = ProcessModeEnum.Always;
    SetProcess(false);
    FillSfxPool();
  }

  private void FillSfxPool() {
    foreach (var (key, data) in GameSfx.Data) {
      var stream = GD.Load<AudioStream>(data.Path);
      var audioPlayer = new AudioStreamPlayer {
        Stream = stream,
        VolumeDb = data.Volume,
        Bus = data.Bus
      };
      stream.SetLooping(false);
      if (data.PitchScale is not null)
        audioPlayer.PitchScale = data.PitchScale.Value;

      _sfxPool[key] = audioPlayer;
      AddChild(audioPlayer);
      audioPlayer.Owner = this;
    }
  }

  private void ConnectSignals() {
    PlaySfx += OnPlaySfx;
    EventHandler.Instance.Events.PlayerJumped += OnPlayerJumped;
    EventHandler.Instance.Events.PlayerRotate += OnPlayerRotate;
    EventHandler.Instance.Events.PlayerLand += OnPlayerLand;
    EventHandler.Instance.Events.GemCollected += OnGemCollected;
    EventHandler.Instance.Events.FullscreenToggled += OnButtonToggle;
    EventHandler.Instance.Events.VsyncToggled += OnButtonToggle;
    EventHandler.Instance.Events.ScreenSizeChanged += OnButtonToggle;
    EventHandler.Instance.Events.OnActionBound += OnKeyBound;
    EventHandler.Instance.Events.TabChanged += OnTabChanged;
    EventHandler.Instance.Events.FocusChanged += OnFocusChanged;
    EventHandler.Instance.Events.MenuBoxRotated += OnMenuBoxRotated;
    EventHandler.Instance.Events.KeyboardActionBinding += OnKeyboardActionBinding;
    EventHandler.Instance.Events.PlayerExplode += OnPlayerExplode;
    EventHandler.Instance.Events.PauseMenuEnter += OnPauseMenuEnter;
    EventHandler.Instance.Events.PauseMenuExit += OnPauseMenuExit;
    EventHandler.Instance.Events.PlayerFall += OnPlayerFalling;
    EventHandler.Instance.Events.TetrisLinesRemoved += OnTetrisLinesRemoved;
    EventHandler.Instance.Events.PickedPowerUp += OnPickedPowerup;
    EventHandler.Instance.Events.BrickBroken += OnBrickBroken;
    EventHandler.Instance.Events.BreakBreakerWin += OnWinMiniGame;
    EventHandler.Instance.Events.BrickBreakerStart += OnBrickBreakerStart;
    EventHandler.Instance.Events.MenuButtonPressed += OnMenuButtonPressed;
    EventHandler.Instance.Events.SfxVolumeChanged += OnButtonToggle;
    EventHandler.Instance.Events.MusicVolumeChanged += OnButtonToggle;
    EventHandler.Instance.Events.PianoNotePressed += OnPianoNotePressed;
    EventHandler.Instance.Events.PianoNotePressed += OnPianoNoteReleased;
    EventHandler.Instance.Events.PageFlipped += OnPageFlipped;
    EventHandler.Instance.Events.WrongPianoNotePlayed += OnWrongPianoNotePlayed;
    EventHandler.Instance.Events.PianoPuzzleWon += OnPianoPuzzleWon;
    EventHandler.Instance.Events.GemTempleTriggered += OnGemTempleTriggered;
    EventHandler.Instance.Events.GemEngineStarted += OnGemEngineStarted;
    EventHandler.Instance.Events.GemPutInTemple += OnGemPutInTemple;
  }

  private void DisconnectSignals() {
    PlaySfx -= OnPlaySfx;
    EventHandler.Instance.Events.PlayerJumped -= OnPlayerJumped;
    EventHandler.Instance.Events.PlayerRotate -= OnPlayerRotate;
    EventHandler.Instance.Events.PlayerLand -= OnPlayerLand;
    EventHandler.Instance.Events.GemCollected -= OnGemCollected;
    EventHandler.Instance.Events.FullscreenToggled -= OnButtonToggle;
    EventHandler.Instance.Events.VsyncToggled -= OnButtonToggle;
    EventHandler.Instance.Events.ScreenSizeChanged -= OnButtonToggle;
    EventHandler.Instance.Events.OnActionBound -= OnKeyBound;
    EventHandler.Instance.Events.TabChanged -= OnTabChanged;
    EventHandler.Instance.Events.FocusChanged -= OnFocusChanged;
    EventHandler.Instance.Events.MenuBoxRotated -= OnMenuBoxRotated;
    EventHandler.Instance.Events.KeyboardActionBinding -= OnKeyboardActionBinding;
    EventHandler.Instance.Events.PlayerExplode -= OnPlayerExplode;
    EventHandler.Instance.Events.PauseMenuEnter -= OnPauseMenuEnter;
    EventHandler.Instance.Events.PauseMenuExit -= OnPauseMenuExit;
    EventHandler.Instance.Events.PlayerFall -= OnPlayerFalling;
    EventHandler.Instance.Events.TetrisLinesRemoved -= OnTetrisLinesRemoved;
    EventHandler.Instance.Events.PickedPowerUp -= OnPickedPowerup;
    EventHandler.Instance.Events.BrickBroken -= OnBrickBroken;
    EventHandler.Instance.Events.BreakBreakerWin -= OnWinMiniGame;
    EventHandler.Instance.Events.BrickBreakerStart -= OnBrickBreakerStart;
    EventHandler.Instance.Events.MenuButtonPressed -= OnMenuButtonPressed;
    EventHandler.Instance.Events.SfxVolumeChanged -= OnButtonToggle;
    EventHandler.Instance.Events.MusicVolumeChanged -= OnButtonToggle;
    EventHandler.Instance.Events.PianoNotePressed -= OnPianoNotePressed;
    EventHandler.Instance.Events.PianoNotePressed -= OnPianoNoteReleased;
    EventHandler.Instance.Events.PageFlipped -= OnPageFlipped;
    EventHandler.Instance.Events.WrongPianoNotePlayed -= OnWrongPianoNotePlayed;
    EventHandler.Instance.Events.PianoPuzzleWon -= OnPianoPuzzleWon;
    EventHandler.Instance.Events.GemTempleTriggered -= OnGemTempleTriggered;
    EventHandler.Instance.Events.GemEngineStarted -= OnGemEngineStarted;
    EventHandler.Instance.Events.GemPutInTemple -= OnGemPutInTemple;
  }

  public override void _ExitTree() {
    DisconnectSignals();
    base._ExitTree();
  }

  private void OnPlaySfx(string sfx) {
    if (_sfxPool.TryGetValue(sfx, out var value)) {
      value.Play();
    }
    else {
      GD.PushError("Invalid sfx name: ", sfx);
    }
  }

  public void StopAll() {
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

  public void PauseAll() {
    foreach (var sfx in _sfxPool.Values) {
      if (sfx.Playing) {
        sfx.StreamPaused = true;
      }
    }
  }

  public void ResumeAll() {
    foreach (var sfx in _sfxPool.Values) {
      if (sfx.Playing) {
        sfx.StreamPaused = false;
      }
    }
  }

  private void OnPlayerJumped() => OnPlaySfx("jump");
  private void OnPlayerRotate(int dir) => OnPlaySfx(dir == -1 ? "rotateLeft" : "rotateRight");
  private void OnPlayerLand() => OnPlaySfx("land");
  private void OnMenuButtonPressed(int menuButton) => OnPlaySfx("menuSelect");
  private void OnGemCollected(string color, Vector2 position, SpriteFrames x) => OnPlaySfx("gemCollect");
  private void OnButtonToggle(bool value) => OnPlaySfx("menuValueChange");
  private void OnButtonToggle(float value) => OnPlaySfx("menuValueChange");
  private void OnButtonToggle(Vector2 value) => OnPlaySfx("menuValueChange");
  private void OnKeyBound(string action, int key) => OnPlaySfx("menuValueChange");
  private void OnTabChanged() => OnPlaySfx("menuFocus");
  private void OnFocusChanged() => OnPlaySfx("menuFocus");
  private void OnMenuBoxRotated() => OnPlaySfx("rotateRight");
  private void OnKeyboardActionBinding() => OnPlaySfx("menuValueChange");
  private void OnPlayerExplode() => OnPlaySfx("playerExplode");
  private void OnPlayerFalling() => OnPlaySfx("playerFalling");
  private void OnTetrisLinesRemoved() => OnPlaySfx("tetrisLine");
  private void OnPickedPowerup() => OnPlaySfx("pickup");
  private void OnBrickBroken(string color, Vector2 _) => OnPlaySfx("brick");
  private void OnWinMiniGame() => OnPlaySfx("winMiniGame");
  private void OnBrickBreakerStart() => OnPlaySfx("bricksSlide");
  private void OnPianoNotePressed(int note) => OnPlaySfx("piano_" + note.ToString());
  private void OnPianoNoteReleased(int note) { /* do nothing */}
  private void OnPageFlipped() => OnPlaySfx("pageFlip");
  private void OnWrongPianoNotePlayed() => OnPlaySfx("wrongAnswer");
  private void OnPianoPuzzleWon() => OnPlaySfx("success");
  private void OnGemTempleTriggered() => OnPlaySfx("shine");
  private void OnGemEngineStarted() => OnPlaySfx("GemEngine");
  private void OnGemPutInTemple() => OnPlaySfx("GemPut");
  private void OnPauseMenuEnter() => OnPlaySfx("menuSelect");
  private void OnPauseMenuExit() => OnPlaySfx("menuSelect");
}
