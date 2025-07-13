namespace Wfc.Core.Audio;

using System;
using System.Collections.Generic;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

[ScenePath]
public partial class MusicTrackManager : Node2D, IMusicTrackManager, IPersistent {
  public partial class Track {
    public string Name { get; }
    public AudioStreamPlayer Stream { get; }
    public float Volume { get; }

    public Track(string name, AudioStreamPlayer stream, float volume) {
      Name = name;
      Stream = stream;
      Volume = volume;
    }
  }

  private enum State {
    STOPPED,
    FADE_IN,
    PLAYING,
    FADE_OUT
  }

  private Tween? _fadeTweener;

  private Dictionary<string, Track> _musicPool = new Dictionary<string, Track>();
  private Track? _currentTrack;
  private Track? _nextTrack;
  private float _pitchScale = 1.0f;

  private const float FADE_DURATION = 1.0f;
  private const float FADE_VOLUME = -40.0f;

  private const string BUS_NAME = "music";
  private const int EFF_INDEX = 0;
  private const int NOTCH_EFF_INDEX = 1;

  private const int BUS_INDEX = 1;

  private State _currentState = State.STOPPED;

  private sealed record SaveData(string? Track = null, float Scale = 1.0f);
  private SaveData _saveData = new SaveData();



  public override void _Ready() {
    AddPitchScaleEffect();
    AddNotchEffect();
    SetPauseMenuEffect(false);
  }

  private static void AddPitchScaleEffect() {
    var shift = new AudioEffectPitchShift {
      PitchScale = 1.0f
    };
    AudioServer.AddBusEffect(BUS_INDEX, shift, EFF_INDEX);
  }

  private static void AddNotchEffect() {
    var notch = new AudioEffectNotchFilter {
      Resonance = 0.05f
    };
    AudioServer.AddBusEffect(BUS_INDEX, notch, NOTCH_EFF_INDEX);
  }

  public void SetPauseMenuEffect(bool isOn) {
    AudioServer.SetBusEffectEnabled(BUS_INDEX, NOTCH_EFF_INDEX, isOn);
  }

  public void SetPitchScale(float _pitch_scale) {
    if (_currentTrack == null)
      return;

    var shift = (AudioEffectPitchShift)AudioServer.GetBusEffect(BUS_INDEX, EFF_INDEX);
    shift.PitchScale = 1.0f / _pitch_scale;
    _currentTrack.Stream.PitchScale = _pitch_scale;
    this._pitchScale = _pitch_scale;
  }

  public void LoadTrack(string name) {
    if (GameMusicTracks.Data.TryGetValue(name, out var track)) {
      AddTrack(name, track.Path, track.Volume);
    }
    else {
      GD.PushError("Invalid track name: ", name);
      return;
    }
  }

  public void AddTrack(string name, string path, float volume) {
    if (_musicPool.ContainsKey(name))
      return;

    var stream = GD.Load<AudioStream>(path);
    var audio_player = new AudioStreamPlayer {
      Stream = stream,
      StreamPaused = false,
      Bus = BUS_NAME,
      VolumeDb = volume
    };
    audio_player.Stream.SetLooping(true);

    _musicPool[name] = new Track(name, audio_player, volume);
    AddChild(audio_player);
    audio_player.Owner = this;
  }

  public void RemoveTrack(string name) {
    if (!_musicPool.TryGetValue(name, out var value)) {
      GD.PushError("Invalid track name to remove: ", name);
      return;
    }

    var music = value;
    if (music != null) {
      RemoveChild(music.Stream);
      _musicPool.Remove(name);
    }
  }

  public void PlayTrack(string name) {
    if (!_musicPool.TryGetValue(name, out var value)) {
      GD.PushError("Invalid track name to play: ", name);
      return;
    }
    var track = value;
    if (_currentState == State.STOPPED) {
      _currentTrack = track;
      FadeIn();
    }
    else if (_currentState == State.FADE_IN) {
      _nextTrack = track;
      FadeOut();
    }
    else if (_currentState == State.FADE_OUT) {
      _nextTrack = track;
    }
    else if (_currentState == State.PLAYING && _currentTrack?.Name != track.Name) {
      _nextTrack = track;
      FadeOut();
    }
  }

