namespace Wfc.test.instrumented.Backgrounds;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Backgrounds;
using Wfc.Screens.Levels;
using Wfc.Skin;
using Wfc.test.instrumented.Helpers;
using Wfc.Utils;

// The city is scenery, so what breaks silently is its wiring: a layer that
// follows the viewport slides out of the screen-fixed backdrop and leaves the
// level bare, a row that misses its repeat opens a seam, and windows drifting
// back toward the skin palette start reading as gameplay color. The scene needs
// no dependency provider - the skin singleton is plain C#.
public class CityBackgroundTests(Node testScene) : TestClass(testScene) {
  private const int PINNED_SEED = 4321;
  private const float TEST_SPAN = 1920f;
  // Saturation and value a lit window has to stay under whatever the skin is,
  // or it starts competing with the colors the player has to match.
  private const float WASHED_SATURATION_MAX = 0.35f;
  private const float WASHED_VALUE_MAX = 0.7f;

  private CityBackground _background = default!;

  [Setup]
  public async Task Setup() {
    _background = SceneHelpers.InstantiateNode<CityBackground>();
    TestScene.AddChild(_background);
    await PhysicsFrames.Frame(TestScene);
  }

  [Cleanup]
  public void Cleanup() => _background.QueueFree();

  [Test]
  public void TheFourColorsLevelDeclaresTheCityBackground() {
    var level = LevelDispatcher.InstantiateLevel(LevelId.FourColors);

    level.ShouldNotBeNull();
    level.GetNodeOrNull("Background").ShouldBeAssignableTo<LevelBackground>(
      "the level lost its background, or it is no longer a LevelBackground");
    level.GetNodeOrNull("Background").ShouldBeOfType<CityBackground>();
    level.QueueFree();
  }

  // Behind the world and pinned to the screen: a background on a positive layer
  // would cover the level, and one following the canvas would scroll away.
  [Test]
  public void TheBackdropSitsBehindTheWorldOnItsOwnScreenFixedLayer() {
    _background.Layer.ShouldBeLessThan(0);
    _background.FollowViewportEnabled.ShouldBeFalse();
  }

  // Each skyline offsets itself against the camera, and how far it may stray
  // depends on whether the layer it lives on already moves. Disagree with the
  // CanvasLayer and the rows slide off the screen partway along a wide level.
  [Test]
  public void EverySkylineTilesItselfAndMatchesTheLayerItSitsOn() {
    foreach (var skyline in _skylines()) {
      skyline.FollowViewport.ShouldBe(_background.FollowViewportEnabled,
        $"{skyline.Name} disagrees with the CanvasLayer about following the viewport");
      skyline.RepeatSize.X.ShouldBeGreaterThan(0f, $"{skyline.Name} has no repeat to tile");
      // One copy covers the screen only when the row happens to be aligned with
      // it; the neighbours either side are what keep it filled in between.
      skyline.RepeatTimes.ShouldBeGreaterThanOrEqualTo(3, $"{skyline.Name} cannot fill a screen");
    }
  }

  // The depth reads from the order: further away scrolls slower and hazes toward
  // the sky, nearer scrolls faster and darkens toward a silhouette.
  [Test]
  public void TheSkylinesRunFromHazeToSilhouette() {
    var skylines = _skylines();

    skylines.Count.ShouldBeGreaterThanOrEqualTo(2, "one layer is not a parallax");
    foreach (var (behind, front) in skylines.Zip(skylines.Skip(1))) {
      front.ScrollScale.X.ShouldBeGreaterThan(behind.ScrollScale.X,
        $"{front.Name} does not scroll past {behind.Name}");
      front.BuildingColor.V.ShouldBeLessThan(behind.BuildingColor.V,
        $"{front.Name} is not darker than {behind.Name}");
    }
  }

  // Height is where the illusion breaks most easily: a skyline that rises and
  // falls with the player reads as a wall a few paces back rather than a city on
  // the horizon, so vertically each layer trails far behind its own scroll.
  [Test]
  public void TheSkylinesBarelyRiseAndFallWithTheCamera() {
    foreach (var skyline in _skylines()) {
      skyline.ScrollScale.Y.ShouldBeGreaterThan(0f, $"{skyline.Name} is pinned vertically");
      skyline.ScrollScale.Y.ShouldBeLessThan(skyline.ScrollScale.X / 2f,
        $"{skyline.Name} follows the camera up and down nearly as fast as sideways");
    }
  }

  [Test]
  public void EachRowFillsItsRepeatWithoutCrossingTheSeam() {
    foreach (var skyline in _skylines()) {
      skyline.Buildings.Count.ShouldBeGreaterThan(0, $"{skyline.Name} laid out no blocks");
      foreach (var building in skyline.Buildings) {
        building.Body.Position.X.ShouldBeGreaterThanOrEqualTo(0f,
          $"a block starts left of {skyline.Name}'s repeat");
        building.Body.End.X.ShouldBeLessThanOrEqualTo(skyline.SpanWidth + 0.01f,
          $"a block runs past {skyline.Name}'s repeat and will overlap the next tile");
      }
    }
  }

