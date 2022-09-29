extends Node2D

class Track:
  func _init(_name: String, _stream: AudioStreamPlayer, _volume: float):
    name = _name; stream = _stream; volume = _volume
  var name: String
  var stream: AudioStreamPlayer
  var volume: float

enum State {
  STOPPED,
  FADE_IN,
  PLAYING,
  FADE_OUT
}

onready var fadeTween = $Fade

var music_pool: Dictionary = {}
var current_track: Track = null
var next_track: Track = null
var pitch_scale = 1.0

const FADE_DURATION = 1.0
const FADE_VOLUME = -40.0

const BUS_NAME  = "music"
const EFF_INDEX = 0
const NOTCH_EFF_INDEX = 1

const BUS_INDEX = 1

var current_state = State.STOPPED

var save_data = {
  "track": null,
  "scale": 1.0,
}

var track_list = {
  "brickBreaker": {
    "path": "res://Assets/music/Enigma-Long-Version-Complete-Version.mp3",
    "licence": "Creative Commons CC BY 3.0",
    "link": "https://www.chosic.com/download-audio/32067/",
    "volume": -8.0
  },
  "fight": {
    "path": "res://Assets/music/Loyalty_Freak_Music_-_04_-_Cant_Stop_My_Feet_.mp3",
    "licence": "Public domain CC0",
    "link": "https://www.chosic.com/download-audio/25495/",
    "volume": -5.0
  },
  "level1": {
    "path": "res://Assets/music/Loyalty Freak Music - Monarch of the street.ogg",
    "licence": "Public domain CC0",
    "link": "https://freemusicarchive.org/music/Loyalty_Freak_Music/TO_CHILL_AND_STAY_AWAKE/Loyalty_Freak_Music_-_TO_CHILL_AND_STAY_AWAKE_-_07_Monarch_of_the_street/",
    "volume": -7.0
  },
  "tetris": {
    "path": "res://Assets/music/Myuu-Tetris-Dark-Version.mp3",
    "licence": "free to use as long as credit is given",
    "link": "https://www.youtube.com/watch?v=eunhYtd8agE&ab_channel=Myuu",
    "volume": -5.0
  },
  "cards": {
    "path": "res://Assets/music/Sneaky-Snitch.mp3",
    "licence": "Creative Commons CC BY 3.0",
    "link": "https://www.chosic.com/download-audio/39325/",
    "volume": -5.0
  }
}

func _ready():
  _add_pitch_scale_effect()
  _add_notch_effect()
  set_pause_menu_effect(false)

func _add_pitch_scale_effect():
  var shift = AudioEffectPitchShift.new()
  shift.pitch_scale = 1.0
  AudioServer.add_bus_effect(BUS_INDEX, shift, EFF_INDEX)

func _add_notch_effect():
  var notch = AudioEffectNotchFilter.new()
  notch.resonance = 0.05
  AudioServer.add_bus_effect(BUS_INDEX, notch, NOTCH_EFF_INDEX)

func set_pause_menu_effect(is_on: bool):
  AudioServer.set_bus_effect_enabled(BUS_INDEX, NOTCH_EFF_INDEX, is_on)

func set_pitch_scale(_pitch_scale):
  if current_track == null: return
  # see https://godotengine.org/qa/88935/how-can-i-change-speed-of-an-audio-without-changing-its-pitch
  var shift = AudioServer.get_bus_effect(BUS_INDEX, EFF_INDEX)
  shift.pitch_scale = 1.0 / _pitch_scale
  current_track.stream.pitch_scale = _pitch_scale
  pitch_scale = _pitch_scale

func load_track(name: String):
  if track_list.has(name):
    var volume := 0.0
    if track_list[name].has("volume"):
      volume =track_list[name]["volume"]
    add_track(name, track_list[name]["path"], volume)
  
func add_track(name: String, path: String, volume: float):
  if music_pool.has(name):
    return
  var stream = load(path)
  var audio_player := AudioStreamPlayer.new()
  audio_player.stream = stream
  audio_player.stream.set_loop(true)
  audio_player.bus = BUS_NAME
  music_pool[name] = Track.new(name, audio_player, volume)
  audio_player.volume_db = volume
  add_child(audio_player)
  audio_player.set_owner(self)
  
func remove_track(name: String) -> void:
  if music_pool.has(name):
    var music = music_pool[name]
    if music != null:
      remove_child(music.player)
      var __ = music_pool.erase(name)

func play_track(name: String):
  if !music_pool.has(name):
    return
  var track = music_pool[name]
  if current_state == State.STOPPED:
    current_track = track
    fade_in()
  elif current_state == State.FADE_IN:
    next_track = track
    fade_out()
  elif current_state == State.FADE_OUT:
    next_track = track
  elif current_state == State.PLAYING:
    if current_track.name != track.name:
      next_track = track
      fade_out()

func stop():
  if current_track != null:
    current_track.stream.stop()
    current_track = null
    next_track = null
  current_state = State.STOPPED

func fade_out():
  fadeTween.remove_all()
  current_state = State.FADE_OUT
  # this is useful in case we changed track during the fade in of one other track so we don't want
  # to wait the whole duration. This is usually the case when loading a checkpoint
  var duration = FADE_DURATION*(current_track.stream.volume_db - FADE_VOLUME+1.0)/(current_track.volume - FADE_VOLUME+1.0)
  fadeTween.interpolate_property(current_track.stream, "volume_db", current_track.stream.volume_db, FADE_VOLUME, duration)
  fadeTween.start()

func fade_in():
  fadeTween.remove_all()
  current_state = State.FADE_IN
  current_track.stream.play()
  current_track.stream.volume_db = FADE_VOLUME
  set_pitch_scale(1.0)
  fadeTween.interpolate_property(current_track.stream, "volume_db", FADE_VOLUME, current_track.volume, FADE_DURATION)
  fadeTween.start()

func _tween_completed(_object, _key):
  if current_state == State.FADE_IN:
    current_state = State.PLAYING
  elif current_state == State.FADE_OUT:
    current_track.stream.stop()
    if next_track != null:
      current_track = next_track
      fade_in()
    else:
      current_state = State.STOPPED
      current_track = null
    
func _on_checkpoint_hit(_checkpoint):
  if current_state == State.FADE_OUT:
    if next_track != null:
      save_data["track"] = next_track.name
  elif current_state != State.STOPPED:     
    save_data["track"] = current_track.name
  save_data["scale"] = pitch_scale

func reset():
  var track = save_data["track"]
  if track != null and current_track != null and track != current_track.name:
    load_track(track)
    play_track(track)
  set_pitch_scale(save_data["scale"])

func save():
  return save_data

func _enter_tree():
  var __ = Event.connect("checkpoint_reached", self, "_on_checkpoint_hit")
  __ = Event.connect("checkpoint_loaded", self, "reset")

func _exit_tree():
  Event.disconnect("checkpoint_reached", self, "_on_checkpoint_hit")
  Event.disconnect("checkpoint_loaded", self, "reset")

  