using Godot;
using System;

public partial class PianoScene : Node2D
{
    private Piano PianoNodeScene;

    public override void _Ready()
    {
        PianoNodeScene = GetNode<Piano>("Piano");
    }

    private void _on_TriggerArea_body_entered(Node2D body)
    {
        if (body != Global.Instance().Player)
            return;

        if (PianoNodeScene.IsStopped())
        {
            PianoNodeScene.StartGame();
        }
    }
}
