using Godot;
using System.Collections.Generic;

public partial class BrickPowerUpHandler : Node2D, IPowerUpHandler {
  private const float COLD_DOWN = 1.5f;
  private const int ITEM_INV_PROBABILITY = 4;

  private PackedScene PowerUp = GD.Load<PackedScene>("res://Assets/Scenes/BrickBreaker/Powerups/PowerUp.tscn");
  private PackedScene SlowPowerUp = GD.Load<PackedScene>("res://Assets/Scenes/BrickBreaker/Powerups/SlowPowerUp.tscn");
  private PackedScene FastPowerUp = GD.Load<PackedScene>("res://Assets/Scenes/BrickBreaker/Powerups/FastPowerUp.tscn");
  private PackedScene ScaleUpPowerUp = GD.Load<PackedScene>("res://Assets/Scenes/BrickBreaker/Powerups/ScaleUpPowerUp.tscn");
  private PackedScene ScaleDownPowerUp = GD.Load<PackedScene>("res://Assets/Scenes/BrickBreaker/Powerups/ScaleDownPowerUp.tscn");
  private PackedScene TripleBallsPowerUp = GD.Load<PackedScene>("res://Assets/Scenes/BrickBreaker/Powerups/TripleBallsPowerUp.tscn");
  private PackedScene ProtectionAreaPowerUp = GD.Load<PackedScene>("res://Assets/Scenes/BrickBreaker/Powerups/ProtectionAreaPowerUp.tscn");
  private List<PackedScene> powerUps = new List<PackedScene>();

  private List<PowerUpScript> activePowerupNodes = new List<PowerUpScript>();
  private bool isActive = true;
  private Godot.Collections.Array<PackedScene> powerUpsRandomList = new Godot.Collections.Array<PackedScene>();

  private Node2D fallingPowerUpsContainer;
  private Timer cooldownTimer;
  private BrickBreaker brickBreakerNode;

  public override void _Ready() {
    powerUps.Add(SlowPowerUp);
    powerUps.Add(FastPowerUp);
    powerUps.Add(ScaleUpPowerUp);
    powerUps.Add(ScaleDownPowerUp);
    powerUps.Add(TripleBallsPowerUp);
    powerUps.Add(ProtectionAreaPowerUp);

    fallingPowerUpsContainer = GetNode<Node2D>("FallingPowerUpsContainer");
    cooldownTimer = GetNode<Timer>("CooldownTimer");
    brickBreakerNode = GetParent().GetParent<BrickBreaker>();

    cooldownTimer.WaitTime = COLD_DOWN;
    GD.Randomize();
  }

  private void ConnectSignals() {
    Event.Instance.Connect("brick_broken", new Callable(this, nameof(OnBrickBroken)));
    Event.Instance.Connect("checkpoint_loaded", new Callable(this, nameof(Reset)));
  }

  private void DisconnectSignals() {
    Event.Instance.Disconnect("brick_broken", new Callable(this, nameof(OnBrickBroken)));
    Event.Instance.Disconnect("checkpoint_loaded", new Callable(this, nameof(Reset)));
  }

  public void Reset() {
    RemoveActivePowerups();
    RemoveFallingPowerups();
    cooldownTimer.Stop();
  }

  private void FillPowerupsRandomList() {
    powerUpsRandomList.Clear();
    foreach (var el in powerUps) {
      powerUpsRandomList.Add(el);
    }
    powerUpsRandomList.Shuffle();
  }

  private PackedScene GetRandomPowerup() {
    if (powerUpsRandomList.Count == 0) {
      FillPowerupsRandomList();
    }
    var powerUp = powerUpsRandomList[powerUpsRandomList.Count - 1];
    powerUpsRandomList.RemoveAt(powerUpsRandomList.Count - 1);
    return powerUp;
  }

  public void RemoveActivePowerups() {
    foreach (var el in activePowerupNodes) {
      el.QueueFree();
    }
    activePowerupNodes.Clear();
  }

  public void RemoveFallingPowerups() {
    foreach (Node2D el in fallingPowerUpsContainer.GetChildren()) {
      el.QueueFree();
    }
  }

  private void CreatePowerup(PackedScene powerUpNode, string color, Vector2 position) {
    var powerUp = powerUpNode.Instantiate<Node2D>();
    powerUp.Set("color_group", color);
    powerUp.Position = position - Position;
    fallingPowerUpsContainer.CallDeferred("add_child", powerUp);
    powerUp.CallDeferred("set_owner", fallingPowerUpsContainer);
    powerUp.Connect("on_player_hit", new Callable(this, nameof(OnPlayerHit)));
    cooldownTimer.Start();
  }

  private void OnBrickBroken(string color, Vector2 position) {
    if (cooldownTimer.TimeLeft < Constants.EPSILON && isActive) {
      if (GD.Randi() % ITEM_INV_PROBABILITY == 0) {
        var powerUp = GetRandomPowerup();
        CreatePowerup(powerUp, color, position);
      }
    }
  }

  private bool CheckIfCanAddPowerup(PackedScene hitNode) {
    foreach (var powerUp in activePowerupNodes) {

      if (!powerUp.IsIncremental
          && powerUp.SceneFilePath == hitNode.ResourcePath) {
        return false;
      }
    }
    return true;
  }

  private void RemoveIrrelevantPowerups() {
    var listToDelete = new List<int>();
    int i = 0;
    foreach (var el in activePowerupNodes) {
      if (!el.IsStillRelevant()) {
        listToDelete.Insert(0, i);
      }
      i++;
    }
    foreach (var index in listToDelete) {
      activePowerupNodes[index].QueueFree();
      activePowerupNodes.RemoveAt(index);
    }
  }

  private void OnPlayerHit(Node2D powerUp, PackedScene hitNode) {
    RemoveIrrelevantPowerups();
    if (CheckIfCanAddPowerup(hitNode)) {
      var hit = hitNode.Instantiate<PowerUpScript>();
      activePowerupNodes.Add(hit);
      hit.SetBrickBreakerNode(brickBreakerNode);
      CallDeferred("add_child", hit);
    }
    powerUp?.Disconnect("on_player_hit", new Callable(this, nameof(OnPlayerHit)));
    Event.Instance.EmitPickedPowerup();
  }

  public override void _EnterTree() {
    ConnectSignals();
  }

  public override void _ExitTree() {
    DisconnectSignals();
  }

  public override void _Process(double delta) {
    SetProcess(false);
  }

  public void SetActive(bool active) {
    isActive = active;
  }
}
