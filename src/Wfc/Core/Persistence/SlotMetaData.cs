namespace Wfc.Core.Persistence;

using Godot;
using Wfc.Screens.Levels;

public class SlotMetaData {

  public int SlotId { get; set; }
  public ulong SaveTimestamp { get; set; }
  public LevelId LevelId { get; set; }
  public int Progress { get; set; }

  public ulong LastLoadDate { get; set; }

  public ImageTexture? Image { get; set; }


  public SlotMetaData(int slotId, ulong saveTimestamp, LevelId levelId, int progress, ulong lastLoadDate) {
    SlotId = slotId;
    SaveTimestamp = saveTimestamp;
    LevelId = levelId;
    Progress = progress;
    LastLoadDate = lastLoadDate;
  }
}
