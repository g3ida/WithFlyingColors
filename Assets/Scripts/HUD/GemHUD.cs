using Godot;
using System;

[Tool]
public class GemHUD : Node2D
{
    private const string TextureCollectedPath = "res://Assets/Sprites/HUD/gem_hud_collected.png";
    private const string TextureEmptyPath = "res://Assets/Sprites/HUD/gem_hud.png";

    private Texture textureCollected;
    private Texture textureEmpty;

    private TextureRect textureRectNode;
    private AnimationPlayer textureRectAnimationNode;
    private TextureRect backgroundNode;
    private AnimationPlayer backgroundAnimationPlayerNode;

    [Export]
    public string Color { get; set; }

    public enum State { EMPTY, COLLECTING, COLLECTED }
    public State currentState = State.EMPTY;

    public Godot.Collections.Dictionary<string, object> save_data = new Godot.Collections.Dictionary<string, object>
    {
        { "state", State.EMPTY }
    };

    private AnimatedSprite animation;
    private SlideAnimation collectedAnimation;

    public override void _Ready()
    {
        textureRectNode = GetNode<TextureRect>("TextureRect");
        textureRectAnimationNode = GetNode<AnimationPlayer>("TextureRect/AnimationPlayer");
        backgroundNode = GetNode<TextureRect>("Background");
        backgroundAnimationPlayerNode = GetNode<AnimationPlayer>("Background/AnimationPlayer");

        textureCollected = GD.Load<Texture>(TextureCollectedPath);
        textureEmpty = GD.Load<Texture>(TextureEmptyPath);

        textureRectNode.Texture = textureEmpty;
        backgroundNode.Visible = false;
        var colorIndex = ColorUtils.GetGroupColorIndex(Color);
        textureRectNode.Modulate = ColorUtils.GetBasicColor(colorIndex);
    }

    private void ConnectSignals()
    {
        if (!Engine.EditorHint)
        {
            Event.GdInstance().Connect("gem_collected", this, nameof(OnGemCollected));
            Event.GdInstance().Connect("checkpoint_reached", this, nameof(OnCheckpointHit));
            Event.GdInstance().Connect("checkpoint_loaded", this, nameof(reset));
        }
    }

    private void DisconnectSignals()
    {
        if (!Engine.EditorHint)
        {
            Event.GdInstance().Disconnect("gem_collected", this, nameof(OnGemCollected));
            Event.GdInstance().Disconnect("checkpoint_reached", this, nameof(OnCheckpointHit));
            Event.GdInstance().Disconnect("checkpoint_loaded", this, nameof(reset));
        }
    }

    private void OnGemCollected(string col, Vector2 position, SpriteFrames frames)
    {
        if (this.Color == col)
        {
            currentState = State.COLLECTING;
            animation = new AnimatedSprite();
            animation.Frames = frames;
            animation.Play();
            animation.Modulate = textureRectNode.Modulate;
            AddChild(animation);
            animation.Owner = this;

            animation.GlobalPosition = position;
            collectedAnimation = new SlideAnimation();
            collectedAnimation.Set("gem_slide", animation, new Vector2(20, 20), 1);
            Event.GdInstance().Connect("slide_animation_ended", this, nameof(OnSlideAnimEnded), flags: (uint)ConnectFlags.Oneshot);
        }
    }

    private void OnSlideAnimEnded(string animName)
    {
        if (animName == "gem_slide")
        {
            if (animation != null)
            {
                RemoveChild(animation);
            }

            if (currentState == State.COLLECTING)
            {
                textureRectNode.Texture = textureCollected;
                textureRectAnimationNode.Play("coin_collected_HUD");
                backgroundNode.Visible = true;
                backgroundAnimationPlayerNode.Play("coin_collected_HUD");
                currentState = State.COLLECTED;
            }

            collectedAnimation = null;
        }
    }

    public override void _EnterTree()
    {
        ConnectSignals();
    }

    public override void _ExitTree()
    {
        DisconnectSignals();
    }

    public override void _Process(float delta)
    {
        collectedAnimation?.Update(delta);
    }

    public void reset()
    {
        if ((State)Helpers.ParseSaveDataInt(save_data, "state") == State.EMPTY)
        {
            textureRectNode.Texture = textureEmpty;
            backgroundNode.Visible = false;
        }
        else
        {
            textureRectNode.Texture = textureCollected;
            backgroundNode.Visible = true;
        }
    }

    private void OnCheckpointHit(object checkpoint)
    {
        save_data["state"] = (int)(currentState != State.COLLECTING ? currentState : State.EMPTY);
    }

    public Godot.Collections.Dictionary<string, object> save()
    {
        return save_data;
    }
}
