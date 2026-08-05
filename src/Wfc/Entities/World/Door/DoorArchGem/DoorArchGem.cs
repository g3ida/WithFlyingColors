namespace Wfc.Entities.World.Door;

using Godot;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;

// One gem set into a door's arch, standing for a single color group of the level
// behind it. A banked gem wears its skin color over a lit halo; one still out
// there is left as a ghost of itself, carved into the stone.
[ScenePath]
public partial class DoorArchGem : Node2D {
  // Ghost gems sit on the arch's pale masonry, so they need to be a good step
  // darker than it while staying faint enough to read as empty.
  private static readonly Color GHOST_COLOR = new("8b8b93");
  private const float GHOST_ALPHA = 0.5f;
  private const float GLOW_ALPHA = 0.75f;

  // The gem arriving in its socket: it lands oversized and springs back, and its halo
  // flares past what it settles at so the landing reads from across the room.
  private const float LANDING_SCALE = 2.2f;
  private const float LANDING_DURATION = 0.45f;
  private const float LANDING_FLARE = 3.0f;

  #region Exports
  [Export]
  public string ColorGroup { get; set; } = ColorUtils.BLUE;
  #endregion Exports

  #region Nodes
  [NodePath("Glow")]
  private Sprite2D _glowNode = default!;
  [NodePath("Gem")]
  private Sprite2D _gemNode = default!;
  #endregion Nodes

  public bool IsCollected { get; private set; }

  // The scale the arch was authored at, which each door sets for itself: a landing that
  // tweened back to one would resize the gem for good the first time it played.
  private Vector2 _restScale = Vector2.One;
  private Vector2 _glowRestScale = Vector2.One;
  private Tween? _landingTween;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _restScale = Scale;
    _glowRestScale = _glowNode.Scale;
    _refresh();
  }

  // The gem dropping into its socket, for a level whose gems the player has just brought
  // back. Only the arrival is animated; what the gem then looks like is _refresh's.
  public void PlayLanding() {
    if (!IsNodeReady() || !IsCollected) {
      return;
    }
    _landingTween?.Kill();
    _landingTween = CreateTween();
    _landingTween.SetParallel(true);
    Scale = _restScale * LANDING_SCALE;
    _glowNode.Scale = _glowRestScale * LANDING_FLARE;
    _landingTween.TweenProperty(this, "scale", _restScale, LANDING_DURATION)
      .SetTrans(Tween.TransitionType.Elastic)
      .SetEase(Tween.EaseType.Out);
    _landingTween.TweenProperty(_glowNode, "scale", _glowRestScale, LANDING_DURATION)
      .SetTrans(Tween.TransitionType.Quad)
      .SetEase(Tween.EaseType.Out);
  }

  public void SetCollected(bool isCollected) {
    IsCollected = isCollected;
    if (IsNodeReady()) {
      _refresh();
    }
  }

  private void _refresh() {
    if (!IsCollected) {
      _gemNode.Modulate = new Color(GHOST_COLOR, GHOST_ALPHA);
      _glowNode.Visible = false;
      return;
    }

    var color = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(ColorGroup),
      SkinColorIntensity.Basic
    );
    _gemNode.Modulate = color;
    _glowNode.Modulate = new Color(color, GLOW_ALPHA);
    _glowNode.Visible = true;
  }
}
