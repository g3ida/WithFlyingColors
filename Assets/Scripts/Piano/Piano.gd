extends Node2D

const NotesPointerScene = preload("res://Assets/Scenes/Piano/NextNotePointer.tscn")
const notes = ["do", "re", "mi", "fa", "sol", "la", "si"]

onready var PianoNotesNodes = $NotesContainer.get_children()
onready var SolfegeBoardNode = $SolfegeBoard
onready var lettersContainerNode = $LettersContainer

var NotesPointerNode = null

func _ready():
  pass

func _on_piano_note_pressed(note):
  var index = note.index - 1
  Event.emit_piano_note_pressed(notes[index])

func _on_piano_note_released(note):
  var index = note.index - 1
  Event.emit_piano_note_released(notes[index])

func _on_SolfegeBoard_board_notes_played():
  Event.emit_piano_puzzle_won()
  _remove_pointer_node()

func _remove_pointer_node():
  if NotesPointerNode != null:
    NotesPointerNode.queue_free()
    NotesPointerNode = null

func _start_game():
  if NotesPointerNode != null and NotesPointerNode.is_inside_tree():
    NotesPointerNode.queue_free()
  NotesPointerNode = _instance_notes_pointer()
  SolfegeBoardNode._start_game()
  var expected_note = SolfegeBoardNode.get_expected_note()
  _update_notes_pointer_position(expected_note)

func _instance_notes_pointer():
  var node = NotesPointerScene.instance()
  lettersContainerNode.add_child(node)
  node.set_owner(lettersContainerNode)
  return node

func _on_SolfegeBoard_expected_note_changed(new_expected_note):
  _update_notes_pointer_position(new_expected_note)

func _update_notes_pointer_position(new_expected_note):
  if NotesPointerNode != null:
    var note = _get_note_node(new_expected_note)
    if note != null:
      NotesPointerNode.move_to_position(Vector2(note.position.x, 0))

func _on_SolfegeBoard_wrong_note_played():
  pass # Replace with function body.

func _get_note_node(new_expected_note: String):
  var idx = notes.find(new_expected_note)
  if idx != -1:
    for note in PianoNotesNodes:
      if note.index == idx+1:
        return note
    return null

func reset():
  _remove_pointer_node()

func _setup_note_pointer_position():
  _update_notes_pointer_position(SolfegeBoardNode.get_expected_note())

func _enter_tree():
  var __ = Event.connect("checkpoint_loaded", self, "reset")

func _exit_tree():
  Event.disconnect("checkpoint_loaded", self, "reset")

func is_stopped():
  return SolfegeBoardNode._is_stopped()
