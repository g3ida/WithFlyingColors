namespace Wfc.Core.Event;

using System;
using Godot;

public partial class Events : GodotObject {
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
  [Signal]
  public delegate void PlayerDashEventHandler(Vector2 direction);
  [Signal]
  public delegate void PlayerSlipperingEventHandler();
  [Signal]
  public delegate void GemCollectedEventHandler(string color, Vector2 position, SpriteFrames frames);
  [Signal]
  public delegate void CheckpointReachedEventHandler(CheckpointArea checkpointObject);
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
  [Signal]
  public delegate void OnActionBoundEventHandler(string action, int key);
  [Signal]
  public delegate void TabChangedEventHandler();
  [Signal]
  public delegate void FocusChangedEventHandler();
  [Signal]
  public delegate void KeyboardActionBindingEventHandler();
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
}
