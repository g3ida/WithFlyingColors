namespace Wfc.Core.Event.Test;

using System.Collections.Generic;
using Chickensoft.GoDotTest;
using Chickensoft.Sync.Primitives;
using Godot;
using Shouldly;
using Wfc.Core.Localization;

// The channel on its own - no scene, no tree, no autoload. Its whole reason for existing is
// that a message carries its own type all the way to the listener, so that is what these
// check, along with a binding going quiet once disposed.
public class GameEventsTests(Node testScene) : TestClass(testScene) {
  private GameEvents _events = default!;

  [Setup]
  public void Setup() => _events = new GameEvents();

  [Cleanup]
  public void Cleanup() => _events.Dispose();

  [Test]
  public void ALanguageArrivesAsALanguageTest() {
    var seen = new List<Language>();
    using var binding = _events.Channel.Bind()
      .On((in IGameEvents.LanguageChanged message) => seen.Add(message.Language));

    _events.OnLanguageChanged(Language.French);

    seen.ShouldBe([Language.French]);
  }

  // Each message is its own type, so a listener for one hears nothing of the others - the
  // whole settings family used to share the bus and be told apart by name.
  [Test]
  public void AListenerHearsOnlyItsOwnMessageTest() {
    var fullscreen = 0;
    var vsync = 0;
    using var binding = _events.Channel.Bind()
      .On((in IGameEvents.FullscreenToggled _) => fullscreen++)
      .On((in IGameEvents.VsyncToggled _) => vsync++);

    _events.OnVsyncToggled(true);
    _events.OnScreenSizeChanged(new Vector2(1920, 1080));
    _events.OnSkinChanged("clear");

    fullscreen.ShouldBe(0);
    vsync.ShouldBe(1);
  }

  [Test]
  public void ADisposedBindingStopsListeningTest() {
    var heard = 0;
    var binding = _events.Channel.Bind()
      .On((in IGameEvents.SkinChanged _) => heard++);

    _events.OnSkinChanged("clear");
    binding.Dispose();
    _events.OnSkinChanged("googl");

    heard.ShouldBe(1);
  }
}
