namespace Wfc.Entities.World.Platforms;

using Godot;

// The small sprite a platform carries to say it is driven rather than laid down: the cog on a
// sliding one, the arrow on a rotating one. A platform is otherwise drawn exactly like a static
// one, so the badge is the only thing telling the player this surface is going somewhere.
//
// A tool script because the platforms hold typed references to their badge: the editor gives a node
// whose script is not one a placeholder instance instead, and the platform that reaches for the
// badge through it comes away with nothing.
[Tool]
public abstract partial class PlatformBadge : Sprite2D {
  private Vector2 _inverseScale = Vector2.One;
  private float _fit = 1.0f;

  public override void _Ready() {
    base._Ready();
    // The body a slider drives is often scaled to the size the level wanted, and this is a sprite:
    // it has to come back out of that scale or the badge is drawn as an ellipse.
    var carried = GetParent<Node2D>().GlobalScale;
    if (!Mathf.IsZeroApprox(carried.X) && !Mathf.IsZeroApprox(carried.Y)) {
      _inverseScale = new Vector2(1.0f / carried.X, 1.0f / carried.Y);
    }
    _apply();
  }

  // Sized by the platform rather than by the art, for the platforms that are given a size instead of
  // a scale. Never grown past the art, so the badge is drawn as it was drawn.
  public void FitTo(float diameter) {
    var art = Texture?.GetHeight() ?? 0;
    _fit = art > 0 ? Mathf.Min(diameter / art, 1.0f) : 1.0f;
    _apply();
  }

  private void _apply() => Scale = _inverseScale * _fit;
}
