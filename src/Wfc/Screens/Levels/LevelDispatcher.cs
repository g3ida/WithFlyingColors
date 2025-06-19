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
          new() { Id = LevelId.Tutorial, Name = TranslationKey.LevelTutorial },
          new() { Id = LevelId.Level1, Name = TranslationKey.LevelDarkGames },
          new() { Id = LevelId.oneMoreLevel, Name = TranslationKey.LevelDarkGames }
  ];

  public partial struct LevelInfo {
    public LevelId Id { get; set; }
    public TranslationKey Name { get; set; }
  }
}
