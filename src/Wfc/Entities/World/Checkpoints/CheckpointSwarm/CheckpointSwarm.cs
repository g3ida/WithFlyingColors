namespace Wfc.Entities.World.Checkpoints;

using System.Collections.Generic;
using Godot;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;

// The only thing that marks a checkpoint. Nothing here says "checkpoint" - a knot of bugs hanging
// in the air is enough to pull a player toward it, and the swarm leaving is the whole of the
// confirmation that the game has been saved.
public partial class CheckpointSwarm : Node2D {
  #region Constants
  private const int FIREFLY_COUNT = 16;

  private const float SWARM_RADIUS = 68.0f;
  // The innermost orbit against the outermost. Kept off zero so no bug hangs dead center, where it
  // would sit still while everything around it moves.
  private const float INNER_RADIUS_FRACTION = 0.4f;
  private const float FLATTEN = 0.72f;

  private const float ANGULAR_SPEED = 1.3f;
  // The outer bugs come round slower than the inner ones, which is what stops the swarm reading as
  // one rigid thing being spun.
  private const float ANGULAR_FALLOFF = 0.45f;

  private const float BREATH_SPEED = 0.7f;
  private const float BOB_SPEED = 1.9f;
  private const float RHYTHM_SPREAD = 0.6f;

  // Spacing successive bugs by the golden angle is what keeps them from lining up into arms, at
  // any count and without a random number anywhere in the swarm.
  private const float GOLDEN_ANGLE = 2.3999632f;

  // The anticipation. Long enough to be seen as the swarm pulling in on itself, short enough that
  // it is over before the player has read it as a pause.
  private const float GATHER_DURATION = 0.16f;
  private const float BURST_STAGGER = 0.018f;

  // The buzz goes with the bugs rather than being cut off with them: it thins out over the escape
  // as they get further away, which is the same thing the distance would have done to it if they
  // could be heard once they were gone.
  private const float BUZZ_FADE = 1.2f;
  private const float BUZZ_SILENT_DB = -40.0f;

  // The player's four faces, so every bug is one flat color off the cube the player is looking
  // at all game.
  private static readonly SkinColor[] FACES = {
    SkinColor.TopFace,
    SkinColor.LeftFace,
    SkinColor.BottomFace,
    SkinColor.RightFace,
  };
  #endregion Constants

  #region Nodes
  [NodePath("Buzz")]
  private AudioStreamPlayer2D _buzzNode = default!;
  #endregion Nodes

  private readonly List<Firefly> _fireflies = new(FIREFLY_COUNT);
  private float _buzzVolumeDb;
  private Tween? _buzzFade;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _buzzVolumeDb = _buzzNode.VolumeDb;
    _keepOwnProportions();
    var skin = SkinManager.Instance.CurrentSkin;
    for (var i = 0; i < FIREFLY_COUNT; i++) {
      var face = FACES[i % FACES.Length];
      var firefly = SceneHelpers.InstantiateNode<Firefly>();
      firefly.Configure(
        skin.GetColor(face, SkinColorIntensity.Basic),
        skin.GetColor(face, SkinColorIntensity.Light),
        _orbitFor(i)
      );
      AddChild(firefly);
      _fireflies.Add(firefly);
    }
  }

  public void Scatter(Vector2 startledFrom) {
    var local = ToLocal(startledFrom);
    for (var i = 0; i < _fireflies.Count; i++) {
      _fireflies[i].Disperse(GATHER_DURATION + (i * BURST_STAGGER), local);
    }
    _fadeBuzzOut();
  }

  // A level stretches a checkpoint's trigger into a column by scaling the area itself, and a swarm
  // that took that scale with it would be drawn as a smear. The bugs are the same bugs whatever
  // shape the trigger around them was given.
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

  // Whether there is a swarm here at all, asked whenever the level is put back to what the save
  // says: a checkpoint already taken never gets one, and one the save has not taken keeps it even
  // if the player has watched it leave since.
  public void Settle(bool taken) {
    foreach (var firefly in _fireflies) {
      if (taken) {
        firefly.Extinguish();
      }
      else {
        firefly.Relight();
      }
    }

    _buzzFade?.Kill();
    _buzzFade = null;
    _buzzNode.VolumeDb = _buzzVolumeDb;
    if (taken) {
      _buzzNode.Stop();
    }
    else if (!_buzzNode.Playing) {
      // Somewhere into the loop rather than the top of it, so two checkpoints in earshot of each
      // other are not the same eight seconds twice.
      _buzzNode.Play(_buzzOffset());
    }
  }

  private void _fadeBuzzOut() {
    _buzzFade?.Kill();
    _buzzFade = CreateTween();
    _buzzFade.TweenProperty(_buzzNode, "volume_db", BUZZ_SILENT_DB, BUZZ_FADE);
    _buzzFade.TweenCallback(Callable.From(_buzzNode.Stop));
  }

  // Off the checkpoint's own place in the level, so it is the same offset every run rather than
  // something the swarm has to be seeded with.
  private float _buzzOffset() {
    var stream = _buzzNode.Stream;
    if (stream is null) {
      return 0.0f;
    }
    var length = (float)stream.GetLength();
    return length <= 0.0f ? 0.0f : Mathf.PosMod(Mathf.Abs(GlobalPosition.X + GlobalPosition.Y), length);
  }

  private static Firefly.Orbit _orbitFor(int index) {
    var t = (index + 0.5f) / FIREFLY_COUNT;
    // Square-rooted so the bugs spread evenly over the area of the swarm instead of crowding its
    // middle, which is where an even spread of radii would put most of them.
    var radius = SWARM_RADIUS * Mathf.Lerp(INNER_RADIUS_FRACTION, 1.0f, Mathf.Sqrt(t));
    // Adjacent orbits run opposite ways, so bugs cross each other rather than shoaling.
    var direction = index % 2 == 0 ? 1.0f : -1.0f;
    return new Firefly.Orbit(
      Radius: radius,
      AngularSpeed: direction * ANGULAR_SPEED * (1.0f - (ANGULAR_FALLOFF * t)),
      Flatten: FLATTEN,
      Phase: index * GOLDEN_ANGLE,
      BreathSpeed: BREATH_SPEED * (1.0f + (RHYTHM_SPREAD * t)),
      BobSpeed: BOB_SPEED * (1.0f + (RHYTHM_SPREAD * (1.0f - t)))
    );
  }
}
