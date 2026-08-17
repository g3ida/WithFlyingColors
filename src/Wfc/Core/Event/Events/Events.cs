namespace Wfc.Core.Event;

using System;
using Godot;
using Wfc.Entities.World.Checkpoints;

// RefCounted rather than a plain GodotObject: the EventHandler autoload holds
// this for the whole run and a bare GodotObject would have to be freed by hand,
// which reports as a leaked instance at exit.
public partial class Events : RefCounted {
  [Signal]
  // Values, not the node that raised them. Only the player reads either of these, and passing
  // the node made two whole classes of bug possible: the deprecated Checkpoint typed itself as
  // an Area2D and failed conversion inside the native emit - taking every other subscriber down
  // with it - and a room wanting a checkpoint of its own fabricated a CheckpointArea it never
  // added to the tree, whose GlobalPosition was the world origin.
  public delegate void CheckpointReachedEventHandler(Vector2 position, string colorGroup);
  [Signal]
  public delegate void CheckpointLoadedEventHandler();
  [Signal]
  public delegate void MenuBoxRotatedEventHandler();
  [Signal]
  public delegate void PauseMenuEnterEventHandler();
  [Signal]
  public delegate void PauseMenuExitEventHandler();
  [Signal]
  public delegate void MenuButtonPressedEventHandler(int menuButton);
  // Settings signals
  [Signal]
  public delegate void OnActionBoundEventHandler(string action, int key);
  [Signal]
  public delegate void FocusChangedEventHandler();
  [Signal]
  public delegate void KeyboardActionBindingEventHandler();
  [Signal]
  public delegate void OnGamepadActionBoundEventHandler(string action, int buttonOrAxis, bool isAxis, float axisDirection);
  // The player just used a device of a different kind than the one before
  // (keyboard/mouse vs. gamepad). Carries a ControllerType.
  [Signal]
  public delegate void LastUsedControllerChangedEventHandler(int controllerType);
  // The player moved the controller row in the settings themselves, to read the
  // other device's key bindings. Carries a ControllerType. Raised for that move
  // alone, not for the row following the device in their hands, so it can be
  // treated as a menu action the player took.
  [Signal]
  public delegate void ControllerSelectionChangedEventHandler(int controllerType);
  [Signal]
  public delegate void GamepadConnectedEventHandler(int deviceId, string deviceName);
  [Signal]
  public delegate void GamepadDisconnectedEventHandler(int deviceId);
  [Signal]
  public delegate void CutSceneRequestStartEventHandler(string id);
  [Signal]
  public delegate void CutSceneRequestEndEventHandler(string id);
  [Signal]
  public delegate void LevelClearedEventHandler();
  // A gem the player brought back settling into the arch of its door, and the four of them
  // merging into the keystone comet. Raised for the ceremony rather than for the banking,
  // which happened while the screen was still covered.
  [Signal]
  public delegate void DoorGemFilledEventHandler();
  [Signal]
  public delegate void DoorCometFormedEventHandler();
  // The LevelId ordinal of the level behind the door, as an int because that is what
  // a Godot signal can carry.
  [Signal]
  public delegate void DoorEnteredEventHandler(int levelId);
  // Start the current level over from its beginning. Nothing is banked: a restart is
  // the player giving up on the run so far, not progress worth recording.
  [Signal]
  public delegate void LevelRestartRequestedEventHandler();
  // The active slot's metadata was rewritten. The hub is built before the clear that
  // sent the player back to it is banked, so anything showing save state has to be
  // told rather than only reading it once on the way in.
  [Signal]
  public delegate void SaveSlotUpdatedEventHandler();
  // A line for the corner of the screen, as a TranslationKey ordinal: what is worth saying is
  // the raiser's business, and what it looks like and how long it stays is the notification
  // system's.
  [Signal]
  public delegate void NotificationRaisedEventHandler(int translationKey);



  // Both carry how hard they want it, so one move can ask for a nudge where it starts and a
  // jolt where it lands without either reading as the other.
}
