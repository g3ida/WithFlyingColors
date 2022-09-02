extends Node2D

onready var ScoreNode = $Score
onready var LevelNode = $Level
onready var HighScoreNode = $HiScore2

var score: int
var level: int
var high_score

func set_high_score(_score: int):
  high_score = _score
  HighScoreNode.text = "SCORE:%04d" % high_score

func set_score(_score: int):
  score = _score
  ScoreNode.set_value("SCORE:  %04d" % score)

func set_level(_level: int):
  level = _level
  LevelNode.set_value("LEVEL:  %04d" % level)

func _ready():
  set_score(0)
  set_level(1)
