namespace Wfc.Entities.World.Player;

using Godot;
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

  // The cube snaps into the bullet shape, holds it for as long as it is actually travelling
  // and only relaxes on the way out. Releasing it any earlier leaves a square cube crossing
  // the screen at full speed.
  private const float DEFORM_ATTACK = 0.15f;
  private const float DEFORM_RELEASE = 0.65f;

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

  // Called the moment the dash commits to a direction, which is later than the state is
  // entered: a dash still inside its permissiveness window has nothing to draw yet.
  public static void Begin(Player player, Vector2 direction, float travel) {
    player.HitstopNode.Start(HITSTOP_DURATION, HITSTOP_TIME_SCALE);
    _spawnStreaks(player, direction, travel);
  }

  public static void Step(Player player, Vector2 direction, float elapsed, float duration) {
    _setDeform(player, direction, _deformStrength(elapsed / Mathf.Max(duration, MathUtils.EPSILON)));
  }

  public static void End(Player player) => _setDeform(player, Vector2.Right, 0.0f);

  private static void _setDeform(Player player, Vector2 direction, float strength) {
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
    return 1.0f - ((clamped - DEFORM_RELEASE) / (1.0f - DEFORM_RELEASE));
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
