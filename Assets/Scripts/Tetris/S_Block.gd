extends Tetromino

func _ready():
  rotation_map = [
    [Vector2(-1,0),Vector2(0,0),Vector2(0,-1),Vector2(1,-1)],
    [Vector2(0,-1),Vector2(0,0),Vector2(1,0),Vector2(1,1)],
    [Vector2(1,0),Vector2(0,0),Vector2(0,1),Vector2(-1,1)],
    [Vector2(0,1),Vector2(0,0),Vector2(-1,0),Vector2(-1,-1)]
   ]
  set_shape()
