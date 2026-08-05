namespace Wfc.Entities.World.Platforms;

using Godot;

// The cog on a sliding platform, turned by however far the platform has just travelled so that it
// reads as what is driving it.
//
// A tool script because the slider holds a typed reference to it: the editor gives a node whose
// script is not one a placeholder instance instead, and the slider that reaches for the cog through
// it comes away with nothing.
[Tool]
public partial class SlidingPlatformGear : Sprite2D {
  private const float SPIN_PER_PIXEL = 0.01f;

  private Vector2 _inverseScale = Vector2.One;
  private float _fit = 1.0f;

  public override void _Ready() {
    base._Ready();
    // The body a slider drives is often scaled to the size the level wanted, and this is a sprite:
    // it has to come back out of that scale or the cog is drawn as an ellipse.
    var carried = GetParent<Node2D>().GlobalScale;
    if (!Mathf.IsZeroApprox(carried.X) && !Mathf.IsZeroApprox(carried.Y)) {
      _inverseScale = new Vector2(1.0f / carried.X, 1.0f / carried.Y);
    }
    _apply();
  }

  // Sized by the platform rather than by the art, for the platforms that are given a size instead of
  // a scale. Never grown past the art, so the cog is drawn as it was drawn.
  public void FitTo(float diameter) {
    var art = Texture?.GetHeight() ?? 0;
    _fit = art > 0 ? Mathf.Min(diameter / art, 1.0f) : 1.0f;
    _apply();
  }

  private void _apply() => Scale = _inverseScale * _fit;

  // Turned by the platform rather than by watching it: the slider already knows how far it moved,
  // and a cog that reads its own position pays for that on every tick it stands still.
  public void Spin(float travelled) => Rotate(SPIN_PER_PIXEL * travelled);
}
