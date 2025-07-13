using System;
using Godot;
using Wfc.Utils;

public partial class SlidingPlatformGear : Sprite2D {
  private Vector2 lastPosition;
  private const float RotationSpeed = 0.01f;

  public override void _Ready() {
    // Set parent scale so the sprite won't be affected.
    var parentPlatformScale = GetParent().GetParent<Node2D>().Scale;
    GetParent<Node2D>().Scale = new Vector2(1 / parentPlatformScale.X, 1 / parentPlatformScale.Y);
    lastPosition = GlobalPosition;
  }

  public override void _PhysicsProcess(double delta) {
    var currentPosition = GlobalPosition;
    var deltaPosition = currentPosition - lastPosition;
    lastPosition = currentPosition;

    int direction = 0;
    if (deltaPosition.X > MathUtils.EPSILON2 || deltaPosition.Y > MathUtils.EPSILON2) {
      direction = 1;
    }
    else if (deltaPosition.X < -MathUtils.EPSILON2 || deltaPosition.Y < -MathUtils.EPSILON2) {
      direction = -1;
    }

    Rotate(RotationSpeed * deltaPosition.Length() * direction);
  }
}
