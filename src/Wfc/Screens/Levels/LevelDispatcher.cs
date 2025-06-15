namespace Wfc.Screens.Levels;

using System.Collections.Generic;
using Wfc.Core.Localization;
using Wfc.Screens.Levels.LevelList;
using Wfc.Utils;

public static class LevelDispatcher {
  public static GameLevel? InstantiateLevel(LevelId levelId) {
    return levelId switch {
      LevelId.Tutorial => SceneHelpers.InstantiateNode<TutorialLevel>(),
      LevelId.Level1 => SceneHelpers.InstantiateNode<Level1>(),
      _ => null,
    };
  }


  public static readonly List<LevelInfo> LEVELS = [
          new() { Id = LevelId.Tutorial, Name = TranslationKey.LevelTutorial },
          new() { Id = LevelId.Level1, Name = TranslationKey.LevelDarkGames },
          new() { Id = LevelId.oneMoreLevel, Name = TranslationKey.LevelDarkGames }
  ];

  public partial struct LevelInfo {
    public LevelId Id { get; set; }
    public TranslationKey Name { get; set; }
  }
}
