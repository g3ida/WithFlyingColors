namespace Wfc.Core.Event;

using System;
using Chickensoft.Sync.Primitives;
using Godot;
using Wfc.Core.Localization;
using Wfc.Core.Input.Controllers;
using Wfc.Entities.World;
using Wfc.Screens.Levels;
using Wfc.Screens.MenuManager;

public class GameEvents : IGameEvents {
  // Reached through the shared instance rather than only as a dependency: most of what raises
  // these has no way to take one - a powerup, a brick, a static settings class, a gem state
  // that is not a node at all - and the ones that could subscribe in _EnterTree, which runs
  // before AutoInject has resolved anything.
  private static GameEvents? _instance;
  public static GameEvents Instance => _instance ??= new GameEvents();

  private readonly AutoChannel _channel = new();
  public IAutoChannel Channel => _channel;

  private bool _disposed;

  #region Settings
  public void OnFullscreenToggled(bool isFullscreen) =>
    _channel.Send(new IGameEvents.FullscreenToggled(isFullscreen));

  public void OnVsyncToggled(bool isEnabled) =>
    _channel.Send(new IGameEvents.VsyncToggled(isEnabled));

  public void OnScreenSizeChanged(Vector2 size) =>
    _channel.Send(new IGameEvents.ScreenSizeChanged(size));

  public void OnLanguageChanged(Language language) =>
    _channel.Send(new IGameEvents.LanguageChanged(language));

  public void OnSkinChanged(string skin) =>
    _channel.Send(new IGameEvents.SkinChanged(skin));

  public void OnPerformanceOverlayToggled(bool isEnabled) =>
    _channel.Send(new IGameEvents.PerformanceOverlayToggled(isEnabled));

  public void OnSfxVolumeChanged(float volume) =>
    _channel.Send(new IGameEvents.SfxVolumeChanged(volume));

  public void OnMusicVolumeChanged(float volume) =>
    _channel.Send(new IGameEvents.MusicVolumeChanged(volume));
  #endregion Settings

  #region Camera
  public void RequestCameraShake(float amplitude) =>
    _channel.Send(new IGameEvents.CameraShakeRequested(amplitude));

  public void RequestCameraZoomPunch(float strength) =>
    _channel.Send(new IGameEvents.CameraZoomPunchRequested(strength));
  #endregion Camera

  #region Paint
  public void OnPaintSpilled(Vector2 position) =>
    _channel.Send(new IGameEvents.PaintSpilled(position));

  public void OnPaintPouring(Vector2 position) =>
    _channel.Send(new IGameEvents.PaintPouring(position));

  public void OnPaintSplashed(Vector2 position) =>
    _channel.Send(new IGameEvents.PaintSplashed(position));

  public void OnPaintGunCooling(Vector2 position) =>
    _channel.Send(new IGameEvents.PaintGunCooling(position));

  public void OnPaintGunFired(Vector2 position) =>
    _channel.Send(new IGameEvents.PaintGunFired(position));

  public void OnBucketShoved(Vector2 position) =>
    _channel.Send(new IGameEvents.BucketShoved(position));
  #endregion Paint

  #region Minigames
  public void OnTetrisLinesRemoved() => _channel.Send(new IGameEvents.TetrisLinesRemoved());

  public void OnTetrisPoolEscaped() => _channel.Send(new IGameEvents.TetrisPoolEscaped());

  public void OnBrickBroken(string colorGroup, Vector2 position) =>
    _channel.Send(new IGameEvents.BrickBroken(colorGroup, position));

  public void OnBouncingBallRemoved(Node2D ball) =>
    _channel.Send(new IGameEvents.BouncingBallRemoved(ball));

  public void OnBrickBreakerStarted() => _channel.Send(new IGameEvents.BrickBreakerStarted());

  public void OnBrickBreakerWon() => _channel.Send(new IGameEvents.BrickBreakerWon());

  public void OnPowerUpPicked() => _channel.Send(new IGameEvents.PowerUpPicked());

  public void OnPianoNotePressed(int noteIndex) =>
    _channel.Send(new IGameEvents.PianoNotePressed(noteIndex));

  public void OnPianoNoteReleased(int noteIndex) =>
    _channel.Send(new IGameEvents.PianoNoteReleased(noteIndex));

  public void OnPageFlipped() => _channel.Send(new IGameEvents.PageFlipped());

  public void OnWrongPianoNotePlayed() => _channel.Send(new IGameEvents.WrongPianoNotePlayed());

  public void OnPianoPuzzleWon() => _channel.Send(new IGameEvents.PianoPuzzleWon());

  public void OnButtonGameNotePlayed(int noteIndex) =>
    _channel.Send(new IGameEvents.ButtonGameNotePlayed(noteIndex));

  public void OnButtonGameWrongNotePlayed() =>
    _channel.Send(new IGameEvents.ButtonGameWrongNotePlayed());

