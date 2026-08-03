namespace Wfc.test.instrumented.Backgrounds;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Backgrounds;
using Wfc.Screens.Levels;
using Wfc.Skin;
using Wfc.test.instrumented.Helpers;
using Wfc.Utils;

// The space backdrop is pure scenery, so what can break silently is wiring: a
// hub that lost its background node, particles tinted off-palette, or scenery
// that turns out to sit in world space and scroll away with the camera. The
// scene needs no dependency provider - the skin singleton is plain C#.
public class SpaceBackgroundTests(Node testScene) : TestClass(testScene) {
  private const int PINNED_SEED = 1234;
  // Mirrors the field's private wrap margin; a floater beyond it has escaped.
  private const float WRAP_MARGIN = 40f;
  private const double COMET_TIMEOUT_SECONDS = 4.0;

  private SpaceBackground _background = default!;

  [Setup]
  public async Task Setup() {
    _background = SceneHelpers.InstantiateNode<SpaceBackground>();
    _particles().Seed = PINNED_SEED;
    _starField().Seed = PINNED_SEED;
    TestScene.AddChild(_background);
    await PhysicsFrames.Frame(TestScene);
  }

  [Cleanup]
  public void Cleanup() => _background.QueueFree();

  [Test]
  public void TheHubDeclaresTheSpaceBackground() {
    var hub = LevelDispatcher.InstantiateLevel(LevelId.Hub);

    hub.ShouldNotBeNull();
    hub.GetNodeOrNull("Background").ShouldBeAssignableTo<LevelBackground>(
      "the hub lost its background, or it is no longer a LevelBackground");
    hub.GetNodeOrNull("Background").ShouldBeOfType<SpaceBackground>();
    hub.QueueFree();
  }

  // Behind the world and pinned to the screen: a background on a positive layer
  // would cover the level, and one following the canvas would scroll away.
  [Test]
  public void TheBackdropSitsBehindTheWorldOnItsOwnScreenFixedLayer() {
    _background.Layer.ShouldBeLessThan(0);
    _background.FollowViewportEnabled.ShouldBeFalse();
  }

  [Test]
  public void TheFieldSpawnsItsFloatersInSkinColorsInsideTheScreen() {
    var field = _particles();
    var screen = TestScene.GetViewport().GetVisibleRect().Grow(WRAP_MARGIN);
    var palette = _skinPalette();

    field.Floaters.Count.ShouldBe(field.FloaterCount);
    foreach (var floater in field.Floaters) {
      palette.ShouldContain(floater.Color, "a floater is tinted off the skin palette");
      screen.HasPoint(floater.Position).ShouldBeTrue("a floater spawned or escaped off-screen");
    }
  }

  [Test]
  public async Task TheFloatersDriftAndKeepTwinklingInsideTheWrapMargin() {
    var field = _particles();
    var before = field.Floaters[0].Position;

    await _wallWait(0.3);

    field.Floaters[0].Position.ShouldNotBe(before, "the field is not animating");
    var screen = TestScene.GetViewport().GetVisibleRect().Grow(WRAP_MARGIN);
    foreach (var floater in field.Floaters) {
      screen.HasPoint(floater.Position).ShouldBeTrue("a drifting floater escaped the wrap margin");
    }
  }

  [Test]
  public async Task CometsLaunchOverTimeAndNeverExceedTheirBudget() {
    // A fresh instance: the launch cadence is latched at ready, so the interval
    // has to be shrunk before the background enters the tree.
    var eager = SceneHelpers.InstantiateNode<SpaceBackground>();
    var field = eager.GetNode<SpaceParticleField>("Particles");
    field.Seed = PINNED_SEED;
    field.CometIntervalSec = 0.05f;
    TestScene.AddChild(eager);
    try {
      (await _waitUntil(() => field.ActiveCometCount > 0, COMET_TIMEOUT_SECONDS))
        .ShouldBeTrue("no comet ever launched");
      field.ActiveCometCount.ShouldBeLessThanOrEqualTo(field.MaxComets);
    }
    finally {
      eager.QueueFree();
    }
  }

