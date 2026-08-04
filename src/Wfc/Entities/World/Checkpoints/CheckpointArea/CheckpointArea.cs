namespace Wfc.Entities.World.Checkpoints;

using System.Collections.Generic;
using Godot;
using Wfc.Core.Localization;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class CheckpointArea : Area2D, IPersistent {
  // How far below the fire the trigger's floor sits, so a cube standing on the same ground is
  // already inside it.
  private const float TRIGGER_FOOT = 30.0f;

  #region Nodes
  [NodePath("Campfire")]
  private Campfire _campfireNode = default!;
  [NodePath("CollisionShape2D")]
  private CollisionShape2D _collisionShapeNode = default!;
  #endregion Nodes

  [Signal]
  public delegate void checkpoint_hitEventHandler();

  [Export]
  public string ColorGroup { get; set; } = "blue";

  // The zone stands on the fire rather than being centred on the checkpoint, so every way in
  // crosses it before the flame: wide enough that the fire is already going out by the time the
  // cube reaches it, tall enough that jumping the fire still takes the checkpoint.
  [Export]
  public Vector2 TriggerSize { get; set; } = new(320.0f, 520.0f);

  private bool _isChecked = false;

  // Read by the orchestrator to work out how far through a level the player is - the count of
  // checkpoints passed against the count in the scene is the only progress measure the level
  // data supports.
  public bool IsChecked => _isChecked;

  private sealed record SaveData(bool isChecked = false);
  private SaveData _saveData = new SaveData();

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    if (string.IsNullOrEmpty(ColorGroup)) {
      GD.PushError("ColorGroup cannot be null or empty");
    }
    _campfireNode.Settle(ColorGroup, _isChecked);
  }

  // The fire cannot look for the floor it stands on until the physics world has been stepped once,
  // and the trigger is not placed until it knows where the fire ended up.
  public override void _PhysicsProcess(double delta) {
    SetPhysicsProcess(false);
    var ground = _campfireNode.SettleOnGround();
    if (_collisionShapeNode.Shape is RectangleShape2D box) {
      box.Size = TriggerSize;
      _collisionShapeNode.Position = new Vector2(0.0f, ground + TRIGGER_FOOT - (TriggerSize.Y * 0.5f));
    }
  }

  public void Reset() {
    _isChecked = _saveData.isChecked;
    // The save can be loaded before the scene has finished coming up, and _Ready settles the
    // campfire again from the same flag once there is a campfire to settle.
    if (IsNodeReady()) {
      _campfireNode.Settle(ColorGroup, _isChecked);
    }
  }

  public void OnCheckpointAreaBodyEntered(Node2D body) {
    if (body is Player.Player && !_isChecked) {
      _isChecked = true;
      _saveData = new SaveData(isChecked: true);
      _campfireNode.Snuff(body.GlobalPosition);
      EmitSignal(nameof(checkpoint_hit));
      EventHandler.Instance.EmitCheckpointReached(GlobalPosition, ColorGroup);
      EventHandler.Instance.EmitNotificationRaised(TranslationKey.game_notification_checkpointReached);
    }
  }

  public string GetSaveId() => this.GetPath();
  public string Save(ISerializer serializer) => serializer.Serialize(_saveData);
  public void Load(ISerializer serializer, string data) {
    var deserializedData = serializer.Deserialize<SaveData>(data);
    this._saveData = deserializedData ?? new SaveData();
    Reset();
  }
}
