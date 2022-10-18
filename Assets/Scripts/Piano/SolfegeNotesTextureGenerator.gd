class_name SolfegeNotesTextureGenerator

const SOLFEGE_KEY_OFFSET = 67
const BACKGROUND_COLOR = Color("fdfbe7")

var background_width: float = 0;
var width: float = 0
var height: float = 0
var bg_notes: Sprite = null
var notes = []
var texture_offset_x = 0 #offset used when generating texture
var texture_offset_y = 0 #offset used when generating texture
var current_texture: Texture

const SolfegeNoteSpriteScene = preload("res://Assets/Scenes/Piano/SulfegeNoteSprite.tscn")

func _init():
  pass
  
func create_from_notes(notes_array: Array, texture_size: Vector2) -> Texture:
  var solfege_sprite = _create_solfege_sprite()
  var note_sprites = _generate_sprites_from_notes_array(notes_array)
  _add_background(solfege_sprite, SOLFEGE_KEY_OFFSET)
  _add_note_sprites(note_sprites)
  var texture = generate_texture(texture_size)
  current_texture = texture
  return texture
  
func _create_solfege_sprite():
  var sprite = SolfegeNoteSpriteScene.instance()
  return sprite
  
func _generate_sprites_from_notes_array(notes_array: Array):
  var sprites_array = []
  for note in notes_array:
    var sprite = SolfegeNoteSpriteScene.instance()
    sprite.note = note
    sprites_array.append(sprite)
  return sprites_array

func _add_background(bg_sprite: Sprite, offset_x: int):
  background_width = offset_x
  width += offset_x
  height = max(height, bg_sprite.texture.get_height())
  _remove_background()
  bg_notes = bg_sprite

func _add_note_sprite(note_sprite: Sprite):
  note_sprite.position.x = width
  width += note_sprite.texture.get_width()
  height = max(height, note_sprite.texture.get_height())
  notes.append(note_sprite)

func _remove_all_note_sprites():
  for note in notes:
    note.queue_free()
  notes = []
  width = background_width
  height = 0

func _add_note_sprites(note_sprites: Array):
  _remove_all_note_sprites()
  for note_sprite in note_sprites:
    _add_note_sprite(note_sprite)

func _remove_background():
  if bg_notes != null:
    bg_notes.queue_free()

func _get_note_position_of_index(index):
  var start_offset = SOLFEGE_KEY_OFFSET + texture_offset_x
  if notes != null:
    for note in notes:
      index -= 1
      if index < 0:
        break
      start_offset += note.texture.get_width() 
  return Vector2(start_offset, 0)

func generate_texture(texture_size: Vector2) -> Texture:
  var image_texture = ImageTexture.new()
  var image = Image.new()
  var format = bg_notes.texture.get_data().get_format()
  image.create(texture_size.x, texture_size.y, false, format)
  image.fill(BACKGROUND_COLOR)
  texture_offset_x = (texture_size.x - width) * 0.5
  texture_offset_y = (texture_size.y - height) * 0.5
  _blit_texture(image, bg_notes.texture, Vector2(texture_offset_x, texture_offset_y))
  for note in notes:
    _blit_texture(image, note.texture, Vector2(texture_offset_x, texture_offset_y) + note.position)
  image_texture.create_from_image(image)
  return image_texture

func _blit_texture(dest_image: Image, src_texture: Texture, pos: Vector2):
  var bounds = Vector2(dest_image.get_width(), dest_image.get_height())
  var src_image = src_texture.get_data()
  var src_bound_x = min(pos.x + src_image.get_width(), bounds.x)
  var src_bound_y = min(pos.y + src_image.get_height(), bounds.y)
  var src_rect = Rect2(Vector2(0, 0), Vector2(src_bound_x, src_bound_y))
  dest_image.blit_rect_mask(src_image, src_image, src_rect, pos) 
