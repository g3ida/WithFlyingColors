namespace Wfc.Entities.World.Player.Test;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Player;

// What decides whether the cube tips off a ledge. The four probes are read as a bit pattern, and
// only the two patterns meaning "one outer probe alone found floor" start a slip, so where they sit
// is the whole of the mechanic.
//
// They are cast from a box slightly larger than the cube. That used to be an accident - a size that
// counted the cube's collision plates twice - and it is now deliberate, because the slippering
// fixtures are tuned to it: narrowing the box to the cube on either axis makes a cube perched on a
// ledge tip twice instead of once. These pin the reach so it cannot drift back by accident.
public class PlayerFloorProbeTests(Node testScene) : TestClass(testScene) {
  private const float HALF_WIDTH = 47.7f;
  private static readonly Vector2 HALF = new Vector2(HALF_WIDTH, HALF_WIDTH);

  [Test]
  public void TheProbesReachPastTheCubeOnPurpose() {
    var offsets = PlayerStandingState.FloorProbeOffsets(HALF_WIDTH);

    offsets[^1].ShouldBeGreaterThan(
      HALF_WIDTH,
      "the outer probes reach past the cube; pulled back onto it, a perched cube tips twice"
    );
  }

  // Both axes, and by the same amount. The vertical reach is what lets a probe still find the
  // ledge a tipping cube is coming off, and it is as load-bearing as the horizontal one.
  [Test]
  public void TheProbeBoxReachesPastTheCubeOnBothAxes() {
    var box = PlayerStandingState.ProbeBox(HALF);

    box.X.ShouldBeGreaterThan(HALF.X);
    box.Y.ShouldBeGreaterThan(HALF.Y);
    (box.X / HALF.X).ShouldBe(box.Y / HALF.Y, 0.0001f, "the reach is not meant to be axis-dependent");
  }

  // It is a tolerance, not a measurement, so it stays modest. A box far larger than the cube
  // samples ground the cube has nothing to do with.
  [Test]
  public void TheReachStaysCloseToTheCube() {
    var box = PlayerStandingState.ProbeBox(HALF);

    (box.X / HALF.X).ShouldBeLessThan(1.15f, "the probe box is a tolerance, not a second cube");
  }

  // Left to right, and symmetric about the middle: the bit pattern names a side, so a probe out of
  // order would report a slip in the wrong direction.
  [Test]
  public void TheProbesRunOutsideInAndAreSymmetric() {
    var offsets = PlayerStandingState.FloorProbeOffsets(HALF_WIDTH);

    offsets.Length.ShouldBe(4);
    offsets[0].ShouldBeLessThan(offsets[1]);
    offsets[1].ShouldBeLessThan(offsets[2]);
    offsets[2].ShouldBeLessThan(offsets[3]);
    offsets[0].ShouldBe(-offsets[3], 0.0001f);
    offsets[1].ShouldBe(-offsets[2], 0.0001f);
  }

  // The inner pair are what separate "overhanging a little" from "barely on at all". They have to
  // stay well inside the outer pair or every landing near an edge reads as a slip.
  [Test]
  public void TheInnerProbesSitWellInsideTheOuterOnes() {
    var offsets = PlayerStandingState.FloorProbeOffsets(HALF_WIDTH);

    Mathf.Abs(offsets[1]).ShouldBeLessThan(Mathf.Abs(offsets[0]) * 0.6f);
    Mathf.Abs(offsets[1]).ShouldBeGreaterThan(Mathf.Abs(offsets[0]) * 0.3f);
  }

  // Scaled by a power-up, the probes have to scale with the cube rather than staying where the
  // full-sized one had them.
  [Test]
  public void TheProbesFollowTheCubeWhenItIsScaled() {
    var full = PlayerStandingState.FloorProbeOffsets(HALF_WIDTH);
    var shrunk = PlayerStandingState.FloorProbeOffsets(HALF_WIDTH * 0.7f);

    shrunk[^1].ShouldBe(full[^1] * 0.7f, 0.0001f);
  }
}
