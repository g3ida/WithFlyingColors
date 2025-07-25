namespace Wfc.Entities.Tetris.Tetrominos;

using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class J_Block : Tetromino {
  protected override Array<Array<Vector2>> rotationMap => new Array<Array<Vector2>>() {
            new Array<Vector2> { new Vector2(1, 0), new Vector2(0, 0), new Vector2(-1, 0), new Vector2(-1, -1) },
            new Array<Vector2> { new Vector2(0, 1), new Vector2(0, 0), new Vector2(0, -1), new Vector2(1, -1) },
            new Array<Vector2> { new Vector2(-1, 0), new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1) },
            new Array<Vector2> { new Vector2(0, -1), new Vector2(0, 0), new Vector2(0, 1), new Vector2(-1, 1) }
  };

  public override void _Ready() {
    SetShape();
  }
}
