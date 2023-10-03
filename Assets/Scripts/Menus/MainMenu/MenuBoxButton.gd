tool
extends Control

const LABEL_COLOR = Color("96464646")
const LABEL_COLOR_HOVER = Color("dc464646")
const LABEL_COLOR_CLICK = Color("f0464646")
const LABEL_COLOR_DISABLED = Color("46464646")

signal pressed()

export(String) var text = ""
export(bool) var disabled = false setget set_disabled, get_disabled

onready var TextureButtonNode = $CenterTexture/TextureButton
onready var LabelNode = $CenterLabel/Label
onready var BlinkTimer = $BlinkTimer

var hovering = false

func _ready():
  LabelNode.text = text
  LabelNode.modulate = LABEL_COLOR

func _on_TextureButton_pressed():
  LabelNode.modulate = LABEL_COLOR_CLICK
  BlinkTimer.start()
  emit_signal("pressed")

func _on_TextureButton_mouse_entered():
  hovering = true
  if disabled: return
  LabelNode.modulate = LABEL_COLOR_HOVER

func _on_TextureButton_mouse_exited():
  hovering = false
  if disabled: return
  LabelNode.modulate = LABEL_COLOR

func _on_BlinkTimer_timeout():
  LabelNode.modulate = LABEL_COLOR_HOVER if hovering else LABEL_COLOR
  if disabled: LabelNode.modulate = LABEL_COLOR_DISABLED

func set_disabled(value):
  disabled = value
  if disabled:
    TextureButtonNode.disabled = true
    LabelNode.modulate = LABEL_COLOR_DISABLED
  else:
    TextureButtonNode.disabled = false
    LabelNode.modulate = LABEL_COLOR_HOVER if hovering else LABEL_COLOR
  
func get_disabled():
  return disabled
