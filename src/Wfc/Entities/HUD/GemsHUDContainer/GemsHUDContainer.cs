namespace Wfc.Entities.HUD;

using System.Collections.Generic;
using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;

[ScenePath]
public partial class GemsHUDContainer : Panel {

  [NodePath("BlueGem")]
  private GemHUD _blueGemNode = default!;
  [NodePath("PinkGem")]
  private GemHUD _pinkGemNode = default!;
  [NodePath("YellowGem")]
  private GemHUD _yellowGemNode = default!;
  [NodePath("PurpleGem")]
  private GemHUD _purpleGemNode = default!;

  private Dictionary<string, GemHUD> _gemsNodes = default!;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _gemsNodes = new Dictionary<string, GemHUD>
    {
      { ColorUtils.BLUE, _blueGemNode },
      { ColorUtils.PINK, _pinkGemNode },
      { ColorUtils.YELLOW, _yellowGemNode },
      { ColorUtils.PURPLE, _purpleGemNode }
    };
  }

  public bool IsGemCollected(string colorGroup) {
    if (_gemsNodes.TryGetValue(colorGroup, out var gemNode)) {
      if (gemNode.currentState == GemHUD.State.Collected) {
        return true;
      }
    }
    return false;
  }
}
