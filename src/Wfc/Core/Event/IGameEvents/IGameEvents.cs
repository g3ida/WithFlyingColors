namespace Wfc.Core.Event;

using System;
using Chickensoft.Sync.Primitives;
using Godot;
using Wfc.Core.Localization;
using Wfc.Core.Input.Controllers;
using Wfc.Entities.World;
using Wfc.Screens.Levels;
using Wfc.Screens.MenuManager;

// Everything the game announces, as typed values rather than Godot signals: these cross no
// Variant boundary, so a payload that does not fit is a compile error rather than a conversion
// failure inside a native emit, and a subscription is dropped by disposing its binding.
//
// One channel rather than one per area. AutoChannel already tells messages apart by their type,
// so a second channel would separate nothing the type system was not separating for free, and a
// listener that cares about both settings and gameplay would need two bindings to say so.
public interface IGameEvents : IDisposable {
  IAutoChannel Channel { get; }

  #region Settings
  // Only what the player picks is announced. Applying a stored setting on startup is not a
  // change and says nothing, which is what lets a listener treat every message as an action.
  readonly record struct FullscreenToggled(bool IsFullscreen);
  readonly record struct VsyncToggled(bool IsEnabled);
  readonly record struct ScreenSizeChanged(Vector2 Size);
  readonly record struct LanguageChanged(Language Language);
  readonly record struct SkinChanged(string Skin);
  readonly record struct PerformanceOverlayToggled(bool IsEnabled);
  readonly record struct SfxVolumeChanged(float Volume);
  readonly record struct MusicVolumeChanged(float Volume);
  #endregion Settings

  #region Camera
  // Asked for by whatever the impact belongs to - a dash landing, a bucket hitting the floor -
  // rather than reached for on the camera, which has no way to know what is worth a shake.
  readonly record struct CameraShakeRequested(float Amplitude);
  readonly record struct CameraZoomPunchRequested(float Strength);
  #endregion Camera

  #region Paint
  // Paint carries where it happened so the sound can be placed; nothing reads it yet.
  readonly record struct PaintSpilled(Vector2 Position);
  readonly record struct PaintPouring(Vector2 Position);
  readonly record struct PaintSplashed(Vector2 Position);
  readonly record struct PaintGunCooling(Vector2 Position);
  readonly record struct PaintGunFired(Vector2 Position);
  readonly record struct BucketShoved(Vector2 Position);
  #endregion Paint

  #region Minigames
  // Every one of these is a cue for a sound and nothing else, except BrickBroken and
  // PianoNotePressed which the games themselves also read.
  readonly record struct TetrisLinesRemoved;
  readonly record struct TetrisPoolEscaped;
  readonly record struct BrickBroken(string ColorGroup, Vector2 Position);
  readonly record struct BouncingBallRemoved(Node2D Ball);
  readonly record struct BrickBreakerStarted;
  readonly record struct BrickBreakerWon;
  readonly record struct PowerUpPicked;
  readonly record struct PianoNotePressed(int NoteIndex);
  readonly record struct PianoNoteReleased(int NoteIndex);
  // Something other than the player landed on a key - the debris a death scatters over the
  // keyboard. It sounds like a note and is not one: the board must not read it as an answer.
  readonly record struct PianoNoteStruck(int NoteIndex);
  readonly record struct PageFlipped;
  readonly record struct WrongPianoNotePlayed;
  readonly record struct PianoPuzzleWon;
  readonly record struct ButtonGameNotePlayed(int NoteIndex);
  readonly record struct ButtonGameWrongNotePlayed;
  readonly record struct ButtonGameWon;
  #endregion Minigames

  #region Player
  // The cube's own reports. PlayerDying carries the EntityType rather than its integer: the
  // bus had to flatten it to cross a Variant, and both readers cast it straight back.
  readonly record struct PlayerLandedOn(Node Area, Vector2 Position);
  readonly record struct PlayerDying(Node? Area, Vector2 Position, EntityType Type);
  readonly record struct PlayerDied;
  readonly record struct PlayerJumped;
  readonly record struct PlayerRotated(int Direction);
  readonly record struct PlayerLanded;
  readonly record struct PlayerExploded;
  readonly record struct PlayerFell;
  readonly record struct PlayerSquashed;
  readonly record struct PlayerDashed(Vector2 Direction);
  readonly record struct PlayerSlippering;
  readonly record struct GemCollected(string ColorGroup, Vector2 Position, SpriteFrames Frames);
  #endregion Player

