extends CanvasLayer

func _ready():
	var screen_width = ProjectSettings.get_setting("display/window/size/width")
	self.offset.x = screen_width * 0.5
	Global.HUD_offset =  self.offset
