namespace Wfc.Core.Audio;

using System;
using System.Collections.Generic;
using Chickensoft.Sync.Primitives;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Logger;
using Wfc.Entities.World.Piano;
using Wfc.Screens.Levels;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

[ScenePath]
public partial class SfxManager : Node2D, ISfxManager {
  [Signal]
  public delegate void PlaySfxEventHandler(string sfxName);

  private readonly Dictionary<string, AudioStreamPlayer> _sfxPool = [];

  // Every settings change the player makes sounds the same, so the four of them share one
  // callback. Disposing the binding is the whole of the unsubscribe.
  private AutoChannel.Binding? _eventsBinding;

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
      // A sound the editor has not imported yet resolves to null, and taking the whole pool
      // down with it would leave the game silent rather than just that one effect.
      if (stream is null) {
        Log.Error($"Could not load sfx stream: {data.Path}");
        continue;
      }
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
    EventHandler.Instance.Events.ControllerSelectionChanged += OnControllerSelectionChanged;
    EventHandler.Instance.Events.OnActionBound += OnKeyBound;
    EventHandler.Instance.Events.FocusChanged += OnFocusChanged;
    EventHandler.Instance.Events.MenuBoxRotated += OnMenuBoxRotated;
    EventHandler.Instance.Events.KeyboardActionBinding += OnKeyboardActionBinding;
    EventHandler.Instance.Events.PauseMenuEnter += OnPauseMenuEnter;
    EventHandler.Instance.Events.PauseMenuExit += OnPauseMenuExit;
    EventHandler.Instance.Events.MenuButtonPressed += OnMenuButtonPressed;
    EventHandler.Instance.Events.NotificationRaised += OnNotificationRaised;
    EventHandler.Instance.Events.DoorGemFilled += OnDoorGemFilled;
    EventHandler.Instance.Events.DoorCometFormed += OnDoorCometFormed;
    _eventsBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.FullscreenToggled _) => OnSettingChanged())
      .On((in IGameEvents.VsyncToggled _) => OnSettingChanged())
      .On((in IGameEvents.ScreenSizeChanged _) => OnSettingChanged())
      .On((in IGameEvents.LanguageChanged _) => OnSettingChanged())
      .On((in IGameEvents.SfxVolumeChanged _) => OnSettingChanged())
      .On((in IGameEvents.MusicVolumeChanged _) => OnSettingChanged())
      .On((in IGameEvents.TetrisLinesRemoved _) => OnPlaySfx("tetrisLine"))
      .On((in IGameEvents.TetrisPoolEscaped _) => OnPlaySfx("winMiniGame"))
      .On((in IGameEvents.BrickBroken _) => OnPlaySfx("brick"))
      .On((in IGameEvents.BrickBreakerStarted _) => OnPlaySfx("bricksSlide"))
      .On((in IGameEvents.BrickBreakerWon _) => OnPlaySfx("winMiniGame"))
      .On((in IGameEvents.PowerUpPicked _) => OnPlaySfx("pickup"))
      // A note is a one-shot sample, so releasing a key sounds nothing. PianoNoteReleased is
      // still announced by the piano for anything that wants it; the sound bank has no answer.
      .On((in IGameEvents.PianoNotePressed message) => OnPlaySfx("piano_" + message.NoteIndex.ToString()))
      .On((in IGameEvents.PageFlipped _) => OnPlaySfx("pageFlip"))
      .On((in IGameEvents.WrongPianoNotePlayed _) => OnPlaySfx("wrongAnswer"))
      .On((in IGameEvents.PianoPuzzleWon _) => OnPlaySfx("success"))
      .On((in IGameEvents.ButtonGameNotePlayed message) => OnPlaySfx("piano_" + message.NoteIndex.ToString()))
      .On((in IGameEvents.ButtonGameWrongNotePlayed _) => OnPlaySfx("wrongAnswer"))
      .On((in IGameEvents.ButtonGameWon _) => OnPlaySfx("success"))
      .On((in IGameEvents.PlayerJumped _) => OnPlaySfx("jump"))
      .On((in IGameEvents.PlayerRotated message) => OnPlayerRotate(message.Direction))
      .On((in IGameEvents.PlayerLanded _) => OnPlaySfx("land"))
      .On((in IGameEvents.PlayerDashed _) => OnPlaySfx("dash"))
      .On((in IGameEvents.PlayerExploded _) => OnPlaySfx("playerExplode"))
      .On((in IGameEvents.PlayerFell _) => OnPlaySfx("playerFalling"))
      .On((in IGameEvents.PlayerSquashed _) => OnPlaySfx("playerSquashed"))
      .On((in IGameEvents.GemCollected _) => OnPlaySfx("gemCollect"))
      .On((in IGameEvents.PaintSpilled _) => OnPlaySfx("bucketFall"))
      .On((in IGameEvents.BucketShoved _) => OnPlaySfx("bucketPush"))
      .On((in IGameEvents.PaintPouring _) => OnPlaySfx("paintPour"))
      .On((in IGameEvents.PaintSplashed _) => OnPlaySfx("paintSplash"))
      .On((in IGameEvents.PaintGunCooling _) => OnPlaySfx("gunCooldown"))
      .On((in IGameEvents.PaintGunFired _) => OnPlaySfx("shooting"));
  }

  private void DisconnectSignals() {
    PlaySfx -= OnPlaySfx;
    EventHandler.Instance.Events.ControllerSelectionChanged -= OnControllerSelectionChanged;
    EventHandler.Instance.Events.OnActionBound -= OnKeyBound;
    EventHandler.Instance.Events.FocusChanged -= OnFocusChanged;
    EventHandler.Instance.Events.MenuBoxRotated -= OnMenuBoxRotated;
    EventHandler.Instance.Events.KeyboardActionBinding -= OnKeyboardActionBinding;
    EventHandler.Instance.Events.PauseMenuEnter -= OnPauseMenuEnter;
    EventHandler.Instance.Events.PauseMenuExit -= OnPauseMenuExit;
    EventHandler.Instance.Events.MenuButtonPressed -= OnMenuButtonPressed;
    EventHandler.Instance.Events.NotificationRaised -= OnNotificationRaised;
    EventHandler.Instance.Events.DoorGemFilled -= OnDoorGemFilled;
    EventHandler.Instance.Events.DoorCometFormed -= OnDoorCometFormed;
    _eventsBinding?.Dispose();
    _eventsBinding = null;
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
      Log.Error($"Invalid sfx name: {sfx}");
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

  private void OnPlayerRotate(int dir) => OnPlaySfx(dir == -1 ? "rotateLeft" : "rotateRight");
  private void OnMenuButtonPressed(int menuButton) => OnPlaySfx("menuSelect");
  private void OnSettingChanged() => OnPlaySfx("menuValueChange");
  private void OnControllerSelectionChanged(int controllerType) => OnPlaySfx("menuValueChange");
  private void OnKeyBound(string action, int key) => OnPlaySfx("menuValueChange");
  private void OnFocusChanged() => OnPlaySfx("menuFocus");
  private void OnMenuBoxRotated() => OnPlaySfx("rotateRight");
  private void OnKeyboardActionBinding() => OnPlaySfx("menuValueChange");
  private void OnNotificationRaised(int translationKey) => OnPlaySfx("notification");
  private void OnDoorGemFilled() => OnPlaySfx("doorGemFill");
  private void OnDoorCometFormed() => OnPlaySfx("doorCometFormed");
  private void OnPauseMenuEnter() => OnPlaySfx("menuSelect");
  private void OnPauseMenuExit() => OnPlaySfx("menuSelect");
}
