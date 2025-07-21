namespace Wfc.Entities.World.BrickBreaker.Powerups;

using System.Collections.Generic;
using Godot;
using Wfc.Core.Event;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class BrickPowerUpHandler : Node2D, IPowerUpHandler {
  private const float COLD_DOWN = 1.5f;
  private const int ITEM_INV_PROBABILITY = 4;
  private const string POWERUP_ASSETS_BASE_PATH = @"res://src/Wfc/Entities/World/BrickBreaker/Powerups/";

  private PackedScene PowerUp = GD.Load<PackedScene>(POWERUP_ASSETS_BASE_PATH + "PowerUp/PowerUp.tscn");
  private PackedScene SlowPowerUp = GD.Load<PackedScene>(POWERUP_ASSETS_BASE_PATH + "SlowPowerUp/SlowPowerUp.tscn");
  private PackedScene FastPowerUp = GD.Load<PackedScene>(POWERUP_ASSETS_BASE_PATH + "FastPowerUp/FastPowerUp.tscn");
  private PackedScene ScaleUpPowerUp = GD.Load<PackedScene>(POWERUP_ASSETS_BASE_PATH + "ScaleUpPowerUp/ScaleUpPowerUp.tscn");
  private PackedScene ScaleDownPowerUp = GD.Load<PackedScene>(POWERUP_ASSETS_BASE_PATH + "ScaleDownPowerUp/ScaleDownPowerUp.tscn");
  private PackedScene TripleBallsPowerUp = GD.Load<PackedScene>(POWERUP_ASSETS_BASE_PATH + "TripleBallsPowerUp/TripleBallsPowerUp.tscn");
  private PackedScene ProtectionAreaPowerUp = GD.Load<PackedScene>(POWERUP_ASSETS_BASE_PATH + "ProtectionAreaPowerUp/ProtectionAreaPowerUp.tscn");
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
    EventHandler.Instance.Events.BrickBroken += OnBrickBroken;
    EventHandler.Instance.Events.CheckpointLoaded += Reset;
  }

  private void DisconnectSignals() {
    EventHandler.Instance.Events.BrickBroken -= OnBrickBroken;
    EventHandler.Instance.Events.CheckpointLoaded -= Reset;
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
    fallingPowerUpsContainer.CallDeferred(Node.MethodName.AddChild, powerUp);
    powerUp.CallDeferred(Node.MethodName.SetOwner, fallingPowerUpsContainer);
    powerUp.Connect("OnPlayerHit", new Callable(this, nameof(OnPlayerHit)));
    cooldownTimer.Start();
  }

  private void OnBrickBroken(string color, Vector2 position) {
    if (cooldownTimer.TimeLeft < MathUtils.EPSILON && isActive) {
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
      CallDeferred(Node.MethodName.AddChild, hit);
    }
    powerUp?.Disconnect("OnPlayerHit", new Callable(this, nameof(OnPlayerHit)));
    EventHandler.Instance.EmitPickedPowerup();
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
