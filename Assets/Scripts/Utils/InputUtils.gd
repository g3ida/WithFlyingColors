class_name InputUtils

static func get_first_key_keyboard_event_from_action_list(action_list: Array) -> InputEvent:
  for el in action_list:
    if el is InputEventKey:
      return el
  return null
