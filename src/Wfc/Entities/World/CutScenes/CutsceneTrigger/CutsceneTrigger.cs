namespace Wfc.Entities.World.Cutscenes;

using System.Collections.Generic;
using Godot;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class CutsceneTrigger : Area2D {
  private Marker2D? _followChild = null;
  private bool _triggered = false;

  public override void _Ready() {
    var children = GetChildren();
    foreach (var ch in children) {
      if (ch is Marker2D position2D) {
        _followChild = position2D;
      }
    }
  }

  private void _onBodyEntered(Node body) {
    if (!_triggered && body == Global.Instance().Player && _followChild != null) {
      _triggered = true;
      Global.Instance().Cutscene.ShowSomeNode(_followChild, 3.0f, 3.2f);
    }
  }

  public override void _EnterTree() {
    base._EnterTree();
    Connect(
      Area2D.SignalName.BodyEntered,
      new Callable(this, nameof(_onBodyEntered)),
      (uint)ConnectFlags.Persist
    );

  }

  public override void _ExitTree() {
    base._ExitTree();
    Disconnect(
      Area2D.SignalName.BodyEntered,
      new Callable(this, nameof(_onBodyEntered))
    );
  }
}
