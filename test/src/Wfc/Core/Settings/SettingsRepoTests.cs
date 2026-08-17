namespace Wfc.Core.Settings.Test;

using System.Collections.Generic;
using Chickensoft.GoDotTest;
using Chickensoft.Sync.Primitives;
using Godot;
using Shouldly;
using Wfc.Core.Localization;
using Wfc.Core.Settings;

// The settings channel on its own - no scene, no tree, no autoload. Its whole reason for
// existing is that a message carries its own type all the way to the listener, so that is
// what these check, along with a binding going quiet once disposed.
public class SettingsRepoTests(Node testScene) : TestClass(testScene) {
  private SettingsRepo _repo = default!;

  [Setup]
  public void Setup() => _repo = new SettingsRepo();

  [Cleanup]
  public void Cleanup() => _repo.Dispose();

  [Test]
  public void ALanguageArrivesAsALanguageTest() {
    var seen = new List<Language>();
    using var binding = _repo.Channel.Bind()
      .On((in ISettingsRepo.LanguageChanged message) => seen.Add(message.Language));

    _repo.OnLanguageChanged(Language.French);

    seen.ShouldBe([Language.French]);
  }

  // Each message is its own type, so a listener for one hears nothing of the others - the
  // whole settings family used to share the bus and be told apart by name.
  [Test]
  public void AListenerHearsOnlyItsOwnMessageTest() {
    var fullscreen = 0;
    var vsync = 0;
    using var binding = _repo.Channel.Bind()
      .On((in ISettingsRepo.FullscreenToggled _) => fullscreen++)
      .On((in ISettingsRepo.VsyncToggled _) => vsync++);

    _repo.OnVsyncToggled(true);
    _repo.OnScreenSizeChanged(new Vector2(1920, 1080));
    _repo.OnSkinChanged("clear");

    fullscreen.ShouldBe(0);
    vsync.ShouldBe(1);
  }

  [Test]
  public void ADisposedBindingStopsListeningTest() {
    var heard = 0;
    var binding = _repo.Channel.Bind()
      .On((in ISettingsRepo.SkinChanged _) => heard++);

    _repo.OnSkinChanged("clear");
    binding.Dispose();
    _repo.OnSkinChanged("googl");

    heard.ShouldBe(1);
  }
}
