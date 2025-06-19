namespace Wfc.Screens.Levels;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Screens.Levels;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[Meta(typeof(IAutoNode))]
public partial class GameLevel : Node2D {

  [Export]
  public string Track { get; set; } = null!;

  [NodePath("Cutscene")]
  private Cutscene CutsceneNode = null!;
  public LevelId LevelId { get; set; }

  public override void _EnterTree() {
    if (Track != null) {
      AudioManager.Instance().MusicTrackManager.LoadTrack(Track);
      AudioManager.Instance().MusicTrackManager.PlayTrack(Track);
    }
  }

  public override void _ExitTree() {
    AudioManager.Instance().MusicTrackManager.Stop();
  }

  public override void _Ready() {
    SetProcess(false);
    this.WireNodes();
    Global.Instance().Cutscene = CutsceneNode;
  }
}
