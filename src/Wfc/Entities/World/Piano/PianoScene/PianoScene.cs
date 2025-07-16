namespace Wfc.Entities.World.Piano;

using System;
using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class PianoScene : Node2D {

  [NodePath("Piano")]
  private Piano _pianoNodeScene = null!;

  public override void _EnterTree() {
    base._EnterTree();
    this.WireNodes();
  }

  private void _onTriggerAreaBodyEntered(Node2D body) {
    if (body != Global.Instance().Player)
      return;

    if (_pianoNodeScene.IsStopped()) {
      _pianoNodeScene.StartGame();
    }
  }
}
