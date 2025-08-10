namespace Wfc.Screens.Levels;

using System.Collections.Generic;
using Godot;
using Wfc.Core.Localization;
using Wfc.Utils;

public static class LevelDispatcher {
  public static GameLevel? InstantiateLevel(LevelId levelId) {
    var levelScene = GD.Load<PackedScene>(levelId.GetLevelPath());
    var level = levelScene.Instantiate<GameLevel>();
    level.LevelId = levelId;
    return level;
  }


  public static readonly List<LevelInfo> LEVELS = [
          new() { Id = LevelId.Level1, TranslationKey = TranslationKey.LevelDarkGames },
          new() { Id = LevelId.Tutorial, TranslationKey = TranslationKey.LevelTutorial },
          new() { Id = LevelId.oneMoreLevel, TranslationKey = TranslationKey.LevelDarkGames }
  ];

  public partial struct LevelInfo {
    public LevelId Id { get; set; }
    public TranslationKey TranslationKey { get; set; }
  }
}
