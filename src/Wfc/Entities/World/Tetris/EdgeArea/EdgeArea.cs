namespace Wfc.Entities.Tetris;

using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;

// The grace zone over a seam between two blocks of different colors. It joins both color
// groups, so a face straddling the boundary satisfies one of them instead of being killed
// by the block it is only half standing on.
//
// It has to carry a block's own layer and mask. Built in code with Godot's defaults it sat
// on layer 1 masking 1, with a 20x20 shape, which no player face can see - so every seam
// in the pool stayed lethal no matter how many of these were spawned.
[ScenePath]
public partial class EdgeArea : Area2D {
  [NodePath("CollisionShape2D")]
  public CollisionShape2D? CollisionShapeNode;

  // Null-conditional because the shape is only wired in _Ready, and _Ready only runs on
  // AddChild when the parent is already inside the tree. TetrisAI builds candidate pieces
  // out of tree and gets away with it by passing permissiveMode: false; reading a size
  // through this would otherwise be one default argument away from throwing.
  public float Width => (CollisionShapeNode?.Shape as RectangleShape2D)?.Size.X ?? 0f;
  public float Height => (CollisionShapeNode?.Shape as RectangleShape2D)?.Size.Y ?? 0f;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
  }
}
