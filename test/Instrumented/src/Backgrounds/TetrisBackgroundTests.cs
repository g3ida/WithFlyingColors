namespace Wfc.test.instrumented.Backgrounds;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Backgrounds;
using Wfc.Screens.Levels;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.test.instrumented.Helpers;

// The tetris backdrop is scenery, and what breaks scenery is silent: a layer that follows the
// viewport slides out of a screen-fixed backdrop, a field that misses its repeat opens a seam, and
// an outline drifting back toward the skin palette starts reading as something to land on. The
// scene needs no dependency provider - the skin singleton is plain C#.
public class TetrisBackgroundTests(Node testScene) : TestClass(testScene) {
  // What an outline has to stay under whatever the skin is, or it starts competing with the four
  // colors the player reads a surface by.
  private const float WASHED_SATURATION_MAX = 0.62f;
  private const float WASHED_VALUE_MAX = 0.78f;

  // Long enough to cover the slowest float the fields are set to, sampled finely enough that a
  // shape cannot reach its furthest between two of them.
  private const int SWAY_SAMPLES = 160;
  private const float SWAY_SAMPLE_SECONDS = 0.2f;

  private TetrisBackground _background = default!;

  [Setup]
  public async Task Setup() {
    _background = SceneHelpers.InstantiateNode<TetrisBackground>();
    TestScene.AddChild(_background);
    await PhysicsFrames.Frame(TestScene);
  }

  [Cleanup]
  public void Cleanup() => _background.QueueFree();

  [Test]
  public void TheTetrisLevelDeclaresTheTetrisBackground() {
    var level = LevelDispatcher.InstantiateLevel(LevelId.Tetris);

    level.ShouldNotBeNull();
    level.GetNodeOrNull("Background").ShouldBeAssignableTo<LevelBackground>(
      "the level lost its background, or it is no longer a LevelBackground");
    level.GetNodeOrNull("Background").ShouldBeOfType<TetrisBackground>();
    level.QueueFree();
  }

  // Behind the world and pinned to the screen: a background on a positive layer would cover the
  // level, and one following the canvas would scroll away.
  [Test]
  public void TheBackdropSitsBehindTheWorldOnItsOwnScreenFixedLayer() {
    _background.Layer.ShouldBeLessThan(0);
    _background.FollowViewportEnabled.ShouldBeFalse();
  }

  // Each field offsets itself against the camera, and how far it may stray depends on whether the
  // layer it lives on already moves. Disagree with the CanvasLayer and the fields slide off the
  // screen partway along a wide level.
  [Test]
  public void EveryFieldTilesItselfAndMatchesTheLayerItSitsOn() {
    foreach (var field in _fields()) {
      field.FollowViewport.ShouldBe(_background.FollowViewportEnabled,
        $"{field.Name} disagrees with the CanvasLayer about following the viewport");
      field.SpanWidth.ShouldBeGreaterThan(0.0f, $"{field.Name} has no repeat to tile across");
      field.SpanHeight.ShouldBeGreaterThan(0.0f, $"{field.Name} has no repeat to tile down");
      field.RepeatTimes.ShouldBeGreaterThanOrEqualTo(3, $"{field.Name} cannot fill a screen");
    }
  }

  // The whole point of washing the palette out. A background shape in full game color hanging in
  // the air is a platform until proven otherwise, and proving otherwise costs a life - so no skin
  // may push one back into the range the player reads a surface by.
  [Test]
  public async Task NoOutlineReachesGameColourUnderAnySkin() {
    var restore = SkinManager.Instance.CurrentSkinName;
    try {
      foreach (var skin in SkinManager.SELECTABLE_SKINS) {
        SkinManager.Instance.SetCurrentSkin(skin);
        // The palette is read once when a field is laid out, so the skin has to be in place before
        // the backdrop is stood up rather than after.
        Cleanup();
        await Setup();

        foreach (var field in _fields()) {
          field.Outlines.ShouldNotBeEmpty($"{field.Name} laid out no shapes under the {skin} skin");
          foreach (var color in field.Outlines.SelectMany(outline => outline.Colors)) {
            color.S.ShouldBeLessThanOrEqualTo(WASHED_SATURATION_MAX,
              $"{field.Name} is drawing a {skin} shape at game saturation");
            color.V.ShouldBeLessThanOrEqualTo(WASHED_VALUE_MAX,
              $"{field.Name} is drawing a {skin} shape at game brightness");
            color.A.ShouldBeLessThan(1.0f, $"{field.Name} is drawing a {skin} shape at full opacity");
          }
        }
      }
    }
    finally {
      SkinManager.Instance.SetCurrentSkin(restore);
    }
  }

