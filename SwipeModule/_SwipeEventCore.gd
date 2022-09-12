class_name _SwipeEventCore, "SwipeEventCore.png"
extends Node

export var SwipeTimeMax		:float	= 0.6	#length of swipe in seconds
export var Tollerance		:float	= 0.1	#length of swipe in seconds
export var CumulativeDrag	:bool	=true	# if TRUE, instead of a EventDrag queue, only a single cumulative DragEvent is emitted
export var DragWhileSwipe	:bool	=true	# if TRUE, parsed drag events won't be suppressed
export var CallDeferred		:bool	=false	# if TRUE, events are created deferred, if FALSE, events are created instantly

class SwipeCondidate:
  var index = -1
  var TimeTolleranceAccum	= 0
  var BoundNode			= null

  var SwipeTime	:float	= 0
  var ProcessTime	:float	= 0
  var ProcessDelta:float	= 0
  var TimeProcessing		= false
  var CollectingEvents	= false
  var isThrowedEvent 		= false

  var WaitNextTouch 		= true

  var EventQueue	= []

func end_parse(_el, idx):
  # items are inserted in reverse order to avoid IndexOutOfRangeExceptions
  remove_list.insert(0, idx)

var swipe_condidates: Array = []
var remove_list: Array = []

func _ready():
  pass
func _process(delta):
  var i = 0
  for el in swipe_condidates:
    el.ProcessDelta = delta
    if  el.ProcessTime  > SwipeTimeMax:		# swipe too long
      is_not_a_swipe(el, i)
    elif el.ProcessTime > el.SwipeTime:
      el.TimeTolleranceAccum = el.TimeTolleranceAccum + delta
      if el.TimeTolleranceAccum <= Tollerance:
        el.SwipeTime = el.ProcessTime 
      else:
        is_not_a_swipe(el, i)
    elif el.ProcessTime == el.SwipeTime:			# continue swipe detection
      if !el.CollectingEvents:
        it_is_a_swipe(el, i)
    elif el.SwipeTime  > el.SwipeTimeMax:			# IMPOSSIBLE swipe too log
      is_not_a_swipe(el, i)
    else:									# swipe ended
      is_not_a_swipe(el, i)
    
    if is_processing():	
      el.ProcessTime	= el.ProcessTime + delta
      if 	el.TimeProcessing:
        el.SwipeTime	= el.SwipeTime + el.ProcessDelta
      el.TimeProcessing = false
    i += 1

  # items are sorted in reverse order to avoid IndexOutOfRangeExceptions
  for el in remove_list:
    swipe_condidates.remove(el)
  remove_list = []
  if swipe_condidates.empty():
    set_process(false)


func ThrowEvent(ev:InputEvent = null):
  if CallDeferred:
    Input.call_deferred("parse_input_event",ev)
  else:
    Input.parse_input_event(ev)

func it_is_a_swipe(el, idx):
  var ev = InputEventSwipe.new()
  ev.set_all_properties(el.EventQueue,el.SwipeTime,el.BoundNode)
  ev.duration = el.SwipeTime
  ThrowEvent(ev)
  end_parse(el, idx)
  
func is_not_a_swipe(el, idx):
  if  (!DragWhileSwipe):
    if CumulativeDrag:
      var ev = el.EventQueue[0]
      for i in range(1, el.EventQueue.size()-1):	
        ev.relative = ev.relative + el.EventQueue[i].relative
      ev.position = el.EventQueue[el.EventQueue.size()-1].position
      ThrowEvent(ThrowedHeader.new())
      ThrowEvent(ev)
      ThrowEvent(ThrowedFooter.new())
    
    else:
      ThrowEvent(ThrowedHeader.new())
      for i in el.EventQueue:			#throw stored events
        ThrowEvent(i)
      ThrowEvent(ThrowedFooter.new())
  end_parse(el, idx)
  pass

func get_swipe_condidate(idx):
  for el in swipe_condidates:
    if el.index == idx:
      return el
  return null
  
func _input(event):
  if event is ThrowedHeader:
    var el = get_swipe_condidate(event.index)
    if el != null: el.isThrowedEvent = true
    get_tree().set_input_as_handled()
  elif event is ThrowedFooter:
    var el = get_swipe_condidate(event.index)
    if el != null: el.isThrowedEvent = true
    get_tree().set_input_as_handled()

  elif (event is InputEventScreenTouch
   and !event.is_pressed()):
    var el = get_swipe_condidate(event.index)
    processInputEventMouseButtonReleased(el)

  elif (event is InputEventScreenTouch
   and  event.is_pressed()):
    var el = get_swipe_condidate(event.index)
    processInputEventMouseButtonPressed(el, event.index)

  elif (event is InputEventScreenDrag):
    var el = get_swipe_condidate(event.index)
    if el != null and el.WaitNextTouch:
      get_tree().set_input_as_handled()
    else:
      processInputEventScreenDrag(event, el)


func processInputEventMouseButtonPressed(el, index):
  if el == null:
    el = SwipeCondidate.new()
    el.index = index
    swipe_condidates.append(el)
  
  if !el.CollectingEvents:
    el.WaitNextTouch	= false
    el.CollectingEvents = true
    el.TimeProcessing  = true

func processInputEventMouseButtonReleased(el):
  if el != null:
    el.WaitNextTouch	 = true	
    el.CollectingEvents = false

func processInputEventScreenDrag(event, el):
  if !is_processing():
    set_process(true)

  if el == null:
    el = SwipeCondidate.new()
    el.index = event.index
    swipe_condidates.append(el)

  el.TimeProcessing = true
  el.EventQueue.append(event)

  if !DragWhileSwipe:
    get_tree().set_input_as_handled()
