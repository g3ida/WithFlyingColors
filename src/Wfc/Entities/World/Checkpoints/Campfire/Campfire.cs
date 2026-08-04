namespace Wfc.Entities.World.Checkpoints;

using Godot;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Layers;

// What a checkpoint looks like from across the level: a fire burning in the colour of the group it
// saves, with the swarm circling the light it throws. Taking the checkpoint puts the fire out, and
// that going-out is the whole of the confirmation the player gets that the game has been saved.
public partial class Campfire : Node2D {
  #region Constants
  // The stones are drawn nearly white so that whatever burns between them is what colours them.
  // Cold, they fall back to the grey of a fire nobody has tended.
  private static readonly Color STONE_COLD = new(0.4f, 0.41f, 0.47f);
  private const float STONE_LIT_TINT = 0.18f;
  private const float STONE_LIT_GAIN = 1.15f;
  private const float STONE_FLASH_GAIN = 1.8f;

  // Two wobbles at frequencies that never come back into step, so the fire never flickers on a
  // beat the player could count.
  private const float FLICKER_BASE = 0.84f;
  private const float FLICKER_AMOUNT = 0.16f;
  private const float FLICKER_SPEED = 7.3f;
  private const float FLICKER_RATIO = 2.7f;

  private const float GLOW_SCALE_BASE = 0.88f;
  private const float GLOW_SCALE_PULSE = 0.12f;

  // A fire is never quite still. The whole flame leans one way and comes back, so the tongues are
  // not just wandering inside a column that stands where it was put.
  private const float SWAY_AMOUNT = 55.0f;
  private const float SWAY_SPEED = 0.9f;
  private const float SWAY_RATIO = 2.3f;
  private const float CORE_SWAY_FRACTION = 0.6f;

  // The fire flares before it dies. It is the one moment the player is meant to look at, and a
  // fire that only faded would be over before it had been noticed.
  private const float SNUFF_FLARE = 2.1f;
  private const float SNUFF_FLARE_TIME = 0.07f;
  private const float SNUFF_FADE = 0.55f;
  private const float STONE_COOL_TIME = 1.1f;

  // The stones take the knock of the fire going out and settle back under it.
  private const float STONE_KNOCK = 1.09f;
  private const float STONE_SETTLE_TIME = 0.45f;

  // What is left burning when the checkpoint is taken gets pulled up and away from whoever walked
  // into it, so the fire reads as blown out by the player going through rather than switched off.
  private const float BLOWOUT_UPDRAFT = 950.0f;
  private const float BLOWOUT_DRIFT = 280.0f;

  // A checkpoint is placed where it is convenient to take, which is rarely where a fire could
  // stand. The probe starts a little above so a fire whose stones were pushed into the floor comes
  // back up onto it, and gives up rather than dropping the fire down a shaft the checkpoint was
  // only ever meant to hang over.
  private const float GROUND_PROBE_RISE = 40.0f;
  private const float GROUND_PROBE_DROP = 700.0f;

  // The crackle goes with the fire rather than being cut off with it, the way the swarm's buzz
  // leaves with the swarm.
  private const float CRACKLE_FADE = 0.9f;
  private const float CRACKLE_SILENT_DB = -40.0f;
  #endregion Constants

  #region Nodes
  [NodePath("Light")]
  private PointLight2D _lightNode = default!;
  [NodePath("Glow")]
  private Sprite2D _glowNode = default!;
  [NodePath("Flame")]
  private CpuParticles2D _flameNode = default!;
  [NodePath("Core")]
  private CpuParticles2D _coreNode = default!;
  [NodePath("Stones")]
  private Sprite2D _stonesNode = default!;
  [NodePath("Sparks")]
  private CpuParticles2D _sparksNode = default!;
  [NodePath("Smoke")]
  private CpuParticles2D _smokeNode = default!;
  [NodePath("Burst")]
  private CpuParticles2D _burstNode = default!;
  [NodePath("Crackle")]
  private AudioStreamPlayer2D _crackleNode = default!;
  [NodePath("Swarm")]
  private CheckpointSwarm _swarmNode = default!;
  #endregion Nodes

  #region Exports
  // Off for a fire the level wants left hanging where it was put - over a pit, or on a ledge the
  // floor below it is not.
  [Export]
  public bool StandOnGround { get; set; } = true;
  #endregion Exports

  #region Fields
  private Color _fireColor = Colors.White;
  private Color _stoneLit = Colors.White;
  private Color _stoneFlash = Colors.White;
  private Vector2 _flameGravity;
  private Vector2 _coreGravity;
  private Vector2 _glowScale;
  private Vector2 _stoneScale;
  private float _lightEnergy;
  private float _glowAlpha;

  private float _crackleVolumeDb;
  private bool _isLit = true;
  private float _fire = 1.0f;
  private float _time;
  private Tween? _snuff;
  private Tween? _crackleFade;
  #endregion Fields

