using Godot;
using System;
using System.Collections.Generic;

public class T_Block : Tetromino
{
    public T_Block() {
        rotationMap = new List<List<Vector2>>()
        {
            new List<Vector2> { new Vector2(-1, 0), new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, -1) },
            new List<Vector2> { new Vector2(0, -1), new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0) },
            new List<Vector2> { new Vector2(1, 0), new Vector2(0, 0), new Vector2(-1, 0), new Vector2(0, 1) },
            new List<Vector2> { new Vector2(0, -1), new Vector2(0, 0), new Vector2(0, 1), new Vector2(-1, 0) }
        };
    }
    public override void _Ready()
    {
        SetShape();
    }
}
