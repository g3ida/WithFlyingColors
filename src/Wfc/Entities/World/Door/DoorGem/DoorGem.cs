namespace Wfc.Entities.World.Door;

using System.Collections.Generic;
using System.Linq;
using Godot;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;

// The pentagon gem on a door's keystone. Each facet is a white mask colored at
// runtime from the current skin - the same reason the cube composites grayscale
// sprites - so a door can tell the level's gem tally: facets of banked gems wear
// their gem's color, the rest stay socket-gray.
[ScenePath]
public partial class DoorGem : Node2D {

  // Empty sockets are carved stone, not faded gems - and they sit on the keystone's
  // pale marble, so they have to be a good step darker than the temple's socket gray
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

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _refreshFacets();
  }

  public void SetCollectedGems(IEnumerable<string> colorGroups) {
    _collectedColorGroups.Clear();
    _collectedColorGroups.UnionWith(colorGroups);
    if (IsNodeReady()) {
      _refreshFacets();
    }
  }

  private void _refreshFacets() {
    // The base pentagon shows through as the purple facet under the others.
    _baseNode.Modulate = _facetColor(ColorUtils.PURPLE, SkinColorIntensity.Basic, DIM_FACET_COLOR);
    _pinkNode.Modulate = _facetColor(ColorUtils.PINK, SkinColorIntensity.Basic, DIM_FACET_COLOR);
    _yellowNode.Modulate = _facetColor(ColorUtils.YELLOW, SkinColorIntensity.Basic, DIM_FACET_COLOR);
    // The blue facet is cut in two shades, the cube's own face-and-edge trick.
    _blueNode.Modulate = _facetColor(ColorUtils.BLUE, SkinColorIntensity.Basic, DIM_FACET_COLOR);
    _blueDarkNode.Modulate = _facetColor(ColorUtils.BLUE, SkinColorIntensity.Dark, DIM_FACET_DARK_COLOR);
    _refreshGlow();
  }

  // The keystone lights up as the level behind it gives up its gems, so the halo
  // is worth exactly the share of the tally that has been banked.
  private void _refreshGlow() {
    var lit = ColorUtils.COLOR_GROUPS.Count(_collectedColorGroups.Contains);
    _glowNode.Visible = lit > 0;
    _glowNode.Modulate = new Color(1f, 1f, 1f, FULL_GLOW_ALPHA * lit / ColorUtils.COLOR_GROUPS.Length);
  }

  private Color _facetColor(string colorGroup, SkinColorIntensity intensity, Color dimColor) =>
    _collectedColorGroups.Contains(colorGroup)
      ? SkinManager.Instance.CurrentSkin.GetColor(GameSkin.ColorGroupToSkinColor(colorGroup), intensity)
      : dimColor;
}
