namespace Wfc.Entities.World.Enemies;

using Godot;
using Wfc.Entities.World;
using Wfc.Entities.World.Player;
using Wfc.Screens.Levels;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Layers;
using EventHandler = Wfc.Core.Event.EventHandler;

[Tool]
[ScenePath]
public partial class LazerBeam : Node2D {
  // Telegraph shows where the beam will land without burning anything - the
  // warning a timed beam gives before it turns back on.
  public enum BeamState { Off, Telegraph, On }

  #region Nodes
  [NodePath("Line2D")]
  private Line2D beamNode = default!;
  [NodePath("Line2DBackground")]
  private Line2D beamBgNode = default!;
  [NodePath("Muzzle")]
  private Marker2D muzzleNode = default!;
  [NodePath("Particles")]
  private CpuParticles2D particlesNode = default!;
  [NodePath("Base")]
  private Sprite2D baseNode = default!;
  [NodePath("MuzzleGlow")]
  private Sprite2D _muzzleGlowNode = default!;
  [NodePath("ImpactGlow")]
  private Sprite2D _impactGlowNode = default!;
  [NodePath("AudioStreamPlayer2D")]
  private AudioStreamPlayer2D _audioNode = default!;
  #endregion Nodes

  [Export]
  public string ColorGroup { get; set; } = "blue";

  public BeamState State { get; private set; } = BeamState.On;

  private bool _wasBurning;

  private const float TELEGRAPH_WIDTH_FACTOR = 0.2f;
  private const float TELEGRAPH_ALPHA = 0.4f;
  private const float GLOW_PULSE_AMOUNT = 0.15f;
  private const float GLOW_PULSE_SPEED = 9.0f;

  private float _bgBeamWidth;
  private Vector2 _muzzleGlowBaseScale;
  private Vector2 _impactGlowBaseScale;
  private float _time;
  private Vector2 _beamLocalEnd;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();

