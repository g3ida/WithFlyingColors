// using Godot;
// using System;
// using System.Collections.Generic;

// public class TouchInput : Node
// {
//     private const float MOVE_DIRECTION_MIN_THRESHOLD = 7.5f;
//     private const float ROTATE_TOUCH_MAX_DELAY = 0.3f;
//     private const float ROTATE_TOUCH_MAX_DRAG_MODULE = 75.0f;
//     private const float ROTATE_TOUCH_MAX_DRAG_MODULE_SQUARED = ROTATE_TOUCH_MAX_DRAG_MODULE * ROTATE_TOUCH_MAX_DRAG_MODULE;
//     private const float DASH_SWIPE_THRESHOLD = 180.0f;
//     private const float JUMP_SWIPE_THRESHOLD = 90.0f;
//     private const float JUMP_FORCE_NORMALIZATION_VALUE = 200.0f;

//     private Vector2 viewportSize;
//     private float screenDpi;

//     private InputEventScreenTouch _movePlayerDragInput;
//     private SwipeModule swipeModule;
//     private List<TimedTouchEvent> rotateLeftCandidates = new List<TimedTouchEvent>();
//     private List<TimedTouchEvent> rotateRightCandidates = new List<TimedTouchEvent>();

//     private class TimedTouchEvent
//     {
//         public InputEventScreenTouch Event { get; set; }
//         public float Time { get; set; }

//         public TimedTouchEvent(InputEventScreenTouch _event, float _time = 0)
//         {
//             Event = _event;
//             Time = _time;
//         }
//     }

//     public override void _Ready()
//     {
//         viewportSize = GetViewport().GetSizeOverride();
//         screenDpi = OS.GetScreenDpi(OS.GetCurrentScreen()); // FIXME: does not work for iOS?

//         swipeModule = new SwipeModule();
//         swipeModule.DragWhileSwipe = true;
//         AddChild(swipeModule);
//         swipeModule.SetOwner(this);
//     }

//     private bool IsInMovePlayerScreenPosition(Vector2 pos)
//     {
//         return pos.x < viewportSize.x * 0.45f;
//     }

//     private bool IsOnOtherActionsDirection(Vector2 pos)
//     {
//         return pos.x > viewportSize.x * 0.55f;
//     }

//     public override void _Input(InputEvent @event)
//     {
//         if (@event is InputEventScreenTouch eventTouch)
//         {
//             if (eventTouch.Pressed)
//             {
//                 if (IsInMovePlayerScreenPosition(eventTouch.Position) && _movePlayerDragInput == null)
//                 {
//                     _movePlayerDragInput = eventTouch;
//                 }
//                 if (IsInRotateLeftPosition(eventTouch.Position))
//                 {
//                     rotateLeftCandidates.Add(new TimedTouchEvent(eventTouch, 0));
//                 }
//                 if (IsInRotateRightPosition(eventTouch.Position))
//                 {
//                     rotateRightCandidates.Add(new TimedTouchEvent(eventTouch, 0));
//                 }
//             }
//             else
//             {
//                 if (_movePlayerDragInput != null && _movePlayerDragInput.Index == eventTouch.Index)
//                 {
//                     _movePlayerDragInput = null;
//                     FireMoveEvent(Vector2.Zero);
//                 }

//                 if (rotateLeftCandidates.Count > 0)
//                 {
//                     rotateLeftCandidates.Clear();
//                     FireRotateEvent(-1);
//                 }
//                 if (rotateRightCandidates.Count > 0)
//                 {
//                     rotateRightCandidates.Clear();
//                     FireRotateEvent(1);
//                 }
//             }
//         }

//         if (@event is InputEventScreenDrag eventDrag)
//         {
//             HandleRotationActionWithDrag(rotateLeftCandidates, eventDrag);
//             HandleRotationActionWithDrag(rotateRightCandidates, eventDrag);
//             if (_movePlayerDragInput != null)
//             {
//                 if (eventDrag.Index == _movePlayerDragInput.Index)
//                 {
//                     if (eventDrag.Relative.x > MOVE_DIRECTION_MIN_THRESHOLD)
//                     {
//                         FireMoveEvent(Vector2.Right);
//                     }
//                     else if (eventDrag.Relative.x < -MOVE_DIRECTION_MIN_THRESHOLD)
//                     {
//                         FireMoveEvent(Vector2.Left);
//                     }
//                 }
//             }
//         }

