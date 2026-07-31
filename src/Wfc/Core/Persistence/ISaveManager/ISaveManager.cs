namespace Wfc.Core.Persistence;

using Godot;
using Wfc.Entities.World.Camera;
using Wfc.Entities.World.Player;
using Wfc.Screens.Levels;

public interface ISaveManager {
  // Two meanings, deliberately the same value: what GetSelectedSlotIndex returns
  // when the player has no slot selected (deleting the selected slot leaves them
  // there), and what the slotIndex parameters below take to mean "the selected
  // slot". Callers that print or index with GetSelectedSlotIndex must check for it
  // first, or use HasSelectedSlot.
  public const int NO_SLOT = -1;

  // How many slots exist, so menu code can reason over all of them (does any slot
  // have a game going, which one was played last) instead of only the selected one.
  public int SlotCount { get; }

  public void SaveGame(SceneTree tree, int slotIndex = NO_SLOT);
  public void RecordProgress(SceneTree tree, LevelId levelId, int progressPercent, int slotIndex = NO_SLOT);
  // Marks a level as cleared for good and moves the resume pointer to the next level
  // in the chain (or parks it at the cleared one when the chain is over), then writes
  // the slot out. One call so a quit between the two updates cannot split them.
  public void RecordLevelCleared(SceneTree tree, LevelId clearedLevelId, LevelId? nextLevelId, int slotIndex = NO_SLOT);
  public void LoadGame(SceneTree tree, Player player, GameCamera camera, int slotIndex = NO_SLOT);
  public bool IsSLotFilled(int slotIndex = NO_SLOT);
  public int GetSelectedSlotIndex();
  public bool HasSelectedSlot();
  public void SelectSlot(int slotIndex = NO_SLOT);
  public SlotMetaData? GetSlotMetaData(int slotIndex = NO_SLOT);
  public void RemoveSaveSlot(int slotIndex);
  public void Init();
}
