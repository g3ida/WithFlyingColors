using Godot;
using System;
using System.Collections.Generic;
using Wfc.Core.Event;

public partial class MusicTrackManager : Node2D, IPersistent {
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

  private Tween fade_tweener;

  private Dictionary<string, Track> music_pool = new Dictionary<string, Track>();
  private Track current_track = null;
  private Track next_track = null;
  private float pitch_scale = 1.0f;

  private const float FADE_DURATION = 1.0f;
  private const float FADE_VOLUME = -40.0f;

  private const string BUS_NAME = "music";
  private const int EFF_INDEX = 0;
  private const int NOTCH_EFF_INDEX = 1;

  private const int BUS_INDEX = 1;

  private State current_state = State.STOPPED;

  private Dictionary<string, object> save_data = new Dictionary<string, object>
    {
        { "track", null },
        { "scale", 1.0f }
    };

  private Dictionary<string, Dictionary<string, object>> track_list = new Dictionary<string, Dictionary<string, object>>
    {
        {
            "brickBreaker", new Dictionary<string, object>
            {
                { "path", "res://Assets/music/Enigma-Long-Version-Complete-Version.mp3" },
                { "license", "Creative Commons CC BY 3.0" },
                { "link", "https://www.chosic.com/download-audio/32067/" },
                { "volume", -8.0f }
            }
        },
        {
            "fight", new Dictionary<string, object>
            {
                { "path", "res://Assets/music/Loyalty_Freak_Music_-_04_-_Cant_Stop_My_Feet_.mp3" },
                { "license", "Public domain CC0" },
                { "link", "https://www.chosic.com/download-audio/25495/" },
                { "volume", -5.0f }
            }
        },
        {
            "level1", new Dictionary<string, object>
            {
                { "path", "res://Assets/music/Loyalty Freak Music - Monarch of the street.ogg" },
                { "license", "Public domain CC0" },
                { "link", "https://freemusicarchive.org/music/Loyalty_Freak_Music/TO_CHILL_AND_STAY_AWAKE/Loyalty_Freak_Music_-_TO_CHILL_AND_STAY_AWAKE_-_07_Monarch_of_the_street/" },
                { "volume", -7.0f }
            }
        },
        {
            "tetris", new Dictionary<string, object>
            {
                { "path", "res://Assets/music/Myuu-Tetris-Dark-Version.mp3" },
                { "license", "free to use as long as credit is given" },
                { "link", "https://www.youtube.com/watch?v=eunhYtd8agE&ab_channel=Myuu" },
                { "volume", -5.0f }
            }
        },
        {
            "cards", new Dictionary<string, object>
            {
                { "path", "res://Assets/music/Sneaky-Snitch.mp3" },
                { "license", "Creative Commons CC BY 3.0" },
                { "link", "https://www.chosic.com/download-audio/39325/" },
                { "volume", -5.0f }
            }
        }
    };

  public override void _Ready() {
    AddPitchScaleEffect();
    AddNotchEffect();
    SetPauseMenuEffect(false);
  }

  private void AddPitchScaleEffect() {
    var shift = new AudioEffectPitchShift {
      PitchScale = 1.0f
    };
    AudioServer.AddBusEffect(BUS_INDEX, shift, EFF_INDEX);
  }

  private void AddNotchEffect() {
    var notch = new AudioEffectNotchFilter {
      Resonance = 0.05f
    };
    AudioServer.AddBusEffect(BUS_INDEX, notch, NOTCH_EFF_INDEX);
  }

  public void SetPauseMenuEffect(bool is_on) {
    AudioServer.SetBusEffectEnabled(BUS_INDEX, NOTCH_EFF_INDEX, is_on);
  }

  public void SetPitchScale(float _pitch_scale) {
    if (current_track == null)
      return;

    var shift = (AudioEffectPitchShift)AudioServer.GetBusEffect(BUS_INDEX, EFF_INDEX);
    shift.PitchScale = 1.0f / _pitch_scale;
    current_track.Stream.PitchScale = _pitch_scale;
    pitch_scale = _pitch_scale;
  }

  public void LoadTrack(string name) {
    if (track_list.ContainsKey(name)) {
      var volume = track_list[name].ContainsKey("volume") ? Convert.ToSingle(track_list[name]["volume"]) : 0.0f;
      AddTrack(name, track_list[name]["path"].ToString(), volume);
    }
  }

