namespace Wfc.Core.Persistence;

using System.Collections.Generic;
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
  // collectedGems banks the gems the player is holding at this write, so hub doors can
  // show them without loading the level. It rides along with the progress or clear
  // write rather than being its own save: one write per event, so a quit in between
  // cannot leave the two halves disagreeing, and the door only ever shows what a
  // reload would actually give back.
  // Where the run has got to. Gems are deliberately not part of this: they are what finishing a
  // level pays out, so RecordLevelCleared is the only thing that banks them.
  public void RecordProgress(SceneTree tree, LevelId levelId, int progressPercent, int slotIndex = NO_SLOT);
  // Marks a level as cleared for good and moves the resume pointer to the next level
  // in the chain (or parks it at the cleared one when the chain is over), then writes
  // the slot out. One call so a quit between the two updates cannot split them.
  public void RecordLevelCleared(SceneTree tree, LevelId clearedLevelId, LevelId? nextLevelId, IEnumerable<string>? collectedGems = null, int slotIndex = NO_SLOT);
  // Marks this run as having been walked into the hub, so the arrival it is shown the first
  // time it steps in is never shown again. Metadata alone, so unlike the calls above it says
  // nothing about where the player is standing and needs no scene tree.
  public void RecordHubArrivalSeen(int slotIndex = NO_SLOT);
  // Counts one more of something the run has done, for the hub's stats board. Metadata only,
  // and deliberately not written out here: these climb constantly while the player is moving,
  // so they ride along with the next progress or clear write.
  public void RecordRunStat(RunStat stat, int slotIndex = NO_SLOT);
  public void LoadGame(SceneTree tree, Player player, GameCamera camera, int slotIndex = NO_SLOT);
  public bool IsSLotFilled(int slotIndex = NO_SLOT);
  public int GetSelectedSlotIndex();
  public bool HasSelectedSlot();
  public void SelectSlot(int slotIndex = NO_SLOT);
  public SlotMetaData? GetSlotMetaData(int slotIndex = NO_SLOT);
  public void RemoveSaveSlot(int slotIndex);
  public void Init();
  // Waits for anything still on its way to disk. Writes are handed to a background thread so a
  // checkpoint cannot drop a frame, which leaves the moments that cannot afford to catch up
  // later - the window closing above all - needing to say so.
  public void Flush();
}
