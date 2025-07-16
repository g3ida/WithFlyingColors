namespace Wfc.Entities.World.Piano;

using System;

public static class MusicNoteHelper {
  public static MusicNote? MusicNoteFromInt(int noteValue) {
    if (Enum.IsDefined(typeof(MusicNote), noteValue)) {
      return (MusicNote)noteValue;
    }
    else {
      return null;
    }
  }
}
