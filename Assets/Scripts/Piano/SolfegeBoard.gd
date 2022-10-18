extends Node2D

const DURATION = 0.8
const NotesCursorScene = preload("res://Assets/Scenes/Piano/NotesCursor.tscn")
const MusicPaperRectTexture = preload("res://Assets/Sprites/Piano/music-paper-rect.png")

signal board_notes_played()

const PAGES = [
  ["do", "do", "sol", "sol", "la", "la", "sol"],
  ["sol", "sol", "mi", "mi", "re", "re", "do"],
  ["sol", "sol", "fa", "fa", "mi", "mi", "re"],
  ["sol", "sol", "fa", "fa", "mi", "mi", "re"],
  ["do", "do", "sol", "sol", "la", "la", "sol"],
  ["sol", "sol", "mi", "mi", "re", "re", "do"],
]
var NUM_PAGES = PAGES.size()

enum BoardState {
  STOPPED,
  PLAYING,
  FINISHED
}

onready var MusicPaperRectNode = $MusicPaperRect
onready var music_paper_rect_size = Vector2(MusicPaperRectNode.texture.get_width(), MusicPaperRectNode.texture.get_height())
onready var time = 0

var solfegeNotesTextureGenerator = SolfegeNotesTextureGenerator.new()

var is_flipping = false
var current_page = 0
var current_note_index = 0
var current_state = BoardState.PLAYING
var notes_cursor = null
var current_texture = MusicPaperRectTexture

func _ready():
  _init_shader()
  _init_state()

func flip_next_page():
  current_page += 1
  if current_page >= NUM_PAGES:
    current_state = BoardState.FINISHED
    emit_signal("board_notes_played")
    _set_flip_page_shader(MusicPaperRectTexture)
    _init_state()
  else:
    var solfege_texture = solfegeNotesTextureGenerator.create_from_notes(PAGES[current_page], music_paper_rect_size)
    _set_flip_page_shader(solfege_texture)

func _set_flip_page_shader(next_texture):
  MusicPaperRectNode.material.set_shader_param("flip_left", true)
  MusicPaperRectNode.material.set_shader_param("cylinder_direction", Vector2(5.0, 1.0))
  current_texture = current_texture 
  MusicPaperRectNode.material.set_shader_param("current_page", current_texture)
  MusicPaperRectNode.material.set_shader_param("next_page", next_texture)
  time = 0.0
  is_flipping = true
  Event.emit_page_flipped()

func _init_shader():
  MusicPaperRectNode.material.set_shader_param("time", 0)
  MusicPaperRectNode.material.set_shader_param("flip_duration", DURATION)
  MusicPaperRectNode.material.set_shader_param("cylinder_ratio", 0.3)
  MusicPaperRectNode.material.set_shader_param("rect", music_paper_rect_size)

func _process(delta):
  if is_flipping:
    time += delta
    MusicPaperRectNode.material.set_shader_param("time", time)
    if time > DURATION:
      is_flipping = false
      MusicPaperRectNode.texture = current_texture

func _enter_tree():
  var __ = Event.connect("piano_note_pressed", self, "_on_note_pressed")
  __ = Event.connect("checkpoint_reached", self, "_on_checkpoint_hit")
  __ = Event.connect("checkpoint_loaded", self, "reset")

func _exit_tree():
  Event.disconnect("piano_note_pressed", self, "_on_note_pressed")
  Event.disconnect("checkpoint_reached", self, "_on_checkpoint_hit")
  Event.disconnect("checkpoint_loaded", self, "reset")

func _init_state():
  if current_state == BoardState.PLAYING:
    current_note_index = 0
    current_page = 0
    current_texture = MusicPaperRectNode.texture
    var reset_texture = solfegeNotesTextureGenerator.create_from_notes(PAGES[current_page], music_paper_rect_size)
    _set_flip_page_shader(reset_texture)
    MusicPaperRectNode.material.set_shader_param("current_page", current_texture)
    if notes_cursor != null:
      notes_cursor.queue_free()
      notes_cursor = null
    notes_cursor = NotesCursorScene.instance()
    MusicPaperRectNode.add_child(notes_cursor)
    notes_cursor.set_owner(MusicPaperRectNode)
    _set_notes_cursor_position()
  elif current_state == BoardState.FINISHED:
    if notes_cursor != null:
      notes_cursor.queue_free()
      notes_cursor = null
      MusicPaperRectNode.texture = MusicPaperRectTexture
    
func _get_note_position_from_index(note_index):
  var gen = solfegeNotesTextureGenerator
  var x = gen.SOLFEGE_KEY_OFFSET + note_index * gen.NOTE_SPRITE_WIDTH
  var y = 0
  return Vector2(x, y)

func _set_notes_cursor_position():
  if notes_cursor != null:
    var pos = _get_note_position_from_index(current_note_index)
    notes_cursor.move_to_position(pos)

func reset():
  _init_state()

func _on_note_pressed(note):
  if current_state != BoardState.PLAYING:
    return
  if note == PAGES[current_page][current_note_index]:
    current_note_index += 1
    if current_note_index >= PAGES[current_page].size():
      current_note_index = 0
      flip_next_page()
  else: # wrong note pressed
    current_note_index = 0
    _emit_wrong_note_event()
  _set_notes_cursor_position()

func _emit_wrong_note_event():
  Event.emit_wrong_piano_note_played()