  public void Stop() {
    if (_currentTrack != null) {
      _currentTrack.Stream.Stop();
      _currentTrack = null;
      _nextTrack = null;
    }
    _currentState = State.STOPPED;
  }

  private void PrepareFadeTween() {
    _fadeTweener?.Kill();
    _fadeTweener = CreateTween();
    _fadeTweener.Connect(
      Tween.SignalName.Finished,
      new Callable(this, nameof(OnTweenCompleted)),
      flags: (uint)ConnectFlags.OneShot
    );
  }

  private void FadeOut() {
    PrepareFadeTween();
    _currentState = State.FADE_OUT;
    if (_currentTrack != null) {
      var duration = FADE_DURATION * (_currentTrack.Stream.VolumeDb - FADE_VOLUME + 1.0f) / (_currentTrack.Volume - FADE_VOLUME + 1.0f);
      _fadeTweener?.TweenProperty(_currentTrack.Stream, "volume_db", FADE_VOLUME, duration);
    }
    else {
      OnTweenCompleted();
    }
  }

  private void FadeIn() {
    PrepareFadeTween();
    if (_currentTrack != null) {
      _currentState = State.FADE_IN;
      _currentTrack.Stream.Play();
      _currentTrack.Stream.VolumeDb = FADE_VOLUME;
      SetPitchScale(1.0f);
      _fadeTweener?.TweenProperty(_currentTrack.Stream, "volume_db", _currentTrack.Volume, FADE_DURATION).From(FADE_VOLUME);
    }
    else {
      _currentState = State.STOPPED;
    }
  }

  private void OnTweenCompleted() {
    if (_currentState == State.FADE_IN) {
      _currentState = State.PLAYING;
    }
    else if (_currentState == State.FADE_OUT) {
      _currentTrack?.Stream.Stop();
      if (_nextTrack != null) {
        _currentTrack = _nextTrack;
        FadeIn();
      }
      else {
        _currentState = State.STOPPED;
        _currentTrack = null;
      }
    }
  }

  private void OnCheckpointHit(Node _checkpoint) {
    if (_currentState == State.FADE_OUT) {
      if (_nextTrack != null) {
        _saveData = new SaveData(_nextTrack.Name, _pitchScale);
      }
    }
    else if (_currentState != State.STOPPED) {
      _saveData = new SaveData(_currentTrack?.Name, _pitchScale);
    }
    else {
      _saveData = new SaveData(_saveData.Track, _pitchScale);
    }
  }

  public void Reset() {
    var track = _saveData.Track;
    if (track != null) {
      if (_currentTrack != null && track == _currentTrack.Name) {
        return;
      }
      LoadTrack(track);
      PlayTrack(track);
    }
    SetPitchScale(_saveData.Scale);
  }

  public override void _EnterTree() {
    // AddToGroup("persist");
    EventHandler.Instance.Connect(EventType.CheckpointReached, new Callable(this, nameof(OnCheckpointHit)));
    EventHandler.Instance.Connect(EventType.CheckpointLoaded, new Callable(this, nameof(Reset)));
  }

  public override void _ExitTree() {
    EventHandler.Instance.Disconnect(EventType.CheckpointReached, new Callable(this, nameof(OnCheckpointHit)));
    EventHandler.Instance.Disconnect(EventType.CheckpointLoaded, new Callable(this, nameof(Reset)));
  }

  public string GetSaveId() => this.GetPath();
  public string Save(ISerializer serializer) => serializer.Serialize(_saveData);
  public void Load(ISerializer serializer, string data) {
    var deserializedData = serializer.Deserialize<SaveData>(data);
    this._saveData = deserializedData ?? new SaveData();
    Reset();
  }
}
