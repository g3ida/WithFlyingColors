extends Tetromino

func _ready():
  rotation_map = [
    [Vector2(-1,0),Vector2(0,0),Vector2(1,0),Vector2(2,0)],
    [Vector2(0,1),Vector2(0,0),Vector2(0,-1),Vector2(0,-2)],
    [Vector2(1,0),Vector2(0,0),Vector2(-1,0),Vector2(-2,0)],
    [Vector2(0,-1),Vector2(0,0),Vector2(0,1),Vector2(0,2)]
   ]
  set_shape()
