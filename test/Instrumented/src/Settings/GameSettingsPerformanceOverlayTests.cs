namespace Wfc.test.instrumented.Settings;

using System.Collections.Generic;
using Chickensoft.GoDotTest;
using Chickensoft.Sync.Primitives;
using Godot;
using Shouldly;
using Wfc.Core.Event;
using Wfc.Core.Settings;
using EventHandler = Wfc.Core.Event.EventHandler;

// The overlay is drawn by a node that is ready before the settings file has been read, so
// the setting has to announce itself rather than wait to be asked.
public class GameSettingsPerformanceOverlayTests(Node testScene) : TestClass(testScene) {
  private const string SCRATCH_CONFIG_PATH = "user://test-performance-overlay-settings.ini";

  private static readonly bool[] ON_THEN_OFF = [true, false];

  private static readonly string[] GAME_ACTIONS =
    ["move_left", "move_right", "jump", "rotate_left", "rotate_right", "pause", "dash", "down"];

  private readonly Dictionary<string, Godot.Collections.Array<InputEvent>> _savedEvents = new();
  private string _configPathBeforeTest = default!;
  private bool _overlayBeforeTest;
  private bool _fullscreenBeforeTest;
  private bool _vsyncBeforeTest;

  // Loading rebinds the InputMap and touches the window, both of which are process wide and
  // neither of which the tests that run after this one asked for.
  [Setup]
  public void Setup() {
    _savedEvents.Clear();
    foreach (var action in GAME_ACTIONS) {
      _savedEvents[action] = InputMap.ActionGetEvents(action);
    }
    _configPathBeforeTest = GameSettings.ConfigFilePath;
    _overlayBeforeTest = GameSettings.PerformanceOverlay;
    _fullscreenBeforeTest = GameSettings.Fullscreen;
    _vsyncBeforeTest = GameSettings.Vsync;
    GameSettings.ConfigFilePath = SCRATCH_CONFIG_PATH;
    GameSettings.PerformanceOverlay = false;
  }

  [Cleanup]
  public void Cleanup() {
    foreach (var (action, events) in _savedEvents) {
      InputMap.ActionEraseEvents(action);
      foreach (var @event in events) {
        InputMap.ActionAddEvent(action, @event);
      }
    }
    DirAccess.RemoveAbsolute(SCRATCH_CONFIG_PATH);
    GameSettings.ConfigFilePath = _configPathBeforeTest;
    GameSettings.PerformanceOverlay = _overlayBeforeTest;
    GameSettings.Fullscreen = _fullscreenBeforeTest;
    GameSettings.Vsync = _vsyncBeforeTest;
  }

  [Test]
  public void TheOverlayIsOffUntilItIsAskedFor() {
    GameSettings.PerformanceOverlay.ShouldBeFalse();
  }

  [Test]
  public void ASavedOverlayComesBackOnTheNextLaunch() {
    GameSettings.PerformanceOverlay = true;
    GameSettings.Save();
    GameSettings.PerformanceOverlay = false;

    GameSettings.Load();

    GameSettings.PerformanceOverlay.ShouldBeTrue();
  }

  [Test]
  public void TurningItOnAnnouncesItself() {
    var announced = new List<bool>();
    using var binding = GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.PerformanceOverlayToggled message) => announced.Add(message.IsEnabled));

    GameSettings.PerformanceOverlay = true;
    // Setting it to what it already is says nothing.
    GameSettings.PerformanceOverlay = true;
    GameSettings.PerformanceOverlay = false;

    announced.ShouldBe(ON_THEN_OFF);
  }

  // The upgrade path: a settings file written before the game had an overlay.
  [Test]
  public void AFileWithNoOverlayInItLeavesItOff() {
    var configFile = new ConfigFile();
    configFile.SetValue("display", "vsync", true);
    configFile.Save(SCRATCH_CONFIG_PATH).ShouldBe(Error.Ok);

    GameSettings.Load();

    GameSettings.PerformanceOverlay.ShouldBeFalse();
  }
}
