class_name PlayerSpriteGenerator

const SPRITE_SIZE = Vector2(96, 96)

enum ImageAlignment {
  TOP_LEFT,
  TOP_RIGHT,
  BOTTOM_LEFT,
  BOTTOM_RIGHT
}

const FACES_TEXTURES = {
    "left": {
      "texture": preload("res://Assets/Sprites/Player/player-left.png"),
      "color": "3-basic",
      "align": ImageAlignment.TOP_LEFT,
    },
    "top": {
      "texture": preload("res://Assets/Sprites/Player/player-top.png"),
      "color": "2-basic",
      "align": ImageAlignment.TOP_RIGHT,
    },
    "bottom": {
      "texture": preload("res://Assets/Sprites/Player/player-bottom.png"),
      "color": "0-basic",
      "align": ImageAlignment.BOTTOM_RIGHT,
    },
    "right": {
      "texture": preload("res://Assets/Sprites/Player/player-right.png"),
      "color": "1-basic",
      "align": ImageAlignment.TOP_RIGHT,
    },
    "left-edge": {
      "texture": preload("res://Assets/Sprites/Player/player-left-edge.png"),
      "color": "3-dark",
      "align": ImageAlignment.TOP_LEFT,
    },
    "right-edge": {
      "texture": preload("res://Assets/Sprites/Player/player-right-edge.png"),
      "color": "1-dark",
      "align": ImageAlignment.TOP_RIGHT,
    },
    "bottom-edge": {
      "texture": preload("res://Assets/Sprites/Player/player-bottom-edge.png"),
      "color": "0-dark",
      "align": ImageAlignment.BOTTOM_LEFT,
    },
    "top-edge": {
      "texture": preload("res://Assets/Sprites/Player/player-top-edge.png"),
      "color": "2-dark",
      "align": ImageAlignment.TOP_LEFT,
    }
}

static func get_texture():
  if Global.selected_skin == null:
    push_error("there are no selected skin !!!")
    return null
  return _merge_into_single_texture()

static func _merge_into_single_texture():
  var image_texture = ImageTexture.new()
  var image = _merge_into_single_image()
  image_texture.create_from_image(image, 5) #needed to disble FLAG_REPEAT that creates some issues
  return image_texture

static func _merge_into_single_image():
  var image = Image.new()
  var format = Image.FORMAT_RGBA8
  image.create(SPRITE_SIZE.x, SPRITE_SIZE.y, false, format)
  image.fill(Color.transparent)
  for entry in FACES_TEXTURES.keys():
    var value = FACES_TEXTURES[entry]
    var texture = value["texture"]
    var color = ColorUtils.get_color(value["color"])
    var alignment = value["align"]
    var img = _create_colored_copy_from_image(texture.get_data(), color)
    var pos = _get_position_from_alignment(texture, alignment)
    ImageUtils._blit_texture(image, img, pos)
  return image

static func _get_position_from_alignment(texture, alignment):
  if alignment == ImageAlignment.TOP_LEFT:
    return Vector2.ZERO
  if alignment == ImageAlignment.TOP_RIGHT:
    return Vector2(SPRITE_SIZE.x - texture.get_width(), 0)
  if alignment == ImageAlignment.BOTTOM_LEFT:
    return Vector2(0, SPRITE_SIZE.y - texture.get_height())
  if alignment == ImageAlignment.BOTTOM_RIGHT:
    return SPRITE_SIZE - Vector2(texture.get_width(), texture.get_height())
  return Vector2.ZERO

static func _create_colored_copy_from_image(src_image: Image, color: Color):
  var width = src_image.get_width()
  var height = src_image.get_height()
  var image = Image.new()
  var format = src_image.get_format()
  image.create(width, height, false, format)
  src_image.lock()
  image.lock()
  for i in range(width):
    for j in range(height):
      var pix = src_image.get_pixel(i, j)
      var col = Color(color.r, color.g, color.b, pix.a)
      image.set_pixel(i, j, col)
  image.unlock()
  src_image.unlock()
  return image