  public void OnButtonGameWon() => _channel.Send(new IGameEvents.ButtonGameWon());
  #endregion Minigames

  #region Player
  public void OnPlayerLandedOn(Node area, Vector2 position) =>
    _channel.Send(new IGameEvents.PlayerLandedOn(area, position));

  public void OnPlayerDying(Node? area, Vector2 position, EntityType type) =>
    _channel.Send(new IGameEvents.PlayerDying(area, position, type));

  public void OnPlayerDying(Vector2 position, EntityType type) =>
    _channel.Send(new IGameEvents.PlayerDying(null, position, type));

  public void OnPlayerDied() => _channel.Send(new IGameEvents.PlayerDied());

  public void OnPlayerJumped() => _channel.Send(new IGameEvents.PlayerJumped());

  public void OnPlayerRotated(int direction) =>
    _channel.Send(new IGameEvents.PlayerRotated(direction));

  public void OnPlayerLanded() => _channel.Send(new IGameEvents.PlayerLanded());

  public void OnPlayerExploded() => _channel.Send(new IGameEvents.PlayerExploded());

  public void OnPlayerFell() => _channel.Send(new IGameEvents.PlayerFell());

  public void OnPlayerSquashed() => _channel.Send(new IGameEvents.PlayerSquashed());

  public void OnPlayerDashed(Vector2 direction) =>
    _channel.Send(new IGameEvents.PlayerDashed(direction));

  public void OnPlayerSlippering() => _channel.Send(new IGameEvents.PlayerSlippering());

  public void OnGemCollected(string colorGroup, Vector2 position, SpriteFrames frames) =>
    _channel.Send(new IGameEvents.GemCollected(colorGroup, position, frames));
  #endregion Player

  #region Menus and input
  public void OnMenuActionPressed(MenuAction action) =>
    _channel.Send(new IGameEvents.MenuActionPressed(action));

  public void OnMenuBoxRotated() => _channel.Send(new IGameEvents.MenuBoxRotated());

  public void OnPauseMenuEntered() => _channel.Send(new IGameEvents.PauseMenuEntered());

  public void OnPauseMenuExited() => _channel.Send(new IGameEvents.PauseMenuExited());

  public void OnFocusChanged() => _channel.Send(new IGameEvents.FocusChanged());

  public void OnActionBound(string action, int key) =>
    _channel.Send(new IGameEvents.ActionBound(action, key));

  public void OnKeyboardActionBinding() => _channel.Send(new IGameEvents.KeyboardActionBinding());

  public void OnGamepadActionBound(string action, int buttonOrAxis, bool isAxis, float axisDirection) =>
    _channel.Send(new IGameEvents.GamepadActionBound(action, buttonOrAxis, isAxis, axisDirection));

  public void OnLastUsedControllerChanged(ControllerType controller) =>
    _channel.Send(new IGameEvents.LastUsedControllerChanged(controller));

  public void OnControllerSelectionChanged(ControllerType controller) =>
    _channel.Send(new IGameEvents.ControllerSelectionChanged(controller));

  public void OnGamepadConnected(int deviceId, string deviceName) =>
    _channel.Send(new IGameEvents.GamepadConnected(deviceId, deviceName));

  public void OnGamepadDisconnected(int deviceId) =>
    _channel.Send(new IGameEvents.GamepadDisconnected(deviceId));

  public void OnNotificationRaised(TranslationKey key) =>
    _channel.Send(new IGameEvents.NotificationRaised(key));
  #endregion Menus and input

  #region Level and doors
  public void OnCheckpointReached(Vector2 position, string colorGroup) =>
    _channel.Send(new IGameEvents.CheckpointReached(position, colorGroup));

  public void OnCheckpointLoaded() => _channel.Send(new IGameEvents.CheckpointLoaded());

  public void OnCutsceneRequestStart(string id) =>
    _channel.Send(new IGameEvents.CutsceneRequestStart(id));

  public void OnCutsceneRequestEnd(string id) =>
    _channel.Send(new IGameEvents.CutsceneRequestEnd(id));

  public void OnLevelCleared() => _channel.Send(new IGameEvents.LevelCleared());

  public void OnLevelRestartRequested() => _channel.Send(new IGameEvents.LevelRestartRequested());

  public void OnDoorGemFilled() => _channel.Send(new IGameEvents.DoorGemFilled());

  public void OnDoorCometFormed() => _channel.Send(new IGameEvents.DoorCometFormed());

  public void OnDoorEntered(LevelId level) => _channel.Send(new IGameEvents.DoorEntered(level));

  public void OnSaveSlotUpdated() => _channel.Send(new IGameEvents.SaveSlotUpdated());
  #endregion Level and doors

  protected virtual void Dispose(bool disposing) {
    if (_disposed) {
      return;
    }
    if (disposing) {
      _channel.Dispose();
    }
    _disposed = true;
  }

  public void Dispose() {
    Dispose(disposing: true);
    GC.SuppressFinalize(this);
  }
}
