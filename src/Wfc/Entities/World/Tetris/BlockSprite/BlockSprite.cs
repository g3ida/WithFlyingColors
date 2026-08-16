namespace Wfc.Entities.Tetris;

using Godot;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;

// [Tool] because TetrominoCell is: the editor leaves a non-tool script uninstantiated, so a
// parent wiring this scene into a BlockSprite-typed field would get a bare Node2D.
[Tool]
[ScenePath]
public partial class BlockSprite : Node2D {
  #region Exports
  [Export]
  public string ColorGroup {
    get => _colorGroup;
    set => SetGroup(value);
  }
  private string _colorGroup = "blue";
  #endregion Exports

  #region Fields
  // The export setter fires while the scene is still loading, before there are any layers to
  // colour.
  private bool _isWired;
  #endregion Fields

  #region Nodes
  [NodePath("Frame")]
  private TextureRect _frameNode = default!;
  [NodePath("Layer1")]
  private Sprite2D _layer1Node = default!;
  [NodePath("Layer2")]
  private Sprite2D _layer2Node = default!;
  [NodePath("TopLayer")]
  private Sprite2D _topLayerNode = default!;
  #endregion Nodes

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _isWired = true;
    SetGroup(_colorGroup);
  }

  public void SetGroup(string group) {
    _colorGroup = group;
    if (!_isWired) {
      return;
    }

    var skin = SkinManager.Instance.CurrentSkin;
    var skinColor = GameSkin.ColorGroupToSkinColor(_colorGroup);
    _frameNode.Modulate = skin.GetColor(skinColor, SkinColorIntensity.VeryDark);
    _layer1Node.Modulate = skin.GetColor(skinColor, SkinColorIntensity.Light);
    _layer2Node.Modulate = skin.GetColor(skinColor, SkinColorIntensity.Dark);
    _topLayerNode.Modulate = skin.GetColor(skinColor, SkinColorIntensity.Basic);
  }

  public string GetGroup() => _colorGroup;
}
