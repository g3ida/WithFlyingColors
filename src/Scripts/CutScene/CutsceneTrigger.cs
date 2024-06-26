using Godot;
using System.Collections.Generic;

public partial class CutsceneTrigger : Area2D
{
    private Marker2D followChild = null;
    private bool triggered = false;

    public override void _Ready()
    {
        var children = GetChildren();
        foreach (var ch in children)
        {
            if (ch is Marker2D position2D)
            {
                followChild = position2D;
            }
        }
    }

    private void _on_Area2D_body_entered(Node body)
    {
        if (!triggered && body == Global.Instance().Player && followChild != null)
        {
            triggered = true;
            Global.Instance().Cutscene.ShowSomeNode(followChild, 3.0f, 3.2f);
        }
    }

    public override void _EnterTree()
    {
        // Connect("body_entered", this, nameof(_on_Area2D_body_entered));
    }

    public override void _ExitTree()
    {
        // Disconnect("body_entered", this, nameof(_on_Area2D_body_entered));
    }
}
