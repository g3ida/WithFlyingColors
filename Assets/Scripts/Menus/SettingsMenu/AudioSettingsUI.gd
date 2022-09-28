extends Control

onready var SfxSliderNode = $GridContainer/SfxSlider
onready var MusicSliderNode = $GridContainer/MusicSlider

func _ready():
  SfxSliderNode.value = Settings.sfx_volume
  MusicSliderNode.value = Settings.music_volume

func _on_SfxSlider_drag_ended(_value):
  Settings.sfx_volume = SfxSliderNode.value
  Event.emit_sfx_volume_changed(SfxSliderNode.value)

func _on_SfxSliderButton_value_changed(value):
  Settings.sfx_volume = value
  Event.emit_sfx_volume_changed(value)

func _on_MusicSlider_value_changed(value):
  Settings.music_volume = value
  Event.emit_music_volume_changed(value)

func on_gain_focus():
  SfxSliderNode.grab_focus()

#just for the audio player to play the click sfx
func _on_SfxSlider_selection_changed(_is_selected):
  Event.emit_music_volume_changed(Settings.sfx_volume)

#just for the audio player to play the click sfx
func _on_MusicSlider_selection_changed(_is_selected):
  Event.emit_music_volume_changed(Settings.music_volume)
