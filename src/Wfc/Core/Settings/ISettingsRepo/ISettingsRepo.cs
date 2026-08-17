namespace Wfc.Core.Settings;

using System;
using Chickensoft.Sync.Primitives;
using Godot;
using Wfc.Core.Localization;

// What the player changed in the settings screen, as values rather than as Godot signals: these
// cross no Variant boundary, so a language arrives as a Language and a bad payload is a compile
// error rather than a conversion failure inside a native emit.
//
// Only what the player picks is announced here. Applying a stored setting on startup is not a
// change and raises nothing, which is what lets a listener treat every message as an action.
public interface ISettingsRepo : IDisposable {
  IAutoChannel Channel { get; }

  readonly record struct FullscreenToggled(bool IsFullscreen);
  readonly record struct VsyncToggled(bool IsEnabled);
  readonly record struct ScreenSizeChanged(Vector2 Size);
  readonly record struct LanguageChanged(Language Language);
  readonly record struct SkinChanged(string Skin);

  void OnFullscreenToggled(bool isFullscreen);
  void OnVsyncToggled(bool isEnabled);
  void OnScreenSizeChanged(Vector2 size);
  void OnLanguageChanged(Language language);
  void OnSkinChanged(string skin);
}
