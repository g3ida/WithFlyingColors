namespace Wfc.test.Helpers.Fakes;

using System.Collections.Generic;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Persistence;
using Wfc.Entities.World.Camera;
using Wfc.Entities.World.Player;
using Wfc.Screens.Levels;

// An ISaveManager backed by memory rather than user:// files, so a test can put the
// game in a given save state without touching the player's own saves.
//
// It mirrors the real manager's two awkward contracts on purpose, because those are
// what the menus have to cope with: NO_SLOT passed as an argument means "the selected
// slot, or slot 0 if there isn't one", while NO_SLOT coming back out of
// GetSelectedSlotIndex means there genuinely isn't one.
public sealed class FakeSaveManager : ISaveManager {
  public const int NUM_SLOTS = 3;

  public int SlotCount => NUM_SLOTS;

  private readonly SlotMetaData?[] _slots = new SlotMetaData?[NUM_SLOTS];

  public int SelectedSlot { get; private set; }

  // Records so tests can assert what the menus asked for.
  public int SaveGameCallCount { get; private set; }
  public int RecordProgressCallCount { get; private set; }
  public int RecordLevelClearedCallCount { get; private set; }
  public int RecordHubArrivalSeenCallCount { get; private set; }
  public int RemoveSaveSlotCallCount { get; private set; }

  public FakeSaveManager(int selectedSlot = 0) {
    SelectedSlot = selectedSlot;
  }

  // Gives a slot some progress, which is what the play sub-menu keys "continue" off.
  // The timestamp is settable so tests can decide which slot was played most recently.
  public FakeSaveManager WithFilledSlot(int slotIndex, int progress = 50, ulong timestamp = 1_700_000_000UL) {
    _slots[slotIndex] = new SlotMetaData(slotIndex, timestamp, LevelId.Level1, progress, timestamp);
    return this;
  }

  // A slot whose only trace of play is a finished level: progress alone must not be
  // the definition of "played", or clearing a level then quitting hides Continue.
  public FakeSaveManager WithClearedLevel(int slotIndex, LevelId levelId) {
    _slots[slotIndex] ??= new SlotMetaData(slotIndex, 1_700_000_000UL, levelId, 0, 1_700_000_000UL);
    _slots[slotIndex]!.ClearedLevels.Add(levelId);
    return this;
  }

  // A run that has already been walked into the hub, so it opens at a door instead of
  // playing the arrival cutscene over whatever the test is trying to watch.
  public FakeSaveManager WithHubArrivalSeen(int slotIndex) {
    _slots[slotIndex] ??= new SlotMetaData(slotIndex, 1_700_000_000UL, LevelId.Hub, 0, 1_700_000_000UL);
    _slots[slotIndex]!.HasSeenHubArrival = true;
    return this;
  }

  // A counter the run has already climbed, which is what the hub's stats board reads.
  public FakeSaveManager WithRunStat(int slotIndex, RunStat stat, ulong count) {
    _slots[slotIndex] ??= new SlotMetaData(slotIndex, 1_700_000_000UL, LevelId.Level1, 0, 1_700_000_000UL);
    _slots[slotIndex]!.Counters[stat] = count;
    return this;
  }

  // Gems already banked for a level, which is what a hub door's pentagon reads.
  public FakeSaveManager WithCollectedGems(int slotIndex, LevelId levelId, params string[] colorGroups) {
    _slots[slotIndex] ??= new SlotMetaData(slotIndex, 1_700_000_000UL, levelId, 0, 1_700_000_000UL);
    _bankGems(_slots[slotIndex]!, levelId, colorGroups);
    return this;
  }

  public void RecordRunStat(RunStat stat, int slotIndex = ISaveManager.NO_SLOT) {
    var index = _resolve(slotIndex);
    if (!_isValid(index) || _slots[index] is not { } metaData) {
      return;
    }
    metaData.Counters[stat] = metaData.CounterOf(stat) + 1;
  }

  private int _resolve(int slotIndex) => slotIndex == ISaveManager.NO_SLOT ? Mathf.Max(0, SelectedSlot) : slotIndex;