//         if (@event is InputEventSwipe eventSwipe)
//         {
//             eventSwipe.SetHeading(eventSwipe.Direction, 2.5f);
//             if (eventSwipe.Up() && Mathf.Abs(eventSwipe.Direction.y) > JUMP_SWIPE_THRESHOLD)
//             {
//                 FireJumpEvent(eventSwipe);
//             }
//             else if (eventSwipe.Left() && Mathf.Abs(eventSwipe.Direction.x) > DASH_SWIPE_THRESHOLD && CheckDuration(eventSwipe, 0.35f))
//             {
//                 FireDashEvent(Vector2.Left);
//             }
//             else if (eventSwipe.Right() && Mathf.Abs(eventSwipe.Direction.x) > DASH_SWIPE_THRESHOLD && CheckDuration(eventSwipe, 0.35f))
//             {
//                 FireDashEvent(Vector2.Right);
//             }
//             else if (eventSwipe.Down() && Mathf.Abs(eventSwipe.Direction.y) > DASH_SWIPE_THRESHOLD && CheckDuration(eventSwipe, 0.55f))
//             {
//                 FireDashEvent(Vector2.Down);
//             }
//         }
//     }

//     public override void _Process(float delta)
//     {
//         UpdateRotateList(rotateLeftCandidates, delta);
//         UpdateRotateList(rotateRightCandidates, delta);
//     }

//     private bool CheckDuration(InputEventSwipe ev, float duration)
//     {
//         return ev.Duration <= duration && ev.Duration > Constants.Epsilon;
//     }

//     private void UpdateRotateList(List<TimedTouchEvent> list, float delta)
//     {
//         List<int> trashList = new List<int>();
//         int i = 0;
//         foreach (var el in list)
//         {
//             el.Time += delta;
//             if (el.Time > ROTATE_TOUCH_MAX_DELAY)
//             {
//                 // items are inserted in reverse order to avoid IndexOutOfRangeExceptions
//                 trashList.Insert(0, i);
//             }
//             i += 1;
//         }
//         foreach (var el in trashList)
//         {
//             list.RemoveAt(el);
//         }
//     }

//     private bool IsInRotateRightPosition(Vector2 pos)
//     {
//         return pos.x > viewportSize.x * 0.8f && pos.y < viewportSize.y * 0.72f && pos.y > viewportSize.y * 0.1f;
//     }

//     private bool IsInRotateLeftPosition(Vector2 pos)
//     {
//         return pos.x > viewportSize.x * 0.5f && pos.x < viewportSize.x * 0.87f && pos.y > viewportSize.y * 0.72f;
//     }

//     private void HandleRotationActionWithDrag(List<TimedTouchEvent> list, InputEventScreenDrag drag)
//     {
//         List<int> listToRemove = new List<int>();
//         int i = 0;
//         foreach (var el in list)
//         {
//             if (el.Event.Index == drag.Index)
//             {
//                 if ((el.Event.Position - drag.Position).LengthSquared() > ROTATE_TOUCH_MAX_DRAG_MODULE_SQUARED)
//                 {
//                     // items are inserted in reverse order to avoid IndexOutOfRangeExceptions
//                     listToRemove.Insert(0, i);
//                 }
//                 i += 1;
//             }
//         }
//         foreach (var el in listToRemove)
//         {
//             list.RemoveAt(el);
//         }
//     }

//     private void FireRotateEvent(int direction)
//     {
//         var ev = new InputTouchRotate { Direction = direction };
//         Input.ParseInputEvent(ev);
//     }

//     private void FireDashEvent(Vector2 direction)
//     {
//         var ev = new InputTouchDash { Direction = direction };
//         Input.ParseInputEvent(ev);
//     }

//     private void FireJumpEvent(InputEventSwipe @event)
//     {
//         var ev = new InputTouchJump { Force = Mathf.Clamp(-@event.Direction.y / JUMP_FORCE_NORMALIZATION_VALUE, 0.0f, 1.0f) };
//         Input.ParseInputEvent(ev);
//     }

//     private void FireMoveEvent(Vector2 direction)
//     {
//         var ev = new InputTouchMove { Direction = direction };
//         Input.ParseInputEvent(ev);
//     }
// }
