extends Node2D

const COLDDOWN = 1.5
const ITEM_INV_PROBA = 5

const PowerUp = preload("res://Assets/Scenes/BrickBreaker/Powerups/PowerUp.tscn")
const SlowPowerUp = preload("res://Assets/Scenes/BrickBreaker/Powerups/SlowPowerUp.tscn")
const FastPowerUp = preload("res://Assets/Scenes/BrickBreaker/Powerups/FastPowerUp.tscn")
const ScaleUpPowerUp = preload("res://Assets/Scenes/BrickBreaker/Powerups/ScaleUpPowerUp.tscn")
const ScaleDownPowerUp = preload("res://Assets/Scenes/BrickBreaker/Powerups/ScaleDownPowerUp.tscn")
const TripleBallsPowerUp = preload("res://Assets/Scenes/BrickBreaker/Powerups/TripleBallsPowerUp.tscn")
const ProtectionAreaPowerUp = preload("res://Assets/Scenes/BrickBreaker/Powerups/ProtectionAreaPowerUp.tscn")
const  powerups = [SlowPowerUp, FastPowerUp, ScaleUpPowerUp, ScaleDownPowerUp, TripleBallsPowerUp, ProtectionAreaPowerUp]

var active_powerup_nodes = []

onready var FallingPowerUpsContainer = $FallingPowerUpsContainer
onready var ColddownTimer = $ColddownTimer
onready var BrickBreakerNode = get_parent().get_parent()

func connect_signals():
  var __ = Event.connect("brick_broken", self, "_on_brick_broken")
  __ = Event.connect("checkpoint_loaded", self, "reset")
  
func disconnect_signals():
  Event.disconnect("brick_broken", self, "_on_brick_broken")
  Event.disconnect("checkpoint_loaded", self, "reset")

func reset():
  remove_active_powerups()
  remove_falling_powerups()
  ColddownTimer.stop()

func remove_active_powerups():
  for el in active_powerup_nodes:
    el.queue_free()
  active_powerup_nodes = []

func remove_falling_powerups():
  for el in FallingPowerUpsContainer.get_children():
    el.queue_free()

func create_powerup(powerup_node, color, _position):
  var powerup = powerup_node.instance()
  powerup.color_group = color
  powerup.position = _position - self.position
  FallingPowerUpsContainer.call_deferred("add_child", powerup)
  powerup.call_deferred("set_owner", FallingPowerUpsContainer)
  var __ = powerup.connect("on_player_hit", self, "on_player_hit")
  ColddownTimer.start()

func _on_brick_broken(color, _position):
  if ColddownTimer.time_left < Constants.EPSILON:
    if (randi() % ITEM_INV_PROBA) == 0:
      var powerup = powerups[randi() % powerups.size()]
      create_powerup(powerup, color, _position)
    
func check_if_can_add_powerup(hit_node: PackedScene):
  for el in active_powerup_nodes:
    if (!el.is_incremental) and el.get_filename() == hit_node.get_path():
      return false
  return true

func remove_irrelevant_powerups():
  var list_to_delete = []
  var i = 0
  for el in active_powerup_nodes:
    if !el.is_still_relevant():
      # items are inserted in reverse order to avoid IndexOutOfRangeExceptions
      list_to_delete.push_front(i)
    i += 1
  for el in list_to_delete:
    active_powerup_nodes[el].queue_free()
    active_powerup_nodes.remove(el)

func on_player_hit(powerup, hit_node: PackedScene):
  remove_irrelevant_powerups()
  if check_if_can_add_powerup(hit_node):
    var hit = hit_node.instance()
    active_powerup_nodes.append(hit)
    hit.set_brick_breaker_node(BrickBreakerNode)
    call_deferred("add_child", hit)
  if powerup != null:
    powerup.disconnect("on_player_hit", self, "on_player_hit")
  
func _process(_delta):
  set_process(false)

func _enter_tree():
  connect_signals()

func _exit_tree():
  disconnect_signals()
  
func _ready():
  ColddownTimer.wait_time = COLDDOWN
  randomize()