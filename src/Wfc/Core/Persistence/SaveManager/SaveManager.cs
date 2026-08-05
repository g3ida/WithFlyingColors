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
using Wfc.Screens.Levels;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class SaveManager : ISaveManager {

  private const int NUM_SLOTS = 3;
  private readonly string LATEST_LOADED_SLOT_FIELD_NAME = "latest_loaded_slot";
  private readonly string SLOT_INFO_PATH = $"user://slots/slots_info.save";
  public int SlotCount => NUM_SLOTS;
  public int LatestLoadedSlot { get; private set; }
  private readonly SaveSlot[] _saveSlots = [.. Enumerable.Range(1, NUM_SLOTS).Select(i => new SaveSlot(i))];
  private readonly ISerializer _serializer = new SimpleJsonSerializer();

  public void SaveGame(SceneTree tree, int slotIndex) {
    slotIndex = slotIndex == ISaveManager.NO_SLOT ? Math.Max(0, LatestLoadedSlot) : slotIndex;
    if (slotIndex is < 0 or >= NUM_SLOTS) {
      GD.PushError($"Invalid slot index: {slotIndex}. Must be 0-{NUM_SLOTS - 1}");
      return;
    }
    _saveSlots[slotIndex].Save(_serializer, tree);
    _loadSlotsMetaData();
    GD.Print("Game saved!");
  }

  // Records how far the player has got and writes the slot out.
  //
  // Nothing used to assign Progress or LevelId anywhere outside the SlotMetaData constructor, and
  // SaveGame had a single call site - creating a blank slot. Reaching a checkpoint wrote nothing
  // to disk at all, so the Continue button never appeared, the resume branch in SceneOrchester
  // was unreachable, the slot panel showed 0% for a finished run, and quitting lost everything.
  // Gems are what finishing a level pays out, so they are not banked here. A mid-level write that
  // carried them - a checkpoint, or the window closing - would light the level's hub door with
  // them, and the clear that was supposed to hand them over would arrive with nothing to show.
  public void RecordProgress(SceneTree tree, LevelId levelId, int progressPercent, int slotIndex = ISaveManager.NO_SLOT) {
    slotIndex = slotIndex == ISaveManager.NO_SLOT ? Math.Max(0, LatestLoadedSlot) : slotIndex;
    if (slotIndex is < 0 or >= NUM_SLOTS) {
      GD.PushError($"Invalid slot index: {slotIndex}. Must be 0-{NUM_SLOTS - 1}");
      return;
    }

    var slot = _saveSlots[slotIndex];
    if (!slot.IsFilled) {
      // The player is in a level without having picked a slot - there is nothing to record into,
      // and writing one here would invent a save they never asked for.
      return;
    }

    slot.RecordProgress(levelId, progressPercent);
    slot.Save(_serializer, tree);
    _loadSlotsMetaData();
  }

  public void RecordLevelCleared(SceneTree tree, LevelId clearedLevelId, LevelId? nextLevelId, IEnumerable<string>? collectedGems = null, int slotIndex = ISaveManager.NO_SLOT) {
    slotIndex = slotIndex == ISaveManager.NO_SLOT ? Math.Max(0, LatestLoadedSlot) : slotIndex;
    if (slotIndex is < 0 or >= NUM_SLOTS) {
      GD.PushError($"Invalid slot index: {slotIndex}. Must be 0-{NUM_SLOTS - 1}");
      return;
    }

    var slot = _saveSlots[slotIndex];
    if (!slot.IsFilled) {
      // Same rule as RecordProgress: clearing a level without a picked slot must not
      // invent a save the player never asked for.
      return;
    }

    slot.RecordCompletion(clearedLevelId);
    if (collectedGems != null) {
      slot.RecordCollectedGems(clearedLevelId, collectedGems);
    }
    // The resume pointer either advances to the next level's start or, at the end of
    // the chain, stays parked on the cleared level: RecordProgress takes the max
    // within a level, so full progress there survives later checkpoint replays.
    slot.RecordProgress(nextLevelId ?? clearedLevelId, nextLevelId == null ? 100 : 0);
    slot.Save(_serializer, tree);
    _loadSlotsMetaData();
  }

  public void RecordHubArrivalSeen(int slotIndex = ISaveManager.NO_SLOT) {
    slotIndex = slotIndex == ISaveManager.NO_SLOT ? Math.Max(0, LatestLoadedSlot) : slotIndex;
    if (slotIndex is < 0 or >= NUM_SLOTS) {
      GD.PushError($"Invalid slot index: {slotIndex}. Must be 0-{NUM_SLOTS - 1}");
      return;
    }

    var slot = _saveSlots[slotIndex];
    // Same rule as the record calls above: with no slot picked there is nothing this run
    // can be remembered in.
    if (!slot.IsFilled || slot.MetaData == null) {
      return;
    }

    slot.RecordHubArrivalSeen();
    // Metadata on its own. A full Save republishes the persist group as well, which would push
    // the level currently on screen into a save file whose metadata still names another one -
    // and this record moves no resume pointer that would make the two agree again.
    slot.SaveMetaData(_serializer);
    _loadSlotsMetaData();
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
    camera.SnapTo(player.GlobalPosition);
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

  // Every path that rewrites a slot ends here, so this is where the rest of the game
  // hears that what GetSlotMetaData hands out has moved.
  private void _loadSlotsMetaData() {
    foreach (var slot in _saveSlots) {
      slot.LoadMetaData(_serializer);
    }
    EventHandler.Instance.EmitSaveSlotUpdated();
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
  // unreadable file leaves no slot selected; every real flow selects one explicitly
  // before gameplay, so nothing should pretend a slot is active on a fresh install.
  private void _loadSlotsInfo() {
    LatestLoadedSlot = ISaveManager.NO_SLOT;
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
