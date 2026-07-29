namespace Wfc.Entities.World.Player;

using Godot;
using Wfc.Core.Input;
using Wfc.State;
using Wfc.Utils;

public abstract partial class PlayerBaseState : GodotObject, IState<Player> {
  const float GRAVITY = 9.8f * Constants.WORLD_TO_SCREEN;
  const float FALL_FACTOR = 2.5f;
  const float RUN_DAMPING = 0.25f;

  protected IInputManager inputManager;
  protected IPlayerStatesStore statesStore;
  protected bool playerMoved = false;

  public PlayerBaseState(IPlayerStatesStore statesStore, IInputManager inputManager) {
    this.inputManager = inputManager;
    this.statesStore = statesStore;
  }

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
    var death = player.TakePendingDeath();
    if (death != EntityType.None) {
      return _dyingStateFor(death);
    }
    if (!player.IsDying()) {
      if (DashActionPressed(player)) {
        return OnDash(player);
      }
      _applyWalkInput(player);
      _applyGravity(player, delta);
    }

    var newState = _PhysicsUpdate(player, delta);
    _move(player, delta);

    return newState;
  }

  protected virtual IState<Player>? _PhysicsUpdate(Player player, float delta) { return null; }

  // Which way the cube dies. What dying then does to it belongs to the state named here.
  private PlayerBaseState? _dyingStateFor(EntityType death) =>
    death == EntityType.FallZone
      ? statesStore.GetState<PlayerFallZoneDyingState>()
      : statesStore.GetState<PlayerExplosionState>();

  // The two directions are exclusive, so holding both is holding neither. A dash owns the run
  // speed for as long as it lasts.
  private void _applyWalkInput(Player player) {
    if (player.HandleInputIsDisabled || player.IsDashing()) {
      return;
    }
    if (inputManager.IsPressed(IInputManager.Action.MoveRight)) {
      playerMoved = true;
      player.Velocity = new Vector2(Mathf.Clamp(player.Velocity.X + player.SpeedUnit, 0, player.SpeedLimit), player.Velocity.Y);
    }
    else if (inputManager.IsPressed(IInputManager.Action.MoveLeft)) {
      playerMoved = true;
      player.Velocity = new Vector2(Mathf.Clamp(player.Velocity.X - player.SpeedUnit, -player.SpeedLimit, 0), player.Velocity.Y);
    }
  }

  private static void _applyGravity(Player player, float delta) =>
    player.Velocity = new Vector2(player.Velocity.X, player.Velocity.Y + GRAVITY * delta * FALL_FACTOR);

  // The same three steps close out every state: travel, bleed off the run speed, and let the
  // squash and stretch follow the cube there.
  private static void _move(Player player, float delta) {
    player.MoveAndSlide();
    player.Velocity = new Vector2(Mathf.Lerp(player.Velocity.X, 0, RUN_DAMPING), player.Velocity.Y);
    player.CurrentAnimation.Step(player, player.AnimatedSpriteNode, delta);
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
