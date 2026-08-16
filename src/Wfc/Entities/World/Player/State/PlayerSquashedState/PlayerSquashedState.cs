namespace Wfc.Entities.World.Player;

using Godot;
using Wfc.Core.Input;
using Wfc.State;
using EventHandler = Wfc.Core.Event.EventHandler;

// Crushed: caught by something under power with somewhere it was being pushed and nothing to push
// back with. The cube is pressed flat against whatever it was pinned to and then goes the way
// every cube goes - it blows apart - so what makes this death its own is the press, not the end.
public partial class PlayerSquashedState : PlayerDyingBaseState {
  private SquashVisuals.Crush _crush = new(Vector2.Down, Vector2.Zero, Vector2.Zero, null, Vector2.Zero);

  public PlayerSquashedState(IPlayerStatesStore statesStore, IInputManager inputManager)
    : base(statesStore, inputManager) {
  }

  // Told before the state is entered, the way the slippering state is told which way it tips.
  // Everything here is read off the report, not off the cube as it stands now: between the report
  // and this state the crusher has shoved the cube some way into whatever it is pinned against.
  public void TakeCrush(Player player, Player.PendingDeath death) {
    var pin = PinDirectionFor(death.SelfPosition, death.Position);
    var farFace = death.SelfPosition + (pin * player.GetCollisionHalfExtents().Dot(pin.Abs()));
    _crush = new SquashVisuals.Crush(
      Pin: pin,
      Contact: death.Position,
      PinnedSurface: _pinnedSurface(player, death, pin) ?? farFace,
      Crusher: death.Source,
      Anchor: death.SourceAnchor
    );
  }

  // The surface the cube is being pressed against, asked of the physics directly. The cube's own
  // far face is only where that surface was if the cube is still where it was reported - and the
  // shove that comes with being crushed means it rarely is, so the probe slides the far face onto
  // the plane the cube actually has under it. Only the depth moves: the point stays on the cube's
  // centre line, where the paint and the flattening sprite both hang off it.
  private static Vector2? _pinnedSurface(Player player, Player.PendingDeath death, Vector2 pin) {
    var reach = player.GetCollisionHalfExtents().Dot(pin.Abs()) * 2.0f;
    var probe = new PhysicsTestMotionParameters2D {
      From = player.GlobalTransform,
      Motion = pin * reach,
    };
    if (death.Source is PhysicsBody2D crusher && GodotObject.IsInstanceValid(crusher)) {
      probe.ExcludeBodies = new Godot.Collections.Array<Rid> { crusher.GetRid() };
    }
    var result = new PhysicsTestMotionResult2D();
    if (!PhysicsServer2D.BodyTestMotion(player.GetRid(), probe, result)) {
      return null;
    }
    var farFace = death.SelfPosition + (pin * reach * 0.5f);
    return farFace + (pin * (result.GetCollisionPoint() - farFace).Dot(pin));
  }

  // The crusher's edge is at `contact`, so the surface holding the cube up is on the far side of
  // the cube: that is the way the cube flattens, and the way the paint then runs. Snapped to one
  // axis, because a crush that reads as diagonal is a crush drawn twice.
  public static Vector2 PinDirectionFor(Vector2 centre, Vector2 contact) {
    var toContact = contact - centre;
    return Mathf.Abs(toContact.Y) >= Mathf.Abs(toContact.X)
      ? new Vector2(0.0f, toContact.Y > 0.0f ? -1.0f : 1.0f)
      : new Vector2(toContact.X > 0.0f ? -1.0f : 1.0f, 0.0f);
  }

  protected override void _Enter(Player player) {
    base._Enter(player);
    // A crushed cube stops where it was caught. The fall zone is the only death that keeps its
    // momentum, because there the fall is the whole of what the player sees.
    player.Velocity = Vector2.Zero;
    // Whatever frame the cube was on, it is a flat square from here on: the press is a scale, and
    // it has nothing to say about a sprite mid-animation.
    player.AnimatedSpriteNode.Play("idle");
    player.AnimatedSpriteNode.Stop();
    SquashVisuals.Begin(player, _crush, EventHandler.Instance.EmitPlayerDied);
  }

  // A pinned cube takes no input, no gravity, and none of the idle transform animation that would
  // take the sprite back to square; what its physics tick is for is the press, which follows the
  // crusher's actual travel from here.
  public override IState<Player>? PhysicsUpdate(Player player, float delta) {
    SquashVisuals.Step(player, delta);
    return null;
  }

  // The crusher is let go of here rather than kept for the next crush: a state store outlives the
  // level, and a handle to a platform in the one before it is a handle to a freed node.
  protected override void _Exit(Player player) {
    base._Exit(player);
    SquashVisuals.End(player);
    _crush = _crush with { Crusher = null };
  }
}
