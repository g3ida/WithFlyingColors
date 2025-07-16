namespace Wfc.Entities.World.Player;

using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.State;
using Wfc.Utils;
using static Wfc.Core.Event.EventHandler;

public abstract partial class PlayerBaseState : GodotObject, IState<Player> {
  const float GRAVITY = 9.8f * Constants.WORLD_TO_SCREEN;
  const float FALL_FACTOR = 2.5f;

  private EntityType _deathCollisionEntityType = EntityType.None;
  protected IInputManager inputManager;
  protected IPlayerStatesStore statesStore;
  protected bool playerMoved = false;

  public PlayerBaseState(IPlayerStatesStore statesStore, IInputManager inputManager) {
    this.inputManager = inputManager;
    this.statesStore = statesStore;
  }

  public void Enter(Player player) {
    _deathCollisionEntityType = EntityType.None;
    player.ScaleCornersBy(player.CurrentDefaultCornerScaleFactor);
    EventHandler.Instance.Events.PlayerDying += _onPlayerDying;
    playerMoved = false;
    _Enter(player);
  }

  public void Exit(Player player) {
    EventHandler.Instance.Events.PlayerDying -= _onPlayerDying;
    _Exit(player);
  }

  protected virtual void _Enter(Player player) { }
  protected virtual void _Exit(Player player) { }

  protected bool DashActionPressed(Player player) {
    return inputManager.IsJustPressed(IInputManager.Action.Dash) && player.CanDash && !player.HandleInputIsDisabled;
  }

  public virtual IState<Player>? PhysicsUpdate(Player player, float delta) {
    if (_deathCollisionEntityType != EntityType.None) {
      return _handlePlayerDying(player);
    }
    if (!player.IsDying()) {
      if (DashActionPressed(player)) {
        return OnDash(player);
      }
      if (!player.HandleInputIsDisabled) {
        if (!player.IsDashing()) {
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

  private void _onPlayerDying(Node? area, Vector2 position, int entityType) {
    _deathCollisionEntityType = (EntityType)entityType;
  }

  private PlayerBaseState? _handlePlayerDying(Player player) {
    if (_deathCollisionEntityType == EntityType.FallZone) {
      return statesStore.GetState<PlayerFallZoneDyingState>();
    }
    else {
      player.Velocity = Vector2.Zero;
      return statesStore.GetState<PlayerExplosionState>();
    }
  }

  public PlayerBaseState? OnLand(Player player) {
    player.CurrentAnimation = player.ScaleAnimation;
    if (!player.CurrentAnimation.IsRunning()) {
      player.CurrentAnimation.Start();
    }
    return null;
  }

  protected IState<Player>? OnDash(Player player) {
    var dashingState = statesStore.GetState<PlayerDashingState>();
    return dashingState;
  }

  protected IState<Player>? OnJump(Player player) {
    var jumpState = statesStore.GetState<PlayerJumpingState>();
    return jumpState;
  }

  protected bool JumpPressed(Player player) {
    if (player.HandleInputIsDisabled)
      return false;
    return inputManager.IsJustPressed(IInputManager.Action.Jump);
  }
}
