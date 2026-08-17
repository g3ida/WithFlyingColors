namespace Wfc.Screens.Levels;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Audio;
using Wfc.Core.Persistence;
using Wfc.Entities.HUD;
using Wfc.Entities.World.Camera;
using Wfc.Entities.World.Cutscenes;
using Wfc.Entities.World.Gems;
using Wfc.Entities.World.Player;
using Wfc.Screens.Levels;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[Meta(typeof(IAutoNode))]
public partial class GameLevel :
  Node2D,
  IGameLevel,
  IProvide<IGameLevel>,
  IProvide<IGameRepo> {
  public override void _Notification(int what) => this.Notify(what);
  [Export]
  public string Track { get; set; } = default!;
  // How long the intro cutscene walks the player forward when this level starts from
  // its beginning. Per level, because spawns differ: a level whose spawn hangs in the
  // air or sits near a ledge sets zero rather than walking the player blind.
  [Export]
  public float IntroWalkTime { get; set; } = 1.2f;
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
    if (!string.IsNullOrEmpty(Track)) {
      MusicTrackManager.LoadTrack(Track);
      MusicTrackManager.PlayTrack(Track);
    }
    _applyBankedGems();
  }

  // Gems this level has already given up stay given up: the HUD opens with their slots
  // filled, and the ones still standing in the world are left as ghosts of themselves.
  // Runs here rather than in the gems' own _Ready because only the level knows which
  // level it is, and the slot is not readable until dependencies resolve.
  private void _applyBankedGems() {
    var bankedGems = SaveManager.GetSlotMetaData()?.GemsCollectedIn(LevelId);
    if (bankedGems == null || bankedGems.Count == 0) {
      return;
    }

    _gemsHUDContainerNode.MarkAlreadyCollected(bankedGems);
    foreach (var gem in this.FindDescendants<Gem>()) {
      if (bankedGems.Contains(gem.GroupName)) {
        gem.MarkAlreadyCollected();
      }
    }
  }

  [Dependency]
  public IMusicTrackManager MusicTrackManager => this.DependOn<IMusicTrackManager>();

  [Dependency]
  public ISaveManager SaveManager => this.DependOn<ISaveManager>();

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
    GameRepo.SetPlayer(_playerNode);
    // The level knows which level it is; the menu inside it does not, and the
    // entries it offers depend on that.
    _pauseMenuNode.ConfigureForLevel(LevelId);
  }

  public IGameLevel Value() => this;

  public IGameRepo GameRepo => Levels.GameRepo.Instance;
  IGameRepo IProvide<IGameRepo>.Value() => GameRepo;
}
