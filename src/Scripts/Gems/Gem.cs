using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Wfc.Core.Event;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class Gem : Area2D {
  [Export]
  public string group_name;

  public PointLight2D LightNode;
  public AudioStreamPlayer2D ShineNode;

  public GemStatesStore StatesStore;
  public GemBaseState CurrentState;

  public GemStatesEnum GemState {
    get {
      return StatesStore.GetStateEnum(CurrentState);
    }
  }

  public CollisionPolygon2D CollisionShapeNode;

  public AnimatedSprite2D AnimatedSpriteNode;

  public AnimationPlayer AnimationPlayerNode;

  private Dictionary<string, object> save_data = new Dictionary<string, object> { { "state", null } };

  public override void _Ready() {
    CollisionShapeNode = GetNode<CollisionPolygon2D>("CollisionShape2D");
    LightNode = GetNode<PointLight2D>("PointLight2D");
    ShineNode = GetNode<AudioStreamPlayer2D>("ShineSfx");
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
    save_data["state"] = (int)StatesStore.GetStateEnum(CurrentState);
    CurrentState.Enter(this);
  }

  private void SwitchState(GemBaseState newState) {
    if (newState != null) {
      CurrentState.Exit(this);
      CurrentState = newState;
      CurrentState.Enter(this);
    }
  }

  public override void _PhysicsProcess(double delta) {
    GemBaseState state = (GemBaseState)CurrentState.PhysicsUpdate(this, (float)delta);
    SwitchState(state);
  }

  public void _on_Gem_area_entered(Area2D area) {
    if (Global.Instance().Player.IsDying() || CurrentState != StatesStore.NotCollected) {
      return;
    }

    var state = (GemBaseState)CurrentState.OnCollisionWithBody(this, area);
    SwitchState(state);
  }

  public void _on_AnimationPlayer_animation_finished(string animName) {
    var state = (GemBaseState)CurrentState.OnAnimationFinished(this, animName);
    SwitchState(state);
  }

  private void ConnectSignals() {
    EventHandler.Instance.Connect(EventType.CheckpointReached, new Callable(this, nameof(_OnCheckpointHit)));
    EventHandler.Instance.Connect(EventType.CheckpointLoaded, new Callable(this, nameof(reset)));
  }

  private void DisconnectSignals() {
    EventHandler.Instance.Disconnect(EventType.CheckpointReached, new Callable(this, nameof(_OnCheckpointHit)));
    EventHandler.Instance.Disconnect(EventType.CheckpointLoaded, new Callable(this, nameof(reset)));
  }

  public override void _EnterTree() {
    ConnectSignals();
  }

  public override void _ExitTree() {
    DisconnectSignals();
  }

  private void _OnCheckpointHit(Node checkpoint) {
    GemBaseState savedState = CurrentState != StatesStore.Collecting ? CurrentState : StatesStore.Collected;
    save_data["state"] = (int)StatesStore.GetStateEnum(savedState);
  }

  public Dictionary<string, object> save() {
    return save_data;
  }

  public void reset() {
    var state = (GemStatesEnum)Helpers.ParseSaveDataInt(save_data, "state");
    SwitchState((GemBaseState)StatesStore.GetState(state));
  }

  // FIXME: This does not override IsInGroup(StringName grp)
  public bool IsInGroup(string grp) {
    // if the player is dying we don't want to collect it
    if (Global.Instance().Player.IsDying()) {
      return false;
    }
    // if the gem is already collecting we don't wan't the player to die
    if (CurrentState == StatesStore.Collecting) {
      if (Constants.COLOR_GROUPS.Contains(grp)) {
        return true;
      }
    }
    // return super method
    return base.IsInGroup(grp);
  }

  public void load(Dictionary<string, object> save_data) {
    this.save_data = save_data;
    reset();
  }
}
