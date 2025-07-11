namespace Wfc.Entities.World.Gems;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

[ScenePath]
public partial class Gem : Area2D, IPersistent {
  [Export]
  public string group_name;

  [NodePath("PointLight2D")]
  public PointLight2D LightNode = null!;

  [NodePath("ShineSfx")]
  public AudioStreamPlayer2D ShineSfxNode = null!;

  public GemStatesStore StatesStore = new GemStatesStore();
  public GemBaseState CurrentState = null!;

  public GemState GemState {
    get {
      return StatesStore.GetStateEnum(CurrentState);
    }
  }

  [NodePath("CollisionShape2D")]
  public CollisionPolygon2D CollisionShapeNode = null!;
  [NodePath("AnimatedSprite2D")]
  public AnimatedSprite2D AnimatedSpriteNode = null!;
  [NodePath("AnimatedSprite2D/AnimationPlayer")]
  public AnimationPlayer AnimationPlayerNode = null!;

  public record SaveData(GemState currentState = GemState.NotCollected);
  private SaveData _saveData = null!;

  public override void _Ready() {
    this.WireNodes();
    CollisionShapeNode = GetNode<CollisionPolygon2D>("CollisionShape2D");
    LightNode = GetNode<PointLight2D>("PointLight2D");
    ShineSfxNode = GetNode<AudioStreamPlayer2D>("ShineSfx");
    AnimatedSpriteNode = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
    AnimationPlayerNode = GetNode<AnimationPlayer>("AnimatedSprite2D/AnimationPlayer");

    AddToGroup(group_name);
    int colorIndex = ColorUtils.GetGroupColorIndex(group_name);
    Color color = ColorUtils.GetLight2Color(colorIndex);
    LightNode.Color = ColorUtils.GetBasicColor(colorIndex);
    GetNode<AnimatedSprite2D>("AnimatedSprite2D").Modulate = color;

    StatesStore = new GemStatesStore();
    StatesStore.Init(this);

    CurrentState = StatesStore.NotCollected;
    CurrentState.Enter(this);
    _saveData = new SaveData(GemState);
  }

  private void SwitchState(GemBaseState? newState) {
    if (newState != null) {
      CurrentState.Exit(this);
      CurrentState = newState;
      CurrentState.Enter(this);
    }
  }

  public override void _PhysicsProcess(double delta) {
    SwitchState((GemBaseState?)CurrentState.PhysicsUpdate(this, (float)delta));
  }

  private void ConnectSignals() {
    EventHandler.Instance.Connect(EventType.CheckpointReached, new Callable(this, nameof(_OnCheckpointHit)));
    EventHandler.Instance.Connect(EventType.CheckpointLoaded, new Callable(this, nameof(Reset)));
  }

  private void DisconnectSignals() {
    EventHandler.Instance.Disconnect(EventType.CheckpointReached, new Callable(this, nameof(_OnCheckpointHit)));
    EventHandler.Instance.Disconnect(EventType.CheckpointLoaded, new Callable(this, nameof(Reset)));
  }

  public override void _EnterTree() {
    ConnectSignals();
  }

  public override void _ExitTree() {
    DisconnectSignals();
  }

  private void _OnCheckpointHit(Node checkpoint) {
    GemBaseState savedState = CurrentState != StatesStore.Collecting ? CurrentState : StatesStore.Collected;
    _saveData = new SaveData(GemState);
  }

  public void Reset() {
    SwitchState((GemBaseState?)StatesStore.GetState(_saveData.currentState));
  }

  public string GetSaveId() => this.GetPath();

  public string Save(ISerializer serializer) => serializer.Serialize(this._saveData);

  public void Load(ISerializer serializer, string data) {
    var deserializedData = serializer.Deserialize<SaveData>(data);
    this._saveData = deserializedData ?? new SaveData();
    Reset();
  }
}
