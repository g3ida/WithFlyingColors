namespace Wfc.Screens.Levels;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Audio;
using Wfc.Entities.World.Camera;
using Wfc.Entities.World.Cutscenes;
using Wfc.Entities.World.Player;
using Wfc.Screens.Levels;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[Meta(typeof(IAutoNode))]
public partial class GameLevel : Node2D {
  public override void _Notification(int what) => this.Notify(what);
  [Export]
  public string Track { get; set; } = null!;
  [NodePath("Cutscene")]
  private Cutscene CutsceneNode = null!;
  [NodePath("Player")]
  private Player Player = null!;
  [NodePath("Camera2D")]
  private GameCamera Camera = null!;
  public LevelId LevelId { get; set; }

  public void OnResolved() {
    if (Track != null) {
      MusicTrackManager.LoadTrack(Track);
      MusicTrackManager.PlayTrack(Track);
    }
  }

  [Dependency]
  public IMusicTrackManager MusicTrackManager => this.DependOn<IMusicTrackManager>();

  public override void _EnterTree() { }

  public override void _ExitTree() {
    MusicTrackManager.Stop();
  }

  public override void _Ready() {
    SetProcess(false);
    this.WireNodes();
    Global.Instance().Cutscene = CutsceneNode;
    Global.Instance().Player = Player;
  }
}
