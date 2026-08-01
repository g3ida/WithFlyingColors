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


  // The canonical play order: the level select numbers its cards from it, the
  // orchestrator advances along it, and the first entry is what a new game boots
  // into. LevelId's own ordinals mean nothing here (they are serialization ids).
  public static readonly List<LevelInfo> LEVELS = [
          new() { Id = LevelId.Tutorial, TranslationKey = TranslationKey.game_level_title_tutorial },
          new() { Id = LevelId.FourColors, TranslationKey = TranslationKey.game_level_title_fourColors },
          new() { Id = LevelId.Level1, TranslationKey = TranslationKey.game_level_title_letsPlayWithColors }
  ];

  // The level after this one in play order, or null at the end of the chain (and for
  // a level the chain does not know, which the caller treats the same way: nowhere
  // to advance to).
  public static LevelId? NextLevel(LevelId levelId) {
    var index = LEVELS.FindIndex(level => level.Id == levelId);
    if (index < 0 || index + 1 >= LEVELS.Count) {
      return null;
    }
    return LEVELS[index + 1].Id;
  }

  public static TranslationKey? TitleKeyOf(LevelId levelId) {
    // The hub is not in LEVELS, but it still fronts a title card and the save slot
    // panel still names it when a run is parked there.
    if (levelId == LevelId.Hub) {
      return TranslationKey.game_level_title_hub;
    }
    var index = LEVELS.FindIndex(level => level.Id == levelId);
    return index < 0 ? null : LEVELS[index].TranslationKey;
  }

  public partial struct LevelInfo {
    public LevelId Id { get; set; }
    public TranslationKey TranslationKey { get; set; }
  }
}
