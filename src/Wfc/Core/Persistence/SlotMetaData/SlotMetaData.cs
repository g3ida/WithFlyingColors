namespace Wfc.Core.Persistence;

using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
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

  // Color groups of the gems banked per level, so the hub doors can show what a level
  // still hides without loading it. Like ClearedLevels this only ever grows: a replay
  // that ends short of an already-banked gem must not take it off the door.
  public Dictionary<LevelId, HashSet<string>> CollectedGems { get; }

  private static readonly FrozenSet<string> _noGems = FrozenSet<string>.Empty;

  public SlotMetaData(int slotId, ulong saveTimestamp, LevelId levelId, int progress, ulong lastLoadDate,
      IEnumerable<LevelId>? clearedLevels = null,
      IReadOnlyDictionary<LevelId, HashSet<string>>? collectedGems = null) {
    SlotId = slotId;
    SaveTimestamp = saveTimestamp;
    LevelId = levelId;
    Progress = progress;
    LastLoadDate = lastLoadDate;
    ClearedLevels = [.. clearedLevels ?? []];
    CollectedGems = collectedGems?.ToDictionary(e => e.Key, e => new HashSet<string>(e.Value)) ?? [];
  }

  // The hub introduces itself by walking the player in from the far end of the room, once
  // per run. This is what remembers that it has: every arrival after it opens at a door.
  public bool HasSeenHubArrival { get; set; }

  public IReadOnlySet<string> GemsCollectedIn(LevelId levelId) =>
    CollectedGems.TryGetValue(levelId, out var gems) ? gems : _noGems;
}
