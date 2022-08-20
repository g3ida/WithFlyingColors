extends Node2D

onready var vsync: bool = OS.vsync_enabled setget setVsync, getVsync
onready var fullscreen: bool = OS.window_fullscreen setget setFullscreen, getFullscreen
onready var window_size: Vector2 = OS.window_size setget setWindowSize, getWindowSize

func setVsync(value: bool):
  vsync = value
  OS.vsync_enabled = vsync

func getVsync() -> bool:
  return vsync

func setFullscreen(value: bool):
	fullscreen = value
	OS.window_fullscreen = fullscreen

func getFullscreen() -> bool:
  return fullscreen

func setWindowSize(value: Vector2):
  window_size = value
  OS.set_window_size(value)

func getWindowSize() -> Vector2:
  return window_size