  public override void _EnterTree() {
    base._EnterTree();
    _keepOwnProportions();
  }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _lightEnergy = _lightNode.Energy;
    _glowAlpha = _glowNode.SelfModulate.A;
    _glowScale = _glowNode.Scale;
    _stoneScale = _stonesNode.Scale;
    _flameGravity = _flameNode.Gravity;
    _coreGravity = _coreNode.Gravity;
    _crackleVolumeDb = _crackleNode.VolumeDb;
  }

  // Where the fire ended up, in the checkpoint's own frame, so the trigger around it can be stood
  // on the same floor. Asked for once, on the first physics frame: nothing has stepped the physics
  // world while the level was still being built, and a ray cast into it before then finds nothing.
  public float SettleOnGround() {
    if (!StandOnGround) {
      return Position.Y;
    }
    var query = PhysicsRayQueryParameters2D.Create(
      GlobalPosition + (Vector2.Up * GROUND_PROBE_RISE),
      GlobalPosition + (Vector2.Down * GROUND_PROBE_DROP),
      PhysicsLayers.Default.Mask
    );
    query.CollideWithAreas = false;
    var hit = GetWorld2D().DirectSpaceState.IntersectRay(query);
    if (hit.Count > 0) {
      GlobalPosition = new Vector2(GlobalPosition.X, ((Vector2)hit["position"]).Y);
    }
    return Position.Y;
  }

  // Whether there is a fire here at all, asked whenever the level is put back to what the save
  // says. A checkpoint already taken is cold from the first frame - nobody was here to watch that
  // one go out.
  public void Settle(string colorGroup, bool taken) {
    _paint(colorGroup);
    if (taken) {
      _goCold();
    }
    else {
      _relight();
    }
    _swarmNode.Settle(taken);
  }

  // The checkpoint has just been taken, with the player still coming through the fire.
  public void Snuff(Vector2 startledFrom) {
    _swarmNode.Scatter(startledFrom);
    if (!_isLit) {
      return;
    }
    _isLit = false;

    var blown = new Vector2(
      Mathf.Sign(GlobalPosition.X - startledFrom.X) * BLOWOUT_DRIFT,
      -BLOWOUT_UPDRAFT
    );
    _flameNode.Gravity = blown;
    _coreNode.Gravity = blown;
    _flameNode.Emitting = false;
    _coreNode.Emitting = false;
    _sparksNode.Emitting = false;
    _burstNode.Restart();
    _smokeNode.Restart();
    _fadeCrackleOut();

    _snuff?.Kill();
    _snuff = CreateTween();
    _snuff.TweenMethod(Callable.From<float>(_setFire), 1.0f, SNUFF_FLARE, SNUFF_FLARE_TIME)
      .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
    _snuff.Parallel().TweenProperty(_stonesNode, "self_modulate", _stoneFlash, SNUFF_FLARE_TIME);
    _snuff.Parallel().TweenProperty(_stonesNode, "scale", _stoneScale * STONE_KNOCK, SNUFF_FLARE_TIME);
    _snuff.TweenMethod(Callable.From<float>(_setFire), SNUFF_FLARE, 0.0f, SNUFF_FADE)
      .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
    _snuff.Parallel().TweenProperty(_stonesNode, "self_modulate", STONE_COLD, STONE_COOL_TIME);
    _snuff.Parallel().TweenProperty(_stonesNode, "scale", _stoneScale, STONE_SETTLE_TIME)
      .SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
    _snuff.TweenCallback(Callable.From(_settleCold));
  }

  public override void _Process(double delta) {
    _time += (float)delta;
    var flicker = FLICKER_BASE
      + (FLICKER_AMOUNT * Mathf.Sin(_time * FLICKER_SPEED))
      + (FLICKER_AMOUNT * 0.5f * Mathf.Sin(_time * FLICKER_SPEED * FLICKER_RATIO));
    var lit = _fire * flicker;

    if (_isLit) {
      var sway = SWAY_AMOUNT * ((0.7f * Mathf.Sin(_time * SWAY_SPEED)) + (0.3f * Mathf.Sin(_time * SWAY_SPEED * SWAY_RATIO)));
      _flameNode.Gravity = new Vector2(sway, _flameGravity.Y);
      _coreNode.Gravity = new Vector2(sway * CORE_SWAY_FRACTION, _coreGravity.Y);
    }

    _lightNode.Energy = _lightEnergy * lit;
    _glowNode.SelfModulate = new Color(_fireColor, _glowAlpha * lit);
    _glowNode.Scale = _glowScale * (GLOW_SCALE_BASE + (GLOW_SCALE_PULSE * lit));
  }

  private void _setFire(float amount) => _fire = amount;

  // Every part of the fire is one of the four colours the player is looking at all game, taken
  // from the group this checkpoint saves. The overlap of the two flames is what burns white in the
  // middle, so nothing here has to be told to be hot.
  private void _paint(string colorGroup) {
    if (string.IsNullOrEmpty(colorGroup)) {
      return;
    }
    var skin = SkinManager.Instance.CurrentSkin;
    var face = GameSkin.ColorGroupToSkinColor(colorGroup);
    _fireColor = skin.GetColor(face, SkinColorIntensity.Basic);

    _flameNode.Color = _fireColor;
    _coreNode.Color = skin.GetColor(face, SkinColorIntensity.Background);
    _sparksNode.Color = skin.GetColor(face, SkinColorIntensity.VeryLight);
    _burstNode.Color = skin.GetColor(face, SkinColorIntensity.Light);
    _lightNode.Color = skin.GetColor(face, SkinColorIntensity.Light);
    _smokeNode.Color = new Color(0.16f, 0.16f, 0.2f, 0.5f);

    _stoneLit = _tinted(STONE_LIT_GAIN);
    _stoneFlash = _tinted(STONE_FLASH_GAIN);
  }

  private Color _tinted(float gain) {
    var lit = STONE_COLD.Lerp(_fireColor, STONE_LIT_TINT);
    return new Color(lit.R * gain, lit.G * gain, lit.B * gain);
  }

  private void _relight() {
    _snuff?.Kill();
    _snuff = null;
    _isLit = true;
    _fire = 1.0f;
    _flameNode.Gravity = _flameGravity;
    _coreNode.Gravity = _coreGravity;
    _flameNode.Restart();
    _coreNode.Restart();
    _sparksNode.Restart();
    _smokeNode.Emitting = false;
    _burstNode.Emitting = false;
    _flameNode.Visible = true;
    _coreNode.Visible = true;
    _sparksNode.Visible = true;
    _stonesNode.SelfModulate = _stoneLit;
    _stonesNode.Scale = _stoneScale;
    _lightNode.Visible = true;
    _glowNode.Visible = true;
    _crackleFade?.Kill();
    _crackleFade = null;
    _crackleNode.VolumeDb = _crackleVolumeDb;
    if (!_crackleNode.Playing) {
      // Somewhere into the loop rather than the top of it, so two fires within earshot of each
      // other are not the same minute twice.
      _crackleNode.Play(_crackleOffset());
    }
    SetProcess(true);
  }

  private void _goCold() {
    _snuff?.Kill();
    _snuff = null;
    _isLit = false;
    _fire = 0.0f;
    _stonesNode.SelfModulate = STONE_COLD;
    _stonesNode.Scale = _stoneScale;
    _smokeNode.Emitting = false;
    _burstNode.Emitting = false;
    _crackleFade?.Kill();
    _crackleFade = null;
    _crackleNode.VolumeDb = _crackleVolumeDb;
    _crackleNode.Stop();
    _settleCold();
  }

  // Nothing is left burning, so there is nothing left to flicker. The emitters keep whatever they
  // still had in the air until they are hidden, which is what lets the snuff run through this.
  private void _settleCold() {
    _flameNode.Emitting = false;
    _coreNode.Emitting = false;
    _sparksNode.Emitting = false;
    _flameNode.Visible = false;
    _coreNode.Visible = false;
    _sparksNode.Visible = false;
    _lightNode.Visible = false;
    _glowNode.Visible = false;
    SetProcess(false);
  }

  private void _fadeCrackleOut() {
    _crackleFade?.Kill();
    _crackleFade = CreateTween();
    _crackleFade.TweenProperty(_crackleNode, "volume_db", CRACKLE_SILENT_DB, CRACKLE_FADE);
    _crackleFade.TweenCallback(Callable.From(_crackleNode.Stop));
  }

  // Off the fire's own place in the level, so it is the same offset every run rather than
  // something the campfire has to be seeded with.
  private float _crackleOffset() {
    var stream = _crackleNode.Stream;
    if (stream is null) {
      return 0.0f;
    }
    var length = (float)stream.GetLength();
    return length <= 0.0f ? 0.0f : Mathf.PosMod(Mathf.Abs(GlobalPosition.X + GlobalPosition.Y), length);
  }

  // A level stretches a checkpoint's trigger into a column by scaling the area itself, and a fire
  // that took that scale with it would be drawn as a smear. Done on the way into the tree so the
  // swarm hanging under this one already reads a parent that has given the scale back.
  private void _keepOwnProportions() {
    if (GetParent() is not Node2D parent) {
      return;
    }
    var inherited = parent.GlobalScale;
    Scale = new Vector2(
      Mathf.IsZeroApprox(inherited.X) ? 1.0f : 1.0f / inherited.X,
      Mathf.IsZeroApprox(inherited.Y) ? 1.0f : 1.0f / inherited.Y
    );
  }
}
