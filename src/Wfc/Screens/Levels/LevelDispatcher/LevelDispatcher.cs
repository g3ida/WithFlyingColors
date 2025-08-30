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
          new() { Id = LevelId.Level1, TranslationKey = TranslationKey.game_level_title_darkGames },
          new() { Id = LevelId.Tutorial, TranslationKey = TranslationKey.game_level_title_tutorial },
          new() { Id = LevelId.oneMoreLevel, TranslationKey = TranslationKey.game_level_title_darkGames }
  ];

  public partial struct LevelInfo {
    public LevelId Id { get; set; }
    public TranslationKey TranslationKey { get; set; }
  }
}
