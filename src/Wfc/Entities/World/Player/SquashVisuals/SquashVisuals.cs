namespace Wfc.Entities.World.Player;

using System;
using Godot;
using Wfc.Core.Event;
using Wfc.Entities.World.Explosion;
using Wfc.Screens.Levels;
using Wfc.Utils;
using EventHandler = Wfc.Core.Event.EventHandler;

// Everything a crushing looks and feels like, kept together so it can be tuned in one place: the
// beat the world holds still for as the cube is caught, the cube coming down against whatever
// caught it, and the explosion it goes to when it runs out of room.
public static class SquashVisuals {
  #region Constants
  // Two stops, because there are two things to feel: the cube being caught, and the cube going.
  // The second is the harder of the two and lands on the frame the cube bursts.
  private const float PRESS_HITSTOP_DURATION = 0.05f;
  private const float PRESS_HITSTOP_TIME_SCALE = 0.1f;
  private const float BURST_HITSTOP_DURATION = 0.07f;
  private const float BURST_HITSTOP_TIME_SCALE = 0.05f;

  private const float PRESS_SHAKE_AMPLITUDE = 7.0f;
  private const float BURST_SHAKE_AMPLITUDE = 14.0f;
  private const float BURST_ZOOM_PUNCH = 0.09f;

  // How much of its height the cube has left when it bursts, and how far it bulges out the other
  // way on the way there. The press stops well short of paper-thin: past this the shape stops
  // reading as a cube being crushed and starts reading as a smear.
  private const float BURST_FLATTEN = 0.45f;
  private const float BULGE = 1.7f;

  // The press keeps no clock of its own - it tracks the crusher - so this is the backstop for a
  // crusher that stalls or dies mid-press. The cube was caught either way, and a half-flattened
  // cube held forever is worse than one that finishes going.
  private const float PRESS_TIMEOUT = 1.0f;

  // The sprite's shader rounds the two free sides out as the cube flattens; this is how far along
  // that it is asked to go, and it has to reach the end at the same moment the press does.
  private const string SQUASH_PARAM = "squash";
  private const string SQUASH_AXIS_PARAM = "squash_axis";
  #endregion Constants

  // What the cube was caught by and how. `Pin` is the way it is being pressed, `Contact` is the
  // plane the crusher met it on, and `PinnedSurface` is the one on the other side - the surface
  // the cube comes down to nothing against. `Anchor` is the contact again, held in the crusher's
  // own frame from the frame the crush was reported: the crusher travels on between the report
  // and the squash, and only the anchor stays on its face.
  public readonly record struct Crush(Vector2 Pin, Vector2 Contact, Vector2 PinnedSurface, Node? Crusher, Vector2 Anchor);

  // What the press carries from one physics tick to the next. Static for the same reason the
  // explosion handle is: a checkpoint taken while the cube is still being flattened has to be
  // able to call off the rest of the squash, the death report waiting at the end of it included.
  private sealed class Press {
    public required Crush Crush;
    public required Node2D? Host;
    public required Vector2 Anchor;
    public required float CubeReach;
    public required Action OnSpent;
    public float Elapsed;
  }

  private static Press? _press;
  private static Explosion? _explosion;

  public static void Begin(Player player, Crush crush, Action onSpent) {
    player.HitstopNode.Start(PRESS_HITSTOP_DURATION, PRESS_HITSTOP_TIME_SCALE);
    GameEvents.Instance.RequestCameraShake(PRESS_SHAKE_AMPLITUDE);
    GameEvents.Instance.OnPlayerSquashed();

    // The face the press measures the closing gap from. On the crusher, the anchor it reported
    // with is the only honest answer - converting the contact now would land it wherever the
    // crusher has got to since. The fallback host never moves, which just leaves the timeout to
    // finish the press.
    var host = _hostFor(player, crush);
    var anchor = host == crush.Crusher ? crush.Anchor : host?.ToLocal(crush.Contact) ?? Vector2.Zero;

    _press = new Press {
      Crush = crush,
      Host = host,
      Anchor = anchor,
      CubeReach = player.GetCollisionHalfExtents().Dot(crush.Pin.Abs()) * 2.0f,
      OnSpent = onSpent,
    };
    Step(player, 0.0f);
  }

  // Driven from the squashed state's physics tick rather than by a clock: the cube is drawn to
  // fit whatever room the crusher has left it, so the squash and the crusher arrive together at
  // any travel speed - and a hitstop that slows the crusher down slows the squash with it.
  public static void Step(Player player, float delta) {
    if (_press is null) {
      return;
    }
    _press.Elapsed += delta;
    var flatten = Mathf.Clamp(_gap(_press) / _press.CubeReach, BURST_FLATTEN, 1.0f);
    _flatten(player, _press.Crush, _press.CubeReach * 0.5f, flatten);
    if (flatten > BURST_FLATTEN && _press.Elapsed < PRESS_TIMEOUT) {
      return;
    }

    var press = _press;
    _press = null;
    _burst(player, press.OnSpent);
  }

