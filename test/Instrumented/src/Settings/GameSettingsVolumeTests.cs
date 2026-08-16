namespace Wfc.test.instrumented.Settings;

using System.Collections.Generic;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Settings;

// A volume is held on its bus as a level and a mute flag, and Save reads the setting back off
// the bus rather than out of a field. Anything the two halves disagree about is therefore what
// gets written to the settings file.
public class GameSettingsVolumeTests(Node testScene) : TestClass(testScene) {
  private const string SCRATCH_CONFIG_PATH = "user://test-volume-settings.ini";

  // Loose enough for the trip through decibels and back, tight enough to tell the positions of
  // a slider apart.
  private const float TOLERANCE = 0.01f;

  private static readonly string[] GAME_ACTIONS =
    ["move_left", "move_right", "jump", "rotate_left", "rotate_right", "pause", "dash", "down"];

  private readonly Dictionary<string, Godot.Collections.Array<InputEvent>> _savedEvents = new();
  private string _configPathBeforeTest = default!;
  private float _sfxBeforeTest;
  private float _musicBeforeTest;
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
    _sfxBeforeTest = GameSettings.SfxVolume;
    _musicBeforeTest = GameSettings.MusicVolume;
    _fullscreenBeforeTest = GameSettings.Fullscreen;
    _vsyncBeforeTest = GameSettings.Vsync;
    GameSettings.ConfigFilePath = SCRATCH_CONFIG_PATH;
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
    GameSettings.SfxVolume = _sfxBeforeTest;
    GameSettings.MusicVolume = _musicBeforeTest;
    GameSettings.Fullscreen = _fullscreenBeforeTest;
    GameSettings.Vsync = _vsyncBeforeTest;
  }

  [Test]
  public void ASliderReadsBackWhereItWasPutTest() {
    GameSettings.SfxVolume = 0.5f;

    GameSettings.SfxVolume.ShouldBe(0.5f, TOLERANCE);
  }

  // The bottom of the range mutes, and a muted bus has to report the position the slider is
  // actually at rather than the one it was at before it was turned down.
  [Test]
  public void ASilencedSliderReadsBackAsSilentTest() {
    GameSettings.SfxVolume = 1.0f;

    GameSettings.SfxVolume = 0f;

    GameSettings.SfxVolume.ShouldBe(0f, TOLERANCE);
    AudioServer.IsBusMute(AudioServer.GetBusIndex("sfx")).ShouldBeTrue();
  }

  [Test]
  public void TurningItBackUpLetsTheSoundThroughAgainTest() {
    GameSettings.SfxVolume = 0f;

    GameSettings.SfxVolume = 0.75f;

    GameSettings.SfxVolume.ShouldBe(0.75f, TOLERANCE);
    AudioServer.IsBusMute(AudioServer.GetBusIndex("sfx")).ShouldBeFalse();
  }

  // The regression this file exists for: silence used to be written out as whatever the bus
  // had been set to before it was muted, so a player who turned the sound off got it back at
  // full volume on the next launch.
  [Test]
  public void SilenceSurvivesTheNextLaunchTest() {
    GameSettings.SfxVolume = 0f;
    GameSettings.MusicVolume = 0f;
    GameSettings.Save();
    GameSettings.SfxVolume = 1.0f;
    GameSettings.MusicVolume = 1.0f;

    GameSettings.Load();

    GameSettings.SfxVolume.ShouldBe(0f, TOLERANCE);
    GameSettings.MusicVolume.ShouldBe(0f, TOLERANCE);
  }

  [Test]
  public void AVolumeInBetweenSurvivesTheNextLaunchTest() {
    GameSettings.SfxVolume = 0.4f;
    GameSettings.Save();
    GameSettings.SfxVolume = 1.0f;

    GameSettings.Load();

    GameSettings.SfxVolume.ShouldBe(0.4f, TOLERANCE);
  }
}
