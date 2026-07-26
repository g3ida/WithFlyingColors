namespace Wfc.test.Helpers.Fakes;

using Godot;
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

  private readonly SlotMetaData?[] _slots = new SlotMetaData?[NUM_SLOTS];

  public int SelectedSlot { get; private set; }

  // Records so tests can assert what the menus asked for.
  public int SaveGameCallCount { get; private set; }
  public int RemoveSaveSlotCallCount { get; private set; }

  public FakeSaveManager(int selectedSlot = 0) {
    SelectedSlot = selectedSlot;
  }

  // Gives a slot some progress, which is what the play sub-menu keys "continue" off.
  public FakeSaveManager WithFilledSlot(int slotIndex, int progress = 50) {
    _slots[slotIndex] = new SlotMetaData(slotIndex, 1_700_000_000UL, LevelId.Level1, progress, 1_700_000_000UL);
    return this;
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

  public ImageTexture? GetSlotImage(int slotIndex = ISaveManager.NO_SLOT) => null;

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

  public void LoadGame(SceneTree tree, Player player, GameCamera camera, int slotIndex = ISaveManager.NO_SLOT) {
    var index = _resolve(slotIndex);
    if (_isValid(index)) {
      SelectedSlot = index;
    }
  }
}
