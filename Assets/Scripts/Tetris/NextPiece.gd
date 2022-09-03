tool
extends Node

export(PackedScene) var next_piece

var next_piece_node: Tetromino = null

func set_next_piece(piece: PackedScene):
  if (next_piece_node != null):
    remove_child(next_piece_node)
  next_piece = piece
  next_piece_node = next_piece.instance()
  add_child(next_piece_node)
  next_piece_node.set_owner(self)
  next_piece_node.position -= get_piece_bounds(next_piece_node)
  
func _ready():
  if (next_piece != null):
    set_next_piece(next_piece)

#used to center piece in container
func get_piece_bounds(piece: Tetromino) -> Vector2:
  var min_i = 3
  var min_j = 3
  var max_i = -3
  var max_j = -3
  for ch in piece.get_children():
    min_i = min(ch.i, min_i)
    min_j = min(ch.j, min_j)
    max_i = max(ch.i, max_i)
    max_j = max(ch.j, max_j)
  return  ((Vector2(min_i, min_j) + (Vector2(max_i-min_i+1, max_j-min_j+1)* 0.5)) * Constants.TETRIS_BLOCK_SIZE)
