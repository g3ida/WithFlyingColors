namespace Wfc.Entities.World.Player;

using System;
using Godot;
using Wfc.Core.Input;
using Wfc.State;

public abstract partial class PlayerBaseState : GodotObject, IState<Player> {
  public PlayerStatesEnum baseState;

  const float GRAVITY = 9.8f * Constants.WORLD_TO_SCREEN;
  const float FALL_FACTOR = 2.5f;

  protected IInputManager inputManager;
  protected IPlayerStatesStore statesStore;

  public PlayerBaseState(IPlayerStatesStore statesStore, IInputManager inputManager) {
    this.inputManager = inputManager;
    this.statesStore = statesStore;
  }

  public bool playerMoved = false;

  public void Enter(Player player) {
    player.ScaleCornersBy(player.CurrentDefaultCornerScaleFactor);
    playerMoved = false;
    _Enter(player);
  }

  public void Exit(Player player) {
    _Exit(player);
  }

  protected virtual void _Enter(Player player) { }
  protected virtual void _Exit(Player player) { }

  protected bool DashActionPressed(Player player) {
    return inputManager.IsJustPressed(IInputManager.Action.Dash) && player.CanDash && !player.HandleInputIsDisabled;
  }

  public virtual IState<Player>? PhysicsUpdate(Player player, float delta) {
    if (player.PlayerState != statesStore.GetState(PlayerStatesEnum.DYING)) {
      if (DashActionPressed(player)) {
        return OnDash(player);
      }

      if (!player.HandleInputIsDisabled) {
        if (player.PlayerState != statesStore.GetState(PlayerStatesEnum.DASHING)) {
          if (inputManager.IsPressed(IInputManager.Action.MoveRight)) {
            playerMoved = true;
            player.Velocity = new Vector2(Mathf.Clamp(player.Velocity.X + player.SpeedUnit, 0, player.SpeedLimit), player.Velocity.Y);

          }
          else if (inputManager.IsPressed(IInputManager.Action.MoveLeft)) {
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

  protected virtual IState<Player>? _PhysicsUpdate(Player player, float delta) { return null; }

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

    var dyingState = statesStore.GetState(PlayerStatesEnum.DYING) as PlayerDyingState;
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

  protected IState<Player>? OnDash(Player player) {
    var dashingState = statesStore.GetState(PlayerStatesEnum.DASHING);
    return dashingState;
  }

  protected IState<Player>? OnJump(Player player) {
    var jumpState = statesStore.GetState(PlayerStatesEnum.JUMPING);
    return jumpState;
  }

  protected bool JumpPressed(Player player) {
    if (player.HandleInputIsDisabled)
      return false;
    return inputManager.IsJustPressed(IInputManager.Action.Jump);
  }

  protected IState<Player>? HandleRotate(Player player) {
    if (player.PlayerState?.baseState != PlayerStatesEnum.DYING && !player.HandleInputIsDisabled) {
      if (inputManager.IsJustPressed(IInputManager.Action.RotateLeft)) {
        return statesStore.GetState(PlayerStatesEnum.ROTATING_LEFT);
      }
      if (inputManager.IsJustPressed(IInputManager.Action.RotateRight)) {
        return statesStore.GetState(PlayerStatesEnum.ROTATING_RIGHT);
      }
    }
    return null;
  }

  public void Init(Player o) { }
}