  private static bool _isValid(int slotIndex) => slotIndex >= 0 && slotIndex < NUM_SLOTS;

  public void Init() { }

  public int GetSelectedSlotIndex() => SelectedSlot;

  public bool HasSelectedSlot() => SelectedSlot != ISaveManager.NO_SLOT;

  public void SelectSlot(int slotIndex = ISaveManager.NO_SLOT) {
    var index = _resolve(slotIndex);
    if (_isValid(index)) {
      SelectedSlot = index;
    }
  }

  public SlotMetaData? GetSlotMetaData(int slotIndex = ISaveManager.NO_SLOT) {
    var index = _resolve(slotIndex);
    return _isValid(index) ? _slots[index] : null;
  }

  public bool IsSLotFilled(int slotIndex = ISaveManager.NO_SLOT) {
    var index = _resolve(slotIndex);
    return _isValid(index) && _slots[index] != null;
  }


  public void RemoveSaveSlot(int slotIndex) {
    RemoveSaveSlotCallCount++;
    if (!_isValid(slotIndex)) {
      return;
    }
    _slots[slotIndex] = null;
    // The real manager leaves the player with no selection at all here. It is the
    // state most of the slot bugs came out of, so the fake reproduces it.
    if (SelectedSlot == slotIndex) {
      SelectedSlot = ISaveManager.NO_SLOT;
    }
  }

  public void SaveGame(SceneTree tree, int slotIndex = ISaveManager.NO_SLOT) {
    SaveGameCallCount++;
    var index = _resolve(slotIndex);
    if (_isValid(index)) {
      _slots[index] ??= new SlotMetaData(index, 1_700_000_000UL, LevelId.Level1, 0, 1_700_000_000UL);
    }
  }

  // Mirrors the real manager: gems are what a clear pays out, so a progress write never banks any.
  public void RecordProgress(SceneTree tree, LevelId levelId, int progressPercent, int slotIndex = ISaveManager.NO_SLOT) {
    RecordProgressCallCount++;
    var index = _resolve(slotIndex);
    if (!_isValid(index) || _slots[index] is not { } slot) {
      return;
    }
    slot.LevelId = levelId;
    slot.Progress = progressPercent;
    GameEvents.Instance.OnSaveSlotUpdated();
  }

  public void RecordLevelCleared(SceneTree tree, LevelId clearedLevelId, LevelId? nextLevelId, IEnumerable<string>? collectedGems = null, int slotIndex = ISaveManager.NO_SLOT) {
    RecordLevelClearedCallCount++;
    var index = _resolve(slotIndex);
    // Mirrors the real manager: clearing a level with no save slot records nothing.
    if (!_isValid(index) || _slots[index] is not { } slot) {
      return;
    }
    slot.ClearedLevels.Add(clearedLevelId);
    slot.LevelId = nextLevelId ?? clearedLevelId;
    slot.Progress = nextLevelId == null ? 100 : 0;
    _bankGems(slot, clearedLevelId, collectedGems);
    GameEvents.Instance.OnSaveSlotUpdated();
  }

  private static void _bankGems(SlotMetaData slot, LevelId levelId, IEnumerable<string>? collectedGems) {
    if (collectedGems == null) {
      return;
    }
    if (!slot.CollectedGems.TryGetValue(levelId, out var gems)) {
      gems = [];
      slot.CollectedGems[levelId] = gems;
    }
    gems.UnionWith(collectedGems);
  }

  public void RecordHubArrivalSeen(int slotIndex = ISaveManager.NO_SLOT) {
    RecordHubArrivalSeenCallCount++;
    var index = _resolve(slotIndex);
    // Mirrors the real manager: with no slot there is nothing to remember it in.
    if (!_isValid(index) || _slots[index] is not { } slot) {
      return;
    }
    slot.HasSeenHubArrival = true;
    GameEvents.Instance.OnSaveSlotUpdated();
  }

  public void LoadGame(SceneTree tree, Player player, GameCamera camera, int slotIndex = ISaveManager.NO_SLOT) {
    var index = _resolve(slotIndex);
    if (_isValid(index)) {
      SelectedSlot = index;
    }
  }
}
