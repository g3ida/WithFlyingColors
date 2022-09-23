extends Control

const PlaySubMenuScene = preload("res://Assets/Scenes/MainMenu/PlaySubMenu.tscn")
const PlayerRotationAction = preload("res://Assets/Scripts/Player/PlayerRotationAction.gd")

const SUB_MENU_POPUP_DURATION = 0.2

var box_rotation: PlayerRotationAction
var buttons = []
var buttons_mappings = []
#menu state vars
var active_index = 0
#subMenu state vars
var play_sub_menu_pos: Vector2

enum States {MENU, SUB_MENU_ENTER, SUB_MENU, SUB_MENU_EXIT, EXIT}
var current_state = States.MENU

onready var play_button = $MenuBox/Sprite/PlayButton
onready var settings_button = $MenuBox/Sprite/SettingsButton
onready var stats_button = $MenuBox/Sprite/StatsButton
onready var quit_button = $MenuBox/Sprite/QuitButton

onready var MenuBoxNode = $MenuBox
onready var PlaySubMenuNode = null
onready var SubMenuTween = $MenuBox/SubMenuTween
onready var SpriteNode = $MenuBox/Sprite
onready var SpriteHeight = SpriteNode.texture.get_height()

func can_respond_to_input() -> bool:
  return current_state != States.EXIT && get_parent().is_enter_ceremony_done()

func _enter_tree():
  current_state = States.MENU
  if MenuManager.previous_menu == MenuManager.Menus.STATS_MENU:
    $MenuBox.rotate(-PI)
    active_index = 2
  elif MenuManager.previous_menu == MenuManager.Menus.SETTINGS_MENU:
    $MenuBox.rotate(-PI / 2)
    active_index = 1

func _ready():
  box_rotation = PlayerRotationAction.new($MenuBox)
  buttons = [
    play_button,
    settings_button,
    stats_button,
    quit_button
    ]
    
  for b in buttons:
    b.disabled = true
  buttons[active_index].disabled = false

func update_position_y(y: float):
  self.rect_position.y = y

func _physics_process(delta):
  box_rotation.step(delta)

  if PlaySubMenuNode != null:
    PlaySubMenuNode.set_position(play_sub_menu_pos)

  if Input.is_action_just_pressed("rotate_left") or Input.is_action_just_pressed("ui_left"):
    _on_LeftButton_pressed()
  elif Input.is_action_just_pressed("rotate_right") or Input.is_action_just_pressed("ui_right"):
    _on_RightButton_pressed()

  elif Input.is_action_just_pressed("ui_accept"):
    click_on_active_button()

func _hide_sub_menu_if_needed():
  if current_state == States.SUB_MENU or current_state == States.SUB_MENU_ENTER:
    _display_or_hide_play_sub_menu(false)

func _on_RightButton_pressed():
  _hide_sub_menu_if_needed()
  if box_rotation.execute(1, Constants.PI2, 0.1, false):
    buttons[active_index].disabled = true
    active_index = (active_index - 1) % buttons.size()
    buttons[active_index].disabled = false
    Event.emit_signal("menu_box_rotated")
  
func _on_LeftButton_pressed():
  _hide_sub_menu_if_needed()
  if box_rotation.execute(-1, Constants.PI2, 0.1, false):
    buttons[active_index].disabled = true
    active_index = (active_index + 1) % buttons.size()
    buttons[active_index].disabled = false
    Event.emit_signal("menu_box_rotated")
  
func _on_PlayButton_pressed():
  if not can_respond_to_input(): return
  if current_state == States.MENU or current_state == States.SUB_MENU_EXIT:
    _display_or_hide_play_sub_menu(true)
    Event.emit_menu_button_pressed(MenuButtons.PLAY)

func _on_QuitButton_pressed():
  _process_button_press(MenuButtons.QUIT)

func _on_SettingsButton_pressed():
  _process_button_press(MenuButtons.SETTINGS)

func _on_StatsButton_pressed():
  _process_button_press(MenuButtons.STATS)

func _process_button_press(menu_button):
  if not can_respond_to_input(): return
  current_state = States.EXIT
  Event.emit_menu_button_pressed(menu_button)

func click_on_active_button():
  if current_state == States.SUB_MENU or current_state == States.SUB_MENU_ENTER:
    return
  if buttons[active_index] == play_button:
    _on_PlayButton_pressed()
  elif buttons[active_index] == quit_button:
    _on_QuitButton_pressed()
  elif buttons[active_index] == settings_button:
    _on_SettingsButton_pressed()
  elif buttons[active_index] == stats_button:
    _on_StatsButton_pressed()

func _display_or_hide_play_sub_menu(should_show = true):
  if PlaySubMenuNode == null:
    PlaySubMenuNode = PlaySubMenuScene.instance()
    MenuBoxNode.add_child(PlaySubMenuNode)
    PlaySubMenuNode.set_owner(MenuBoxNode)
    var sz = PlaySubMenuNode.rect_min_size
    var source = Vector2(-sz.x*0.5, -sz.y)
    var destination = source + Vector2.UP*SpriteHeight*0.5
    if should_show:
      current_state = States.SUB_MENU_ENTER
      interpolate_sub_menu(source, destination)
  elif not should_show: 
    var sz = PlaySubMenuNode.rect_min_size
    var destination = Vector2(-sz.x*0.5, -sz.y)
    var source = play_sub_menu_pos
    current_state = States.SUB_MENU_EXIT
    interpolate_sub_menu(source, destination)

func interpolate_sub_menu(source: Vector2, destination: Vector2):
  SubMenuTween.remove_all()
  play_sub_menu_pos = source
  SubMenuTween.interpolate_property(
    self,
    "play_sub_menu_pos",
    play_sub_menu_pos,
    destination,
    SUB_MENU_POPUP_DURATION,
    Tween.TRANS_LINEAR,
    Tween.EASE_IN_OUT)
  SubMenuTween.start()

func _on_SubMenuTween_tween_completed(_object, _key):
  if current_state == States.SUB_MENU_ENTER:
    current_state = States.SUB_MENU
  elif current_state == States.SUB_MENU_EXIT:
    current_state = States.MENU
    PlaySubMenuNode.queue_free()
    PlaySubMenuNode = null

func _on_OutsideButton_pressed():
  _hide_sub_menu_if_needed()
