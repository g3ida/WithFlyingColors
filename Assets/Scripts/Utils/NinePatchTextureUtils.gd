func _init():
  pass

func scale_texture(ninePatchRect: NinePatchRect, scale: Vector2):
  assert(scale.x >= 1 && scale.y >= 1)
  ninePatchRect.rect_scale = Vector2(1/scale.x, 1/scale.y)
  var size = ninePatchRect.rect_size
  
  var new_size = Vector2(size.x * scale.x, size.y * scale.y)
  ninePatchRect.rect_size = Vector2(new_size.x, new_size.y)
  
  var rect_pos = Vector2(
    ninePatchRect.rect_position.x - (new_size.x - size.x) / (scale.x * 2),
    ninePatchRect.rect_position.y - (new_size.y - size.y) / (scale.y * 2))
  ninePatchRect.rect_position = rect_pos
  
func set_texture(ninePatchRect: NinePatchRect, texture: Texture):
  ninePatchRect.texture = texture
