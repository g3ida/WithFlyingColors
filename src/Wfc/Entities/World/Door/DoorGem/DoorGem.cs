namespace Wfc.Entities.World.Door;

using System.Collections.Generic;
using System.Linq;
using Godot;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;

// The comet on a door's keystone. Each facet is a white mask colored at runtime from the
// current skin - the same reason the cube composites grayscale sprites - but the pentagon
// is all or nothing: the four gems set into the arch already count the tally, and this is
// what the four of them make once they are all home. Anything less is carved stone.
[ScenePath]
public partial class DoorGem : Node2D {

  // Empty sockets are carved stone, not faded gems - and they sit on the keystone's
  // pale marble, so they have to be a good step darker than the stone behind them
  // or they disappear into it.
  private static readonly Color DIM_FACET_COLOR = new("a3a3ab");
  private static readonly Color DIM_FACET_DARK_COLOR = new("8b8b93");

  // How bright the keystone burns with every facet lit. The halo is additive, so
  // it wants to stay well short of opaque or a full pentagon blows out to white.
  private const float FULL_GLOW_ALPHA = 0.65f;

  #region Nodes
  [NodePath("Glow")]
  private Sprite2D _glowNode = default!;
  [NodePath("Base")]
  private Sprite2D _baseNode = default!;
  [NodePath("Pink")]
  private Sprite2D _pinkNode = default!;
  [NodePath("Yellow")]
  private Sprite2D _yellowNode = default!;
  [NodePath("BlueDark")]
  private Sprite2D _blueDarkNode = default!;
  [NodePath("Blue")]
  private Sprite2D _blueNode = default!;
  #endregion Nodes

  private readonly HashSet<string> _collectedColorGroups = [];
  private Vector2 _restPosition;
  private Vector2 _restScale = Vector2.One;
  private Tween? _formingTween;

  // Every gem of the level behind this door is home, which is the only state the comet has.
  public bool IsComplete => ColorUtils.COLOR_GROUPS.All(_collectedColorGroups.Contains);

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _restPosition = Position;
    _restScale = Scale;
    _refreshFacets();
  }

  public void SetCollectedGems(IEnumerable<string> colorGroups) {
    _collectedColorGroups.Clear();
    _collectedColorGroups.UnionWith(colorGroups);
    if (IsNodeReady()) {
      _refreshFacets();
    }
  }

  // The comet coming together out of the gems' light and being set into the keystone: it
  // gathers where they met, above the door, and is carried down into its socket.
  public void FormAt(Vector2 formingPosition, float formDuration, float travelDuration) {
    if (!IsNodeReady()) {
      return;
    }
    _formingTween?.Kill();
    Position = formingPosition;
    Scale = Vector2.Zero;
    Rotation = -Mathf.Tau;
    _formingTween = CreateTween();
    _formingTween.SetParallel(true);
    _formingTween.TweenProperty(this, "scale", _restScale, formDuration)
      .SetTrans(Tween.TransitionType.Back)
      .SetEase(Tween.EaseType.Out);
    _formingTween.TweenProperty(this, "rotation", 0.0f, formDuration)
      .SetTrans(Tween.TransitionType.Cubic)
      .SetEase(Tween.EaseType.Out);
    _formingTween.SetParallel(false);
    _formingTween.TweenProperty(this, "position", _restPosition, travelDuration)
      .SetTrans(Tween.TransitionType.Back)
      .SetEase(Tween.EaseType.InOut);
  }

  // Whatever a ceremony was in the middle of, dropped: the door is being rebuilt or told
  // something new, and the comet belongs in its socket at the size it was authored.
  public void SnapToRest() {
    _formingTween?.Kill();
    _formingTween = null;
    if (IsNodeReady()) {
      Position = _restPosition;
      Scale = _restScale;
      Rotation = 0.0f;
    }
  }

  private void _refreshFacets() {
    var isComplete = IsComplete;
    // The base pentagon shows through as the purple facet under the others.
    _baseNode.Modulate = _facetColor(ColorUtils.PURPLE, SkinColorIntensity.Basic, DIM_FACET_COLOR, isComplete);
    _pinkNode.Modulate = _facetColor(ColorUtils.PINK, SkinColorIntensity.Basic, DIM_FACET_COLOR, isComplete);
    _yellowNode.Modulate = _facetColor(ColorUtils.YELLOW, SkinColorIntensity.Basic, DIM_FACET_COLOR, isComplete);
    // The blue facet is cut in two shades, the cube's own face-and-edge trick.
    _blueNode.Modulate = _facetColor(ColorUtils.BLUE, SkinColorIntensity.Basic, DIM_FACET_COLOR, isComplete);
    _blueDarkNode.Modulate = _facetColor(ColorUtils.BLUE, SkinColorIntensity.Dark, DIM_FACET_DARK_COLOR, isComplete);
    _glowNode.Visible = isComplete;
    _glowNode.Modulate = new Color(1f, 1f, 1f, FULL_GLOW_ALPHA);
  }

  private static Color _facetColor(string colorGroup, SkinColorIntensity intensity, Color dimColor, bool isComplete) =>
    isComplete
      ? SkinManager.Instance.CurrentSkin.GetColor(GameSkin.ColorGroupToSkinColor(colorGroup), intensity)
      : dimColor;
}
