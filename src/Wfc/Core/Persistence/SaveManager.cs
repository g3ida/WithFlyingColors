namespace Wfc.Core.Persistence;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

public partial class SaveManager : Node, ISaveManager {

  private readonly JsonSerializerOptions _serializerOptions = new() {
    WriteIndented = true
  };
  private const int NUM_SLOTS = 3;
  private readonly SaveSlot[] _saveSlots = [.. Enumerable.Range(1, NUM_SLOTS).Select(i => new SaveSlot(i))];
  public int LatestLoadedSlot { get; private set; }
  private readonly string LATEST_LOADED_SLOT_FIELD_NAME = "latest_loaded_slot";
  private readonly string SLOT_INFO_PATH = $"user://slots/slots_info.save";

  public void SaveGame(SceneTree tree, int slotIndex) {
    slotIndex = slotIndex == -1 ? Math.Max(0, LatestLoadedSlot) : slotIndex;
    if (slotIndex is < 0 or >= NUM_SLOTS) {
      GD.PushError($"Invalid slot index: {slotIndex}. Must be 0-{NUM_SLOTS - 1}");
      return;
    }
    _saveSlots[slotIndex].Save(tree, false);
    _loadSlotsMetaData();
    GD.Print("Game saved!");
  }

  public void LoadGame(SceneTree tree, int slotIndex = -1) {
    slotIndex = slotIndex == -1 ? Math.Max(0, LatestLoadedSlot) : slotIndex;
    if (slotIndex is < 0 or >= NUM_SLOTS) {
      GD.PushError($"Invalid slot index: {slotIndex}. Must be 0-{NUM_SLOTS - 1}");
      return;
    }
    _saveSlots[slotIndex].Load(tree);
    // Update camera position to the player position avoiding smoothing
    // which would make you see the camera move quickly to the checkpoint position
    // when we load a level. We put it here instead of the reset method because
    // I like the smoothing effect when the player loses
    Global.Instance().Camera.update_position(Global.Instance().Player.GlobalPosition);
    LatestLoadedSlot = slotIndex;
    _saveSlotsInfo();
    GD.Print("Game loaded!");
  }

  public bool IsSLotFilled(int slotIndex = -1) {
    slotIndex = slotIndex == -1 ? Math.Max(0, LatestLoadedSlot) : slotIndex;
    if (slotIndex is < 0 or >= NUM_SLOTS) {
      GD.PushError($"Invalid slot index: {slotIndex}. Must be 0-{NUM_SLOTS - 1}");
      return false;
    }
    return _saveSlots[slotIndex].IsFilled;
  }

  public SlotMetaData? GetSlotMetaData(int slotIndex = -1) {
    slotIndex = slotIndex == -1 ? Math.Max(0, LatestLoadedSlot) : slotIndex;
    if (slotIndex is < 0 or >= NUM_SLOTS) {
      GD.PushError($"Invalid slot index: {slotIndex}. Must be 0-{NUM_SLOTS - 1}");
      return null;
    }
    return _saveSlots[slotIndex].MetaData;
  }

  public ImageTexture? GetSlotImage(int slotIndex = -1) {
    slotIndex = slotIndex == -1 ? Math.Max(0, LatestLoadedSlot) : slotIndex;
    if (slotIndex is < 0 or >= NUM_SLOTS) {
      GD.PushError($"Invalid slot index: {slotIndex}. Must be 0-{NUM_SLOTS - 1}");
      return null;
    }
    return _saveSlots[slotIndex].MetaData?.Image;
  }

  private void _loadSlotsMetaData() {
    foreach (var slot in _saveSlots) {
      slot.LoadMetaData();
    }
  }

  public int GetSelectedSlotIndex() => LatestLoadedSlot;

  public void Init() {
    _loadSlotsInfo();
    _loadSlotsMetaData();
  }

  public void SelectSlot(int slotIndex = -1) {
    slotIndex = slotIndex == -1 ? Math.Max(0, LatestLoadedSlot) : slotIndex;
    if (slotIndex is < 0 or >= NUM_SLOTS) {
      GD.PushError($"Invalid slot index: {slotIndex}. Must be 0-{NUM_SLOTS - 1}");
      return;
    }
    LatestLoadedSlot = slotIndex;
    _saveSlotsInfo();
  }

  public void RemoveSaveSlot(int slotIndex) {
    if (slotIndex is < 0 or >= NUM_SLOTS) {
      GD.PushError($"Invalid slot index: {slotIndex}. Must be 0-{NUM_SLOTS - 1}");
      return;
    }
    _saveSlots[slotIndex].Delete();
    _loadSlotsMetaData();
    if (LatestLoadedSlot == slotIndex) {
      LatestLoadedSlot = -1;
    }
  }

  private void _loadSlotsInfo() {
    if (FileAccess.FileExists(SLOT_INFO_PATH)) {
      var metaDataFile = FileAccess.Open(SLOT_INFO_PATH, FileAccess.ModeFlags.Read);
      var data = JsonSerializer.Deserialize<Dictionary<string, object>>(metaDataFile.GetLine());
      if (data == null) {
        LatestLoadedSlot = 0;
      }
      else {
        var lastLoadedDate = data[LATEST_LOADED_SLOT_FIELD_NAME];
        LatestLoadedSlot = lastLoadedDate != null ? (int)lastLoadedDate : 0;
        metaDataFile.Close();
      }
    }
    else {
      LatestLoadedSlot = 0;
    }
  }

  private void _saveSlotsInfo() {
    var saveFile = FileAccess.Open(SLOT_INFO_PATH, FileAccess.ModeFlags.Write);
    var data = new Dictionary<string, object> { { LATEST_LOADED_SLOT_FIELD_NAME, LatestLoadedSlot } };
    saveFile.StoreLine(JsonSerializer.Serialize(data));
    saveFile.Close();
  }
}
