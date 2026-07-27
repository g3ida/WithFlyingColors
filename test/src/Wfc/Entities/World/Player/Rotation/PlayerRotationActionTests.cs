namespace Wfc.Entities.World.Player.Test;

using System.Collections.Generic;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Entities.World.Player;
using Wfc.Utils;

// Mashing rotate queues quarter turns onto the one already running, so N taps owe the player exactly
// N quarter turns. The angle that bookkeeping runs on has to be accumulated, because Node2D.Rotation
// is derived from the transform and always comes back inside (-pi, pi]: once a queued target passed
// pi, the start and the target sat on opposite branches of the same angle and closing what was
// really a quarter turn was computed as most of a full one. Four taps whipped the cube through about
// two and a half turns, and during those frames its faces swept the floor fast enough to report a
// wrong-color contact and kill the player.
public class PlayerRotationActionTests(Node testScene) : TestClass(testScene) {
  private const float FRAME = 0.02f;
  private const float DURATION = 0.1f;

  // How far the cube actually travels under `taps` presses, two frames apart, a frame at a time.
  // Every turn is in the same direction, so the distance covered is the sum of the per-frame steps.
  private static float _travelUnderTaps(PlayerRotationAction action, int taps) {
    var tapFrames = new HashSet<int>();
    for (var tap = 1; tap < taps; tap++) {
      tapFrames.Add(tap * 2);
    }

    // Long enough for the last tap - which restarts the timer - to run out.
    var frames = (taps * 2) + 10;
    var traveled = 0.0f;

    action.Execute(1, MathUtils.PI2, DURATION);
    for (var frame = 0; frame < frames; frame++) {
      if (tapFrames.Contains(frame)) {
        action.Execute(1, MathUtils.PI2, DURATION);
      }

      var wasRotating = !action.CanRotate;
      var before = action.CurrentAngle;
      action.Step(FRAME);

      // The frame that releases the lock is also the one that folds whole turns back out of the
      // accumulator, and that bookkeeping does not move the body.
      if (wasRotating && action.CanRotate) {
        continue;
      }
      traveled += Mathf.Abs(action.CurrentAngle - before);
    }

    action.CanRotate.ShouldBeTrue("the taps should all have been spent by now");
    return traveled;
  }

  private static PlayerRotationAction _action(out CharacterBody2D body) {
    body = new CharacterBody2D();
    var action = new PlayerRotationAction();
    action.SetBody(body);
    return action;
  }

  [Test]
  public void OneTapTurnsTheCubeAQuarterTurn() {
    var action = _action(out var body);

    _travelUnderTaps(action, taps: 1).ShouldBe(MathUtils.PI2, 0.001f);

    body.Free();
  }

  // The one that was broken: four taps land while the previous turn is still running, and together
  // they owe exactly one full turn.
  [Test]
  public void FourChainedTapsTurnTheCubeExactlyOneFullTurn() {
    var action = _action(out var body);

    _travelUnderTaps(action, taps: 4).ShouldBe(4.0f * MathUtils.PI2, 0.01f);

    body.Free();
  }

  // Chaining is what pushes the target past pi, which is where reading the body's own wrapped
  // rotation back came apart - so the same claim, held well past the branch cut and across the
  // point where the accumulator folds turns out of itself.
  [Test]
  public void ChainingStaysHonestPastAFullTurn() {
    var action = _action(out var body);

    _travelUnderTaps(action, taps: 9).ShouldBe(9.0f * MathUtils.PI2, 0.01f);

    body.Free();
  }

  // A respawn snaps the cube to a stored angle without any rotation happening, so the accumulator
  // has to be told - otherwise the next turn interpolates from wherever the player died facing.
  [Test]
  public void ResetAdoptsTheRespawnAngle() {
    var action = _action(out var body);
    action.Execute(1, MathUtils.PI2, DURATION);
    action.Step(FRAME);

    action.Reset(Mathf.Pi);

    action.CurrentAngle.ShouldBe(Mathf.Pi, 0.001f);
    action.ThetaZero.ShouldBe(Mathf.Pi, 0.001f);
    action.CanRotate.ShouldBeTrue();

    _travelUnderTaps(action, taps: 1).ShouldBe(MathUtils.PI2, 0.001f);

    body.Free();
  }
}
