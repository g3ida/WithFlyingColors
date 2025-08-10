namespace Wfc.Screens.Levels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wfc.Entities.HUD;
using Wfc.Entities.World.Camera;
using Wfc.Entities.World.Cutscenes;
using Wfc.Entities.World.Player;

public interface IGameLevel {
  public Player PlayerNode { get; }
  public GameCamera CameraNode { get; }
  public Cutscene CutsceneNode { get; }
  public PauseMenu PauseMenuNode { get; }
  public GemsHUDContainer GemsHUDContainerNode { get; }
  public LevelId LevelId { get; }
}
