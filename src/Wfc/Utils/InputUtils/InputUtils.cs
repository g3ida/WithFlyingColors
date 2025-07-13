namespace Wfc.Utils;

using System.Collections.Generic;
using Godot;

public static class InputUtils {
  public static InputEventKey? GetFirstKeyKeyboardEventFromActionList(IEnumerable<InputEvent> actionList) {
    foreach (var el in actionList) {
      if (el is InputEventKey keyEvent) {
        return keyEvent;
      }
    }
    return null;
  }
}
