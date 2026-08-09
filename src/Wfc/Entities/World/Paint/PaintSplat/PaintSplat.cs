namespace Wfc.Entities.World.Paint;

using Godot;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;

// What a dropped bucket leaves behind: a run of paint lying along the surface it broke over, in
// the bucket's colour, which from then on is a surface of that colour - the cube crosses it on
// the matching face or dies on it, exactly as it would a platform painted the same way.
//
// The node is placed by the point of impact, with its origin on the surface: the paint pools
// below the origin the way it would on top of a platform, and the fingers run off the underside.
//
// The colour it claims is a little narrower than the paint it draws. The pool thins out to
// nothing at both ends, and a cube dying against a film of paint too thin to see reads as dying
// to nothing at all - so the last of it is left as decoration.
[ScenePath]
public partial class PaintSplat : Node2D {
  #region Constants
  private static readonly StringName SizeParam = "u_size";
  private static readonly StringName PoolParam = "u_pool";
  private static readonly StringName ReachParam = "u_reach";
  private static readonly StringName SpreadParam = "u_spread";
  private static readonly StringName RunParam = "u_run";
  private static readonly StringName SeedParam = "u_seed";
  private static readonly StringName ColorParam = "u_color";
  private static readonly StringName ShadeParam = "u_shade";

  // How deep the paint lies over the surface, and how far past it the longest finger runs. Both
  // are the shader's, and the pool is also what the cube walks on, so the colour area is cut to
  // the same depth.
  public const float POOL_DEPTH = 36f;
  public const float FINGER_REACH = 110f;

  private const float CLAIMED_WIDTH = 0.9f;

  // The paint is thrown out along the surface and only then starts to run off it.
  private const float SPREAD_DURATION = 0.26f;
  private const float RUN_DELAY = 0.06f;
  private const float RUN_DURATION = 0.95f;

  private const float DROPLET_SPEED_PER_WIDTH = 0.9f;

  private const SkinColorIntensity PAINT = SkinColorIntensity.Basic;
  private const SkinColorIntensity PAINT_SHADE = SkinColorIntensity.Dark;
  #endregion Constants

  #region Nodes
  [NodePath("Paint")]
  private ColorRect _paintNode = default!;
  [NodePath("Area2D")]
  private Area2D _areaNode = default!;
  [NodePath("Area2D/ColorAreaShape")]
  private CollisionShape2D _colorAreaShapeNode = default!;
  [NodePath("Droplets")]
  private CpuParticles2D _dropletsNode = default!;
  #endregion Nodes

  private string _group = "purple";
  private float _width = 256f;
  private bool _dried;

  // Called before the splat is in the tree, so what it is told is kept until _Ready has the
  // nodes to put it on.
  //
  // Paint put back from a saved game is already dry: it was thrown long before the game was
  // last closed, and playing the throw again on load would have the room splash itself as the
  // player arrives in it.
  public void Setup(string colorGroup, float width, bool dried = false) {
    _group = colorGroup;
    _width = width;
    _dried = dried;
  }

  public string Group => _group;

  public float Width => _width;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();

    // The paint starts exactly on the surface it landed on: a pool drawn even a few pixels above
    // it stands on a step of its own, and reads as a band laid over the platform rather than as
    // paint lying on it.
    var height = POOL_DEPTH + FINGER_REACH;
    _paintNode.Position = new Vector2(-_width / 2f, 0f);
    _paintNode.Size = new Vector2(_width, height);

    var skinColor = GameSkin.ColorGroupToSkinColor(_group);
    var color = SkinManager.Instance.CurrentSkin.GetColor(skinColor, PAINT);

    if (_paintNode.Material is ShaderMaterial material) {
      material.SetShaderParameter(SizeParam, new Vector2(_width, height));
      material.SetShaderParameter(PoolParam, POOL_DEPTH);
      material.SetShaderParameter(ReachParam, FINGER_REACH);
      material.SetShaderParameter(ColorParam, color);
      material.SetShaderParameter(ShadeParam, SkinManager.Instance.CurrentSkin.GetColor(skinColor, PAINT_SHADE));
      material.SetShaderParameter(SeedParam, GD.Randf() * 100f);
      material.SetShaderParameter(SpreadParam, 0f);
      material.SetShaderParameter(RunParam, 0f);
    }

    if (_colorAreaShapeNode.Shape is RectangleShape2D rectangle) {
      rectangle.Size = new Vector2(_width * CLAIMED_WIDTH, POOL_DEPTH);
    }
    _colorAreaShapeNode.Position = new Vector2(0f, POOL_DEPTH / 2f);
    _areaNode.AddToGroup(_group);
    // Paint that is still in the air has not landed on anything: the area opens when the throw
    // is over, which is also what lets it catch a cube standing where the bucket was aimed.
    _areaNode.Monitorable = false;

    _dropletsNode.Color = color;
    _dropletsNode.InitialVelocityMax = _width * DROPLET_SPEED_PER_WIDTH;
    _dropletsNode.InitialVelocityMin = _dropletsNode.InitialVelocityMax * 0.35f;

    if (_dried) {
      _settled();
      return;
    }
    _dropletsNode.Emitting = true;
    _play();
  }

  // Where the throw would have ended: fully spread, fully run off, and lethal from the outset.
  private void _settled() {
    if (_paintNode.Material is ShaderMaterial material) {
      material.SetShaderParameter(SpreadParam, 1f);
      material.SetShaderParameter(RunParam, 1f);
    }
    _areaNode.Monitorable = true;
  }

  private void _play() {
    if (_paintNode.Material is not ShaderMaterial material) {
      return;
    }

    var tween = CreateTween();
    tween.SetParallel(true);
    tween.TweenMethod(
        Callable.From((float value) => material.SetShaderParameter(SpreadParam, value)), 0f, 1f, SPREAD_DURATION)
      .SetTrans(Tween.TransitionType.Quint)
      .SetEase(Tween.EaseType.Out);
    tween.TweenMethod(
        Callable.From((float value) => material.SetShaderParameter(RunParam, value)), 0f, 1f, RUN_DURATION)
      .SetTrans(Tween.TransitionType.Cubic)
      .SetEase(Tween.EaseType.Out)
      .SetDelay(RUN_DELAY);
    tween.TweenCallback(Callable.From(() => _areaNode.Monitorable = true))
      .SetDelay(SPREAD_DURATION);
  }
}