  // A shape is an outline of something, so it has to come back round to where it started. An open
  // one reads as a stray line across the backdrop.
  [Test]
  public void EveryOutlineClosesOnItself() {
    foreach (var field in _fields()) {
      foreach (var outline in field.Outlines) {
        outline.Points.Length.ShouldBeGreaterThan(3, $"{field.Name} has an outline that is not a shape");
        outline.Points[^1].ShouldBe(outline.Points[0], $"{field.Name} has an outline that never closes");
        outline.Colors.Length.ShouldBe(outline.Points.Length,
          $"{field.Name} has an outline with a color per point missing");
      }
    }
  }

  // The field is a tile, so a shape crossing its edge is a shape sliced in half every time the tile
  // repeats - which is what a seam looks like. Measured across the float rather than at rest: one
  // that only clears the edge while it happens to be still is sliced twice a cycle.
  [Test]
  public void NoShapeIsCutInHalfWhereTheFieldRepeats() {
    foreach (var field in _fields()) {
      var span = new Rect2(Vector2.Zero, new Vector2(field.SpanWidth, field.SpanHeight));
      foreach (var outline in field.Outlines) {
        span.Encloses(_sweptBounds(field, outline))
          .ShouldBeTrue($"{field.Name} has a shape that floats outside the tile it repeats");
      }
    }
  }

  // Adrift, not animated: every shape has to be somewhere else a moment later, and nowhere far.
  [Test]
  public void EveryShapeFloatsWithoutWanderingOff() {
    foreach (var field in _fields()) {
      foreach (var outline in field.Outlines) {
        var resting = _boundsOf(outline.Points);
        var swept = _sweptBounds(field, outline);

        (swept.Size - resting.Size).Length().ShouldBeGreaterThan(
          1.0f, $"{field.Name} has a shape that never moves");
        // The room the scatter reserves around every shape, which is what keeps two of them from
        // meeting in the air and a shape from reaching over the edge of the tile.
        resting.Grow(field.Sway).Encloses(swept).ShouldBeTrue(
          $"{field.Name} has a shape that wanders further than the room reserved for it");
      }
    }
  }

  // Depth reads from the order: further away scrolls slower and fades toward the backdrop, nearer
  // scrolls faster and holds more of its color.
  [Test]
  public void TheFieldsRunFromFarToNear() {
    var fields = _fields();
    fields.Count.ShouldBeGreaterThan(1, "one field is a flat backdrop, not a depth");

    for (var index = 1; index < fields.Count; index++) {
      fields[index].ScrollScale.X.ShouldBeGreaterThan(fields[index - 1].ScrollScale.X,
        $"{fields[index].Name} does not scroll past {fields[index - 1].Name}");
      fields[index].Alpha.ShouldBeGreaterThan(fields[index - 1].Alpha,
        $"{fields[index].Name} is no nearer to hand than {fields[index - 1].Name}");
    }
  }

  // Pinned seeds, so the backdrop the level ships with is the one that was looked at rather than
  // whatever comes up on the day.
  [Test]
  public void EveryFieldIsPinnedToASeed() {
    foreach (var field in _fields()) {
      field.Seed.ShouldNotBe(0, $"{field.Name} re-scatters itself on every visit");
    }
  }

  // Every place the shape's outline reaches over a stretch long enough to cover the slowest float.
  private static Rect2 _sweptBounds(BlockOutlineField field, BlockOutlineField.Outline outline) {
    var swept = new Rect2(field.FloatOf(outline, 0.0f) * outline.Points[0], Vector2.Zero);
    for (var sample = 0; sample <= SWAY_SAMPLES; sample++) {
      var at = field.FloatOf(outline, sample * SWAY_SAMPLE_SECONDS);
      foreach (var point in outline.Points) {
        swept = swept.Expand(at * point);
      }
    }
    return swept;
  }

  private static Rect2 _boundsOf(Vector2[] points) {
    var bounds = new Rect2(points[0], Vector2.Zero);
    foreach (var point in points) {
      bounds = bounds.Expand(point);
    }
    return bounds;
  }

  private List<BlockOutlineField> _fields() =>
    _background.GetChildren().OfType<BlockOutlineField>().ToList();
}
