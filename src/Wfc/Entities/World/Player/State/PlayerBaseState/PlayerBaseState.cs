namespace Wfc.Entities.World.Player;

using Godot;
using Wfc.Core.Input;
using Wfc.State;
using Wfc.Utils;

public abstract class PlayerBaseState : IState<Player> {
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
    if (death.Type != EntityType.None) {
      player.CarriedSpeed = 0.0f;
      return _dyingStateFor(player, death);
    }
    if (!player.IsDying()) {
      if (DashActionPressed(player)) {
        return OnDash(player);
      }
      _releaseFoughtCarry(player);
      _applyWalkInput(player);
      _applyGravity(player, delta);
    }

    var newState = _PhysicsUpdate(player, delta);
    _move(player, delta);

    return newState;
  }

  protected virtual IState<Player>? _PhysicsUpdate(Player player, float delta) { return null; }

  // Which way the cube dies. What dying then does to it belongs to the state named here.
  private PlayerBaseState? _dyingStateFor(Player player, Player.PendingDeath death) => death.Type switch {
    EntityType.FallZone => statesStore.GetState<PlayerFallZoneDyingState>(),
    EntityType.Crusher => _squashedBy(player, death),
    _ => statesStore.GetState<PlayerExplosionState>(),
  };

  // Told before it is entered, the way the slippering state is told which way it tips: a squash is
  // drawn entirely from which side of the cube was caught.
  private PlayerSquashedState? _squashedBy(Player player, Player.PendingDeath death) {
    var squashed = statesStore.GetState<PlayerSquashedState>();
    squashed?.TakeCrush(player, death);
    return squashed;
  }

  // The two directions are exclusive, so holding both is holding neither. A dash owns the run
  // speed for as long as it lasts.
  private void _applyWalkInput(Player player) {
    if (player.HandleInputIsDisabled || player.IsDashing()) {
      return;
    }
    // The limit is what the cube can run at, not what it can travel at, so it is held against the
    // run with the carry taken out - a cube thrown by the floor it jumped off still works its own
    // run up to the limit on top of the throw.
    var run = player.Velocity.X - player.CarriedSpeed;
    if (inputManager.IsPressed(IInputManager.Action.MoveRight)) {
      playerMoved = true;
      run = Mathf.Clamp(run + player.SpeedUnit, 0, player.SpeedLimit);
    }
    else if (inputManager.IsPressed(IInputManager.Action.MoveLeft)) {
      playerMoved = true;
      run = Mathf.Clamp(run - player.SpeedUnit, -player.SpeedLimit, 0);
    }
    else {
      return;
    }
    player.Velocity = new Vector2(run + player.CarriedSpeed, player.Velocity.Y);
  }

  // A push the player is steering out of stops being a push: released, it is ordinary run speed
  // again, which the clamp and the damping take off within a tick.
  private void _releaseFoughtCarry(Player player) {
    if (Mathf.IsZeroApprox(player.CarriedSpeed) || player.HandleInputIsDisabled) {
      return;
    }
    var against = player.CarriedSpeed > 0.0f
      ? IInputManager.Action.MoveLeft
      : IInputManager.Action.MoveRight;
    if (inputManager.IsPressed(against)) {
      player.CarriedSpeed = 0.0f;
    }
  }

  private static void _applyGravity(Player player, float delta) =>
    player.Velocity = new Vector2(player.Velocity.X, player.Velocity.Y + GRAVITY * delta * FALL_FACTOR);

  // The same three steps close out every state: travel, bleed off the run speed, and let the
  // squash and stretch follow the cube there.
  private void _move(Player player, float delta) {
    var wasOnFloor = player.IsOnFloor();
    var floorSpeed = player.GetPlatformVelocity().X;
    var before = player.Velocity.X;

    player.MoveAndSlide();
    _readCarry(player, wasOnFloor, floorSpeed, before);

    var carry = player.CarriedSpeed;
    player.Velocity = new Vector2(Mathf.Lerp(player.Velocity.X - carry, 0, RUN_DAMPING) + carry, player.Velocity.Y);
    player.CurrentAnimation.Step(player, player.AnimatedSpriteNode, delta);
  }

  // What the floor gave the cube on the tick it left it. The engine adds a moving floor's own
  // velocity there, and only what it actually added is exempted from the run rules: the same call
  // moves a cube that left along a wall, and the wall owes it nothing. A push the player is
  // already steering against is not taken at all, so a jump made against a moving floor is the
  // jump it always was.
  private void _readCarry(Player player, bool wasOnFloor, float floorSpeed, float before) {
    if (player.IsOnFloor()) {
      player.CarriedSpeed = 0.0f;
      return;
    }
    if (!wasOnFloor) {
      return;
    }
    var gained = player.Velocity.X - before;
    player.CarriedSpeed = Mathf.Sign(gained) == Mathf.Sign(floorSpeed)
      ? Mathf.Sign(floorSpeed) * Mathf.Min(Mathf.Abs(gained), Mathf.Abs(floorSpeed))
      : 0.0f;
    _releaseFoughtCarry(player);
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

  // One reused query per state: building one per probe allocates three engine objects a ray,
  // and the standing state probes four times every physics tick the cube spends stood still.
  private PhysicsRayQueryParameters2D? _rayQuery;

  protected PhysicsRayQueryParameters2D FloorRayQuery(Player player, Vector2 from, Vector2 to) {
    _rayQuery ??= new PhysicsRayQueryParameters2D {
      Exclude = new Godot.Collections.Array<Rid> { player.GetRid() },
    };
    _rayQuery.From = from;
    _rayQuery.To = to;
    return _rayQuery;
  }
}
