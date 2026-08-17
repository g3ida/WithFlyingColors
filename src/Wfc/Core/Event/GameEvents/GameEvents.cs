namespace Wfc.Core.Event;

using System;
using Chickensoft.Sync.Primitives;
using Godot;
using Wfc.Core.Localization;
using Wfc.Entities.World;

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
