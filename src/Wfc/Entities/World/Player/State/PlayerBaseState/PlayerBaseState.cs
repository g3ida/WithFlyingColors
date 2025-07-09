namespace Wfc.Entities.World.Player;

using System;
using Godot;
using Wfc.State;

public partial class PlayerBaseState : BaseState<Player> {
  public PlayerStatesEnum baseState;

  const float GRAVITY = 9.8f * Constants.WORLD_TO_SCREEN;
  const float FALL_FACTOR = 2.5f;

  public PlayerBaseState() { }

  public bool playerMoved = false;

  public sealed override void Enter(Player player) {
    player.ScaleCornersBy(player.CurrentDefaultCornerScaleFactor);
    playerMoved = false;
    _Enter(player);
  }

  public sealed override void Exit(Player player) {
    _Exit(player);
  }

  protected virtual void _Enter(Player player) { }
  protected virtual void _Exit(Player player) { }

  public sealed override void _Input(Player player, InputEvent ev) {
    Input(player, ev);
  }

  protected virtual void Input(Player player, InputEvent ev) { }

  protected bool DashActionPressed(Player player) {
    return Godot.Input.IsActionJustPressed("dash") && player.CanDash && !player.HandleInputIsDisabled;
  }

  public override BaseState<Player>? PhysicsUpdate(Player player, float delta) {
    if (player.PlayerState != player.StatesStore.GetState(PlayerStatesEnum.DYING)) {
      if (DashActionPressed(player)) {
        return OnDash(player);
      }

      if (!player.HandleInputIsDisabled) {
        if (player.PlayerState != player.StatesStore.GetState(PlayerStatesEnum.DASHING)) {
          if (Godot.Input.IsActionPressed("move_right")) {
            playerMoved = true;
            player.Velocity = new Vector2(Mathf.Clamp(player.Velocity.X + player.SpeedUnit, 0, player.SpeedLimit), player.Velocity.Y);

          }
          else if (Godot.Input.IsActionPressed("move_left")) {
            playerMoved = true;
            player.Velocity = new Vector2(Mathf.Clamp(player.Velocity.X - player.SpeedUnit, -player.SpeedLimit, 0), player.Velocity.Y);

          }
        }
      }

      player.Velocity = new Vector2(player.Velocity.X, player.Velocity.Y + GRAVITY * delta * FALL_FACTOR);
    }

    var newState = _PhysicsUpdate(player, delta);

    player.MoveAndSlide();
    player.Velocity = new Vector2(Mathf.Lerp(player.Velocity.X, 0, 0.25f), player.Velocity.Y);
    player.CurrentAnimation.Step(player, player.AnimatedSpriteNode, delta);

    return newState;
  }

  protected virtual BaseState<Player>? _PhysicsUpdate(Player player, float delta) { return null; }

  protected void SetPlayerDeathAnimationType(PlayerDyingState dyingState, Constants.EntityType entityType) {
    if (entityType == Constants.EntityType.FALL_ZONE) {
      dyingState.deathAnimationType = DeathAnimationType.Fall;
    }
    else {
      dyingState.deathAnimationType = DeathAnimationType.ExplosionReal;
    }
  }

  public PlayerBaseState? OnPlayerDying(Player player, Node? area, Vector2 position, Constants.EntityType entityType) {
    if (entityType != Constants.EntityType.FALL_ZONE) {
      player.Velocity = Vector2.Zero;
    }

    var dyingState = player.StatesStore.GetState(PlayerStatesEnum.DYING) as PlayerDyingState;
    if (dyingState != null) {
      SetPlayerDeathAnimationType(dyingState, entityType);
    }
    return dyingState;
  }

  public PlayerBaseState? OnLand(Player player) {
    player.CurrentAnimation = player.ScaleAnimation;
    if (!player.CurrentAnimation.IsRunning()) {
      player.CurrentAnimation.Start();
    }
    return null;
  }

  protected BaseState<Player>? OnDash(Player player) {
    var dashingState = player.StatesStore.GetState(PlayerStatesEnum.DASHING);
    return dashingState;
  }

  protected BaseState<Player>? OnJump(Player player) {
    var jumpState = player.StatesStore.GetState(PlayerStatesEnum.JUMPING);
    return jumpState;
  }

  protected bool JumpPressed(Player player) {
    if (player.HandleInputIsDisabled)
      return false;
    return Godot.Input.IsActionJustPressed("jump");
  }

  protected BaseState<Player>? HandleRotate(Player player) {
    if (player.PlayerState.baseState != PlayerStatesEnum.DYING && !player.HandleInputIsDisabled) {
      if (Godot.Input.IsActionJustPressed("rotate_left")) {
        return player.StatesStore.GetState(PlayerStatesEnum.ROTATING_LEFT);
      }
      if (Godot.Input.IsActionJustPressed("rotate_right")) {
        return player.StatesStore.GetState(PlayerStatesEnum.ROTATING_RIGHT);
      }
    }
    return null;
  }
}
