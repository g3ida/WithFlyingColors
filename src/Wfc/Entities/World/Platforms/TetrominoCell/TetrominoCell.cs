namespace Wfc.Entities.World.Platforms;

using Godot;
using Wfc.Entities.Tetris;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;

// One cell of a falling tetromino, drawn with the tetris minigame's own block art: the same bevel
// and the same light and dark faces, so a piece raining through a level and a piece coming down the
// pool are the same object seen twice.
//
// It is rooted in an AnimatableBody2D rather than a static body, which is what carries a player
// standing on a piece down with it instead of leaving them in the air on the row it has just left.
[Tool]
[ScenePath]
public partial class TetrominoCell : AnimatableBody2D {
  #region Constants
  // What the block art is drawn at. The sprite is scaled by Size against it, so a cell sized off
  // the minigame's grid keeps the bevel in proportion instead of growing a border.
  private const float ART_CELL_SIZE = Constants.TETRIS_BLOCK_ART_SIZE;

  // The colour area stands a shade proud of the body, so a cube resting on a cell is already inside
  // the colour it has to match rather than exactly touching its edge.
  private const float COLOR_AREA_OVERSIZE = 1.03f;

  private const float MIN_SIZE = 8.0f;
  #endregion Constants

  #region Exports
  // The cell is square: it is a tetris cell, and the piece it belongs to steps down by exactly this
  // much on every row it reaches.
  [Export]
  public float Size {
    get => _size;
    set {
      _size = Mathf.Max(value, MIN_SIZE);
      _applyShape();
    }
  }
  private float _size = ART_CELL_SIZE;

  // Which colour the cell wears, and so which face of the player may land on it. A tetromino always
  // wears one of the four - there is no neutral piece, the same way there is no neutral face.
  [Export(PropertyHint.Enum, "blue,pink,yellow,purple")]
  public string Group {
    get => _group;
    set {
      _group = value;
      _applyColor();
    }
  }
  private string _group = ColorUtils.BLUE;
  #endregion Exports

  #region Fields
  // The exported setters fire while the scene is still loading, before there are any nodes to push
  // the new value into.
  private bool _isWired;
  #endregion Fields

  #region Nodes
  [NodePath("BlockSprite")]
  private BlockSprite _spriteNode = default!;
  [NodePath("CollisionShape")]
  private CollisionShape2D _collisionShapeNode = default!;
  [NodePath("Area2D")]
  private Area2D _areaNode = default!;
  [NodePath("Area2D/ColorAreaShape")]
  private CollisionShape2D _colorAreaShapeNode = default!;
  #endregion Nodes

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _isWired = true;

    _applyShape();
    _applyColor();
  }

  private void _applyShape() {
    if (!_isWired) {
      return;
    }
    _spriteNode.Scale = Vector2.One * (Size / ART_CELL_SIZE);
    _resizeShape(_collisionShapeNode, Size);
    _resizeShape(_colorAreaShapeNode, Size * COLOR_AREA_OVERSIZE);
  }

  // The group follows the export rather than being fixed at load: a cell given a new colour that
  // still answers to its old one kills whoever lands on what they can see.
  private void _applyColor() {
    if (!_isWired) {
      return;
    }

    foreach (var colorGroup in ColorUtils.COLOR_GROUPS) {
      if (_areaNode.IsInGroup(colorGroup)) {
        _areaNode.RemoveFromGroup(colorGroup);
      }
    }
    _areaNode.AddToGroup(Group);
    _spriteNode.SetGroup(Group);
  }

  private static void _resizeShape(CollisionShape2D collisionShape, float size) {
    if (collisionShape.Shape is RectangleShape2D rectangle) {
      rectangle.Size = new Vector2(size, size);
    }
  }
}
