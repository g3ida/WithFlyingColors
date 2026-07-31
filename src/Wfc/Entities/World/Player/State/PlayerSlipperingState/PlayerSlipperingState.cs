namespace Wfc.Entities.World.Player;

using System;
using System.Linq;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.State;
using Wfc.Utils;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class PlayerSlipperingState : PlayerBaseState {
  public int direction = 1;
  private const float RAYCAST_Y_OFFSET = -1.0f;
  private const float RAY_LENGTH_FOR_SLIPPER = 2.0f;
  private const float RAY_LEN_FOR_FALLING = 100.0f;
  private const float RAY_LEN_FOR_ON_GROUND = 10.0f;

  private const float CORRECT_ROTATION_FALL_SPEED = 0.3f;
  private const float CORRECT_ROTATION_JUMP_SPEED = 0.07f;
  private const float PLAYER_SPEED_THRESHOLD_TO_STAND = 350.0f;
  private const float PLAYER_GROUND_SLIPPERING_FACTOR = 5.0f;
  private const float SLIPPERING_ROTATION_DURATION = 2.0f;
  private const float SLIPPERING_RECOVERY_INITIAL_DURATION = 0.8f;
  private const float SLIPPERING_RECOVERY_HANDOFF = 0.05f;

  private float _exitRotationSpeed = CORRECT_ROTATION_JUMP_SPEED;
  private bool _skipExitRotation = false;
  private float _initialRotation = 0f;


  public PlayerSlipperingState(IPlayerStatesStore statesStore, IInputManager inputManager)
    : base(statesStore, inputManager) { }

  protected override void _Enter(Player player) {
    player.AnimatedSpriteNode.Play("idle");
    player.AnimatedSpriteNode.Stop();
    _skipExitRotation = false;
    _exitRotationSpeed = CORRECT_ROTATION_JUMP_SPEED;
    player.PlayerRotationAction.Execute(direction, MathUtils.PI2, SLIPPERING_ROTATION_DURATION, true, false, true);
    EventHandler.Instance.EmitPlayerSlippering();
    player.CanDash = true;
    // The rotation action's accumulator, not player.Rotation: the transform folds the angle
    // into (-pi, pi], so a cube slipping while upside down crosses the branch and the
    // "how far have I tipped" measurements below jump by a full turn.
    _initialRotation = player.PlayerRotationAction.CurrentAngle;
  }

  protected override void _Exit(Player player) {
    // the fact that I am splitting this into a slow then rapid action is for these reasons:
    // 1- to prevent collision if the player jumped (if rotation speed is high move_and_slide
    //    won't work because the player will touch the platform before jump is completed)
    //  2- to make falling less sudden (rotation should be slow for visual appeal and fast
    //    for gameplay so the combination is the best option )
    if (!_skipExitRotation) {
      player.PlayerRotationAction.Execute(-direction, MathUtils.PI2, SLIPPERING_RECOVERY_INITIAL_DURATION, true, false, false);

      // Captured by value: this state is a singleton, so by the time the timer fires the
      // fields may already describe a second slip in the other direction and the cube would
      // snap the wrong way. The generation covers a respawn landing inside the handoff, which
      // would otherwise start turning a cube the checkpoint just stood back up. The validity
      // check covers the level being torn down, which leaves the timer holding the only
      // reference to a freed player.
      var recoveryDirection = -direction;
      var recoveryDuration = _exitRotationSpeed;
      var generation = player.PlayerRotationAction.Generation;
      player.GetTree().CreateTimer(SLIPPERING_RECOVERY_HANDOFF).Connect(Timer.SignalName.Timeout, Callable.From(() => {
        if (!GodotObject.IsInstanceValid(player) || !player.IsInsideTree()) {
          return;
        }
        if (player.PlayerRotationAction.Generation != generation) {
          return;
        }
        player.PlayerRotationAction.Execute(recoveryDirection, MathUtils.PI2, recoveryDuration, true, false, false);
      }));
    }
  }

  private bool _isPlayerTouchingTheFloor(Player player) {
    return player.IsOnFloor() || _checkIfGroundIsNear(player, -direction, RAY_LEN_FOR_ON_GROUND);
  }

  protected override IState<Player>? _PhysicsUpdate(Player player, float delta) {
    if (JumpPressed(player) && _isPlayerTouchingTheFloor(player)) {
      _exitRotationSpeed = CORRECT_ROTATION_JUMP_SPEED;
      return OnJump(player);
    }

    if (!_isPlayerTouchingTheFloor(player)) {
      var fallingState = statesStore.GetState<PlayerFallingState>();
      if (fallingState != null) {
        // added to avoid complete rotation when falling if the current angle is small enough or if the floor is
        // too close
        if (Mathf.Abs(player.PlayerRotationAction.CurrentAngle - player.PlayerRotationAction.ThetaZero) > MathUtils.PI10
            && !_checkIfGroundIsNear(player, direction, RAY_LEN_FOR_FALLING)
        ) {
          _exitRotationSpeed = CORRECT_ROTATION_FALL_SPEED;
          fallingState!.WasOnFloor = true;
          direction = -direction;
        }
      }
      return fallingState;
    }

    if (!player.IsRotationIdle()) {
      _skipExitRotation = true;
      return statesStore.GetState<PlayerStandingState>();
    }

    if (player.PlayerRotationAction.CanRotate || playerMoved) {
      return statesStore.GetState<PlayerStandingState>();
    }

    // Scale.X is the cube's size and never a facing sign - `direction` is the only thing that
    // decides which way it slides. A power-up that resizes the cube resizes how hard it pushes.
    if (_checkIfGroundIsNear(player, direction, RAY_LENGTH_FOR_SLIPPER)) {
      player.Velocity = new Vector2(
        player.Velocity.X + player.Scale.X * direction * PLAYER_SPEED_THRESHOLD_TO_STAND,
        player.Velocity.Y
      );
      return statesStore.GetState<PlayerStandingState>();
    }

    // A small speed depending on the current angle to simulate a slippering effect
    var rotCoef = Mathf.Abs(_initialRotation - player.PlayerRotationAction.CurrentAngle) / MathUtils.PI2;
    player.Velocity = new Vector2(
      player.Velocity.X + player.Scale.X * direction * rotCoef * PLAYER_GROUND_SLIPPERING_FACTOR,
      player.Velocity.Y
    );

    return null;
  }

  // The cube's lower corner on the side `dir` points to, in the player's own frame. Which
  // physical corner that is changes with every quarter turn, so it is measured rather than named.
  //
  // A cube resting on an exact diagonal has no corner that is both to one side and below, and the
  // corner directly underneath is the one worth probing from.
  public Vector2 _getPlayerEdgePosition(Player player, int dir) {
    var half = player.CollisionHalfExtentsLocal;
    var corners = new[] {
      new Vector2(-half.X, -half.Y),
      new Vector2(half.X, -half.Y),
      new Vector2(-half.X, half.Y),
      new Vector2(half.X, half.Y)
    };

    var center = player.GlobalPosition;
    var toTheSide = Vector2.Zero;
    var lowest = Vector2.Zero;
    var toTheSideY = float.NegativeInfinity;
    var lowestY = float.NegativeInfinity;

    foreach (var corner in corners) {
      var global = player.ToGlobal(corner);
      if (global.Y > lowestY) {
        lowest = corner;
        lowestY = global.Y;
      }
      if ((global.X - center.X) * dir > 0f && global.Y > center.Y && global.Y > toTheSideY) {
        toTheSide = corner;
        toTheSideY = global.Y;
      }
    }

    return toTheSideY > float.NegativeInfinity ? toTheSide : lowest;
  }

  // This method is used to raycast a ray of one of the two players ground corners.
  // We need this for 3 cases:
  // 1- Check if the player is still on the ground by raycasting from the opposite direction ground edge.
  // 2- Check if the player is on "stairs" and should not fall completely. It is the case of two grounds
  // near each other with little difference of hight. we try to detect this case and then a give the
  // player a little push to fall on the near ground and avoid complete rotation and weird slippering.
  // 3- We check if there is near ground (kind of a large "stairs") then we avoid complete rotation.
  private bool _checkIfGroundIsNear(Player player, int dir, float rayLength) {
    var from = player.ToGlobal(_getPlayerEdgePosition(player, dir)) + Vector2.Up * RAYCAST_Y_OFFSET;
    var to = from + new Vector2(0.0f, rayLength);

    var spaceState = player.GetWorld2D().DirectSpaceState;
    using var result = spaceState.IntersectRay(FloorRayQuery(player, from, to));
    return result.Count > 0;
  }
}
