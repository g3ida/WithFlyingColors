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
  // How much of a ghost is left to see. Enough to read the shape and its color, not
  // enough to be mistaken for something still worth crossing the room for.
  private const float GHOST_ALPHA = 0.3f;
  private const float GHOST_LIGHT_SCALE = 0.25f;

  [Export]
  public string GroupName = "blue";

  // This gem's color was banked for this level on an earlier run, so the level owes
  // the player nothing for it. It still stands there as a ghost of itself, and any
  // face may walk through it.
  public bool IsAlreadyCollected { get; private set; }

  // What the shine is worth on this gem, for the state that animates it.
  public float LightEnergyScale => IsAlreadyCollected ? GHOST_LIGHT_SCALE : 1f;

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
    LightNode.Color = color;
    _applyAppearance();

    _statesStore = new GemStatesStore(this);
    _currentState = _statesStore.GetState<GemNotCollectedState>();
    _currentState?.Enter(this);
  }

  // Called by the level once it knows what the slot has already banked. The gem is
  // built long before that, so the appearance is applied rather than chosen once.
  public void MarkAlreadyCollected() {
    IsAlreadyCollected = true;
    _applyAppearance();
  }

  private void _applyAppearance() {
    var lightColor = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(GroupName),
      SkinColorIntensity.VeryLight
    );
    AnimatedSpriteNode.Modulate = IsAlreadyCollected
      ? new Color(lightColor, GHOST_ALPHA)
      : lightColor;
    LightNode.Energy = LightEnergyScale;
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

  // A gem in the middle of its collection animation counts as collected: the pickup is
  // already committed and GemCollectingState only exists until the animation ends. The
  // flag used to be written inverted, which turned every uncollected gem into a collected
  // one on the next death - hidden, uncollectable, and missing from the HUD's set.
  private void _OnCheckpointHit(Vector2 _position, string _colorGroup) {
    _saveData = new SaveData(_currentState is GemCollectedState or GemCollectingState);
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
