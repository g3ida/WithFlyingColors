namespace Wfc.Screens.Levels;

using System;
using System.Linq;
using System.Reflection;
using Wfc.Utils.Attributes;

public enum LevelId {

  [LevelPath("TutorialLevel")]
  Tutorial,
  [LevelPath]
  Level1,
  [LevelPath]
  oneMoreLevel,
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

