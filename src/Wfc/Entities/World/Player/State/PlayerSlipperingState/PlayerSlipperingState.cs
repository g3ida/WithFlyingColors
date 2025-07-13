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
    _initialRotation = player.Rotation;
  }

  protected override void _Exit(Player player) {
    // the fact that I am splitting this into a slow then rapid action is for these reasons:
    // 1- to prevent collision if the player jumped (if rotation speed is high move_and_slide
    //    won't work because the player will touch the platform before jump is completed)
    //  2- to make falling less sudden (rotation should be slow for visual appeal and fast
    //    for gameplay so the combination is the best option )
    if (!_skipExitRotation) {
      player.PlayerRotationAction.Execute(-direction, MathUtils.PI2, SLIPPERING_RECOVERY_INITIAL_DURATION, true, false, false);
      player.GetTree().CreateTimer(0.05f).Connect(Timer.SignalName.Timeout, Callable.From(() => {
        player.PlayerRotationAction.Execute(-direction, MathUtils.PI2, _exitRotationSpeed, true, false, false);
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
        if (Mathf.Abs(player.Rotation - player.PlayerRotationAction.ThetaZero) > MathUtils.PI10
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

    if (_checkIfGroundIsNear(player, direction, RAY_LENGTH_FOR_SLIPPER)) {
      player.Velocity = new Vector2(
        player.Velocity.X + player.Scale.X * direction * PLAYER_SPEED_THRESHOLD_TO_STAND,
        player.Velocity.Y
      );
      return statesStore.GetState<PlayerStandingState>();
    }

    // A small speed depending on the current angle to simulate a slippering effect
    var rotCoef = Mathf.Abs(_initialRotation - player.Rotation) / MathUtils.PI2;
    player.Velocity = new Vector2(
      player.Velocity.X + player.Scale.X * direction * rotCoef * PLAYER_GROUND_SLIPPERING_FACTOR,
      player.Velocity.Y
    );

    return null;
  }

  public Vector2 _getPlayerEdgePosition(Player player, int dir) {
    var corners = new[] {
      ( player.FaceCollisionShapeTL_node, new Vector2(-0.5f, -0.5f) ),
      ( player.FaceCollisionShapeTR_node, new Vector2(0.5f, -0.5f) ),
      ( player.FaceCollisionShapeBL_node, new Vector2(-0.5f, 0.5f) ),
      ( player.FaceCollisionShapeBR_node, new Vector2(0.5f, 0.5f) )
    };

    var pp = player.GlobalPosition;
    var position = pp;
    var size = player.GetCollisionShapeSize() * 0.5f * player.Scale;
    var positionLocal = pp;

    foreach (var (cc, offset) in corners) {
      var cp = cc.GlobalPosition;
      if (Mathf.Sign(pp.X - cp.X) == -dir && cp.Y > position.Y) {
        position = cp;
        size = (cc.Shape as RectangleShape2D)?.Size ?? Vector2.Zero;
        positionLocal = cc.Position + offset * size * player.Scale;
        return positionLocal;
      }
    }
    return positionLocal;
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
    var physicsRayQueryParameters = PhysicsRayQueryParameters2D.Create(
        from, to, exclude: new Godot.Collections.Array<Rid> { player.GetRid() }
    );

    var result = spaceState.IntersectRay(physicsRayQueryParameters);
    return result.ContainsKey("collider");
  }
}
