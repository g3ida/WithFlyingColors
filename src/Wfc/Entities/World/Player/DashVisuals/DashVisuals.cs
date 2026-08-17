namespace Wfc.Entities.World.Player;

using Godot;
using Wfc.Core.Event;
using Wfc.Screens.Levels;
using Wfc.Skin;
using Wfc.Utils;

// Everything a dash draws and feels like, kept together so it can be tuned in one place: the
// beat the world holds still for, the bullet the cube deforms into and the colored speed lines
// it leaves behind.
public static class DashVisuals {
  #region Constants
  // Long enough to register as a punch, short enough that it never reads as a stutter.
  private const float HITSTOP_DURATION = 0.045f;
  private const float HITSTOP_TIME_SCALE = 0.08f;

  // Leaving is sold by the zoom pulse and the held frame, so the shake only has to keep the
  // frame from sitting perfectly still. Arriving has neither, and takes the harder one.
  private const float LAUNCH_SHAKE_AMPLITUDE = 4.0f;
  private const float IMPACT_SHAKE_AMPLITUDE = 9.0f;
  private const float ZOOM_PUNCH_STRENGTH = 0.06f;

  // The cube snaps into the bullet shape, holds it for as long as it is actually travelling
  // and only relaxes on the way out. Releasing it any earlier leaves a square cube crossing
  // the screen at full speed.
  private const float DEFORM_ATTACK = 0.15f;
  private const float DEFORM_RELEASE = 0.65f;

  // What the release stops at rather than runs out to. The dash ends with the cube still
  // stretched and the last of it comes off over the coast that follows, which is what makes
  // the stop read as one: a cube back to square on the final frame has already stopped.
  private const float DEFORM_TAIL = 0.3f;
  private const float COAST_RELAX = 0.14f;

  // A wall takes the stretch away rather than letting it unwind, but not inside one frame -
  // that reads as a dropped frame instead of a hit.
  private const float IMPACT_RELAX = 0.05f;

  private const string DASH_DIR_PARAM = "dash_dir";
  private const string STRENGTH_PARAM = "strength";

  // One speed line per face color. Every head follows the cube, so TailFraction alone decides
  // how long a line ends up, and staggering it is what makes the four read as a fan rather than
  // one thick band. Thickness and Offset are multiples of the cube's width, so the trail keeps
  // its proportions when a power-up resizes it; Offset being the line's centre, it has to keep
  // half a Thickness in hand at either end to stay over the face it is coloured after.
  private readonly record struct StreakShape(
    SkinColor Color,
    float TailFraction,
    float Offset,
    float Thickness
  );

  private static readonly StreakShape[] STREAKS = {
    new(SkinColor.LeftFace, TailFraction: 0.3f, Offset: -0.39f, Thickness: 0.2f),
    new(SkinColor.TopFace, TailFraction: 0.0f, Offset: -0.13f, Thickness: 0.26f),
    new(SkinColor.RightFace, TailFraction: 0.45f, Offset: 0.13f, Thickness: 0.18f),
    new(SkinColor.BottomFace, TailFraction: 0.12f, Offset: 0.37f, Thickness: 0.24f),
  };
  #endregion Constants

  // The stretch outlives the dash that started it, so whatever writes the strength next has to
  // take the unwind back first: a second dash begun inside the tail would otherwise spend its
  // opening frames being pulled back to square by the one before it.
  private static Tween? _deformTail;

  // Called the moment the dash commits to a direction, which is later than the state is
  // entered: a dash still inside its permissiveness window has nothing to draw yet.
  public static void Begin(Player player, Vector2 direction, float travel) {
    player.HitstopNode.Start(HITSTOP_DURATION, HITSTOP_TIME_SCALE);
    GameEvents.Instance.RequestCameraZoomPunch(ZOOM_PUNCH_STRENGTH);
    GameEvents.Instance.RequestCameraShake(LAUNCH_SHAKE_AMPLITUDE);
    _burst(player.DashLaunchParticlesNode, player, Vector2.Zero, -direction);
    _spawnStreaks(player, direction, travel);
  }

  public static void Step(Player player, Vector2 direction, float elapsed, float duration) {
    _setDeform(player, direction, _deformStrength(elapsed / Mathf.Max(duration, MathUtils.EPSILON)));
  }

