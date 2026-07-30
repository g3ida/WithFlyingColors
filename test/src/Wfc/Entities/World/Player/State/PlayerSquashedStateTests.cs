namespace Wfc.Entities.World.Player.Test;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Player;

// Which way a crushed cube flattens. Everything the squash draws hangs off this one vector - the
// axis the sprite comes down on, the edge it stays pinned to, and the way the paint then runs - so
// getting the sign wrong sends the cube up through the platform that just killed it.
public class PlayerSquashedStateTests(Node testScene) : TestClass(testScene) {
  private static readonly Vector2 CENTRE = new Vector2(400f, 200f);

  [Test]
  public void ACrusherFromAboveFlattensTheCubeDownwards() {
    PlayerSquashedState.PinDirectionFor(CENTRE, CENTRE + new Vector2(0f, -30f))
      .ShouldBe(Vector2.Down);
  }

  [Test]
  public void ACrusherFromBelowFlattensTheCubeUpwards() {
    PlayerSquashedState.PinDirectionFor(CENTRE, CENTRE + new Vector2(0f, 30f))
      .ShouldBe(Vector2.Up);
  }

  [Test]
  public void ACrusherFromTheSideFlattensTheCubeAcross() {
    PlayerSquashedState.PinDirectionFor(CENTRE, CENTRE + new Vector2(-30f, 0f))
      .ShouldBe(Vector2.Right);
    PlayerSquashedState.PinDirectionFor(CENTRE, CENTRE + new Vector2(30f, 0f))
      .ShouldBe(Vector2.Left);
  }

  // The contact plane is only ever reported across the middle of the cube, but a cube caught
  // mid-rotation is wider than it is authored and the numbers stop being clean. The dominant
  // component still has to win outright: a squash drawn on two axes is a squash drawn twice.
  [Test]
  public void AnOffCentreContactStillNamesOneAxis() {
    PlayerSquashedState.PinDirectionFor(CENTRE, CENTRE + new Vector2(12f, -30f))
      .ShouldBe(Vector2.Down);
    PlayerSquashedState.PinDirectionFor(CENTRE, CENTRE + new Vector2(-30f, 12f))
      .ShouldBe(Vector2.Right);
  }
}
