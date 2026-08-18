namespace Wfc.Core.Persistence;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using Wfc.Core.Logger;
using Wfc.Core.Serialization;
using Wfc.Screens.Levels;

public partial class SaveSlot {
  private readonly int _slotIndex;
  public string Path => $"{SavePaths.SlotDirectory(_slotIndex)}/save_slot.save";
  public string MetaPath => $"{SavePaths.SlotDirectory(_slotIndex)}/save_slot_meta.save";
  // Asked of the writer rather than the disk: a slot saved for the first time is filled from
  // that moment, not from whenever the filesystem gets round to it.
  public bool IsFilled => SaveWriter.Exists(MetaPath);
  public bool HasProgress => SaveWriter.Exists(Path);

  public SlotMetaData? MetaData { get; private set; }

  private const string SAVE_TIMESTAMP_KEY = "save_timestamp";
  private const string LAST_LOAD_TIMESTAMP_KEY = "last_load_timestamp";
  private const string LEVEL_ID_KEY = "level_id";

  private const string NODE_PATH_KEY = "node_path";
  private const string PROGRESS_KEY = "progress";

  // Where the play clock was last read. It lives in memory, not in the save: the gap
  // between two writes of one session is time the player spent in this slot, while the
  // gap across a game that was closed and reopened is not.
  private ulong _playTimeAnchor = _getUnixTimestamp();

  public SaveSlot(int slotIndex) {
    _slotIndex = slotIndex;

  }

  // Stamps the slot as touched and banks the time since the last stamp. Every record below
  // goes through this rather than writing SaveTimestamp itself, so no path can move the
  // slot on without the clock following it.
  private void _touch() {
    var now = _getUnixTimestamp();
    if (MetaData != null) {
      MetaData.PlayTimeSeconds += now - _playTimeAnchor;
      MetaData.SaveTimestamp = now;
    }
    _playTimeAnchor = now;
  }

  // A slot only starts counting from the moment it materializes: the object itself is built
  // at boot for every slot, and the menu time before one was picked is nobody's play time.
  private SlotMetaData _startMetaData(LevelId levelId, int progress, IEnumerable<LevelId>? clearedLevels = null) {
    _playTimeAnchor = _getUnixTimestamp();
    return new SlotMetaData(_slotIndex, _playTimeAnchor, levelId, progress, _playTimeAnchor, clearedLevels);
  }

  public void Load(ISerializer serializer, SceneTree sceneTree) {
    if (IsFilled) {
      LoadMetaData(serializer);
      if (HasProgress) {
        _loadLevelState(serializer, sceneTree);
      }
    }
    if (MetaData != null) {
      _playTimeAnchor = _getUnixTimestamp();
      MetaData.SaveTimestamp = _playTimeAnchor;
    }
    else {
      MetaData = _startMetaData(LevelId.Tutorial, 0);
    }
  }

  public void Save(ISerializer serializer, SceneTree sceneTree) {
    // A blank slot picked from a menu has never been loaded or played, so nothing has
    // materialized its metadata yet. Without this the meta file was never written, the
    // slot stayed "unfilled", and every later RecordProgress refused to save into it -
    // a fresh install could play forever and keep nothing.
    MetaData ??= _startMetaData(LevelId.Tutorial, 0);
    // Every write is a moment the slot was being played, and the panel reads this as when it
    // last was. Left to the record calls alone, a write that moves nothing they track - the
    // quit that only banks where the player is standing - showed the slot as untouched since
    // the last checkpoint.
    _touch();
    SaveMetaData(serializer);
    _saveLevelState(serializer, sceneTree);
  }

  // Where the player has got to. Progress is measured within a level, so reaching a new one
  // starts it over rather than appearing to go backwards; within a level it only ever climbs,
  // so dying back to an earlier checkpoint does not undo what was already reached.
  public void RecordProgress(LevelId levelId, int progressPercent) {
    var clamped = Math.Clamp(progressPercent, 0, 100);
    if (MetaData == null) {
      MetaData = _startMetaData(levelId, clamped);
      return;
    }

    var isNewLevel = MetaData.LevelId != levelId;
    MetaData.LevelId = levelId;
    MetaData.Progress = isNewLevel ? clamped : Math.Max(MetaData.Progress, clamped);
    _touch();
  }

  // Banked gems are forever, like completions: the union only grows, so a replay that
  // dies before an already-banked gem cannot take it back off the hub door.
  public void RecordCollectedGems(LevelId levelId, IEnumerable<string> colorGroups) {
    MetaData ??= _startMetaData(levelId, 0);
    if (!MetaData.CollectedGems.TryGetValue(levelId, out var gems)) {
      gems = [];
      MetaData.CollectedGems[levelId] = gems;
    }
    gems.UnionWith(colorGroups);
    _touch();
  }

  // The hub only introduces itself once, so the run remembers having been led in rather
  // than the walk working out for itself whether it has run before.
  //
  // The only record here that materializes nothing: its siblings are handed the level they
  // are recording, while an invented slot would have to guess a resume pointer - and a run
  // with nowhere to be remembered is simply shown the room again.
  public void RecordHubArrivalSeen() {
    if (MetaData == null) {
      return;
    }
    MetaData.HasSeenHubArrival = true;
    _touch();
  }

