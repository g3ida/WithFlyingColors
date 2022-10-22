class_name ImageUtils

static func _blit_texture(dest_image: Image, src_image: Image, pos: Vector2):
  var bounds = Vector2(dest_image.get_width(), dest_image.get_height())
  var src_bound_x = min(pos.x + src_image.get_width(), bounds.x)
  var src_bound_y = min(pos.y + src_image.get_height(), bounds.y)
  var src_rect = Rect2(Vector2(0, 0), Vector2(src_bound_x, src_bound_y))
  dest_image.blend_rect(src_image, src_rect, pos)

# 4 times percent slower than buit-in blend function.
# maybe it should be better implemented in c++
static func _alpha_blend(src: Image, dst: Image, color: Color, pos: Vector2):
  assert(pos.x >= 0 and pos.y >= 0, "expected pos coords to be positive")
  var width = min(src.get_width(), dst.get_width()-pos.x)
  var height = min(src.get_height(), dst.get_height()-pos.y)
  src.lock()
  dst.lock()
  for i in range(width):
    for j in range(height):
      var dst_pos = Vector2(i+pos.x, j+pos.y) 
      var src_pix = src.get_pixel(i, j)
      var dst_pix = dst.get_pixel(dst_pos.x, dst_pos.y)
      src_pix = Color(color.r, color.g, color.b, src_pix.a) #edit src color
      var col = _alpha_blend_colors(src_pix, dst_pix)
      dst.set_pixel(dst_pos.x, dst_pos.y, col)
  dst.unlock()
  src.unlock()
  
# blending c1 on c2
static func _alpha_blend_colors(c1: Color, c2: Color) -> Color:
  var c: Color = Color.transparent
  var inv_c1a = 1-c1.a
  c.a = c1.a + c2.a*inv_c1a
  if c.a > 0.01:
    c.r = (c1.r*c1.a + c2.r*c2.a*inv_c1a)/c.a
    c.g = (c1.g*c1.a + c2.g*c2.a*inv_c1a)/c.a
    c.b = (c1.b*c1.a + c2.b*c2.a*inv_c1a)/c.a
  return c