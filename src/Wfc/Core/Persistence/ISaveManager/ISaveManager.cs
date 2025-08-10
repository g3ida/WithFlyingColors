namespace Wfc.Core.Persistence;

using Godot;
using Wfc.Entities.World.Camera;
using Wfc.Entities.World.Player;

public interface ISaveManager {
  public void SaveGame(SceneTree tree, int slotIndex = -1);
  public void LoadGame(SceneTree tree, Player player, GameCamera camera, int slotIndex = -1);
  public bool IsSLotFilled(int slotIndex = -1);
  public ImageTexture? GetSlotImage(int slotIndex = -1);
  public int GetSelectedSlotIndex();
  public void SelectSlot(int slotIndex = -1);
  public SlotMetaData? GetSlotMetaData(int slotIndex = -1);
  public void RemoveSaveSlot(int slotIndex);
  public void Init();
}
