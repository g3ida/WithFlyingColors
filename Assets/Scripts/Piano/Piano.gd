extends Node2D

const notes = ["do", "re", "mi", "fa", "sol", "la", "si"]

func _ready():
  pass

func _on_piano_note_pressed(note):
  var index = note.index - 1
  Event.emit_piano_note_pressed(notes[index])

func _on_piano_note_released(note):
  var index = note.index - 1
  Event.emit_piano_note_released(notes[index])
