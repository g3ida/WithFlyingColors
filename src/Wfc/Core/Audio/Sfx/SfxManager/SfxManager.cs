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

[ScenePath]
public partial class SfxManager : Node2D, ISfxManager {
  [Signal]
  public delegate void PlaySfxEventHandler(string sfxName);

  private readonly Dictionary<string, AudioStreamPlayer> _sfxPool = [];

  // Disposing this is the whole of the unsubscribe, however many messages it covers.
  private AutoChannel.Binding? _eventsBinding;

  // The pool is filled before the bindings exist, not in _Ready: _Ready lands a good deal
  // later than _EnterTree - the autoloads finish building themselves in between - and a
  // message arriving in that gap would find an empty pool and report every sound as invalid.
  public override void _EnterTree() {
    base._EnterTree();
    FillSfxPool();
    ConnectSignals();
  }

  public override void _Ready() {
    ProcessMode = ProcessModeEnum.Always;
    SetProcess(false);
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
    _eventsBinding ??= _bindPaint(_bindPlayer(_bindMinigames(_bindMenus(
      _bindSettings(GameEvents.Instance.Channel.Bind())))));
  }

  // Every one of these sounds the same: what the player changed does not matter, only that
  // they changed something.
  private AutoChannel.Binding _bindSettings(AutoChannel.Binding binding) => binding
    .On((in IGameEvents.FullscreenToggled _) => _playValueChanged())
    .On((in IGameEvents.VsyncToggled _) => _playValueChanged())
    .On((in IGameEvents.ScreenSizeChanged _) => _playValueChanged())
    .On((in IGameEvents.LanguageChanged _) => _playValueChanged())
    .On((in IGameEvents.SfxVolumeChanged _) => _playValueChanged())
    .On((in IGameEvents.MusicVolumeChanged _) => _playValueChanged())
    .On((in IGameEvents.ControllerSelectionChanged _) => _playValueChanged())
    .On((in IGameEvents.ActionBound _) => _playValueChanged())
    .On((in IGameEvents.KeyboardActionBinding _) => _playValueChanged());

  private AutoChannel.Binding _bindMenus(AutoChannel.Binding binding) => binding
    .On((in IGameEvents.FocusChanged _) => OnPlaySfx("menuFocus"))
    .On((in IGameEvents.MenuBoxRotated _) => OnPlaySfx("rotateRight"))
    .On((in IGameEvents.MenuActionPressed _) => OnPlaySfx("menuSelect"))
    .On((in IGameEvents.PauseMenuEntered _) => OnPauseMenuEnter())
    .On((in IGameEvents.PauseMenuExited _) => OnPauseMenuExit())
    .On((in IGameEvents.NotificationRaised _) => OnPlaySfx("notification"))
    .On((in IGameEvents.DoorGemFilled _) => OnPlaySfx("doorGemFill"))
    .On((in IGameEvents.DoorCometFormed _) => OnPlaySfx("doorCometFormed"));

  private AutoChannel.Binding _bindMinigames(AutoChannel.Binding binding) => binding
    .On((in IGameEvents.TetrisLinesRemoved _) => OnPlaySfx("tetrisLine"))
    .On((in IGameEvents.TetrisPoolEscaped _) => OnPlaySfx("winMiniGame"))
    .On((in IGameEvents.BrickBroken _) => OnPlaySfx("brick"))
    .On((in IGameEvents.BrickBreakerStarted _) => OnPlaySfx("bricksSlide"))
    .On((in IGameEvents.BrickBreakerWon _) => OnPlaySfx("winMiniGame"))
    .On((in IGameEvents.PowerUpPicked _) => OnPlaySfx("pickup"))
    // A note is a one-shot sample, so releasing a key sounds nothing. PianoNoteReleased is
    // still announced by the piano for anything that wants it; the sound bank has no answer.
    .On((in IGameEvents.PianoNotePressed message) => OnPlaySfx("piano_" + message.NoteIndex.ToString()))
    .On((in IGameEvents.PianoNoteStruck message) => OnPlaySfx("piano_" + message.NoteIndex.ToString()))
    .On((in IGameEvents.PageFlipped _) => OnPlaySfx("pageFlip"))
    .On((in IGameEvents.WrongPianoNotePlayed _) => OnPlaySfx("wrongAnswer"))
    .On((in IGameEvents.PianoPuzzleWon _) => OnPlaySfx("success"))
    .On((in IGameEvents.ButtonGameNotePlayed message) => OnPlaySfx("piano_" + message.NoteIndex.ToString()))
    .On((in IGameEvents.ButtonGameWrongNotePlayed _) => OnPlaySfx("wrongAnswer"))
    .On((in IGameEvents.ButtonGameWon _) => OnPlaySfx("success"));

  private AutoChannel.Binding _bindPlayer(AutoChannel.Binding binding) => binding
    .On((in IGameEvents.PlayerJumped _) => OnPlaySfx("jump"))
    .On((in IGameEvents.PlayerRotated message) => OnPlayerRotate(message.Direction))
    .On((in IGameEvents.PlayerLanded _) => OnPlaySfx("land"))
    .On((in IGameEvents.PlayerDashed _) => OnPlaySfx("dash"))
    .On((in IGameEvents.PlayerExploded _) => OnPlaySfx("playerExplode"))
    .On((in IGameEvents.PlayerFell _) => OnPlaySfx("playerFalling"))
    .On((in IGameEvents.PlayerSquashed _) => OnPlaySfx("playerSquashed"))
    .On((in IGameEvents.GemCollected _) => OnPlaySfx("gemCollect"));

  private AutoChannel.Binding _bindPaint(AutoChannel.Binding binding) => binding
    .On((in IGameEvents.PaintSpilled _) => OnPlaySfx("bucketFall"))
    .On((in IGameEvents.BucketShoved _) => OnPlaySfx("bucketPush"))
    .On((in IGameEvents.PaintPouring _) => OnPlaySfx("paintPour"))
    .On((in IGameEvents.PaintSplashed _) => OnPlaySfx("paintSplash"))
    .On((in IGameEvents.PaintGunCooling _) => OnPlaySfx("gunCooldown"))
    .On((in IGameEvents.PaintGunFired _) => OnPlaySfx("shooting"));

  private void DisconnectSignals() {
    PlaySfx -= OnPlaySfx;
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
  private void _playValueChanged() => OnPlaySfx("menuValueChange");
  private void OnPauseMenuEnter() => OnPlaySfx("menuSelect");
  private void OnPauseMenuExit() => OnPlaySfx("menuSelect");
}
