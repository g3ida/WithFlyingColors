namespace Wfc.Entities.World.BrickBreaker.Powerups;

using Godot;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class ProtectionArea : StaticBody2D {
  public override void _Ready() {
    // Initialization code goes here
  }

  public void _on_Area2D_body_entered(Node body) {
    if (body is BouncingBall) {
      QueueFree();
    }
  }
}
