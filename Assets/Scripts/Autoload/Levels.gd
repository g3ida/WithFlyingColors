extends Node2D

const LEVELS = [
  {
    "id": 1,
    "name": "Tutorial",
    "scene": "res://Levels/TutorialLevel.tscn"
  },
  {
    "id": 2,
    "name": "Dark Games",
    "scene": "res://Levels/Level1.tscn"
  },
  {
    "id": 3,
    "name": "One More Level",
    "scene": "res://Levels/Level1.tscn"
  }
]

func _ready():
  set_process(false)
