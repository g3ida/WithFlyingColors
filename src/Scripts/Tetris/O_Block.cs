using Godot;
using System;
using System.Collections.Generic;

public partial class O_Block : Tetromino
{
    public O_Block() {
        rotationMap = new List<List<Vector2>>()
        {
            new List<Vector2> { new Vector2(-1, 0), new Vector2(0, 0), new Vector2(-1, 1), new Vector2(0, 1) },
            new List<Vector2> { new Vector2(-1, 0), new Vector2(0, 0), new Vector2(-1, 1), new Vector2(0, 1) },
            new List<Vector2> { new Vector2(-1, 0), new Vector2(0, 0), new Vector2(-1, 1), new Vector2(0, 1) },
            new List<Vector2> { new Vector2(-1, 0), new Vector2(0, 0), new Vector2(-1, 1), new Vector2(0, 1) }
        };
    }
    public override void _Ready()
    {
        SetShape();
    }
}