  public void AddTrack(string name, string path, float volume) {
    if (music_pool.ContainsKey(name))
      return;

    var stream = GD.Load<AudioStream>(path);
    var audio_player = new AudioStreamPlayer {
      Stream = stream,
      StreamPaused = false,
      Bus = BUS_NAME,
      VolumeDb = volume
    };
    Helpers.SetLooping(audio_player.Stream, true);

    music_pool[name] = new Track(name, audio_player, volume);
    AddChild(audio_player);
    audio_player.Owner = this;
  }

  public void RemoveTrack(string name) {
    if (!music_pool.ContainsKey(name))
      return;

    var music = music_pool[name];
    if (music != null) {
      RemoveChild(music.Stream);
      music_pool.Remove(name);
    }
  }

  public void PlayTrack(string name) {
    if (!music_pool.ContainsKey(name))
      return;
    var track = music_pool[name];
    if (current_state == State.STOPPED) {
      current_track = track;
      FadeIn();
    }
    else if (current_state == State.FADE_IN) {
      next_track = track;
      FadeOut();
    }
    else if (current_state == State.FADE_OUT) {
      next_track = track;
    }
    else if (current_state == State.PLAYING && current_track.Name != track.Name) {
      next_track = track;
      FadeOut();
    }
  }

  public void Stop() {
    if (current_track != null) {
      current_track.Stream.Stop();
      current_track = null;
      next_track = null;
    }
    current_state = State.STOPPED;
  }

  private void PrepareFadeTween() {
    fade_tweener?.Kill();
    fade_tweener = CreateTween();
    fade_tweener.Connect("finished", new Callable(this, nameof(OnTweenCompleted)), flags: (uint)ConnectFlags.OneShot);
  }

  private void FadeOut() {
    PrepareFadeTween();
    current_state = State.FADE_OUT;
    var duration = FADE_DURATION * (current_track.Stream.VolumeDb - FADE_VOLUME + 1.0f) / (current_track.Volume - FADE_VOLUME + 1.0f);
    fade_tweener.TweenProperty(current_track.Stream, "volume_db", FADE_VOLUME, duration);
  }

  private void FadeIn() {
    PrepareFadeTween();
    current_state = State.FADE_IN;
    current_track.Stream.Play();
    current_track.Stream.VolumeDb = FADE_VOLUME;
    SetPitchScale(1.0f);
    fade_tweener.TweenProperty(current_track.Stream, "volume_db", current_track.Volume, FADE_DURATION).From(FADE_VOLUME);
  }

  private void OnTweenCompleted() {
    if (current_state == State.FADE_IN) {
      current_state = State.PLAYING;
    }
    else if (current_state == State.FADE_OUT) {
      current_track.Stream.Stop();
      if (next_track != null) {
        current_track = next_track;
        FadeIn();
      }
      else {
        current_state = State.STOPPED;
        current_track = null;
      }
    }
  }

  private void OnCheckpointHit(Node _checkpoint) {
    if (current_state == State.FADE_OUT) {
      if (next_track != null) {
        save_data["track"] = next_track.Name;
      }
    }
    else if (current_state != State.STOPPED) {
      save_data["track"] = current_track.Name;
    }
    save_data["scale"] = pitch_scale;
  }

  // FIXME: rename to Reset after C# migration
  public void reset() {
    var track = Convert.ToString(save_data["track"]);
    if (track != null) {
      if (current_track != null && track == current_track.Name) {
        return;
      }
      LoadTrack(track);
      PlayTrack(track);
    }
    SetPitchScale(Convert.ToSingle(save_data["scale"]));
  }

  // FIXME: this has to be uppercase after C# migration
  public Dictionary<string, object> save() {
    return save_data;
  }

  public override void _EnterTree() {
    // AddToGroup("persist");
    Event.Instance.Connect(EventType.CheckpointReached, new Callable(this, nameof(OnCheckpointHit)));
    Event.Instance.Connect(EventType.CheckpointLoaded, new Callable(this, nameof(reset)));
  }

  public override void _ExitTree() {
    Event.Instance.Disconnect(EventType.CheckpointReached, new Callable(this, nameof(OnCheckpointHit)));
    Event.Instance.Disconnect(EventType.CheckpointLoaded, new Callable(this, nameof(reset)));
  }

  public void load(Dictionary<string, object> save_data) {
    this.save_data = save_data;
    reset();
  }
}