    Color color = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(ColorGroup),
      SkinColorIntensity.Basic
    );
    Color darkColor = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(ColorGroup),
      SkinColorIntensity.Dark
    );
    beamNode.DefaultColor = color;
    beamBgNode.DefaultColor = color;
    beamBgNode.DefaultColor = new Color(beamBgNode.DefaultColor, 0.63f);
    particlesNode.Color = darkColor;
    baseNode.Modulate = color;
    _muzzleGlowNode.Modulate = color;
    _impactGlowNode.Modulate = color;

    _bgBeamWidth = beamBgNode.Width;
    _muzzleGlowBaseScale = _muzzleGlowNode.Scale;
    _impactGlowBaseScale = _impactGlowNode.Scale;
    // The scene's defaults already show the beam firing, and applying the state
    // in the editor would start the audio there.
    if (!Engine.IsEditorHint()) {
      _applyBeamState();
    }
  }

  public void SetBeamState(BeamState state) {
    if (State == state) {
      return;
    }
    State = state;
    // A face already standing in the path when the beam turns back on is a
    // fresh contact, not a continuation of the one before the rest.
    _wasBurning = false;
    _applyBeamState();
  }

  private void _applyBeamState() {
    var firing = State == BeamState.On;
    beamNode.Visible = firing;
    beamBgNode.Visible = State != BeamState.Off;
    beamBgNode.Width = firing ? _bgBeamWidth : _bgBeamWidth * TELEGRAPH_WIDTH_FACTOR;
    beamBgNode.Modulate = firing ? Colors.White : new Color(1f, 1f, 1f, TELEGRAPH_ALPHA);
    particlesNode.Emitting = firing;
    _muzzleGlowNode.Visible = firing;
    _impactGlowNode.Visible = firing;
    if (firing && !_audioNode.Playing) {
      _audioNode.Play();
    }
    else if (!firing && _audioNode.Playing) {
      _audioNode.Stop();
    }
  }

  // A perfectly steady glow reads as a sticker; a slight counter-phased
  // breathing on the two endpoints sells the beam as live energy.
  private void _pulseGlows(float delta) {
    _time += delta;
    var pulse = GLOW_PULSE_AMOUNT * Mathf.Sin(_time * GLOW_PULSE_SPEED);
    _muzzleGlowNode.Scale = _muzzleGlowBaseScale * (1.0f - pulse);
    _impactGlowNode.Scale = _impactGlowBaseScale * (1.0f + pulse);
  }

  private const float BEAM_RANGE = 1000.0f;

  // How far into its own mount the beam senses faces.
  //
  // A beam sits flush on the surface it fires from, and a player's faces reach slightly inside
  // whatever they rest on - that overlap is how standing on a colored surface is detected at all.
  // So the face presented to the beam is behind the muzzle, where no ray cast from the muzzle can
  // see it, and the beam answers with the face on the far side of the cube instead.
  //
  // Must exceed how far a face reaches past the surface it rests on, and stay under how deep the
  // emitter may reach into its mount, or a face on the far side of a thin one answers instead.
  // Reaching back is otherwise free: the query takes areas on the face layer only, and no face is
  // ever inside solid ground.
  private const float MOUNT_CLEARANCE = 24.0f;

  // What stops the beam: the solid world. The player is a body too, but the cube's hull is
  // not what the beam cares about - the color it presents is, and that lives on the face
  // areas, which a bodies-only ray cannot see.
  private static readonly uint SOLID_MASK = PhysicsLayers.Default.Mask | PhysicsLayers.Platform.Mask;

  public PhysicsDirectSpaceState2D SpaceState => GetWorld2D().DirectSpaceState;

  private static readonly StringName _positionKey = "position";
  private static readonly StringName _colliderKey = "collider";

  // One query object per cast, reused: the beam fires both every physics tick for as long as
  // it exists, and building them fresh allocates engine objects every one of those ticks.
  private PhysicsRayQueryParameters2D? _worldQuery;
  private PhysicsRayQueryParameters2D? _faceQuery;

  // Normalized, because Transform.X carries the node's scale and a scaled beam would otherwise
  // read as a longer or shorter one.
  private Vector2 _beamDirection => Transform.X.Normalized();

  // How far the solid world lets the beam travel. Bodies only, so a trigger volume or a gem in
  // the way does not cut the beam short.
  private Vector2 _castToWorld() {
    var from = muzzleNode.GlobalPosition;
    var to = from + _beamDirection * BEAM_RANGE;

    _worldQuery ??= new PhysicsRayQueryParameters2D { CollisionMask = SOLID_MASK };
    _worldQuery.From = from;
    _worldQuery.To = to;
    using var result = SpaceState.IntersectRay(_worldQuery);

    // IntersectRay returns an empty dictionary when it hits nothing, so the endpoint has to
    // be defaulted rather than read out of it - this used to throw every physics frame the
    // beam pointed at open sky.
    return result.Count > 0 ? (Vector2)result[_positionKey] : to;
  }

  // The face the beam lands on and where it lands, if it reaches one before the world stops it.
  // Areas only and masked to the face layer: the default Create() leaves CollideWithAreas false,
  // which is why `collider is BoxFace` could never be true and the beam has been decorative since
  // the port.
  private (BaseFace Face, Vector2 Position)? _castToFace(Vector2 worldEnd) {
    var from = muzzleNode.GlobalPosition - _beamDirection * MOUNT_CLEARANCE;
    _faceQuery ??= new PhysicsRayQueryParameters2D {
      CollisionMask = PhysicsLayers.BoxFace.Mask,
      CollideWithBodies = false,
      CollideWithAreas = true,
    };
    _faceQuery.From = from;
    _faceQuery.To = worldEnd;
    using var result = SpaceState.IntersectRay(_faceQuery);

    // An empty dictionary means the ray reached the end of the segment, and indexing a Godot
    // dictionary that does not hold the key throws rather than handing back nil.
    if (result.Count == 0 || result[_colliderKey].As<Node>() is not BaseFace face) {
      return null;
    }
    return (face, (Vector2)result[_positionKey]);
  }

  // The beam is absorbed by whatever it lands on, so it is drawn only that far. A face resting on
  // the mount lands behind the emitter: clamping reads that as the beam blocked at its base, which
  // is what standing on the emitter looks like.
  private void _drawBeamTo(Vector2 endPoint) {
    var from = muzzleNode.GlobalPosition;
    var along = Mathf.Max((endPoint - from).Dot(_beamDirection), 0.0f);
    var localEnd = (from + _beamDirection * along) * Transform;

    beamNode.SetPointPosition(1, localEnd);
    beamBgNode.SetPointPosition(1, localEnd);
    particlesNode.Position = localEnd;
    _impactGlowNode.Position = localEnd;
    _beamLocalEnd = localEnd;
  }

  // The hum belongs to the beam, not to its emitter: the audio player sits on
  // whatever point of the beam is nearest the player, so standing anywhere
  // along a long beam sounds like standing next to it.
  private void _placeAudioAlongBeam() {
    var player = GameRepo.Instance.Player.Value;
    if (player is null || !IsInstanceValid(player) || !player.IsInsideTree()) {
      return;
    }
    var from = muzzleNode.Position;
    var span = _beamLocalEnd - from;
    var lengthSquared = span.LengthSquared();
    if (lengthSquared <= Mathf.Epsilon) {
      _audioNode.Position = from;
      return;
    }
    var t = Mathf.Clamp((ToLocal(player.GlobalPosition) - from).Dot(span) / lengthSquared, 0f, 1f);
    _audioNode.Position = from + (span * t);
  }

  public override void _PhysicsProcess(double delta) {
    if (Engine.IsEditorHint() || State == BeamState.Off) {
      return;
    }

    var worldEnd = _castToWorld();
    var hit = _castToFace(worldEnd);

    // Ending on the player, not on the wall behind them.
    _drawBeamTo(hit?.Position ?? worldEnd);
    _pulseGlows((float)delta);
    _placeAudioAlongBeam();

    // A telegraphed beam shows where it will land but burns nothing yet.
    if (State != BeamState.On) {
      return;
    }

    // The beam crossing a face it burns is one event, not one per frame it goes on crossing it.
    var burns = hit is { } landing && !landing.Face.AcceptsColor(ColorGroup);
    if (burns && !_wasBurning) {
      EventHandler.Instance.EmitPlayerDying(GlobalPosition, EntityType.Lazer);
    }
    _wasBurning = burns;
  }
}
