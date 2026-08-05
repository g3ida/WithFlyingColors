namespace Wfc.Screens.Levels.Test;

using System.Collections.Generic;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Screens.Levels;

// Where the hub puts the player down. Coming out of a level is the strong case - the player
// knows which door they went through and expects to be standing at it - and everything else
// is the run's own place in the chain.
public class HubSpawnPolicyTests(Node testScene) : TestClass(testScene) {
  private static readonly List<LevelId> CHAIN = [LevelId.Tutorial, LevelId.FourColors, LevelId.Level1];

  [Test]
  public void StandsAtTheDoorOfTheLevelJustLeft() {
    HubSpawnPolicy.DoorToStandAt(LevelId.FourColors, CHAIN, new HashSet<LevelId> { LevelId.Tutorial })
      .ShouldBe(LevelId.FourColors);
  }

  // A clear of the last level in the chain still puts the player at its door, rather than
  // sending them back to the first one because there is nothing unfinished left.
  [Test]
  public void StandsAtTheDoorOfAClearedLastLevel() {
    HubSpawnPolicy.DoorToStandAt(LevelId.Level1, CHAIN, new HashSet<LevelId>(CHAIN))
      .ShouldBe(LevelId.Level1);
  }

  [Test]
  public void OpensARunOnTheFirstLevelItHasNotFinished() {
    HubSpawnPolicy.DoorToStandAt(null, CHAIN, new HashSet<LevelId> { LevelId.Tutorial })
      .ShouldBe(LevelId.FourColors);
  }

  [Test]
  public void OpensAFreshRunOnTheFirstDoor() {
    HubSpawnPolicy.DoorToStandAt(null, CHAIN, new HashSet<LevelId>())
      .ShouldBe(LevelId.Tutorial);
  }

  // Nothing left to finish: the last door is where the run left off, and the only one worth
  // walking back into.
  [Test]
  public void OpensAFinishedRunOnTheLastDoor() {
    HubSpawnPolicy.DoorToStandAt(null, CHAIN, new HashSet<LevelId>(CHAIN))
      .ShouldBe(LevelId.Level1);
  }

  // The intro has no door - it is played on the way in and never offered again - so coming out
  // of it cannot stand the player anywhere, and the run falls through to its place in the chain.
  [Test]
  public void FallsThroughForALevelTheHubHasNoDoorFor() {
    List<LevelId> doorChain = [LevelId.FourColors, LevelId.Level1];

    HubSpawnPolicy.DoorToStandAt(LevelId.Tutorial, doorChain, new HashSet<LevelId> { LevelId.Tutorial })
      .ShouldBe(LevelId.FourColors);
  }

  // The hub is not a level anyone comes out of: leaving it for itself - the pause menu's own
  // way back - falls through to the run's place in the chain.
  [Test]
  public void IgnoresTheHubAsALevelJustLeft() {
    HubSpawnPolicy.DoorToStandAt(LevelId.Hub, CHAIN, new HashSet<LevelId> { LevelId.Tutorial })
      .ShouldBe(LevelId.FourColors);
  }

  [Test]
  public void AnswersNothingWithoutAChain() {
    HubSpawnPolicy.DoorToStandAt(null, [], new HashSet<LevelId>()).ShouldBeNull();
  }
}