  // The dash ran into something. Fired where the cube was stopped rather than where it was
  // aimed, and thrown back off the surface, since that is the way the cube itself would have
  // gone if it had anywhere left to go.
  public static void Impact(Player player, Vector2 direction) {
    var forward = direction.Normalized();
    GameEvents.Instance.RequestCameraShake(IMPACT_SHAKE_AMPLITUDE);
    _burst(player.DashImpactParticlesNode, player, forward * player.CollisionHalfExtentsLocal.X, -forward);
    _relaxDeform(player, IMPACT_RELAX);
  }

  // The dash ran out with the ground ahead of it still clear. What is left of the stretch comes
  // off over the speed the cube carries out of it.
  public static void Coast(Player player) => _relaxDeform(player, COAST_RELAX);

  public static void End(Player player) => _setDeform(player, Vector2.Right, 0.0f);

  private static void _setDeform(Player player, Vector2 direction, float strength) {
    _killDeformTail();
    if (player.AnimatedSpriteNode.Material is not ShaderMaterial material) {
      return;
    }
    // The shader works in the sprite's own frame, and the cube spends most of the game
    // turned some other way up than the world is.
    material.SetShaderParameter(DASH_DIR_PARAM, direction.Normalized().Rotated(-player.GlobalRotation));
    material.SetShaderParameter(STRENGTH_PARAM, strength);
  }

  private static float _deformStrength(float progress) {
    var clamped = Mathf.Clamp(progress, 0.0f, 1.0f);
    if (clamped < DEFORM_ATTACK) {
      return clamped / DEFORM_ATTACK;
    }
    if (clamped < DEFORM_RELEASE) {
      return 1.0f;
    }
    var released = (clamped - DEFORM_RELEASE) / (1.0f - DEFORM_RELEASE);
    return Mathf.Lerp(1.0f, DEFORM_TAIL, released);
  }

  // Unwinds whatever stretch the cube is holding, from wherever the dash left it. Driven by a
  // tween rather than the state, because everything it has to outlast - the dash, the state,
  // and on a hit the cube's own motion - is over by the time it starts.
  private static void _relaxDeform(Player player, float duration) {
    _killDeformTail();
    if (!player.IsInsideTree() || player.AnimatedSpriteNode.Material is not ShaderMaterial material) {
      return;
    }
    _deformTail = player.CreateTween();
    _deformTail.TweenProperty(material, $"shader_parameter/{STRENGTH_PARAM}", 0.0f, duration)
      .SetTrans(Tween.TransitionType.Quad)
      .SetEase(Tween.EaseType.Out);
  }

  private static void _killDeformTail() {
    _deformTail?.Kill();
    _deformTail = null;
  }

  // The emitters hang off the cube, which spends most of the game turned some other way up than
  // the world is, so both where the burst comes from and the way it is thrown are given in the
  // cube's own frame.
  private static void _burst(CpuParticles2D particles, Player player, Vector2 origin, Vector2 away) {
    var toLocal = -player.GlobalRotation;
    particles.Position = origin.Rotated(toLocal);
    particles.Direction = away.Normalized().Rotated(toLocal);
    particles.Restart();
  }

  private static void _spawnStreaks(Player player, Vector2 direction, float travel) {
    var forward = direction.Normalized();
    var side = new Vector2(-forward.Y, forward.X);
    var width = player.GetCollisionHalfExtents().X * 2.0f;
    var skin = SkinManager.Instance.CurrentSkin;

    foreach (var shape in STREAKS) {
      var streak = SceneHelpers.InstantiateNode<DashStreak>();
      streak.Tint = skin.GetColor(shape.Color, SkinColorIntensity.Basic);
      streak.Thickness = width * shape.Thickness;
      _addBehindPlayer(player, streak);
      // The line waits where its tail will be until the cube reaches it, and unrolls from there
      // for as far as the cube actually gets. Placed in world terms, like the direction it was
      // laid out along, so a rotated parent cannot turn the line off the dash it belongs to.
      streak.GlobalRotation = forward.Angle();
      streak.GlobalPosition = player.GlobalPosition
        + (forward * travel * shape.TailFraction)
        + (side * width * shape.Offset);
      // The line is laid out after it is in the tree, so the transform it is interpolated from
      // is still the one it entered on, at the level's own origin. Without this it is drawn
      // sweeping in from there over every frame the dash's own hitstop stretches this tick into.
      streak.ResetPhysicsInterpolation();
      streak.Follow(player, forward);
    }
  }

  // The trail belongs behind the cube it came off, and between siblings that is nothing more
  // than child order.
  private static void _addBehindPlayer(Player player, Node2D node) {
    var parent = player.GetParent();
    parent.AddChild(node);
    parent.MoveChild(node, player.GetIndex());
  }
}
