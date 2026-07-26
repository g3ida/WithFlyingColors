namespace Wfc.Screens.Levels;

using System.Collections.Generic;
using Godot;
using Wfc.Core.Localization;
using Wfc.Utils;

public static class LevelDispatcher {
  public static GameLevel? InstantiateLevel(LevelId levelId) {
    var path = levelId.GetLevelPath();
    // GD.Load returns null for a missing resource rather than throwing, and the caller
    // already handles null: without this the miss surfaces as an NRE a frame later.
    var levelScene = GD.Load<PackedScene>(path);
    if (levelScene == null) {
      GD.PushError($"No scene found for level {levelId} at '{path}'");
      return null;
    }
    var level = levelScene.Instantiate<GameLevel>();
    level.LevelId = levelId;
    return level;
  }


  public static readonly List<LevelInfo> LEVELS = [
          new() { Id = LevelId.Level1, TranslationKey = TranslationKey.game_level_title_darkGames },
          new() { Id = LevelId.Tutorial, TranslationKey = TranslationKey.game_level_title_tutorial }
  ];

  public partial struct LevelInfo {
    public LevelId Id { get; set; }
    public TranslationKey TranslationKey { get; set; }
  }
}
