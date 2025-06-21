namespace Wfc.Core.Persistence;

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
  public string ImagePath => $"user://slots/{_slotIndex}/save_slot_image.save";
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

  public void Save(ISerializer serializer, SceneTree sceneTree, bool newEmptySlot = false) {
    _saveMetaData(serializer, sceneTree, newEmptySlot);
    _saveScreenshot(sceneTree);
    if (!newEmptySlot) {
      _saveLevelState(serializer, sceneTree, newEmptySlot);
    }
  }

  public void LoadMetaData(ISerializer serializer) {
    if (!IsFilled) {
      return; // We don't have a save slot to load.
    }

    var saveGameMetaDataFile = FileAccess.Open(MetaPath, FileAccess.ModeFlags.Read);
    MetaData = serializer.Deserialize<SlotMetaData>(saveGameMetaDataFile.GetLine());
    saveGameMetaDataFile.Close();
    _loadImage();
  }

  public void Delete() {
    if (FileAccess.FileExists(Path)) {
      DirAccess.RemoveAbsolute(Path);
    }
    if (FileAccess.FileExists(MetaPath)) {
      DirAccess.RemoveAbsolute(MetaPath);
    }
    if (FileAccess.FileExists(ImagePath)) {
      DirAccess.RemoveAbsolute(ImagePath);
    }
  }

  private void _loadLevelState(ISerializer serializer, SceneTree sceneTree) {
    if (!HasProgress) {
      return; // We don't have progress to load.
    }

    var saveFile = FileAccess.Open(Path, FileAccess.ModeFlags.Read);
    var nodesData = serializer.Deserialize<Dictionary<string, string>>(saveFile.GetLine());
    var persistingNodes = sceneTree
      .GetNodesInGroup("persist")
      .OfType<IPersistent>()
      .ToDictionary(n => n.GetSaveId(), n => n);

    if (persistingNodes == null) {
      return;
    }

    if (nodesData != null) {
      foreach (var nodeData in nodesData) {
        if (persistingNodes.TryGetValue(nodeData.Key, out var node)) {
          node.Load(serializer, nodeData.Value);
        }
        else {
          // not found
        }
      }
    }
    else {
      GD.PushError($"Empty save file!");
    }
    saveFile.Close();
  }

  private void _saveMetaData(ISerializer serializer, SceneTree sceneTree, bool newEmptySlot = false) {
    var metaDataFile = FileAccess.Open(MetaPath, FileAccess.ModeFlags.Write);
    metaDataFile.StoreLine(serializer.Serialize(MetaData));
    metaDataFile.Close();
  }

  private void _saveLevelState(ISerializer serializer, SceneTree sceneTree, bool newEmptySlot = false) {
    var saveFile = FileAccess.Open(Path, FileAccess.ModeFlags.Write);
    var saveData = new Dictionary<string, string>();
    var saveNodes = sceneTree.GetNodesInGroup("persist");
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
    saveFile.StoreLine(JsonSerializer.Serialize(saveData));
    saveFile.Close();
  }

  private static ulong _getUnixTimestamp() {
    return (ulong)Time.GetUnixTimeFromSystem();
  }

  private void _saveScreenshot(SceneTree sceneTree) {
    var image = sceneTree.CurrentScene.GetViewport().GetTexture().GetImage();
    image.FlipY();
    image.ShrinkX2(); // TODO: resize image to fixed size
    image.SavePng(ImagePath);
    if (MetaData != null) {
      MetaData.Image = ImageTexture.CreateFromImage(image);
    }
  }

  private void _loadImage() {
    if (FileAccess.FileExists(ImagePath)) {
      var image = new Image();
      image.Load(ImagePath);
      if (MetaData != null) {
        MetaData.Image = ImageTexture.CreateFromImage(image);
      }
    }
  }
}
