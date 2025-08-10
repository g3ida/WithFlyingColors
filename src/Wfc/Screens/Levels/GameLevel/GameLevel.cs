namespace Wfc.Screens.Levels;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Audio;
using Wfc.Entities.HUD;
using Wfc.Entities.World.Camera;
using Wfc.Entities.World.Cutscenes;
using Wfc.Entities.World.Player;
using Wfc.Screens.Levels;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[Meta(typeof(IAutoNode))]
public partial class GameLevel :
  Node2D,
  IGameLevel,
  IProvide<IGameLevel> {
  public override void _Notification(int what) => this.Notify(what);
  [Export]
  public string Track { get; set; } = default!;
  [NodePath("Cutscene")]
  private Cutscene _cutsceneNode = default!;
  [NodePath("Player")]
  private Player _playerNode = default!;
  [NodePath("Camera2D")]
  private GameCamera _cameraNode = default!;
  [NodePath("Camera2D/PauseMenu")]
  private PauseMenu _pauseMenuNode = default!;
  [NodePath("HUD/GemContainerHUD")]
  private GemsHUDContainer _gemsHUDContainerNode = default!;

  public LevelId LevelId { get; set; }

  public void OnResolved() {
    if (Track != null) {
      MusicTrackManager.LoadTrack(Track);
      MusicTrackManager.PlayTrack(Track);
    }
  }

  [Dependency]
  public IMusicTrackManager MusicTrackManager => this.DependOn<IMusicTrackManager>();

  public Player PlayerNode => _playerNode;

  public GameCamera CameraNode => _cameraNode;

  public Cutscene CutsceneNode => _cutsceneNode;

  public PauseMenu PauseMenuNode => _pauseMenuNode;

  public GemsHUDContainer GemsHUDContainerNode => _gemsHUDContainerNode;

  public override void _EnterTree() {
    base._EnterTree();
    this.WireNodes();
    this.Provide();
  }

  public override void _ExitTree() {
    base._ExitTree();
    MusicTrackManager.Stop();
  }

  public override void _Ready() {
    base._Ready();
    SetProcess(false);
    Global.Instance().Player = _playerNode;
  }

  public IGameLevel Value() => this;
}
