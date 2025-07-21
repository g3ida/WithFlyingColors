namespace Wfc.Entities.World.Gems;

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using Wfc.Skin;
using Wfc.State;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

[ScenePath]
public partial class Gem : Area2D, IPersistent {
  [Export]
  public string GroupName = "blue";

  [NodePath("PointLight2D")]
  public PointLight2D LightNode = null!;

  [NodePath("ShineSfx")]
  public AudioStreamPlayer2D ShineSfxNode = null!;

  private GemStatesStore _statesStore = null!;
  private IState<Gem>? _currentState = null;

  [NodePath("CollisionShape2D")]
  public CollisionPolygon2D CollisionShapeNode = null!;
  [NodePath("AnimatedSprite2D")]
  public AnimatedSprite2D AnimatedSpriteNode = null!;
  [NodePath("AnimatedSprite2D/AnimationPlayer")]
  public AnimationPlayer AnimationPlayerNode = null!;

  public record SaveData(bool isGemCollected = false);
  private SaveData _saveData = new SaveData();

  public override void _Ready() {
    this.WireNodes();
    CollisionShapeNode = GetNode<CollisionPolygon2D>("CollisionShape2D");
    LightNode = GetNode<PointLight2D>("PointLight2D");
    ShineSfxNode = GetNode<AudioStreamPlayer2D>("ShineSfx");
    AnimatedSpriteNode = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
    AnimationPlayerNode = GetNode<AnimationPlayer>("AnimatedSprite2D/AnimationPlayer");

    AddToGroup(GroupName);
    var color = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(GroupName),
      SkinColorIntensity.Basic
    );
    var lightColor = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(GroupName),
      SkinColorIntensity.VeryLight
    );
    LightNode.Color = color;
    GetNode<AnimatedSprite2D>("AnimatedSprite2D").Modulate = lightColor;

    _statesStore = new GemStatesStore(this);
    _currentState = _statesStore.GetState<GemNotCollectedState>();
    _currentState?.Enter(this);
  }

  private void SwitchState(IState<Gem>? newState) {
    if (newState != null) {
      _currentState?.Exit(this);
      _currentState = newState;
      _currentState?.Enter(this);
    }
  }

  public override void _PhysicsProcess(double delta) {
    SwitchState(_currentState?.PhysicsUpdate(this, (float)delta));
  }

  private void ConnectSignals() {
    EventHandler.Instance.Events.CheckpointReached += _OnCheckpointHit;
    EventHandler.Instance.Events.CheckpointLoaded += Reset;
  }

  private void DisconnectSignals() {
    EventHandler.Instance.Events.CheckpointReached -= _OnCheckpointHit;
    EventHandler.Instance.Events.CheckpointLoaded -= Reset;
  }

  public override void _EnterTree() {
    ConnectSignals();
  }

  public override void _ExitTree() {
    DisconnectSignals();
  }

  private void _OnCheckpointHit(Node checkpoint) {
    _saveData = new SaveData(_currentState! is GemNotCollectedState);
  }

  public void Reset() {
    if (_saveData.isGemCollected) {
      SwitchState(_statesStore.GetState<GemCollectedState>());
    }
    else {
      SwitchState(_statesStore.GetState<GemNotCollectedState>());
    }
  }

  public string GetSaveId() => this.GetPath();

  public string Save(ISerializer serializer) => serializer.Serialize(this._saveData);

  public void Load(ISerializer serializer, string data) {
    var deserializedData = serializer.Deserialize<SaveData>(data);
    this._saveData = deserializedData ?? new SaveData();
    Reset();
  }
}
