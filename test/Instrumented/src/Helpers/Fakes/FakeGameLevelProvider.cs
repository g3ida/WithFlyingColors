namespace Wfc.test.instrumented.Helpers.Fakes;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Entities.HUD;
using Wfc.Entities.World.Camera;
using Wfc.Entities.World.Cutscenes;
using Wfc.Entities.World.Player;
using Wfc.Screens;
using Wfc.Screens.Levels;

// Stands in for the level a world entity is placed in, for the entities that ask it for the camera
// or the player. Only what a test reaches for is filled in - the rest of a level is a great deal of
// scene to build for something that is not being looked at.
[Meta(typeof(IAutoNode))]
public partial class FakeGameLevelProvider : Node, IProvide<IGameLevel>, IGameLevel {
  public override void _Notification(int what) => this.Notify(what);

  public Player PlayerNode { get; set; } = null!;
  public GameCamera CameraNode { get; set; } = null!;
  public Cutscene CutsceneNode => null!;
  public PauseMenu PauseMenuNode => null!;
  public GemsHUDContainer GemsHUDContainerNode => null!;
  public LevelId LevelId => LevelId.Level1;

  IGameLevel IProvide<IGameLevel>.Value() => this;

  public void OnReady() => this.Provide();
}