  #region Menus and input
  // MenuAction, ControllerType, TranslationKey and LevelId all made the same round trip through
  // an int to cross a Variant, and every reader cast them straight back.
  readonly record struct MenuActionPressed(MenuAction Action);
  readonly record struct MenuBoxRotated;
  readonly record struct PauseMenuEntered;
  readonly record struct PauseMenuExited;
  readonly record struct FocusChanged;
  readonly record struct ActionBound(string Action, int Key);
  readonly record struct KeyboardActionBinding;
  readonly record struct GamepadActionBound(string Action, int ButtonOrAxis, bool IsAxis, float AxisDirection);
  readonly record struct LastUsedControllerChanged(ControllerType Controller);
  readonly record struct ControllerSelectionChanged(ControllerType Controller);
  readonly record struct GamepadConnected(int DeviceId, string DeviceName);
  readonly record struct GamepadDisconnected(int DeviceId);
  readonly record struct NotificationRaised(TranslationKey Key);
  #endregion Menus and input

  #region Level and doors
  // Reached carries values, not the node that raised them: passing the node made a deprecated
  // Checkpoint fail conversion inside the native emit and take every other subscriber with it.
  readonly record struct CheckpointReached(Vector2 Position, string ColorGroup);
  readonly record struct CheckpointLoaded;
  readonly record struct CutsceneRequestStart(string Id);
  readonly record struct CutsceneRequestEnd(string Id);
  readonly record struct LevelCleared;
  readonly record struct LevelRestartRequested;
  readonly record struct DoorGemFilled;
  readonly record struct DoorCometFormed;
  readonly record struct DoorEntered(LevelId Level);
  readonly record struct SaveSlotUpdated;
  #endregion Level and doors

  void OnFullscreenToggled(bool isFullscreen);
  void OnVsyncToggled(bool isEnabled);
  void OnScreenSizeChanged(Vector2 size);
  void OnLanguageChanged(Language language);
  void OnSkinChanged(string skin);
  void OnPerformanceOverlayToggled(bool isEnabled);
  void OnSfxVolumeChanged(float volume);
  void OnMusicVolumeChanged(float volume);

  void RequestCameraShake(float amplitude);
  void RequestCameraZoomPunch(float strength);

  void OnPaintSpilled(Vector2 position);
  void OnPaintPouring(Vector2 position);
  void OnPaintSplashed(Vector2 position);
  void OnPaintGunCooling(Vector2 position);
  void OnPaintGunFired(Vector2 position);
  void OnBucketShoved(Vector2 position);

  void OnTetrisLinesRemoved();
  void OnTetrisPoolEscaped();
  void OnBrickBroken(string colorGroup, Vector2 position);
  void OnBouncingBallRemoved(Node2D ball);
  void OnBrickBreakerStarted();
  void OnBrickBreakerWon();
  void OnPowerUpPicked();
  void OnPianoNotePressed(int noteIndex);
  void OnPianoNoteReleased(int noteIndex);
  void OnPianoNoteStruck(int noteIndex);
  void OnPageFlipped();
  void OnWrongPianoNotePlayed();
  void OnPianoPuzzleWon();
  void OnButtonGameNotePlayed(int noteIndex);
  void OnButtonGameWrongNotePlayed();
  void OnButtonGameWon();

  void OnPlayerLandedOn(Node area, Vector2 position);
  void OnPlayerDying(Node? area, Vector2 position, EntityType type);
  void OnPlayerDying(Vector2 position, EntityType type);
  void OnPlayerDied();
  void OnPlayerJumped();
  void OnPlayerRotated(int direction);
  void OnPlayerLanded();
  void OnPlayerExploded();
  void OnPlayerFell();
  void OnPlayerSquashed();
  void OnPlayerDashed(Vector2 direction);
  void OnPlayerSlippering();
  void OnGemCollected(string colorGroup, Vector2 position, SpriteFrames frames);

  void OnMenuActionPressed(MenuAction action);
  void OnMenuBoxRotated();
  void OnPauseMenuEntered();
  void OnPauseMenuExited();
  void OnFocusChanged();
  void OnActionBound(string action, int key);
  void OnKeyboardActionBinding();
  void OnGamepadActionBound(string action, int buttonOrAxis, bool isAxis, float axisDirection);
  void OnLastUsedControllerChanged(ControllerType controller);
  void OnControllerSelectionChanged(ControllerType controller);
  void OnGamepadConnected(int deviceId, string deviceName);
  void OnGamepadDisconnected(int deviceId);
  void OnNotificationRaised(TranslationKey key);

  void OnCheckpointReached(Vector2 position, string colorGroup);
  void OnCheckpointLoaded();
  void OnCutsceneRequestStart(string id);
  void OnCutsceneRequestEnd(string id);
  void OnLevelCleared();
  void OnLevelRestartRequested();
  void OnDoorGemFilled();
  void OnDoorCometFormed();
  void OnDoorEntered(LevelId level);
  void OnSaveSlotUpdated();
}
