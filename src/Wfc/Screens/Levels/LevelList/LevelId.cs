namespace Wfc.Screens.Levels;

using System;
using System.Linq;
using System.Reflection;
using Wfc.Utils.Attributes;

public enum LevelId {

  // Exported on SceneCard, so the ordinals are serialized into .tscn files: only ever
  // append members here, and only ever remove from the end. Play order is not defined
  // here - it is the order of LevelDispatcher.LEVELS.
  [LevelPath("TutorialLevel")]
  Tutorial,
  [LevelPath]
  Level1,
  [LevelPath("Level1-1")]
  FourColors,
  // The walkable arena whose doors are the level select. Deliberately absent from
  // LevelDispatcher.LEVELS: it is where levels are chosen from, not a level on offer.
  [LevelPath("HubLevel")]
  Hub,
  [LevelPath("TetrisLevel")]
  Tetris,
  [LevelPath("PaintLevel")]
  Paint,
}


public static class LevelIdExtensions {
  public static string GetLevelPath(this Enum levelId) {
    var field = levelId.GetType().GetField(levelId.ToString());
    var attr = field?.GetCustomAttributes(typeof(LevelPathAttribute), false)
        .FirstOrDefault() as LevelPathAttribute;

    if (attr == null)
      return string.Empty;

    return attr.ResolvePath(levelId.ToString());
  }
}