  // One more of something the run has done. Deliberately does not write: these climb many
  // times a second, and they ride out with the next progress or clear write like the rest of
  // the metadata rather than putting the disk under a jump counter.
  public void RecordRunStat(RunStat stat, ulong amount = 1) {
    if (MetaData == null) {
      return;
    }
    MetaData.Counters[stat] = MetaData.CounterOf(stat) + amount;
    _touch();
  }

  // Cleared is forever: the set only grows, unlike the resume pointer RecordProgress
  // moves around. Replays of a finished level therefore cannot re-lock anything.
  public void RecordCompletion(LevelId levelId) {
    if (MetaData == null) {
      MetaData = _startMetaData(levelId, 0, [levelId]);
      return;
    }
    MetaData.ClearedLevels.Add(levelId);
    _touch();
  }

  public void LoadMetaData(ISerializer serializer) {
    if (!IsFilled) {
      // Nothing on disk is the answer, not "keep whatever was here": a re-read that leaves
      // a deleted slot's record in place hands it to the next run started in that slot.
      MetaData = null;
      return;
    }

    var line = _readLine(MetaPath);
    if (line == null) {
      return;
    }

    try {
      MetaData = serializer.Deserialize<SlotMetaData>(line);
    }
    catch (JsonException error) {
      // A truncated or hand-edited file used to be an unhandled exception during start-up, which
      // meant the player could not even reach the screen that would let them delete the slot.
      // Degrading to "empty" leaves the slot visible and deletable.
      Log.Error($"Could not read {MetaPath}: {error.Message}. Treating the slot as empty.");
      MetaData = null;
    }
  }

  public void Delete() {
    // Before the files go, or a line still queued for them lands afterwards and puts the slot
    // back - with the record below already cleared, so nothing would be watching it.
    SaveWriter.Discard(Path);
    SaveWriter.Discard(MetaPath);
    if (FileAccess.FileExists(Path)) {
      DirAccess.RemoveAbsolute(Path);
    }
    if (FileAccess.FileExists(MetaPath)) {
      DirAccess.RemoveAbsolute(MetaPath);
    }
    // The files are only half of the slot; this is the other half, and it outlived them.
    // A new game started over an old one wipes the files, keeps writing into the record
    // still standing here, and inherits its cleared levels and banked gems - the fresh run
    // opened with the last one's gems already on its doors and in its HUD.
    MetaData = null;
  }

  private void _loadLevelState(ISerializer serializer, SceneTree sceneTree) {
    if (!HasProgress) {
      return; // We don't have progress to load.
    }

    var line = _readLine(Path);
    if (line == null) {
      return;
    }

    Dictionary<string, string>? nodesData;
    try {
      nodesData = serializer.Deserialize<Dictionary<string, string>>(line);
    }
    catch (JsonException error) {
      Log.Error($"Could not read {Path}: {error.Message}. Starting the level from the top.");
      return;
    }

    if (nodesData == null) {
      Log.Error($"Empty save file!");
      return;
    }

    var persistingNodes = sceneTree
      .GetNodesInGroup(IPersistent.PERSISTENT_GROUP_NAME)
      .OfType<IPersistent>()
      .ToDictionary(n => n.GetSaveId(), n => n);

    foreach (var nodeData in nodesData) {
      if (persistingNodes.TryGetValue(nodeData.Key, out var node)) {
        node.Load(serializer, nodeData.Value);
      }
      else {
        Log.Warning($"Save entry '{nodeData.Key}' matches no node in the persist group; ignored.");
      }
    }
  }

  // Public for the records that describe the slot rather than the run inside it: what a level
  // state written beside them would say has nothing to do with what they changed.
  public void SaveMetaData(ISerializer serializer) {
    if (MetaData == null) {
      Log.Error($"Slot {_slotIndex} has no metadata to write.");
      return;
    }
    SaveWriter.Write(MetaPath, serializer.Serialize(MetaData));
  }

  private void _saveLevelState(ISerializer serializer, SceneTree sceneTree) {
    var saveData = new Dictionary<string, string>();
    var saveNodes = sceneTree.GetNodesInGroup(IPersistent.PERSISTENT_GROUP_NAME);
    foreach (var node in saveNodes) {
      if (node.SceneFilePath.Length == 0) {
        Log.Error($"persistent node '{node.Name}' is not an instanced scene, skipped");
        continue;
      }
      if (node is not IPersistent persistent) {
        Log.Error($"persistent node '{node.Name}' does not implement IPersistent, skipped");
        continue;
      }
      var nodeData = persistent.Save(serializer);
      saveData[persistent.GetSaveId()] = nodeData;
    }
    SaveWriter.Write(Path, JsonSerializer.Serialize(saveData));
  }

  private static string? _readLine(string path) {
    if (SaveWriter.PendingLine(path) is { } pending) {
      return pending;
    }
    using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
    if (file == null) {
      // Open returns null rather than throwing, so an unchecked handle is a
      // NullReferenceException raised inside whichever signal callback asked for the load.
      Log.Error($"Could not open {path} for reading: {FileAccess.GetOpenError()}");
      return null;
    }
    return file.GetLine();
  }

  private static ulong _getUnixTimestamp() {
    return (ulong)Time.GetUnixTimeFromSystem();
  }
}
