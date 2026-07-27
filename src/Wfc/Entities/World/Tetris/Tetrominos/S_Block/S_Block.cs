namespace Wfc.Entities.Tetris.Tetrominos;

using Godot;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class S_Block : Tetromino {
  private static readonly Vector2[][] ROTATIONS = {
    new Vector2[] { new Vector2(-1, 0), new Vector2(0, 0), new Vector2(0, -1), new Vector2(1, -1) },
    new Vector2[] { new Vector2(0, -1), new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1) },
    new Vector2[] { new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 1), new Vector2(-1, 1) },
    new Vector2[] { new Vector2(0, 1), new Vector2(0, 0), new Vector2(-1, 0), new Vector2(-1, -1) },
  };

  protected override Vector2[][] RotationMap => ROTATIONS;

  public override void _Ready() {
    SetShape();
  }
}
