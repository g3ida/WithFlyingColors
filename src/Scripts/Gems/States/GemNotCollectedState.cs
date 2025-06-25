using System;
using Godot;
using Wfc.State;

public partial class GemNotCollectedState : GemBaseState {
  private const float AMPLITUDE = 4.0f;
  private const float ANIMATION_DURATION = 4.0f;
  private const float SHINE_VARIANCE = 0.08f;
  private const float ROTATION_SPEED = 0.002f;

  private bool isActive = false;

  private NodeOscillator oscillator;

  public GemNotCollectedState() : base() { }

  public override void Init(Gem gem) {
    base.Init(gem);
    oscillator = new NodeOscillator();
    oscillator.Set(gem, AMPLITUDE, ANIMATION_DURATION);
  }

  public override void Enter(Gem gem) {
    isActive = true;
    gem.AnimationPlayerNode.Play("RESET");
    gem.ShineNode.Play();

  }

  public override void Exit(Gem gem) {
    isActive = false;
    gem.ShineNode.Stop();
  }

  public override BaseState<Gem>? PhysicsUpdate(Gem gem, float delta) {
    gem.LightNode.Position = gem.AnimatedSpriteNode.Position;
    oscillator.Update(delta);
    gem.LightNode.Energy = 1 + SHINE_VARIANCE * (float)Math.Sin(2 * Mathf.Pi * oscillator.Timer / ANIMATION_DURATION);
    gem.LightNode.Rotate(ROTATION_SPEED);
    return null;
  }

  public override BaseState<Gem>? OnCollisionWithBody(Gem gem, Area2D area) {
    if (!isActive)
      return null;
    // FIXME: This would be better implemented on the player's side
    if (Global.Instance().Player.IsDying())
      return null;
    if (area.IsInGroup(gem.group_name)) {
      isActive = false;
      return gem.StatesStore.Collecting;
    }
    return null;
  }
}
