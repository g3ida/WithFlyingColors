namespace Wfc.Entities.World.BrickBreaker.Test;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.BrickBreaker;

// The ball-versus-paddle geometry, which decides both where the ball goes and which colored face of
// the cube gets to judge it. The paddle can dash and jump faster than the ball travels, so the cube
// routinely reaches the ball already well inside itself, and every one of those cases has to name the
// face that swept into it rather than the one that happens to be nearest.
public class BoxContactTests(Node testScene) : TestClass(testScene) {
  private static readonly Vector2 HALF = new(48.0f, 48.0f);
  private const float RADIUS = 12.0f;
  private static readonly Vector2 AT_REST = Vector2.Zero;

  [Test]
  public void ABallClearOfTheCubeIsNotTouchingIt() {
    BoxContact.Find(new Vector2(HALF.X + RADIUS + 1.0f, 0.0f), RADIUS, HALF, AT_REST, out _)
      .ShouldBeFalse();
  }

  [Test]
  public void ABallJustTouchingASideRestsAgainstItWithNothingToPushOut() {
    BoxContact.Find(new Vector2(HALF.X + RADIUS, 0.0f), RADIUS, HALF, AT_REST, out var contact)
      .ShouldBeTrue();

    contact.Normal.ShouldBe(Vector2.Right);
    contact.Depth.ShouldBe(0.0f, 0.001f);
  }

  [Test]
  public void ABallOverlappingASideIsPushedBackOutOfIt() {
    BoxContact.Find(new Vector2(HALF.X + RADIUS - 5.0f, 0.0f), RADIUS, HALF, AT_REST, out var contact)
      .ShouldBeTrue();

    contact.Normal.ShouldBe(Vector2.Right);
    contact.Point.ShouldBe(new Vector2(HALF.X, 0.0f));
    contact.Depth.ShouldBe(5.0f, 0.001f);
  }

  // Out past the corner in both axes, the surface point is the corner itself - which is the seam two
  // faces share, and the only place a contact is safe in either of their colors.
  [Test]
  public void ABallBeyondACornerTouchesTheCornerItself() {
    BoxContact.Find(new Vector2(HALF.X + 4.0f, HALF.Y + 4.0f), RADIUS, HALF, AT_REST, out var contact)
      .ShouldBeTrue();

    contact.Point.ShouldBe(HALF);
    contact.Normal.X.ShouldBeGreaterThan(0.0f);
    contact.Normal.Y.ShouldBeGreaterThan(0.0f);
  }

  // The dash. The cube crosses more than the ball's own width in a frame, so the first look at the
  // contact already has the ball deep inside - here nearer the bottom face than the side it came in
  // through. Naming the bottom face would eject the ball downwards and hand it the bottom's color to
  // be judged against, which is the wrong answer on both counts.
  [Test]
  public void ABallTheCubeHasDashedOntoLeavesBySideItCameIn() {
    var buried = new Vector2(18.0f, 24.0f);
    var dashingRight = new Vector2(-1920.0f, 0.0f);

    BoxContact.Find(buried, RADIUS, HALF, dashingRight, out var contact).ShouldBeTrue();

    contact.Normal.ShouldBe(Vector2.Right);
    contact.Point.ShouldBe(new Vector2(HALF.X, buried.Y));
    contact.Depth.ShouldBe(HALF.X - buried.X + RADIUS, 0.001f);
  }

  // The other half of the same rule: a cube jumping into a falling ball reaches it from below, and
  // the ball has to be thrown back up off the top face however deep the jump carried it.
  [Test]
  public void ABallTheCubeHasJumpedIntoLeavesByTheFaceThatStruckIt() {
    var buried = new Vector2(30.0f, -10.0f);
    var jumpingUp = new Vector2(0.0f, 1620.0f);

    BoxContact.Find(buried, RADIUS, HALF, jumpingUp, out var contact).ShouldBeTrue();

    contact.Normal.ShouldBe(Vector2.Up);
    contact.Point.ShouldBe(new Vector2(buried.X, -HALF.Y));
  }

  // Nothing is moving, so there is no side it came in from; the nearest way out is all that is left.
  [Test]
  public void ABallSittingInsideAStillCubeLeavesByTheNearestSide() {
    BoxContact.Find(new Vector2(6.0f, HALF.Y - 2.0f), RADIUS, HALF, AT_REST, out var contact)
      .ShouldBeTrue();

    contact.Normal.ShouldBe(Vector2.Down);
  }

  // Whatever the case, the push-out has to leave the ball resting against the cube rather than still
  // inside it: a ball the cube keeps hold of is a ball it carries around the arena.
  [Test]
  public void EveryContactPushesTheBallClearOfTheCube() {
    var traveling = new Vector2(-500.0f, 120.0f);
    foreach (var start in new[] {
      new Vector2(52.0f, 0.0f),
      new Vector2(50.0f, 50.0f),
      new Vector2(18.0f, 24.0f),
      new Vector2(0.0f, -40.0f),
      new Vector2(-30.0f, 12.0f),
    }) {
      BoxContact.Find(start, RADIUS, HALF, traveling, out var contact).ShouldBeTrue();

      var settled = start + (contact.Normal * contact.Depth);
      var nearest = new Vector2(
        Mathf.Clamp(settled.X, -HALF.X, HALF.X),
        Mathf.Clamp(settled.Y, -HALF.Y, HALF.Y)
      );
      settled.DistanceTo(nearest).ShouldBe(RADIUS, 0.01f, $"a ball at {start} was left inside the cube");
    }
  }
}
