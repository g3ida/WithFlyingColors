namespace Wfc.Core.Persistence;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Wfc.Core.Serialization;
using Wfc.Entities.World.Camera;
using Wfc.Entities.World.Player;

public partial class SaveManager : ISaveManager {

  private const int NUM_SLOTS = 3;
  private readonly string LATEST_LOADED_SLOT_FIELD_NAME = "latest_loaded_slot";
  private readonly string SLOT_INFO_PATH = $"user://slots/slots_info.save";
  public int LatestLoadedSlot { get; private set; }
  private readonly SaveSlot[] _saveSlots = [.. Enumerable.Range(1, NUM_SLOTS).Select(i => new SaveSlot(i))];
  private readonly ISerializer _serializer = new SimpleJsonSerializer();

  public void SaveGame(SceneTree tree, int slotIndex) {
    slotIndex = slotIndex == ISaveManager.NO_SLOT ? Math.Max(0, LatestLoadedSlot) : slotIndex;
    if (slotIndex is < 0 or >= NUM_SLOTS) {
      GD.PushError($"Invalid slot index: {slotIndex}. Must be 0-{NUM_SLOTS - 1}");
      return;
    }
    _saveSlots[slotIndex].Save(_serializer, tree, false);
    _loadSlotsMetaData();
    GD.Print("Game saved!");
  }

  public void LoadGame(SceneTree tree, Player player, GameCamera camera, int slotIndex = ISaveManager.NO_SLOT) {
    slotIndex = slotIndex == ISaveManager.NO_SLOT ? Math.Max(0, LatestLoadedSlot) : slotIndex;
    if (slotIndex is < 0 or >= NUM_SLOTS) {
      GD.PushError($"Invalid slot index: {slotIndex}. Must be 0-{NUM_SLOTS - 1}");
      return;
    }
    _saveSlots[slotIndex].Load(_serializer, tree);
    // Update camera position to the player position avoiding smoothing
    // which would make you see the camera move quickly to the checkpoint position
    // when we load a level. We put it here instead of the reset method because
    // I like the smoothing effect when the player loses
    camera.UpdatePosition(player.GlobalPosition);
    LatestLoadedSlot = slotIndex;
    _saveSlotsInfo();
    GD.Print("Game loaded!");
  }

  public bool IsSLotFilled(int slotIndex = ISaveManager.NO_SLOT) {
    slotIndex = slotIndex == ISaveManager.NO_SLOT ? Math.Max(0, LatestLoadedSlot) : slotIndex;
    if (slotIndex is < 0 or >= NUM_SLOTS) {
      GD.PushError($"Invalid slot index: {slotIndex}. Must be 0-{NUM_SLOTS - 1}");
      return false;
    }
    return _saveSlots[slotIndex].IsFilled;
  }

  public SlotMetaData? GetSlotMetaData(int slotIndex = ISaveManager.NO_SLOT) {
    slotIndex = slotIndex == ISaveManager.NO_SLOT ? Math.Max(0, LatestLoadedSlot) : slotIndex;
    if (slotIndex is < 0 or >= NUM_SLOTS) {
      GD.PushError($"Invalid slot index: {slotIndex}. Must be 0-{NUM_SLOTS - 1}");
      return null;
    }
    return _saveSlots[slotIndex].MetaData;
  }

  public ImageTexture? GetSlotImage(int slotIndex = ISaveManager.NO_SLOT) {
    slotIndex = slotIndex == ISaveManager.NO_SLOT ? Math.Max(0, LatestLoadedSlot) : slotIndex;
    if (slotIndex is < 0 or >= NUM_SLOTS) {
      GD.PushError($"Invalid slot index: {slotIndex}. Must be 0-{NUM_SLOTS - 1}");
      return null;
    }
    return _saveSlots[slotIndex].MetaData?.Image;
  }

  private void _loadSlotsMetaData() {
    foreach (var slot in _saveSlots) {
      slot.LoadMetaData(_serializer);
    }
  }

  public int GetSelectedSlotIndex() => LatestLoadedSlot;

  public bool HasSelectedSlot() => LatestLoadedSlot != ISaveManager.NO_SLOT;

  public void Init() {
    _loadSlotsInfo();
    _loadSlotsMetaData();
  }

  public void SelectSlot(int slotIndex = ISaveManager.NO_SLOT) {
    slotIndex = slotIndex == ISaveManager.NO_SLOT ? Math.Max(0, LatestLoadedSlot) : slotIndex;
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
      LatestLoadedSlot = ISaveManager.NO_SLOT;
    }
  }

  // Deserialized as JsonElement rather than object: System.Text.Json hands back the
  // raw element for an untyped value, so unboxing one straight to int threw. Any
  // unreadable file falls back to the first slot rather than stranding the player.
  private void _loadSlotsInfo() {
    LatestLoadedSlot = 0;
    if (!FileAccess.FileExists(SLOT_INFO_PATH)) {
      return;
    }

    using var metaDataFile = FileAccess.Open(SLOT_INFO_PATH, FileAccess.ModeFlags.Read);
    if (metaDataFile == null) {
      return;
    }

    try {
      var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(metaDataFile.GetLine());
      if (data != null
          && data.TryGetValue(LATEST_LOADED_SLOT_FIELD_NAME, out var latest)
          && latest.TryGetInt32(out var slotIndex)
          && slotIndex is >= 0 and < NUM_SLOTS) {
        LatestLoadedSlot = slotIndex;
      }
    }
    catch (JsonException error) {
      GD.PushError($"Could not read {SLOT_INFO_PATH}: {error.Message}");
    }
  }

  private void _saveSlotsInfo() {
    // The slots folder doesn't exist on a first run, and FileAccess won't create the
    // path it is handed: opening for write under a missing directory hands back null
    // rather than failing loudly, which surfaced as a NullReferenceException out of
    // the first screen to select a slot - taking the rest of that screen's _Ready
    // with it.
    DirAccess.MakeDirRecursiveAbsolute(SLOT_INFO_PATH.GetBaseDir());

    var saveFile = FileAccess.Open(SLOT_INFO_PATH, FileAccess.ModeFlags.Write);
    if (saveFile == null) {
      GD.PushError($"Could not write {SLOT_INFO_PATH}: {FileAccess.GetOpenError()}");
      return;
    }
    var data = new Dictionary<string, object> { { LATEST_LOADED_SLOT_FIELD_NAME, LatestLoadedSlot } };
    saveFile.StoreLine(JsonSerializer.Serialize(data));
    saveFile.Close();
  }
}
