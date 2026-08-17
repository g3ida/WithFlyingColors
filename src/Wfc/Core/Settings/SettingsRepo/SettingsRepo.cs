namespace Wfc.Core.Settings;

using System;
using Chickensoft.Sync.Primitives;
using Godot;
using Wfc.Core.Localization;

public class SettingsRepo : ISettingsRepo {
  // Reachable both ways for the same reason EventHandler is: the settings screen's nodes take
  // it as a dependency, while the ones that cannot - a [Tool] label, a manager the autoloads
  // build by hand - still have to hear the same messages.
  private static SettingsRepo? _instance;
  public static SettingsRepo Instance => _instance ??= new SettingsRepo();

  private readonly AutoChannel _channel = new();
  public IAutoChannel Channel => _channel;

  private bool _disposed;

  public void OnFullscreenToggled(bool isFullscreen) =>
    _channel.Send(new ISettingsRepo.FullscreenToggled(isFullscreen));

  public void OnVsyncToggled(bool isEnabled) =>
    _channel.Send(new ISettingsRepo.VsyncToggled(isEnabled));

  public void OnScreenSizeChanged(Vector2 size) =>
    _channel.Send(new ISettingsRepo.ScreenSizeChanged(size));

  public void OnLanguageChanged(Language language) =>
    _channel.Send(new ISettingsRepo.LanguageChanged(language));

  public void OnSkinChanged(string skin) =>
    _channel.Send(new ISettingsRepo.SkinChanged(skin));

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
