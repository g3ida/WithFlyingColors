namespace Wfc.Core.Persistence;

using System.Collections.Generic;
using Wfc.Screens.Levels;

public class SlotMetaData {

  public int SlotId { get; set; }
  public ulong SaveTimestamp { get; set; }
  public LevelId LevelId { get; set; }
  public int Progress { get; set; }

  public ulong LastLoadDate { get; set; }

  // Completion is kept apart from the resume pointer above: LevelId/Progress move to
  // whatever the player is doing now, while a level once cleared stays cleared. The
  // level select's unlock rule reads this, so folding it into Progress would mean
  // revisiting an old level locks everything after it again.
  public HashSet<LevelId> ClearedLevels { get; }

  public SlotMetaData(int slotId, ulong saveTimestamp, LevelId levelId, int progress, ulong lastLoadDate,
      IEnumerable<LevelId>? clearedLevels = null) {
    SlotId = slotId;
    SaveTimestamp = saveTimestamp;
    LevelId = levelId;
    Progress = progress;
    LastLoadDate = lastLoadDate;
    ClearedLevels = [.. clearedLevels ?? []];
  }
}
