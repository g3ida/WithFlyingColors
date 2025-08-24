namespace Wfc.Entities.World.BrickBreaker.Powerups;

using Godot;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class ProtectionArea : StaticBody2D {
  public override void _Ready() {
    // Initialization code goes here
  }

  private void _onArea2DBodyEntered(Node body) {
    if (body is BouncingBall) {
      QueueFree();
    }
  }
}
