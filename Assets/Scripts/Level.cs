using Godot;
using System;

public class Level : Node2D
{
    [Export]
    public string Track { get; set; }

    private Cutscene CutsceneNode;

    public override void _EnterTree()
    {
        if (Track != null)
        {
            AudioManager.Instance().MusicTrackManager.LoadTrack(Track);
            AudioManager.Instance().MusicTrackManager.PlayTrack(Track);
        }
    }

    public override void _ExitTree()
    {
        AudioManager.Instance().MusicTrackManager.Stop();
    }

    public override void _Ready()
    {
        SetProcess(false);
        CutsceneNode = GetNode<Cutscene>("Cutscene");
        Global.Instance().Cutscene = CutsceneNode;
    }
}
