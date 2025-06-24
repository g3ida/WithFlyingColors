namespace Wfc.Entities.World.Player;

using System;
using Godot;
using Wfc.Core.Event;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class PlayerSlipperingState : PlayerBaseState {
  public int direction = 1;
  private PlayerRotationAction playerRotation;

  private const float RAYCAST_Y_OFFSET = -3.0f;
  private const float RAYCAST_LENGTH = 5.0f;
  private const float CORRECT_ROTATION_FALL_SPEED = 0.3f;
  private const float CORRECT_ROTATION_JUMP_SPEED = 0.07f;
  private const float PLAYER_SPEED_THRESHOLD_TO_STAND = -300.0f;
  private const float SLIPPERING_ROTATION_DURATION = 2.0f;
  private const float SLIPPERING_RECOVERY_INITIAL_DURATION = 0.8f;

  private float exitRotationSpeed = CORRECT_ROTATION_JUMP_SPEED;
  private bool skipExitRotation = false;

  public PlayerSlipperingState() : base() {
    this.baseState = PlayerStatesEnum.SLIPPERING;
  }

  protected override void _Enter(Player player) {
    playerRotation = player.PlayerRotationAction;
    player.AnimatedSpriteNode.Play("idle");
    player.AnimatedSpriteNode.Stop();
    skipExitRotation = false;
    exitRotationSpeed = CORRECT_ROTATION_JUMP_SPEED;
    playerRotation.Execute(direction, Constants.PI2, SLIPPERING_ROTATION_DURATION, true, false, true);
    EventHandler.Instance.EmitPlayerSlippering();
    player.CanDash = true;
  }

  protected override void _Exit(Player player) {
    //   # the fact that I am splitting this into a slow then rapid action is for these reasons:
    //   # 1- to prevent collision if the player jumped (if rotation speed is high move_and_slide
    //   #    won't work because the player will touch the platform before jump is completed)
    //   # 2- to make falling less sudden (rotation should be slow for visual appeal and fast
    //   #    for gameplay so the combination is the best option )
    if (!skipExitRotation) {
      playerRotation.Execute(-direction, Constants.PI2, SLIPPERING_RECOVERY_INITIAL_DURATION, true, false, false);
      player.GetTree().CreateTimer(0.05f).Connect("timeout", Callable.From(() => {
        playerRotation.Execute(-direction, Constants.PI2, exitRotationSpeed, true, false, false);
      }));
    }
  }

  protected override BaseState<Player> _PhysicsUpdate(Player player, float delta) {
    if (JumpPressed(player) && player.IsOnFloor()) {
      exitRotationSpeed = CORRECT_ROTATION_JUMP_SPEED;
      return OnJump(player);
    }

    if (!player.IsOnFloor()) {
      var fallingState = (PlayerFallingState)player.StatesStore.GetState(PlayerStatesEnum.FALLING);
      // added to avoid complete rotation when falling if the current angle is small enough or if the floor is
      // too close
      if (Mathf.Abs(player.Rotation - playerRotation.ThetaZero) > Constants.PI8 && !CheckIfGroundIsNear(player)) {
        exitRotationSpeed = CORRECT_ROTATION_FALL_SPEED;
        fallingState.wasOnFloor = true;
        direction = -direction;
      }
      return fallingState;
    }

    if (player.PlayerRotationState.baseState != PlayerStatesEnum.IDLE) {
      skipExitRotation = true;
      return player.StatesStore.GetState(PlayerStatesEnum.STANDING);
    }

    if (playerRotation.CanRotate || playerMoved) {
      return player.StatesStore.GetState(PlayerStatesEnum.STANDING);
    }

    return HandleGroundIsNear(player);
  }

  public Vector2 GetFallingEdgePosition(Player player) {
    var corners = new CollisionShape2D[] { player.FaceCollisionShapeTL_node, player.FaceCollisionShapeTR_node,
                                      player.FaceCollisionShapeBL_node, player.FaceCollisionShapeBR_node };

    var pp = player.GlobalPosition;
    var position = pp;
    var size = player.GetCollisionShapeSize() * 0.5f * player.Scale;

    foreach (var cc in corners) {
      var cp = cc.GlobalPosition;
      if (Mathf.Sign(pp.X - cp.X) == -direction && cp.Y > position.Y) {
        position = cp;
        size = (cc.Shape as RectangleShape2D).Size;
      }
    }

    return position + new Vector2(-0.5f * direction * size.X, 0.5f * size.Y) * player.Scale;
  }

  // # the case of two grounds near each other with little difference of hight
  // # we try to detect this case a give the player a little push to fall on the
  // # near ground and avoid complete rotation
  private bool CheckIfGroundIsNear(Player player) {
    var spaceState = player.GetWorld2D().DirectSpaceState;
    var from = GetFallingEdgePosition(player) + Vector2.Up * RAYCAST_Y_OFFSET;
    var to = from + new Vector2(0.0f, RAYCAST_LENGTH);
    var physicsRayQueryParameters = PhysicsRayQueryParameters2D.Create(
        from, to, exclude: new Godot.Collections.Array<Rid> { player.GetRid() }
    );
    var result = spaceState.IntersectRay(physicsRayQueryParameters);
    return result.ContainsKey("collider");
  }

  private BaseState<Player> HandleGroundIsNear(Player player) {
    if (CheckIfGroundIsNear(player)) {
      player.Velocity = new Vector2(player.Velocity.X - player.Scale.X * direction * PLAYER_SPEED_THRESHOLD_TO_STAND, player.Velocity.Y);
      return player.StatesStore.GetState(PlayerStatesEnum.STANDING);
    }
    return null;
  }
}
