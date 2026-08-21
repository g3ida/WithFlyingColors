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
    var cut = PlayerJumpingState.ApplyJumpCut(new Vector2(300f, -1200f), 0.5f, 0f);

    cut.Y.ShouldBe(-600f);
    cut.X.ShouldBe(300f, "cutting a jump short must not brake the player mid-air");
  }

  // The guard around the cut is Velocity.Y < 0, so the cut only ever sees a rising cube.
  [Test]
  public void JumpCutAlwaysReducesTheUpwardSpeed() {
    var cut = PlayerJumpingState.ApplyJumpCut(new Vector2(-120f, -900f), 0.5f, 0f);

    Mathf.Abs(cut.Y).ShouldBeLessThan(900f);
  }

  // The third axis mix-up of the same kind: the cut owns how high the cube jumps, and a lift it
  // was handed by the floor it jumped off is not part of that.
  [Test]
  public void JumpCutLeavesALiftFromTheFloorWhole() {
    var cut = PlayerJumpingState.ApplyJumpCut(new Vector2(0f, -1200f), 0.5f, -600f);

    cut.Y.ShouldBe(-900f, "the cut took the floor's lift along with the cube's own jump");
  }

  // A lift steeper than what is left of the jump would otherwise have the cut speeding the cube up.
  [Test]
  public void JumpCutNeverSpeedsTheRiseUp() {
    var cut = PlayerJumpingState.ApplyJumpCut(new Vector2(0f, -400f), 0.5f, -600f);

    cut.Y.ShouldBe(-400f);
  }

  // The cut is owed a lift, not a shove: honouring a downward one would take more off the jump
  // than the cut is worth, which is the opposite of what the guard above stands for.
  [Test]
  public void JumpCutIgnoresADownwardCarry() {
    var cut = PlayerJumpingState.ApplyJumpCut(new Vector2(0f, -1200f), 0.5f, 600f);

    cut.Y.ShouldBe(-600f, "a sinking floor's own fall was cut out of the cube's jump");
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