  [Test]
  public void EveryLitWindowSitsOnABlock() {
    foreach (var skyline in _skylines()) {
      foreach (var window in skyline.Windows) {
        skyline.Buildings.Any(building => building.Body.Encloses(window.Rect))
          .ShouldBeTrue($"a window on {skyline.Name} is not on any block");
      }
    }
  }

  // The whole point of the tint: a window borrows the skin's hue and nothing
  // else, so it can never be mistaken for a color the player has to match.
  [Test]
  public void LitWindowsAreWashedOutOfTheSkinPalette() {
    var skin = SkinManager.Instance.CurrentSkin;
    var faces = Enum.GetValues<SkinColor>();

    foreach (var skyline in _skylines()) {
      skyline.Palette.Count.ShouldBe(faces.Length);
      for (var i = 0; i < faces.Length; i++) {
        var washed = skyline.Palette[i];
        var source = skin.GetColor(faces[i], SkinColorIntensity.Light);

        washed.S.ShouldBeLessThan(source.S / 2f, $"{faces[i]} kept too much of its saturation");
        washed.S.ShouldBeLessThan(WASHED_SATURATION_MAX,
          $"{faces[i]} still reads as a game color - WindowVibrance is too high");
        washed.V.ShouldBeLessThan(WASHED_VALUE_MAX, $"{faces[i]} is too bright for a night window");
      }
      foreach (var window in skyline.Windows) {
        skyline.Palette.ShouldContain(window.Color, $"a window on {skyline.Name} is off-palette");
      }
    }
  }

  [Test]
  public async Task SomeWindowsSwitchWhileTheRestStayLit() {
    var skyline = _lone(seed: PINNED_SEED, toggleIntervalSec: 0.05f);
    try {
      skyline.Switchers.Count.ShouldBeGreaterThan(0, "no window can ever switch");
      var before = skyline.Windows.Select(window => window.Alpha).ToArray();

      await _wallWait(0.6);

      var switchers = skyline.Switchers.ToHashSet();
      var changed = 0;
      for (var i = 0; i < skyline.Windows.Count; i++) {
        var alpha = skyline.Windows[i].Alpha;
        alpha.ShouldBeInRange(0f, 1f, "a window faded outside its own range");
        if (!switchers.Contains(i)) {
          alpha.ShouldBe(before[i], "a window that never switches went out anyway");
        }
        else if (alpha != before[i]) {
          changed++;
        }
      }
      changed.ShouldBeGreaterThan(0, "nothing ever switched");
    }
    finally {
      skyline.QueueFree();
    }
  }

  // Distracting is the failure mode here: at the cadence the scene ships, a
  // window cannot finish going out inside the time this test watches it.
  [Test]
  public async Task TheSwitchingIsTooSlowToDistract() {
    var skyline = _lone(seed: PINNED_SEED);
    try {
      var before = skyline.Windows.Select(window => window.Alpha).ToArray();

      await _wallWait(0.5);

      for (var i = 0; i < skyline.Windows.Count; i++) {
        Mathf.Abs(skyline.Windows[i].Alpha - before[i])
          .ShouldBeLessThan(0.5f, "a window is blinking rather than fading");
      }
    }
    finally {
      skyline.QueueFree();
    }
  }

  // Same seed, same city: the layout is reproducible, not a fresh roll.
  [Test]
  public void TheRowIsPinnedBySeed() {
    var first = _lone(seed: PINNED_SEED);
    var second = _lone(seed: PINNED_SEED);
    try {
      second.Buildings.Count.ShouldBe(first.Buildings.Count);
      second.Buildings[0].ShouldBe(first.Buildings[0]);
      second.Windows.Count.ShouldBe(first.Windows.Count);
      second.Windows[0].Rect.ShouldBe(first.Windows[0].Rect);
    }
    finally {
      first.QueueFree();
      second.QueueFree();
    }
  }

  private List<CitySkyline> _skylines() => [.. _background.GetChildren().OfType<CitySkyline>()];

  // A skyline built by hand rather than off the scene: the cadence is latched at
  // ready, so it has to be set before the node enters the tree.
  private CitySkyline _lone(int seed, float toggleIntervalSec = 12f) {
    var skyline = new CitySkyline {
      Seed = seed,
      RepeatSize = new Vector2(TEST_SPAN, 0),
      ToggleIntervalSec = toggleIntervalSec,
    };
    TestScene.AddChild(skyline);
    return skyline;
  }

  // Wall-clock rather than frame-counting: the fades and the toggle cadence
  // accumulate _Process deltas, which follow real time however fast a headless
  // run frames.
  private async Task _wallWait(double seconds) {
    var tree = TestScene.GetTree();
    var deadline = Time.GetTicksMsec() + (ulong)(seconds * 1000);
    while (Time.GetTicksMsec() < deadline) {
      await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
  }
}
