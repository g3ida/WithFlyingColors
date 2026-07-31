namespace Wfc.Screens.Levels.Test;

using System.Collections.Generic;
using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Screens.Levels;

// What the level select may offer: the first level always, anything whose predecessor
// has been cleared, and wherever the save is currently parked. No warnings anywhere -
// jumping is safe by construction, so the gate is the only protection that matters.
public class LevelUnlockPolicyTests(Node testScene) : TestClass(testScene) {
  private static readonly List<LevelId> _chain = [LevelId.Tutorial, LevelId.FourColors, LevelId.Level1];

  [Test]
  public void TheFirstLevelIsAlwaysUnlocked() {
    LevelUnlockPolicy.IsUnlocked(LevelId.Tutorial, _chain, new HashSet<LevelId>(), null)
      .ShouldBeTrue();
  }

  [Test]
  public void ALevelUnlocksWhenItsPredecessorIsCleared() {
    var cleared = new HashSet<LevelId> { LevelId.Tutorial };

    LevelUnlockPolicy.IsUnlocked(LevelId.FourColors, _chain, cleared, null).ShouldBeTrue();
    LevelUnlockPolicy.IsUnlocked(LevelId.Level1, _chain, cleared, null).ShouldBeFalse();
  }

  // The slot can be parked on a level whose predecessor was never cleared on this
  // slot (the resume pointer only tracks where the player is). Locking the level the
  // save lives in would lock the player out of their own game.
  [Test]
  public void TheResumeLevelIsAlwaysUnlocked() {
    LevelUnlockPolicy.IsUnlocked(LevelId.Level1, _chain, new HashSet<LevelId>(), LevelId.Level1)
      .ShouldBeTrue();
  }

  [Test]
  public void ALevelOutsideTheChainStaysLocked() {
    var shortChain = new List<LevelId> { LevelId.Tutorial };

    LevelUnlockPolicy.IsUnlocked(LevelId.FourColors, shortChain, new HashSet<LevelId> { LevelId.Tutorial }, null)
      .ShouldBeFalse();
  }
}
