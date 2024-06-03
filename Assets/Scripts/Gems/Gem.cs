using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class Gem : Area2D, IPersistant
{
    [Export]
    public string group_name;

    public Light2D LightNode;
    public AudioStreamPlayer2D ShineNode;

    public GemStatesStore StatesStore;
    public GemBaseState CurrentState;

    public GemStatesEnum GemState {
        get {
            return StatesStore.GetStateEnum(CurrentState);
        }
    }

    public CollisionPolygon2D CollisionShapeNode;

    public AnimatedSprite AnimatedSpriteNode;

    public AnimationPlayer AnimationPlayerNode;

    private Dictionary<string, object> save_data = new Dictionary<string, object> { { "state", null } };

    public override void _Ready()
    {
        CollisionShapeNode = GetNode<CollisionPolygon2D>("CollisionShape2D");
        LightNode = GetNode<Light2D>("Light2D");
        ShineNode = GetNode<AudioStreamPlayer2D>("ShineSfx");
        AnimatedSpriteNode = GetNode<AnimatedSprite>("AnimatedSprite");
        AnimationPlayerNode = GetNode<AnimationPlayer>("AnimatedSprite/AnimationPlayer");

        AddToGroup(group_name);
        int colorIndex = ColorUtils.GetGroupColorIndex(group_name);
        Color color = ColorUtils.GetLight2Color(colorIndex);
        LightNode.Color = ColorUtils.GetBasicColor(colorIndex);
        GetNode<AnimatedSprite>("AnimatedSprite").Modulate = color;

        StatesStore = new GemStatesStore();
        StatesStore.Init(this);

        CurrentState = StatesStore.NotCollected;
        save_data["state"] = (int)StatesStore.GetStateEnum(CurrentState);
        CurrentState.Enter(this);
    }

    private void SwitchState(GemBaseState newState)
    {
        if (newState != null)
        {
            CurrentState.Exit(this);
            CurrentState = newState;
            CurrentState.Enter(this);
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        GemBaseState state = (GemBaseState)CurrentState.PhysicsUpdate(this, delta);
        SwitchState(state);
    }

    public void _on_Gem_area_entered(Area2D area)
    {
        if (Global.Instance().Player.IsDying() || CurrentState != StatesStore.NotCollected)
        {
            return;
        }

        GemBaseState state = (GemBaseState)CurrentState.OnCollisionWithBody(this, area);
        SwitchState(state);
    }

    public void _on_AnimationPlayer_animation_finished(string animName)
    {
        GemBaseState state = (GemBaseState)CurrentState.OnAnimationFinished(this, animName);
        SwitchState(state);
    }

    private void ConnectSignals()
    {
        Event.Instance().Connect("checkpoint_reached", this, nameof(_OnCheckpointHit));
        Event.Instance().Connect("checkpoint_loaded", this, nameof(reset));
    }

    private void DisconnectSignals()
    {
        Event.Instance().Disconnect("checkpoint_reached", this, nameof(_OnCheckpointHit));
        Event.Instance().Disconnect("checkpoint_loaded", this, nameof(reset));
    }

    public override void _EnterTree()
    {
        ConnectSignals();
    }

    public override void _ExitTree()
    {
        DisconnectSignals();
    }

    private void _OnCheckpointHit(Node checkpoint)
    {
        GemBaseState savedState = CurrentState != StatesStore.Collecting ? CurrentState : StatesStore.Collected;
        save_data["state"] = (int)StatesStore.GetStateEnum(savedState);
    }

    public Dictionary<string, object> save()
    {
        return save_data;
    }

    public void reset()
    {
        var state = (GemStatesEnum)Helpers.ParseSaveDataInt(save_data, "state");
        SwitchState((GemBaseState)StatesStore.GetState(state));
    }

    public new bool IsInGroup(string grp)
    {
        // if the player is dying we don't want to collect it
        if (Global.Instance().Player.IsDying())
        {
            return false;
        }
        // if the gem is already collecting we don't wan't the player to die
        if (CurrentState == StatesStore.Collecting)
        {
            if (Constants.COLOR_GROUPS.Contains(grp))
            {
                return true;
            }
        }
        // return super method
        return base.IsInGroup(grp);
    }

    public void load(Dictionary<string, object> save_data)
    {
        this.save_data = save_data;
        reset();
    }
}
