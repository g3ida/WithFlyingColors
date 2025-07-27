namespace Wfc.Entities.World.BrickBreaker.Powerups;

using System.Collections.Generic;
using Godot;
using Wfc.Core.Event;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class BrickPowerUpHandler : Node2D, IPowerUpHandler {
  #region Constants
  private const float COLD_DOWN = 1.5f;
  private const int ITEM_INV_PROBABILITY = 4;
  private const string POWERUP_ASSETS_BASE_PATH = @"res://src/Wfc/Entities/World/BrickBreaker/Powerups/";
  #endregion Constants
  private readonly PackedScene PowerUp = GD.Load<PackedScene>(POWERUP_ASSETS_BASE_PATH + "PowerUp/PowerUp.tscn");
  private readonly PackedScene SlowPowerUp = GD.Load<PackedScene>(POWERUP_ASSETS_BASE_PATH + "SlowPowerUp/SlowPowerUp.tscn");
  private readonly PackedScene FastPowerUp = GD.Load<PackedScene>(POWERUP_ASSETS_BASE_PATH + "FastPowerUp/FastPowerUp.tscn");
  private readonly PackedScene ScaleUpPowerUp = GD.Load<PackedScene>(POWERUP_ASSETS_BASE_PATH + "ScaleUpPowerUp/ScaleUpPowerUp.tscn");
  private readonly PackedScene ScaleDownPowerUp = GD.Load<PackedScene>(POWERUP_ASSETS_BASE_PATH + "ScaleDownPowerUp/ScaleDownPowerUp.tscn");
  private readonly PackedScene TripleBallsPowerUp = GD.Load<PackedScene>(POWERUP_ASSETS_BASE_PATH + "TripleBallsPowerUp/TripleBallsPowerUp.tscn");
  private readonly PackedScene ProtectionAreaPowerUp = GD.Load<PackedScene>(POWERUP_ASSETS_BASE_PATH + "ProtectionAreaPowerUp/ProtectionAreaPowerUp.tscn");
  private List<PackedScene> _powerUps = new List<PackedScene>();

  private List<PowerUpScript> _activePowerupNodes = new List<PowerUpScript>();
  private bool isActive = true;
  private Godot.Collections.Array<PackedScene> _powerUpsRandomList = new Godot.Collections.Array<PackedScene>();

  #region Nodes
  [NodePath("FallingPowerUpsContainer")]
  private Node2D _fallingPowerUpsContainer = default!;
  [NodePath("CooldownTimer")]
  private Timer _cooldownTimer = default!;
  private BrickBreaker _brickBreakerNode = default!;
  #endregion  Nodes

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _powerUps.Add(SlowPowerUp);
    _powerUps.Add(FastPowerUp);
    _powerUps.Add(ScaleUpPowerUp);
    _powerUps.Add(ScaleDownPowerUp);
    _powerUps.Add(TripleBallsPowerUp);
    _powerUps.Add(ProtectionAreaPowerUp);

    _brickBreakerNode = GetParent().GetParent<BrickBreaker>();
    _cooldownTimer.WaitTime = COLD_DOWN;
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
    _cooldownTimer.Stop();
  }

  private void FillPowerupsRandomList() {
    _powerUpsRandomList.Clear();
    foreach (var el in _powerUps) {
      _powerUpsRandomList.Add(el);
    }
    _powerUpsRandomList.Shuffle();
  }

  private PackedScene GetRandomPowerup() {
    if (_powerUpsRandomList.Count == 0) {
      FillPowerupsRandomList();
    }
    var powerUp = _powerUpsRandomList[_powerUpsRandomList.Count - 1];
    _powerUpsRandomList.RemoveAt(_powerUpsRandomList.Count - 1);
    return powerUp;
  }

  public void RemoveActivePowerups() {
    foreach (var el in _activePowerupNodes) {
      el.QueueFree();
    }
    _activePowerupNodes.Clear();
  }

  public void RemoveFallingPowerups() {
    foreach (Node2D el in _fallingPowerUpsContainer.GetChildren()) {
      el.QueueFree();
    }
  }

  private void CreatePowerup(PackedScene powerUpNode, string color, Vector2 position) {
    var powerUp = powerUpNode.Instantiate<Node2D>();
    powerUp.Set("ColorGroup", color);
    powerUp.Position = position - Position;
    _fallingPowerUpsContainer.CallDeferred(Node.MethodName.AddChild, powerUp);
    powerUp.CallDeferred(Node.MethodName.SetOwner, _fallingPowerUpsContainer);
    powerUp.Connect("OnPlayerHit", new Callable(this, nameof(OnPlayerHit)));
    _cooldownTimer.Start();
  }

  private void OnBrickBroken(string color, Vector2 position) {
    if (_cooldownTimer.TimeLeft < MathUtils.EPSILON && isActive) {
      if (GD.Randi() % ITEM_INV_PROBABILITY == 0) {
        var powerUp = GetRandomPowerup();
        CreatePowerup(powerUp, color, position);
      }
    }
  }

  private bool CheckIfCanAddPowerup(PackedScene hitNode) {
    foreach (var powerUp in _activePowerupNodes) {

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
    foreach (var el in _activePowerupNodes) {
      if (!el.IsStillRelevant()) {
        listToDelete.Insert(0, i);
      }
      i++;
    }
    foreach (var index in listToDelete) {
      _activePowerupNodes[index].QueueFree();
      _activePowerupNodes.RemoveAt(index);
    }
  }

  private void OnPlayerHit(Node2D powerUp, PackedScene hitNode) {
    RemoveIrrelevantPowerups();
    if (CheckIfCanAddPowerup(hitNode)) {
      var hit = hitNode.Instantiate<PowerUpScript>();
      _activePowerupNodes.Add(hit);
      hit.SetBrickBreakerNode(_brickBreakerNode);
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
