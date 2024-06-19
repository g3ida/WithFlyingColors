using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class GemHUD : Node2D, IPersistent {
  private const string TextureCollectedPath = "res://Assets/Sprites/HUD/gem_hud_collected.png";
  private const string TextureEmptyPath = "res://Assets/Sprites/HUD/gem_hud.png";

  private Texture2D textureCollected;
  private Texture2D textureEmpty;

  private TextureRect textureRectNode;
  private AnimationPlayer textureRectAnimationNode;
  private TextureRect backgroundNode;
  private AnimationPlayer backgroundAnimationPlayerNode;

  [Export]
  public string Color { get; set; }

  public enum State { EMPTY, COLLECTING, COLLECTED }
  public State currentState = State.EMPTY;

  public Dictionary<string, object> save_data = new Dictionary<string, object>
    {
        { "state", State.EMPTY }
    };

  private AnimatedSprite2D animation;
  private SlideAnimation collectedAnimation;

  public override void _Ready() {
    textureRectNode = GetNode<TextureRect>("TextureRect");
    textureRectAnimationNode = GetNode<AnimationPlayer>("TextureRect/AnimationPlayer");
    backgroundNode = GetNode<TextureRect>("Background");
    backgroundAnimationPlayerNode = GetNode<AnimationPlayer>("Background/AnimationPlayer");

    textureCollected = GD.Load<Texture2D>(TextureCollectedPath);
    textureEmpty = GD.Load<Texture2D>(TextureEmptyPath);

    textureRectNode.Texture = textureEmpty;
    backgroundNode.Visible = false;
    var colorIndex = ColorUtils.GetGroupColorIndex(Color);
    textureRectNode.Modulate = ColorUtils.GetBasicColor(colorIndex);
  }

  private void ConnectSignals() {
    if (!Engine.IsEditorHint()) {
      Event.Instance.Connect("gem_collected", new Callable(this, nameof(OnGemCollected)));
      Event.Instance.Connect("checkpoint_reached", new Callable(this, nameof(OnCheckpointHit)));
      Event.Instance.Connect("checkpoint_loaded", new Callable(this, nameof(reset)));
    }
  }

  private void DisconnectSignals() {
    if (!Engine.IsEditorHint()) {
      Event.Instance.Disconnect("gem_collected", new Callable(this, nameof(OnGemCollected)));
      Event.Instance.Disconnect("checkpoint_reached", new Callable(this, nameof(OnCheckpointHit)));
      Event.Instance.Disconnect("checkpoint_loaded", new Callable(this, nameof(reset)));
    }
  }

  private void OnGemCollected(string col, Vector2 position, SpriteFrames frames) {
    if (this.Color == col) {
      currentState = State.COLLECTING;
      animation = new AnimatedSprite2D();
      animation.SpriteFrames = frames;
      animation.Play();
      animation.Modulate = textureRectNode.Modulate;
      AddChild(animation);
      animation.Owner = this;

      animation.GlobalPosition = position;
      collectedAnimation = new SlideAnimation();
      collectedAnimation.Set("gem_slide", animation, new Vector2(20, 20), 1);
      Event.Instance.Connect("slide_animation_ended", new Callable(this, nameof(OnSlideAnimEnded)), flags: (uint)ConnectFlags.OneShot);
    }
  }

  private void OnSlideAnimEnded(string animName) {
    if (animName == "gem_slide") {
      if (animation != null) {
        RemoveChild(animation);
      }

      if (currentState == State.COLLECTING) {
        textureRectNode.Texture = textureCollected;
        textureRectAnimationNode.Play("coin_collected_HUD");
        backgroundNode.Visible = true;
        backgroundAnimationPlayerNode.Play("coin_collected_HUD");
        currentState = State.COLLECTED;
      }

      collectedAnimation = null;
    }
  }

  public override void _EnterTree() {
    ConnectSignals();
  }

  public override void _ExitTree() {
    DisconnectSignals();
  }

  public override void _Process(double delta) {
    collectedAnimation?.Update((float)delta);
  }

  public void reset() {
    if ((State)Helpers.ParseSaveDataInt(save_data, "state") == State.EMPTY) {
      textureRectNode.Texture = textureEmpty;
      backgroundNode.Visible = false;
    }
    else {
      textureRectNode.Texture = textureCollected;
      backgroundNode.Visible = true;
    }
  }

  private void OnCheckpointHit(Node checkpoint) {
    save_data["state"] = (int)(currentState != State.COLLECTING ? currentState : State.EMPTY);
  }

  public Dictionary<string, object> save() {
    return save_data;
  }

  public void load(Dictionary<string, object> save_data) {
    this.save_data = save_data;
    reset();
  }
}
