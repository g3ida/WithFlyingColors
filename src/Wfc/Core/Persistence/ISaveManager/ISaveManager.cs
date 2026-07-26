namespace Wfc.Core.Persistence;

using Godot;
using Wfc.Entities.World.Camera;
using Wfc.Entities.World.Player;

public interface ISaveManager {
  // Two meanings, deliberately the same value: what GetSelectedSlotIndex returns
  // when the player has no slot selected (deleting the selected slot leaves them
  // there), and what the slotIndex parameters below take to mean "the selected
  // slot". Callers that print or index with GetSelectedSlotIndex must check for it
  // first, or use HasSelectedSlot.
  public const int NO_SLOT = -1;

  public void SaveGame(SceneTree tree, int slotIndex = NO_SLOT);
  public void LoadGame(SceneTree tree, Player player, GameCamera camera, int slotIndex = NO_SLOT);
  public bool IsSLotFilled(int slotIndex = NO_SLOT);
  public int GetSelectedSlotIndex();
  public bool HasSelectedSlot();
  public void SelectSlot(int slotIndex = NO_SLOT);
  public SlotMetaData? GetSlotMetaData(int slotIndex = NO_SLOT);
  public void RemoveSaveSlot(int slotIndex);
  public void Init();
}
