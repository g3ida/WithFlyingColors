namespace Wfc.Entities.World.Gems;

using Chickensoft.Sync.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using Wfc.Skin;
using Wfc.State;
using Wfc.Utils.Attributes;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class Gem : Area2D, IPersistent {
  private AutoChannel.Binding? _checkpointBinding;

  public override void _Notification(int what) => this.Notify(what);

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

  // Settled the moment a face of this gem's color reaches it, a physics frame before the state
  // that plays the pickup runs. Whatever else lands on the gem inside that frame reads this
  // rather than the state, which has not moved yet.
  public bool IsBeingCollected { get; private set; }

  // What the shine is worth on this gem, for the state that animates it.
  public float LightEnergyScale => IsAlreadyCollected ? GHOST_LIGHT_SCALE : 1f;

  // The pale core the sprite is tinted with, and the deeper shade its light casts.
  public Color CoreColor => _skinColor(SkinColorIntensity.VeryLight);
  public Color ShineColor => _skinColor(SkinColorIntensity.Basic);

  #region Nodes
  [Node("PointLight2D")]
  public IPointLight2D LightNode { get; set; } = default!;
  [Node("ShineSfx")]
  public IAudioStreamPlayer2D ShineSfxNode { get; set; } = default!;
  [Node("CollisionShape2D")]
  public ICollisionPolygon2D CollisionShapeNode { get; set; } = default!;
  [Node("AnimatedSprite2D")]
  public IAnimatedSprite2D AnimatedSpriteNode { get; set; } = default!;
  [Node("AnimatedSprite2D/AnimationPlayer")]
  public IAnimationPlayer AnimationPlayerNode { get; set; } = default!;
  #endregion Nodes

  private GemStatesStore _statesStore = null!;
  private IState<Gem>? _currentState = null;

  public record SaveData(bool isGemCollected = false);
  private SaveData _saveData = new SaveData();

  public override void _Ready() {
    AddToGroup(GroupName);
    LightNode.Color = ShineColor;
    _applyAppearance();

    _statesStore = new GemStatesStore(this);
    _currentState = _statesStore.GetState<GemNotCollectedState>();
    _currentState?.Enter(this);
  }

  // The shape can only be taken away by a deferred call - the contact that took the gem is still
  // being flushed - so the flag stands in for it until the call lands.
  public void Take() {
    IsBeingCollected = true;
    CollisionShapeNode.SetDeferred(CollisionPolygon2D.PropertyName.Disabled, true);
  }

  // Called by the level once it knows what the slot has already banked. The gem is
  // built long before that, so the appearance is applied rather than chosen once.
  public void MarkAlreadyCollected() {
    IsAlreadyCollected = true;
    _applyAppearance();
  }

  private void _applyAppearance() {
    AnimatedSpriteNode.Modulate = IsAlreadyCollected
      ? new Color(CoreColor, GHOST_ALPHA)
      : CoreColor;
    LightNode.Energy = LightEnergyScale;
  }

  private Color _skinColor(SkinColorIntensity intensity) =>
    SkinManager.Instance.CurrentSkin.GetColor(GameSkin.ColorGroupToSkinColor(GroupName), intensity);

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
    _checkpointBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.CheckpointReached m) => _OnCheckpointHit(m.Position, m.ColorGroup))
      .On((in IGameEvents.CheckpointLoaded _) => Reset());
  }

  private void DisconnectSignals() {
    _checkpointBinding?.Dispose();
    _checkpointBinding = null;
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
      IsBeingCollected = false;
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
