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
  private const float FLIGHT_DURATION = 0.8f;
  // The gem leaves the level bigger than the slot it is heading for and tightens down
  // into it on the way.
  private const float FLIGHT_START_SCALE = 1.15f;
  private const float FLIGHT_END_SCALE = 0.55f;

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
      _animation.Scale = Vector2.One * FLIGHT_START_SCALE;
      _playFlightTween(_animation);
      _collectedAnimation = new SlideAnimation("gem_slide", _animation, new Vector2(20, 20), FLIGHT_DURATION);
      _collectedAnimation.SetOnAnimationEndedCallback(this.OnSlideAnimEnded);
    }
  }

  // The gem tightens up as it homes in on its slot, so the flight ends on the pose the
  // slot pops out of rather than on a jump in size. A single turn on the way in, landing
  // upright.
  private static void _playFlightTween(AnimatedSprite2D gem) {
    var tween = gem.CreateTween();
    tween.SetParallel(true);
    tween.TweenProperty(gem, "scale", Vector2.One * FLIGHT_END_SCALE, FLIGHT_DURATION)
      .SetTrans(Tween.TransitionType.Cubic)
      .SetEase(Tween.EaseType.In);
    tween.TweenProperty(gem, "rotation", Mathf.Tau, FLIGHT_DURATION)
      .SetTrans(Tween.TransitionType.Sine)
      .SetEase(Tween.EaseType.InOut);
  }

  private void OnSlideAnimEnded() {
    if (_animation != null) {
      RemoveChild(_animation);
      _animation.QueueFree();
      _animation = null;
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

  // The slot already holds this gem for the level being played, so the HUD opens with
  // it filled. Written into the saved state as well as the live one, or the first
  // death would hand it back.
  public void MarkAlreadyCollected() {
    _saveData = new SaveData(State.Collected);
    Reset();
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

  private void OnCheckpointHit(Vector2 _position, string _colorGroup) {
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
