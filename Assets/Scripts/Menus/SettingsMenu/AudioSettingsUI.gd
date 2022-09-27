extends Control

onready var SfxSliderNode = $GridContainer/SfxSlider
onready var MusicSliderNode = $GridContainer/MusicSlider

func _ready():
  SfxSliderNode.value = Settings.sfx_volume
  MusicSliderNode.value = Settings.music_volume

func _on_SfxSlider_drag_ended(_value):
  Settings.sfx_volume = SfxSliderNode.value
  Event.emit_sfx_volume_changed(SfxSliderNode.value)

func _on_MusicSlider_drag_ended(_value):
  Settings.music_volume = MusicSliderNode.value
  Event.emit_music_volume_changed(MusicSliderNode.value)
