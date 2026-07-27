namespace Wfc.Entities.World.Player;

using System;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.State;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class PlayerStandingState : PlayerBaseState {
  private const float RAYCAST_LENGTH = 10.0f;
  private const float RAYCAST_Y_OFFSET = -5.0f; // https://godotengine.org/qa/63336/raycast2d-doesnt-collide-with-tilemap
  private const float SLIPPERING_LIMIT = 0.42f; // higher is less slippering

  // The probes are cast from a box a little larger than the cube, on both axes.
  //
  // That reads like a mistake and it used to be one: the size came from a hand-rolled sum that
  // counted the cube's collision plates twice, so nothing in the scene was ever that big. But the
  // slippering fixtures are tuned to where these rays land to within a pixel - narrow the box to
  // the cube itself, on either axis, and a cube perched on a ledge catches its own edge and tips
  // a second time instead of committing to the fall. So it stays, stated as what it actually is:
  // a reach past the cube, and not a second opinion about how big the cube is.
  private const float PROBE_REACH = 1.0796f;

  public PlayerStandingState(IPlayerStatesStore statesStore, IInputManager inputManager)
    : base(statesStore, inputManager) { }

  protected override void _Enter(Player player) {
    player.AnimatedSpriteNode.Play("idle");
    player.AnimatedSpriteNode.Stop();
    player.CanDash = true;
  }

  protected override void _Exit(Player player) {
  }

  protected override IState<Player>? _PhysicsUpdate(Player player, float delta) {
    if (JumpPressed(player) && player.IsOnFloor()) {
      return OnJump(player);
    }
    if (!player.IsOnFloor()) {
      var fallingState = statesStore.GetState<PlayerFallingState>();
      if (fallingState != null) {
        fallingState.WasOnFloor = true;
      }
      return fallingState;
    }
    else {
      if (Math.Abs(player.Velocity.X) < player.SpeedUnit && player.IsRotationIdle()) {
        return RaycastFloor(player);
      }
    }
    return null;
  }

  // The box the probes are cast from, given the cube's true half-extents.
  internal static Vector2 ProbeBox(Vector2 halfExtents) => halfExtents * PROBE_REACH;

  // Where the four probes sit across it. The result is read as a bit pattern, and only the two
  // patterns meaning "one outer probe alone found floor" start a slip - so these positions are
  // the whole of the mechanic.
  internal static float[] FloorProbeOffsets(float halfWidth) {
    var reach = halfWidth * PROBE_REACH;
    return new[] { -reach, -reach * SLIPPERING_LIMIT, reach * SLIPPERING_LIMIT, reach };
  }

  private PlayerSlipperingState? RaycastFloor(Player player) {
    var spaceState = player.GetWorld2D().DirectSpaceState;
    var half = player.GetCollisionHalfExtents();
    var probeBox = ProbeBox(half);

    int combination = 0;
    int i = 1;

    foreach (var offset in FloorProbeOffsets(half.X)) {
      Vector2 from = player.GlobalPosition + new Vector2(offset, probeBox.Y + RAYCAST_Y_OFFSET);
      Vector2 to = from + new Vector2(0.0f, RAYCAST_LENGTH);
      var physicsRayQueryParameters = PhysicsRayQueryParameters2D.Create(
          from, to, exclude: new Godot.Collections.Array<Rid> { player.GetRid() }
      );

      var result = spaceState.IntersectRay(physicsRayQueryParameters);
      if (result.ContainsKey("collider")) {
        combination += i;
      }
      i *= 2;
    }
    if (combination == 1 || combination == 8) // flag values
    {
      var slipperingState = statesStore.GetState<PlayerSlipperingState>();
      if (slipperingState != null) {
        slipperingState.direction = combination == 1 ? 1 : -1;
      }
      return slipperingState;
    }
    return null;
  }
}
