tool
extends Label

export var content = ""
export var underline_color: Color
export var underline_shadow_color: Color

func update_label_position_x(value):
	self.rect_position.x = value

func _ready():
	self.text = content
	$Shadow.text = content
	
	var scale = get_minimum_size().x / $Underline.rect_size.x
	$Underline.rect_scale = Vector2(scale, $Underline.rect_scale.y)
	$Underline.modulate = underline_color
	
	var shadow_scale = get_minimum_size().x / $UnderlineShadow.rect_size.x
	$UnderlineShadow.rect_scale = Vector2(shadow_scale, $UnderlineShadow.rect_scale.y)
	$UnderlineShadow.modulate = underline_shadow_color
