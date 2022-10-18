class_name SolfegeNoteSprite
extends Sprite

const SOLFEGE_TEXTURE = preload("res://Assets/Sprites/Piano/sheet/key-sol.png")

const NOTES_TEXTURES = {
  "do": preload("res://Assets/Sprites/Piano/sheet/do.png"),
  "re": preload("res://Assets/Sprites/Piano/sheet/re.png"),
  "mi": preload("res://Assets/Sprites/Piano/sheet/mi.png"),
  "fa": preload("res://Assets/Sprites/Piano/sheet/fa.png"),
  "sol": preload("res://Assets/Sprites/Piano/sheet/sol.png"),
  "la": preload("res://Assets/Sprites/Piano/sheet/la.png"),
  "si": preload("res://Assets/Sprites/Piano/sheet/si.png")
}

export(String) var note setget set_note, get_note

func _ready():
  self.centered = false

func set_note(note_value):
  self.texture = NOTES_TEXTURES[note_value]
  self.centered = false
  note = note_value

func get_note():
  return note
