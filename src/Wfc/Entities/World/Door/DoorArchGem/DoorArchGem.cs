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

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _refresh();
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
