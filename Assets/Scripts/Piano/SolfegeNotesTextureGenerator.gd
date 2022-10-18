class_name SolfegeNotesTextureGenerator

const SOLFEGE_KEY_OFFSET = 67
const NOTE_SPRITE_WIDTH =  39
const BACKGROUND_COLOR = Color("fdfbe7")

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

func _init():
  pass
  
func create_from_notes(notes_array: Array, texture_size: Vector2) -> Texture:
  var notes_textures = _generate_notes_texture_array(notes_array)
  var texture = _generate_texture(notes_textures, texture_size)
  return texture
  
func _generate_notes_texture_array(notes_array: Array):
  var texture_array = []
  for note in notes_array:
    var texture = NOTES_TEXTURES[note]
    texture_array.append(texture)
  return texture_array

func _generate_texture(notes_textures: Array ,texture_size: Vector2) -> Texture:
  var image_texture = ImageTexture.new()
  var image = Image.new()
  var format = SOLFEGE_TEXTURE.get_data().get_format()
  image.create(texture_size.x, texture_size.y, false, format)
  image.fill(BACKGROUND_COLOR)
  var offset_x = (texture_size.x - SOLFEGE_TEXTURE.get_width()) * 0.5
  var offset_y = (texture_size.y - SOLFEGE_TEXTURE.get_height()) * 0.5
  _blit_texture(image, SOLFEGE_TEXTURE, Vector2(offset_x, offset_y))
  var note_offset = SOLFEGE_KEY_OFFSET
  for note in notes_textures:
    _blit_texture(image, note, Vector2(offset_x + note_offset, offset_y))
    note_offset += NOTE_SPRITE_WIDTH
  image_texture.create_from_image(image)
  return image_texture

func _blit_texture(dest_image: Image, src_texture: Texture, pos: Vector2):
  var bounds = Vector2(dest_image.get_width(), dest_image.get_height())
  var src_image = src_texture.get_data()
  var src_bound_x = min(pos.x + src_image.get_width(), bounds.x)
  var src_bound_y = min(pos.y + src_image.get_height(), bounds.y)
  var src_rect = Rect2(Vector2(0, 0), Vector2(src_bound_x, src_bound_y))
  dest_image.blit_rect_mask(src_image, src_image, src_rect, pos) 
