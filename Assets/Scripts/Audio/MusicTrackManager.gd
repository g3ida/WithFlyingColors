extends Node2D

onready var fadeTween = $Fade

var music_pool: Dictionary = {}
var is_switching = false
var current_track: AudioStreamPlayer = null

func _ready():
  var shift = AudioEffectPitchShift.new()
  shift.pitch_scale = 1.0
  AudioServer.add_bus_effect(BUS_INDEX, shift, EFF_INDEX)

const FADE_DURATION = 3.0
const FADE_VOLUME = -30.0

const BUS_NAME  = "music"
const EFF_INDEX = 0
const BUS_INDEX = 1
var pitch_scale = 2.0

func set_pitch_scale(_pitch_scale):
  var shift = AudioServer.get_bus_effect(BUS_INDEX, EFF_INDEX)
  shift.pitch_scale = 1.0 / _pitch_scale
  current_track.pitch_scale = _pitch_scale
  pitch_scale = _pitch_scale
  
func add_track(name: String, path: String, volume: float):
  var stream = load(path)
  var audio_player := AudioStreamPlayer.new()
  audio_player.stream = stream
  audio_player.stream.set_loop(true)
  audio_player.bus = BUS_NAME
  music_pool[name] = {"player": audio_player, "volume": volume}
  audio_player.volume_db = volume
  add_child(audio_player)
  
func remove_track(name: String) -> void:
  if music_pool.has(name):
    var music = music_pool[name]
    if music != null:
      remove_child(music["player"])
      var __ = music_pool.erase(name)
  
func play_track(name: String):
  if (is_switching): return
  is_switching = true
  if (current_track != null):
    fade_out()
    yield(fadeTween, "tween_completed")
    current_track.stop()
  if music_pool.has(name):
    var track = music_pool[name]
    current_track = track["player"] 
    var volume = track["volume"]
    set_pitch_scale(1.0)
    current_track.volume_db = FADE_VOLUME
    current_track.play()
    fade_in(volume)
    yield(fadeTween, "tween_completed")
  is_switching =false
  
func stop():
  current_track.stop()

func fade_out():
  fadeTween.interpolate_property(current_track, "volume_db", current_track.volume_db, FADE_VOLUME, FADE_DURATION)
  fadeTween.start()
func fade_in(volume):
  fadeTween.interpolate_property(current_track, "volume_db", FADE_VOLUME, volume, FADE_DURATION)
  fadeTween.start()