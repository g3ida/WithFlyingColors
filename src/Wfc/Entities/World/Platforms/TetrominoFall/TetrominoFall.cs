namespace Wfc.Entities.World.Platforms;

using System;
using Wfc.Utils;

// The descent a tetromino makes, kept out of the node that hosts it the way PlatformSlide is: a
// cell at a time, standing still on each row it reaches. That pause is the whole point - it is
// what turns a falling hazard into a surface the player can read, wait for and step onto, and it
// is the same rhythm the tetris minigame drops its pieces on.
//
// A row's period splits into the descent and a hold on the row it arrives at, in that order, so a
// piece is standing still on the row it spawned on before it first moves. Bounding the descent by
// speed rather than by a fraction of the period keeps the per-frame displacement small however
// short the period is; past the point where a period is shorter than the descent the hold vanishes
// and the fall becomes continuous, which is as slow as it can be made.
public sealed class TetrominoFall {
  #region Settings
  // How long one row takes, descent and hold together.
  public float StepInterval = 0.35f;

  // How far one row is, in the units the piece is placed in.
  public float CellSize = Constants.TETRIS_BLOCK_SIZE;

  // The ceiling on how fast the piece is allowed to cross a row. Bodies are carried by transform,
  // so a row crossed in a single frame arrives inside whoever was standing on it.
  public float MaxSpeed = Constants.TETRIS_MAX_FALL_SPEED;
  #endregion Settings

  private float _elapsed;

  // How far below where it started the piece now stands.
  public float Descended { get; private set; }

  // How far it came down on the last tick. What the crush check reads to know the piece is under
  // power rather than parked.
  public float Travelled { get; private set; }

  public float TravelDuration => Math.Min(StepInterval, CellSize / MaxSpeed);

  public void Step(double delta) {
    _elapsed += (float)delta;
    var descended = _descentAt(_elapsed);
    Travelled = descended - Descended;
    Descended = descended;
  }

  public void Reset() {
    _elapsed = 0.0f;
    Descended = 0.0f;
    Travelled = 0.0f;
  }

  // Winds the clock forward, for a piece that is meant to be found already on its way down.
  public void Skip(float seconds) {
    _elapsed = Math.Max(seconds, 0.0f);
    Descended = _descentAt(_elapsed);
    Travelled = 0.0f;
  }

  // Read off the clock rather than accumulated tick by tick, so a piece's rows land on the same
  // instants however the frames fell and two pieces set going together stay in step.
  private float _descentAt(float elapsed) {
    // A period of nothing is a piece with no rhythm left to keep, which is the fastest it is
    // allowed to go rather than a division by zero.
    if (StepInterval <= 0.0f || CellSize <= 0.0f) {
      return elapsed * MaxSpeed;
    }

    var travel = TravelDuration;
    var rows = Math.Floor(elapsed / StepInterval);
    var into = elapsed - ((float)rows * StepInterval);
    var hold = StepInterval - travel;
    var progress = into <= hold ? 0.0f : Math.Min((into - hold) / travel, 1.0f);
    return ((float)rows + progress) * CellSize;
  }
}