  // The room the crusher has left the cube: from its face - carried onwards in its own frame - to
  // the surface the cube is pinned against.
  private static float _gap(Press press) =>
    (press.Crush.PinnedSurface - _crusherFace(press)).Dot(press.Crush.Pin);

  private static Vector2 _crusherFace(Press press) =>
    press.Host is { } host && GodotObject.IsInstanceValid(host) && host.IsInsideTree()
      ? host.ToGlobal(press.Anchor)
      : press.Crush.Contact;

  // Everything the squash borrowed from the cube, handed back - and the explosion called off with
  // it, debris timer and all, so a death report from the last life cannot land in this one.
  public static void End(Player player) {
    _press = null;
    if (_explosion is { } explosion) {
      _explosion = null;
      if (GodotObject.IsInstanceValid(explosion)) {
        explosion.QueueFree();
      }
    }

    var sprite = player.AnimatedSpriteNode;
    sprite.Position = Vector2.Zero;
    sprite.Visible = true;
    if (sprite.Material is ShaderMaterial material) {
      material.SetShaderParameter(SQUASH_PARAM, 0.0f);
    }
    // Takes back the scale and the offset the press wrote, and the landing animation's clock
    // with them: a cube crushed mid-bounce leaves that clock part-run, and Start() will not
    // restart one it thinks is still going.
    player.CurrentAnimation.Reset(sprite);
  }

  // The cube is pinned, so the edge against the surface stays put and the rest of the cube comes
  // down to meet it. Anchored on the surface the crush recorded rather than on the body: the body
  // itself was shoved some way into that surface before anything could act on the report, and the
  // corpse must not be drawn where the body ended up. The offset is then taken back into the
  // cube's own frame, which is not the world's - the cube spends most of the game some other way
  // up.
  private static void _flatten(Player player, Crush crush, float halfReach, float flatten) {
    var sprite = player.AnimatedSpriteNode;
    var local = crush.Pin.Rotated(-player.GlobalRotation);
    var alongX = Mathf.Abs(local.X);
    var bulge = 1.0f + ((1.0f - flatten) * BULGE);

    sprite.Scale = new Vector2(Mathf.Lerp(bulge, flatten, alongX), Mathf.Lerp(flatten, bulge, alongX));
    var centre = crush.PinnedSurface - (crush.Pin * halfReach * flatten);
    sprite.Position = (centre - player.GlobalPosition).Rotated(-player.GlobalRotation);

    if (sprite.Material is ShaderMaterial material) {
      material.SetShaderParameter(SQUASH_AXIS_PARAM, local);
      material.SetShaderParameter(SQUASH_PARAM, Mathf.InverseLerp(1.0f, BURST_FLATTEN, flatten));
    }
  }

  // The squeezed cube goes the way every other cube goes: it blows apart. The explosion carries
  // the death report - it comes back once the debris has settled, the same beat the explosion
  // death gives it. The squash sound from the press is still running over all of this, which is
  // why the explosion's own sound is not asked for.
  private static void _burst(Player player, Action onSpent) {
    player.HitstopNode.Start(BURST_HITSTOP_DURATION, BURST_HITSTOP_TIME_SCALE);
    GameEvents.Instance.RequestCameraShake(BURST_SHAKE_AMPLITUDE);
    GameEvents.Instance.RequestCameraZoomPunch(BURST_ZOOM_PUNCH);

    // Hiding the sprite takes its light occluder with it, which is the whole reason the cube's
    // shadow does not outlive the cube.
    player.AnimatedSpriteNode.Visible = false;

    var explosion = SceneHelpers.InstantiateNode<Explosion>();
    _explosion = explosion;
    explosion.Connect(
      Explosion.SignalName.ObjectDetonated,
      Callable.From<Explosion>(spent => {
        _explosion = null;
        spent.QueueFree();
        onSpent();
      }),
      (uint)GodotObject.ConnectFlags.OneShot
    );
    explosion.Connect(Node.SignalName.Ready, Callable.From(() => {
      explosion.Setup(player);
      explosion.FireExplosion();
    }), (uint)GodotObject.ConnectFlags.OneShot);
    // Deferred because the burst comes out of a physics tick - and dropped instead if a
    // checkpoint gets in ahead of it, or the last life's debris would rain into this one.
    Callable.From(() => {
      if (_explosion != explosion) {
        explosion.QueueFree();
        return;
      }
      player.AddChild(explosion);
      explosion.Owner = player;
    }).CallDeferred();
  }

  // Whatever face the gap is measured against: the thing doing the crushing, so a platform still
  // under power is followed rather than remembered where it was.
  private static Node2D? _hostFor(Player player, Crush crush) =>
    GodotObject.IsInstanceValid(crush.Crusher) && crush.Crusher is Node2D crusher && crusher.IsInsideTree()
      ? crusher
      : player.GetParent() as Node2D;
}
