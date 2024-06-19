using Godot;
using System;

public partial class ProtectionAreaPowerUp : PowerUpScript
{
    private PackedScene ProtectionArea = GD.Load<PackedScene>("res://Assets/Scenes/BrickBreaker/Powerups/ProtectionArea.tscn");

    private bool isRelevant = true;
    private Node2D protectionArea;

    public override void _Ready()
    {
        SetProcess(false);
        protectionArea = ProtectionArea.Instantiate<Node2D>();
        protectionArea.Position = BrickBreakerNode.ProtectionAreaSpawnerPositionNode.Position;
        BrickBreakerNode.CallDeferred("add_child", protectionArea);
        protectionArea.CallDeferred("set_owner", BrickBreakerNode);
        protectionArea.Connect("tree_exited", new Callable(this, nameof(_OnProtectionAreaDestroyed)));
    }

    public override void _ExitTree()
    {
        if (IsStillRelevant() && protectionArea != null)
        {
            protectionArea.QueueFree();
        }
    }

    private void _OnProtectionAreaDestroyed()
    {
        isRelevant = false;
    }

    public override bool IsStillRelevant()
    {
        return isRelevant;
    }
}
