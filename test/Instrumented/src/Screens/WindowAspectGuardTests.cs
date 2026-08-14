namespace Wfc.test.instrumented.Screens;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Core.Display;

// A window the player drags keeps the shape the game is drawn at. Only the geometry
// is exercised here - the guard itself answers the real window, and resizing that
// mid-suite stalls a frame and upsets the suites that measure against the clock.
public class WindowAspectGuardTests(Node testScene) : TestClass(testScene) {
  private const float ASPECT = 1920f / 1080f;
  private static readonly Vector2I NO_SCREEN = Vector2I.Zero;

  [Test]
  public void PullingSidewaysKeepsTheWidthAndFollowsWithTheHeight() {
    var corrected = WindowAspectGuard.ToAspect(
      size: new Vector2I(1600, 1080), lastSize: new Vector2I(1920, 1080), ASPECT, NO_SCREEN);

    corrected.ShouldBe(new Vector2I(1600, 900), "the edge the player pulled was not the one that was kept");
  }

  [Test]
  public void PullingDownwardsKeepsTheHeightAndFollowsWithTheWidth() {
    var corrected = WindowAspectGuard.ToAspect(
      size: new Vector2I(1920, 720), lastSize: new Vector2I(1920, 1080), ASPECT, NO_SCREEN);

    corrected.ShouldBe(new Vector2I(1280, 720), "the edge the player pulled was not the one that was kept");
  }

  // A corner drag moves both edges. Whichever moved furthest is the one meant.
  [Test]
  public void ACornerDragFollowsTheEdgeThatMovedFurthest() {
    var corrected = WindowAspectGuard.ToAspect(
      size: new Vector2I(1900, 600), lastSize: new Vector2I(1920, 1080), ASPECT, NO_SCREEN);

    corrected.ShouldBe(new Vector2I(1067, 600), "the smaller of the two movements won the drag");
  }

  [Test]
  public void ASizeAlreadyOfTheRightShapeIsLeftAlone() {
    var corrected = WindowAspectGuard.ToAspect(
      size: new Vector2I(1280, 720), lastSize: new Vector2I(1280, 720), ASPECT, NO_SCREEN);

    corrected.ShouldBe(new Vector2I(1280, 720), "a window nobody had reshaped was reshaped anyway");
  }

  // Correcting one edge can push the other past the screen, so the result is brought
  // back inside from whichever edge binds.
  [Test]
  public void TheCorrectedSizeStaysOnTheScreen() {
    var corrected = WindowAspectGuard.ToAspect(
      size: new Vector2I(1920, 1400), lastSize: new Vector2I(1920, 1080), ASPECT, new Vector2I(1920, 1200));

    corrected.X.ShouldBeLessThanOrEqualTo(1920, "the corrected window is wider than the screen");
    corrected.Y.ShouldBeLessThanOrEqualTo(1200, "the corrected window is taller than the screen");
    ((float)corrected.X / corrected.Y).ShouldBe(ASPECT, 0.01, "fitting the screen lost the shape");
  }
}
