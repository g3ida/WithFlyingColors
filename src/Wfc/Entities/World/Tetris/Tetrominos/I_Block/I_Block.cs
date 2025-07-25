namespace Wfc.Entities.Tetris.Tetrominos;

using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class I_Block : Tetromino {
  protected override Array<Array<Vector2>> rotationMap => new Array<Array<Vector2>>()
    {
            new Array<Vector2> { new Vector2(-1, 0), new Vector2(0, 0), new Vector2(1, 0), new Vector2(2, 0) },
            new Array<Vector2> { new Vector2(0, 1), new Vector2(0, 0), new Vector2(0, -1), new Vector2(0, -2) },
            new Array<Vector2> { new Vector2(1, 0), new Vector2(0, 0), new Vector2(-1, 0), new Vector2(-2, 0) },
            new Array<Vector2> { new Vector2(0, -1), new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 2) }
    };

  public override void _Ready() {
    SetShape();
  }
}
