using Godot;
using System;
using System.Collections.Generic;

public class BrickPowerUpHandler : Node2D
{
    private const float COLDDOWN = 1.5f;
    private const int ITEM_INV_PROBA = 4;

    private PackedScene PowerUp = GD.Load<PackedScene>("res://Assets/Scenes/BrickBreaker/Powerups/PowerUp.tscn");
    private PackedScene SlowPowerUp = GD.Load<PackedScene>("res://Assets/Scenes/BrickBreaker/Powerups/SlowPowerUp.tscn");
    private PackedScene FastPowerUp = GD.Load<PackedScene>("res://Assets/Scenes/BrickBreaker/Powerups/FastPowerUp.tscn");
    private PackedScene ScaleUpPowerUp = GD.Load<PackedScene>("res://Assets/Scenes/BrickBreaker/Powerups/ScaleUpPowerUp.tscn");
    private PackedScene ScaleDownPowerUp = GD.Load<PackedScene>("res://Assets/Scenes/BrickBreaker/Powerups/ScaleDownPowerUp.tscn");
    private PackedScene TripleBallsPowerUp = GD.Load<PackedScene>("res://Assets/Scenes/BrickBreaker/Powerups/TripleBallsPowerUp.tscn");
    private PackedScene ProtectionAreaPowerUp = GD.Load<PackedScene>("res://Assets/Scenes/BrickBreaker/Powerups/ProtectionAreaPowerUp.tscn");
    private List<PackedScene> powerups = new List<PackedScene>();

    private List<PowerUpScript> activePowerupNodes = new List<PowerUpScript>();
    private bool isActive = true;
    private Godot.Collections.Array<PackedScene> powerupsRandomList = new Godot.Collections.Array<PackedScene>();

    private Node2D fallingPowerUpsContainer;
    private Timer colddownTimer;
    private BrickBreaker brickBreakerNode;

    public override void _Ready()
    {
        powerups.Add(SlowPowerUp);
        powerups.Add(FastPowerUp);
        powerups.Add(ScaleUpPowerUp);
        powerups.Add(ScaleDownPowerUp);
        powerups.Add(TripleBallsPowerUp);
        powerups.Add(ProtectionAreaPowerUp);

        fallingPowerUpsContainer = GetNode<Node2D>("FallingPowerUpsContainer");
        colddownTimer = GetNode<Timer>("ColddownTimer");
        brickBreakerNode = GetParent().GetParent<BrickBreaker>();

        colddownTimer.WaitTime = COLDDOWN;
        GD.Randomize();
    }

    private void ConnectSignals()
    {
        Event.Instance().Connect("brick_broken", this, nameof(OnBrickBroken));
        Event.Instance().Connect("checkpoint_loaded", this, nameof(Reset));
    }

    private void DisconnectSignals()
    {
        Event.Instance().Disconnect("brick_broken", this, nameof(OnBrickBroken));
        Event.Instance().Disconnect("checkpoint_loaded", this, nameof(Reset));
    }

    public void Reset()
    {
        RemoveActivePowerups();
        RemoveFallingPowerups();
        colddownTimer.Stop();
    }

    private void FillPowerupsRandomList()
    {
        powerupsRandomList.Clear();
        foreach (var el in powerups)
        {
            powerupsRandomList.Add(el);
        }
        powerupsRandomList.Shuffle();
    }

    private PackedScene GetRandomPowerup()
    {
        if (powerupsRandomList.Count == 0)
        {
            FillPowerupsRandomList();
        }
        var powerup = powerupsRandomList[powerupsRandomList.Count - 1];
        powerupsRandomList.RemoveAt(powerupsRandomList.Count - 1);
        return powerup;
    }

    private void RemoveActivePowerups()
    {
        foreach (var el in activePowerupNodes)
        {
            el.QueueFree();
        }
        activePowerupNodes.Clear();
    }

    private void RemoveFallingPowerups()
    {
        foreach (Node2D el in fallingPowerUpsContainer.GetChildren())
        {
            el.QueueFree();
        }
    }

    private void CreatePowerup(PackedScene powerupNode, string color, Vector2 position)
    {
        var powerup = (Node2D)powerupNode.Instance();
        powerup.Set("color_group", color);
        powerup.Position = position - Position;
        fallingPowerUpsContainer.CallDeferred("add_child", powerup);
        powerup.CallDeferred("set_owner", fallingPowerUpsContainer);
        powerup.Connect("on_player_hit", this, nameof(OnPlayerHit));
        colddownTimer.Start();
    }

    private void OnBrickBroken(string color, Vector2 position)
    {
        if (colddownTimer.TimeLeft < Constants.EPSILON && isActive)
        {
            if (GD.Randi() % ITEM_INV_PROBA == 0)
            {
                var powerup = GetRandomPowerup();
                CreatePowerup(powerup, color, position);
            }
        }
    }

    private bool CheckIfCanAddPowerup(PackedScene hitNode)
    {
        foreach (var el in activePowerupNodes)
        {
            if (!el.IsIncremental && el.Filename == hitNode.ResourcePath)
            {
                return false;
            }
        }
        return true;
    }

    private void RemoveIrrelevantPowerups()
    {
        var listToDelete = new List<int>();
        int i = 0;
        foreach (var el in activePowerupNodes)
        {
            if (!el.IsStillRelevant())
            {
                listToDelete.Insert(0, i);
            }
            i++;
        }
        foreach (var index in listToDelete)
        {
            activePowerupNodes[index].QueueFree();
            activePowerupNodes.RemoveAt(index);
        }
    }

    private void OnPlayerHit(Node2D powerup, PackedScene hitNode)
    {
        RemoveIrrelevantPowerups();
        if (CheckIfCanAddPowerup(hitNode))
        {
            var hit = hitNode.Instance<PowerUpScript>();
            activePowerupNodes.Add(hit);
            hit.SetBrickBreakerNode(brickBreakerNode);
            CallDeferred("add_child", hit);
        }
        powerup?.Disconnect("on_player_hit", this, nameof(OnPlayerHit));
        Event.Instance().EmitPickedPowerup();
    }

    public override void _EnterTree()
    {
        ConnectSignals();
    }

    public override void _ExitTree()
    {
        DisconnectSignals();
    }

    public override void _Process(float delta)
    {
        SetProcess(false);
    }
}
