namespace Wfc.Entities.World.Player.Test;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Player;

// Two axis mix-ups that no type could catch: both compile, both run every frame, and
// both quietly spend a mechanic on the wrong component of the velocity.
public class PlayerMotionTests(Node testScene) : TestClass(testScene) {
  [Test]
  public void JumpCutDampsTheRiseAndLeavesRunSpeedAlone() {
    var cut = PlayerJumpingState.ApplyJumpCut(new Vector2(300f, -1200f), 0.5f);

    cut.Y.ShouldBe(-600f);
    cut.X.ShouldBe(300f, "cutting a jump short must not brake the player mid-air");
  }

  // The guard around the cut is Velocity.Y < 0, so the cut only ever sees a rising cube.
  [Test]
  public void JumpCutAlwaysReducesTheUpwardSpeed() {
    var cut = PlayerJumpingState.ApplyJumpCut(new Vector2(-120f, -900f), 0.5f);

    Mathf.Abs(cut.Y).ShouldBeLessThan(900f);
  }

  [Test]
  public void AHorizontalDashHoldsItsHeight() {
    PlayerDashingState.HoldsHeightDuringDash(new Vector2(1f, 0f)).ShouldBeTrue();
    PlayerDashingState.HoldsHeightDuringDash(new Vector2(-1f, 0f)).ShouldBeTrue();
  }

  // The one that was broken: a down-dash's payload is its Y velocity, so the frame-by-
  // frame gravity cancel has to stand aside or the dash is consumed for nothing.
  [Test]
  public void ADownDashDoesNotHoldItsHeight() {
    PlayerDashingState.HoldsHeightDuringDash(new Vector2(0f, 1f)).ShouldBeFalse();
    PlayerDashingState.HoldsHeightDuringDash(new Vector2(1f, 1f)).ShouldBeFalse();
  }
}
