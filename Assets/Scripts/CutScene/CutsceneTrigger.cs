using Godot;
using System.Collections.Generic;

public class CutsceneTrigger : Area2D
{
    private Position2D followChild = null;
    private bool triggered = false;

    public override void _Ready()
    {
        var children = GetChildren();
        foreach (var ch in children)
        {
            if (ch is Position2D position2D)
            {
                followChild = position2D;
            }
        }
    }

    private void _OnArea2DBodyEntered(Node body)
    {
        if (!triggered && body == Global.Instance().Player && followChild != null)
        {
            triggered = true;
            Global.Instance().Cutscene.ShowSomeNode(followChild, 3.0f, 3.2f);
        }
    }

    public override void _EnterTree()
    {
        Connect("body_entered", this, nameof(_OnArea2DBodyEntered));
    }

    public override void _ExitTree()
    {
        Disconnect("body_entered", this, nameof(_OnArea2DBodyEntered));
    }
}