  [Test]
  public void TheSkyIsLaidOutOnceWithinTheScreenAndPinnedBySeed() {
    var sky = _starField();
    // Grown a pixel: the random range placing stars includes the far edge.
    var screen = TestScene.GetViewport().GetVisibleRect().Grow(1);

    sky.Stars.Count.ShouldBe(sky.StarCount);
    sky.Constellations.Count.ShouldBe(sky.ConstellationCount);
    foreach (var star in sky.Stars) {
      screen.HasPoint(star.Position).ShouldBeTrue("a star was placed off-screen");
    }
    foreach (var chart in sky.Constellations) {
      chart.Length.ShouldBeGreaterThanOrEqualTo(2, "a constellation needs at least one line");
      foreach (var point in chart) {
        screen.HasPoint(point).ShouldBeTrue("a constellation wandered off-screen");
      }
    }

    // Same seed, same sky: the layout is reproducible, not a fresh roll.
    var twin = SceneHelpers.InstantiateNode<SpaceBackground>();
    var twinSky = twin.GetNode<StarField>("StarField");
    twinSky.Seed = PINNED_SEED;
    TestScene.AddChild(twin);
    try {
      twinSky.Stars[0].ShouldBe(sky.Stars[0]);
      twinSky.Constellations[0][0].ShouldBe(sky.Constellations[0][0]);
    }
    finally {
      twin.QueueFree();
    }
  }

  // The galaxies and constellations deliberately stay out of the skin palette:
  // pure greys, so the colored particles carry all the color.
  [Test]
  public void TheGalaxiesKeepTheirGreyscaleArt() {
    foreach (var galaxy in _background.GetNode("Galaxies").GetChildren()) {
      var sprite = galaxy.ShouldBeOfType<Sprite2D>($"{galaxy.Name} is not a sprite");
      sprite.Texture.ShouldNotBeNull($"{sprite.Name} lost its texture");

      var image = sprite.Texture.GetImage();
      image.ShouldNotBeNull();
      var center = image.GetPixel(image.GetWidth() / 2, image.GetHeight() / 2);
      center.A.ShouldBeGreaterThan(0f, $"{sprite.Name} is fully transparent at its core");
      center.R.ShouldBe(center.G, 0.02f, $"{sprite.Name} is not greyscale");
      center.G.ShouldBe(center.B, 0.02f, $"{sprite.Name} is not greyscale");
    }
  }

  private SpaceParticleField _particles() => _background.GetNode<SpaceParticleField>("Particles");

  private StarField _starField() => _background.GetNode<StarField>("StarField");

  private static List<Color> _skinPalette() {
    var skin = SkinManager.Instance.CurrentSkin;
    var palette = new List<Color>();
    foreach (var face in Enum.GetValues<SkinColor>()) {
      palette.Add(skin.GetColor(face, SkinColorIntensity.Basic));
      palette.Add(skin.GetColor(face, SkinColorIntensity.Light));
    }
    return palette;
  }

  // Wall-clock rather than frame-counting: drift and comet cadence accumulate
  // _Process deltas, which follow real time however fast a headless run frames.
  private async Task<bool> _waitUntil(Func<bool> condition, double timeoutSeconds) {
    var tree = TestScene.GetTree();
    var deadline = Time.GetTicksMsec() + (ulong)(timeoutSeconds * 1000);
    while (Time.GetTicksMsec() < deadline) {
      if (condition()) {
        return true;
      }
      await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
    return condition();
  }

  private async Task _wallWait(double seconds) {
    var tree = TestScene.GetTree();
    var deadline = Time.GetTicksMsec() + (ulong)(seconds * 1000);
    while (Time.GetTicksMsec() < deadline) {
      await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
  }
}
