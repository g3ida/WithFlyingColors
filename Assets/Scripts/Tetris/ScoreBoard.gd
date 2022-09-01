extends Node2D

onready var ScoreNode = $Score
onready var LevelNode = $Level

var score: int
var level: int

func set_score(_score: int):
  score = _score
  ScoreNode.text = "SCORE:%03d" % score

func set_level(_level: int):
  level = _level
  LevelNode.text = "LEVEL:%03d" % level

func _ready():
  set_score(0)
  set_level(1)
