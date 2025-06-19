using System.Collections.Generic;
using Godot;

public partial class CutsceneTrigger : Area2D {
  private Marker2D followChild = null;
  private bool triggered = false;

  public override void _Ready() {
    var children = GetChildren();
    foreach (var ch in children) {
      if (ch is Marker2D position2D) {
        followChild = position2D;
      }
    }
  }

  private void _onBodyEntered(Node body) {
    if (!triggered && body == Global.Instance().Player && followChild != null) {
      triggered = true;
      Global.Instance().Cutscene.ShowSomeNode(followChild, 3.0f, 3.2f);
    }
  }

  public override void _EnterTree() {
    Connect("body_entered", new Callable(this, nameof(_onBodyEntered)), (uint)ConnectFlags.Persist);

  }

  public override void _ExitTree() {
    Disconnect("body_entered", new Callable(this, nameof(_onBodyEntered)));
  }
}
