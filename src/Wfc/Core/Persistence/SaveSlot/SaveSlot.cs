namespace Wfc.Core.Persistence;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using Wfc.Core.Serialization;
using Wfc.Screens.Levels;

public partial class SaveSlot {
  private readonly int _slotIndex;
  public string Path => $"user://slots/{_slotIndex}/save_slot.save";
  public string MetaPath => $"user://slots/{_slotIndex}/save_slot_meta.save";
  public bool IsFilled => FileAccess.FileExists(MetaPath);
  public bool HasProgress => FileAccess.FileExists(Path);

  public SlotMetaData? MetaData { get; private set; }

  private const string SAVE_TIMESTAMP_KEY = "save_timestamp";
  private const string LAST_LOAD_TIMESTAMP_KEY = "last_load_timestamp";
  private const string LEVEL_ID_KEY = "level_id";

  private const string NODE_PATH_KEY = "node_path";
  private const string PROGRESS_KEY = "progress";

  public SaveSlot(int slotIndex) {
    _slotIndex = slotIndex;

  }

  public void Load(ISerializer serializer, SceneTree sceneTree) {
    if (IsFilled) {
      LoadMetaData(serializer);
      if (HasProgress) {
        _loadLevelState(serializer, sceneTree);
      }
    }
    if (MetaData != null) {
      MetaData.SaveTimestamp = _getUnixTimestamp();
    }
    else {
      MetaData = new SlotMetaData(
        _slotIndex,
        _getUnixTimestamp(),
        LevelId.Tutorial,
        0,
        _getUnixTimestamp()
      );
    }
  }

  public void Save(ISerializer serializer, SceneTree sceneTree) {
    _saveMetaData(serializer);
    _saveLevelState(serializer, sceneTree);
  }

  // Where the player has got to. Progress is measured within a level, so reaching a new one
  // starts it over rather than appearing to go backwards; within a level it only ever climbs,
  // so dying back to an earlier checkpoint does not undo what was already reached.
  public void RecordProgress(LevelId levelId, int progressPercent) {
    var clamped = Math.Clamp(progressPercent, 0, 100);
    if (MetaData == null) {
      MetaData = new SlotMetaData(_slotIndex, _getUnixTimestamp(), levelId, clamped, _getUnixTimestamp());
      return;
    }

    var isNewLevel = MetaData.LevelId != levelId;
    MetaData.LevelId = levelId;
    MetaData.Progress = isNewLevel ? clamped : Math.Max(MetaData.Progress, clamped);
    MetaData.SaveTimestamp = _getUnixTimestamp();
  }

  public void LoadMetaData(ISerializer serializer) {
    if (!IsFilled) {
      return; // We don't have a save slot to load.
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
      GD.PushError($"Could not read {MetaPath}: {error.Message}. Treating the slot as empty.");
      MetaData = null;
    }
  }

  public void Delete() {
    if (FileAccess.FileExists(Path)) {
      DirAccess.RemoveAbsolute(Path);
    }
    if (FileAccess.FileExists(MetaPath)) {
      DirAccess.RemoveAbsolute(MetaPath);
    }
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
      GD.PushError($"Could not read {Path}: {error.Message}. Starting the level from the top.");
      return;
    }

    if (nodesData == null) {
      GD.PushError($"Empty save file!");
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
        GD.PushWarning($"Save entry '{nodeData.Key}' matches no node in the persist group; ignored.");
      }
    }
  }

  private void _saveMetaData(ISerializer serializer) {
    if (MetaData == null) {
      GD.PushError($"Slot {_slotIndex} has no metadata to write.");
      return;
    }
    _writeLineAtomic(MetaPath, serializer.Serialize(MetaData));
  }

  private void _saveLevelState(ISerializer serializer, SceneTree sceneTree) {
    var saveData = new Dictionary<string, string>();
    var saveNodes = sceneTree.GetNodesInGroup(IPersistent.PERSISTENT_GROUP_NAME);
    foreach (var node in saveNodes) {
      if (node.SceneFilePath.Length == 0) {
        GD.PushError($"persistent node '{node.Name}' is not an instanced scene, skipped");
        continue;
      }
      if (node is not IPersistent persistent) {
        GD.PushError($"persistent node '{node.Name}' does not implement IPersistent, skipped");
        continue;
      }
      var nodeData = persistent.Save(serializer);
      saveData[persistent.GetSaveId()] = nodeData;
    }
    _writeLineAtomic(Path, JsonSerializer.Serialize(saveData));
  }

  private static string? _readLine(string path) {
    using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
    if (file == null) {
      // Open returns null rather than throwing, so an unchecked handle is a
      // NullReferenceException raised inside whichever signal callback asked for the load.
      GD.PushError($"Could not open {path} for reading: {FileAccess.GetOpenError()}");
      return null;
    }
    return file.GetLine();
  }

  // Gather the data first, then write it, and never open the destination directly.
  //
  // ModeFlags.Write truncates the instant it succeeds, and the loop that gathers a level's state
  // calls every IPersistent implementation in the scene - so any one of them throwing used to
  // destroy the previous save on its way out, with the handle left open on top. Writing beside
  // the file and renaming over it means an interrupted save leaves the old one untouched.
  //
  // The per-slot directory is created here too: nothing else ever created it, so the first save
  // into a fresh slot handed back a null handle - the "first save on a new install crashes" bug.
  private static bool _writeLineAtomic(string path, string line) {
    var directory = path.GetBaseDir();
    var directoryError = DirAccess.MakeDirRecursiveAbsolute(directory);
    if (directoryError is not Error.Ok and not Error.AlreadyExists) {
      GD.PushError($"Could not create {directory}: {directoryError}");
      return false;
    }

    var tempPath = $"{path}.tmp";
    using (var file = FileAccess.Open(tempPath, FileAccess.ModeFlags.Write)) {
      if (file == null) {
        GD.PushError($"Could not open {tempPath} for writing: {FileAccess.GetOpenError()}");
        return false;
      }
      file.StoreLine(line);
    }

    var renameError = DirAccess.RenameAbsolute(tempPath, path);
    if (renameError != Error.Ok) {
      GD.PushError($"Could not move {tempPath} onto {path}: {renameError}");
      return false;
    }
    return true;
  }

  private static ulong _getUnixTimestamp() {
    return (ulong)Time.GetUnixTimeFromSystem();
  }
}
