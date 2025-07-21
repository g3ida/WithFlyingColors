namespace Wfc.Entities.HUD;

using System;
using System.Collections.Generic;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Animation;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

[Tool]
[ScenePath]
public partial class GemHUD : Node2D, IPersistent {
  private const string TEXTURE_COLLECTED_PATH = "res://Assets/Sprites/HUD/gem_hud_collected.png";
  private const string TEXTURE_EMPTY_PATH = "res://Assets/Sprites/HUD/gem_hud.png";

  #region Exports
  [Export]
  public string Color { get; set; } = "blue";
  #endregion Exports

  private Texture2D _textureCollected = GD.Load<Texture2D>(TEXTURE_COLLECTED_PATH);
  private Texture2D _textureEmpty = GD.Load<Texture2D>(TEXTURE_EMPTY_PATH);

  #region Nodes
  [NodePath("TextureRect")]
  private TextureRect _textureRectNode = default!;
  [NodePath("TextureRect/AnimationPlayer")]
  private AnimationPlayer _textureRectAnimationNode = default!;
  [NodePath("Background")]
  private TextureRect _backgroundNode = null!;
  [NodePath("Background/AnimationPlayer")]
  private AnimationPlayer _backgroundAnimationPlayerNode = default!;
  #endregion Nodes

  public enum State { Empty, Collecting, Collected }
  public State currentState = State.Empty;
  private sealed record SaveData(State savedState = State.Empty);
  private SaveData _saveData = new SaveData();

  private AnimatedSprite2D? _animation = null;
  private SlideAnimation? _collectedAnimation = null!;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();

    _textureRectNode.Texture = _textureEmpty;
    _backgroundNode.Visible = false;
    var color = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(Color),
      SkinColorIntensity.Basic
    );
    _textureRectNode.Modulate = color;
  }

  private void ConnectSignals() {
    if (!Engine.IsEditorHint()) {
      EventHandler.Instance.Events.GemCollected += OnGemCollected;
      EventHandler.Instance.Events.CheckpointReached += OnCheckpointHit;
      EventHandler.Instance.Events.CheckpointLoaded += Reset;
    }
  }

  private void DisconnectSignals() {
    if (!Engine.IsEditorHint()) {
      EventHandler.Instance.Events.GemCollected -= OnGemCollected;
      EventHandler.Instance.Events.CheckpointReached -= OnCheckpointHit;
      EventHandler.Instance.Events.CheckpointLoaded -= Reset;
    }
  }

  private void OnGemCollected(string col, Vector2 position, SpriteFrames frames) {
    if (Color == col) {
      currentState = State.Collecting;
      _animation = new AnimatedSprite2D {
        SpriteFrames = frames
      };
      _animation.Play();
      _animation.Modulate = _textureRectNode.Modulate;
      AddChild(_animation);
      _animation.Owner = this;

      _animation.GlobalPosition = position;
      _collectedAnimation = new SlideAnimation("gem_slide", _animation, new Vector2(20, 20), 1);
      _collectedAnimation.SetOnAnimationEndedCallback(this.OnSlideAnimEnded);
    }
  }

  private void OnSlideAnimEnded() {
    if (_animation != null) {
      RemoveChild(_animation);
    }
    if (currentState == State.Collecting) {
      _textureRectNode.Texture = _textureCollected;
      _textureRectAnimationNode.Play("coin_collected_HUD");
      _backgroundNode.Visible = true;
      _backgroundAnimationPlayerNode.Play("coin_collected_HUD");
      currentState = State.Collected;
    }
    _collectedAnimation = null;
  }

  public override void _EnterTree() {
    base._EnterTree();
    ConnectSignals();
  }

  public override void _ExitTree() {
    base._ExitTree();
    DisconnectSignals();
  }

  public override void _Process(double delta) {
    base._Process(delta);
    _collectedAnimation?.Update((float)delta);
  }

  public void Reset() {
    currentState = _saveData.savedState;
    if (currentState == State.Empty) {
      _textureRectNode.Texture = _textureEmpty;
      _backgroundNode.Visible = false;
    }
    else {
      _textureRectNode.Texture = _textureCollected;
      _backgroundNode.Visible = true;
    }
  }

  private void OnCheckpointHit(Node checkpoint) {
    var state = currentState == State.Empty ? State.Empty : State.Collected;
    _saveData = new SaveData(state);
  }

  public string GetSaveId() => this.GetPath();
  public string Save(ISerializer serializer) => serializer.Serialize(_saveData);
  public void Load(ISerializer serializer, string data) {
    var deserializedData = serializer.Deserialize<SaveData>(data);
    this._saveData = deserializedData ?? new SaveData();
    Reset();
  }
}
