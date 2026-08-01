namespace Wfc.Core.Event;

using System;
using Godot;
using Wfc.Entities.World.Checkpoints;

// RefCounted rather than a plain GodotObject: the EventHandler autoload holds
// this for the whole run and a bare GodotObject would have to be freed by hand,
// which reports as a leaked instance at exit.
public partial class Events : RefCounted {
  [Signal]
  public delegate void PlayerLandedEventHandler(Node area, Vector2 position);
  [Signal]
  public delegate void PlayerDyingEventHandler(Node? area, Vector2 position, int entityType);
  [Signal]
  public delegate void PlayerDiedEventHandler();
  [Signal]
  public delegate void PlayerJumpedEventHandler();
  [Signal]
  public delegate void PlayerRotateEventHandler(int dir);
  [Signal]
  public delegate void PlayerLandEventHandler();
  [Signal]
  public delegate void PlayerExplodeEventHandler();
  [Signal]
  public delegate void PlayerFallEventHandler();
  // Caught between a moving platform and something solid.
  [Signal]
  public delegate void PlayerSquashedEventHandler();
  [Signal]
  public delegate void PlayerDashEventHandler(Vector2 direction);
  [Signal]
  public delegate void PlayerSlipperingEventHandler();
  [Signal]
  public delegate void GemCollectedEventHandler(string color, Vector2 position, SpriteFrames frames);
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
  public delegate void FullscreenToggledEventHandler(bool value);
  [Signal]
  public delegate void VsyncToggledEventHandler(bool value);
  [Signal]
  public delegate void ScreenSizeChangedEventHandler(Vector2 value);
  // The player picked another language in the settings. Carries a Language.
  // Raised for the change itself, not for the locale being restored on startup,
  // so it can be treated as a menu action the player took.
  [Signal]
  public delegate void LanguageChangedEventHandler(int language);
  [Signal]
  public delegate void OnActionBoundEventHandler(string action, int key);
  [Signal]
  public delegate void FocusChangedEventHandler();
  [Signal]
  public delegate void KeyboardActionBindingEventHandler();
  [Signal]
  public delegate void GamepadActionBindingEventHandler();
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
  public delegate void SfxVolumeChangedEventHandler(float volume);
  [Signal]
  public delegate void MusicVolumeChangedEventHandler(float volume);
  [Signal]
  public delegate void TetrisLinesRemovedEventHandler();
  [Signal]
  public delegate void BrickBrokenEventHandler(string color, Vector2 position);
  [Signal]
  public delegate void BouncingBallRemovedEventHandler(Node2D ball);
  [Signal]
  public delegate void PickedPowerUpEventHandler();
  [Signal]
  public delegate void BreakBreakerWinEventHandler();
  [Signal]
  public delegate void BrickBreakerStartEventHandler();
  [Signal]
  public delegate void PianoNotePressedEventHandler(int noteIndex);
  [Signal]
  public delegate void PianoNoteReleasedEventHandler(int noteIndex);
  [Signal]
  public delegate void PageFlippedEventHandler();
  [Signal]
  public delegate void WrongPianoNotePlayedEventHandler();
  [Signal]
  public delegate void PianoPuzzleWonEventHandler();
  [Signal]
  public delegate void CutSceneRequestStartEventHandler(string id);
  [Signal]
  public delegate void CutSceneRequestEndEventHandler(string id);
  [Signal]
  public delegate void GemTempleTriggeredEventHandler();
  [Signal]
  public delegate void GemEngineStartedEventHandler();
  [Signal]
  public delegate void LevelClearedEventHandler();
  [Signal]
  public delegate void GemPutInTempleEventHandler();
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
  // Both carry how hard they want it, so one move can ask for a nudge where it starts and a
  // jolt where it lands without either reading as the other.
  [Signal]
  public delegate void CameraShakeRequestEventHandler(float amplitude);
  [Signal]
  public delegate void CameraZoomPunchRequestEventHandler(float strength);
}
