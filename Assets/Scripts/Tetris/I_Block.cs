using Godot;
using System;
using System.Collections.Generic;

public class I_Block : Tetromino
{

    public I_Block() {
        rotationMap = new List<List<Vector2>>()
        {
            new List<Vector2> { new Vector2(-1, 0), new Vector2(0, 0), new Vector2(1, 0), new Vector2(2, 0) },
            new List<Vector2> { new Vector2(0, 1), new Vector2(0, 0), new Vector2(0, -1), new Vector2(0, -2) },
            new List<Vector2> { new Vector2(1, 0), new Vector2(0, 0), new Vector2(-1, 0), new Vector2(-2, 0) },
            new List<Vector2> { new Vector2(0, -1), new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 2) }
        };
    }
    public override void _Ready()
    {
        SetShape();
    }
}
