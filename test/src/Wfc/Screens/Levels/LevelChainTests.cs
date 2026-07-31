namespace Wfc.Screens.Levels.Test;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using Wfc.Screens.Levels;

// The play order lives in LevelDispatcher.LEVELS, not in the LevelId ordinals (those
// are serialization ids). These pin the chain the auto-advance walks, so reordering
// the list by accident shows up here instead of in a playtest.
public class LevelChainTests(Node testScene) : TestClass(testScene) {
  [Test]
  public void TheChainStartsWithTheTutorial() {
    LevelDispatcher.LEVELS[0].Id.ShouldBe(LevelId.Tutorial);
  }

  [Test]
  public void EachLevelAdvancesToTheNextInPlayOrder() {
    LevelDispatcher.NextLevel(LevelId.Tutorial).ShouldBe(LevelId.FourColors);
    LevelDispatcher.NextLevel(LevelId.FourColors).ShouldBe(LevelId.Level1);
  }

  [Test]
  public void TheLastLevelHasNothingToAdvanceTo() {
    LevelDispatcher.NextLevel(LevelId.Level1).ShouldBeNull();
  }

  [Test]
  public void EveryOfferedLevelHasATitle() {
    foreach (var info in LevelDispatcher.LEVELS) {
      LevelDispatcher.TitleKeyOf(info.Id).ShouldBe(info.TranslationKey);
    }
  }
}
