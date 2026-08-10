namespace Wfc.Entities.World.Player;

using System.Collections.Generic;
using Godot;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;

// The discharge that jumps between the cube and a color it must not touch. It is drawn in the
// hazard's color alone - which face is charged is already plain from where the wires leave the
// cube, and carrying the face's color along them as well read as a third color that is not in the
// game anywhere else. Several thin wires leave at once rather than one bolt leaving a point: the
// whole face is what is charged.
//
// It fires in bursts rather than burning for as long as the two are close. Standing still beside a
// hazard is not news: a burst needs the cube to be moving, and dies the moment it stops.
[ScenePath]
public partial class ColorArc : Node2D {
  #region Constants
  // How far past the cube's own surface an area still draws an arc. Short of a tetris cell on
  // purpose: the arc is about a hazard the cube is nearly touching, and further out it fires at
  // things the player was never going to hit. The sensor's shape has to cover the cube's half
  // diagonal on top of this.
  private const float REACH = 50.0f;
  // Any closer and the two are touching, where the contact speaks for itself - fatally, most of
  // the time.
  private const float MIN_GAP = 3.0f;
  private const float BURST_DURATION = 0.6f;
  // Silence enforced after a burst, so running past a row of hazards crackles rather than burns.
  private const float QUIET_DURATION = 0.45f;
  // Under this the cube counts as standing still.
  private const float MOVING_SPEED = 30.0f;
  private const float FADE_IN_DURATION = 0.05f;
  private const float FADE_OUT_DURATION = 0.14f;

  private const int WIRES = 3;
  private const int SEGMENTS = 9;
  // How far out along the face the outermost wires leave it, as a fraction of its half length.
  private const float FACE_SPREAD = 0.62f;
  // The far ends gather in much tighter than the near ones: the discharge leaves a whole face and
  // arrives on one spot.
  private const float ARRIVAL_SPREAD = 0.22f;
  // Sideways wander of each wire, as a fraction of the gap it crosses.
  private const float SCATTER = 0.11f;
  private const float SCRAMBLE_INTERVAL = 0.045f;
  #endregion Constants

  #region Exports
  // Off turns the whole thing into a node that costs a branch per frame, so a level or a player
  // that does not want it can say so without the scene having to change shape.
  [Export]
  public bool Enabled { get; set; } = true;

  // How loud the discharge is overall. The two Line2D nodes carry their own alpha on top of this,
  // which is what balances the halo against the wire inside it - this is the one knob that turns
  // the whole effect down without disturbing that balance.
  [Export(PropertyHint.Range, "0,1,0.01")]
  public float Opacity { get; set; } = 0.6f;
  #endregion Exports

  #region Nodes
  [NodePath("Sensor")]
  private Area2D _sensorNode = default!;
  [NodePath("Glow")]
  private Line2D _glowNode = default!;
  [NodePath("Arc")]
  private Line2D _arcNode = default!;
  [NodePath("Sound")]
  private AudioStreamPlayer2D _soundNode = default!;
  #endregion Nodes

  // A colored area within sensor range, with the shape a point has to be found on. Collected as
  // areas come and go rather than polled, so a frame with nothing nearby costs nothing.
  private readonly record struct Candidate(Area2D Area, CollisionShape2D? Shape, string ColorGroup);

  // Where a discharge would run, and along which face of the cube the wires spread to leave it.
  private readonly record struct Contact(
    Area2D Area,
    Vector2 CubePoint,
    Vector2 AreaPoint,
    Vector2 FaceTangent,
    float FaceHalfLength,
    string ColorGroup
  );

  #region Fields
  private readonly List<Candidate> _candidates = new();
  private readonly Line2D[] _glowNodes = new Line2D[WIRES];
  private readonly Line2D[] _arcNodes = new Line2D[WIRES];
  private readonly Vector2[] _points = new Vector2[SEGMENTS + 1];
  private readonly float[] _scatter = new float[WIRES * (SEGMENTS + 1)];
  private readonly RandomNumberGenerator _rng = new();

  private Player _cube = default!;
  private Vector2 _lastCubePosition;
  private Area2D? _target;
  private string? _areaColorGroup;
  private Color _areaColor = Colors.White;
  private float _burstLeft;
  private float _quietLeft;
  private float _sinceScramble;
  private bool _armed = true;
  #endregion Fields

  // Whether an arc is being drawn this frame, and the color it is drawn in.
  public bool IsDischarging => _arcNode.Visible;
  public string? AreaColorGroup => IsDischarging ? _areaColorGroup : null;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _cube = GetParent<Player>();
    _lastCubePosition = _cube.GlobalPosition;

    _buildWires();
    _scramble();
    _setVisible(false);

    _sensorNode.AreaEntered += _onAreaEntered;
    _sensorNode.AreaExited += _onAreaExited;
  }

  // The pair authored in the scene is the first wire; the rest are copies of it, so there is one
  // place to restyle it and the shaders stay shared between all of them.
  private void _buildWires() {
    _glowNodes[0] = _glowNode;
    _arcNodes[0] = _arcNode;
    for (var wire = 1; wire < WIRES; wire++) {
      _glowNodes[wire] = (Line2D)_glowNode.Duplicate();
      _arcNodes[wire] = (Line2D)_arcNode.Duplicate();
      AddChild(_glowNodes[wire]);
      AddChild(_arcNodes[wire]);
    }
  }

  public override void _ExitTree() {
    base._ExitTree();
    _candidates.Clear();
    _target = null;
    _burstLeft = 0.0f;
  }

  private void _onAreaEntered(Area2D area) {
    var colorGroup = ColorUtils.ColorGroupOf(area);
    if (colorGroup is null) {
      return;
    }
    _candidates.Add(new Candidate(area, _shapeOf(area), colorGroup));
  }

  private void _onAreaExited(Area2D area) {
    for (var i = _candidates.Count - 1; i >= 0; i--) {
      if (_candidates[i].Area == area) {
        _candidates.RemoveAt(i);
        return;
      }
    }
  }

  private static CollisionShape2D? _shapeOf(Area2D area) {
    foreach (var child in area.GetChildren()) {
      if (child is CollisionShape2D shape) {
        return shape;
      }
    }
    return null;
  }

  public override void _PhysicsProcess(double delta) {
    base._PhysicsProcess(delta);
    if (!Enabled || !IsInstanceValid(_cube) || _cube.IsDying()) {
      if (IsDischarging) {
        _stop();
      }
      return;
    }

    var step = (float)delta;
    var travelled = (_cube.GlobalPosition - _lastCubePosition).Length();
    _lastCubePosition = _cube.GlobalPosition;
    var moving = travelled > MOVING_SPEED * step;

    var contact = _findContact();
    // A hazard arriving within reach is news even without a pause, which is what makes running
    // into one fire rather than stay dark because the last burst already went off.
    if (contact?.Area != _target) {
      _armed = true;
    }
    _target = contact?.Area;
    if (!moving) {
      _armed = true;
    }

    _advanceBurst(step, moving, contact is not null);

    if (_burstLeft <= 0.0f || contact is null) {
      _setVisible(false);
      return;
    }
    _drawWires(contact.Value, step);
  }

  private void _advanceBurst(float step, bool moving, bool hasContact) {
    if (_burstLeft > 0.0f) {
      _burstLeft = moving && hasContact ? _burstLeft - step : 0.0f;
      if (_burstLeft <= 0.0f) {
        _quietLeft = QUIET_DURATION;
        _soundNode.Stop();
      }
      return;
    }

    _quietLeft = Mathf.Max(_quietLeft - step, 0.0f);
    if (_armed && moving && hasContact && _quietLeft <= 0.0f) {
      _burstLeft = BURST_DURATION;
      _armed = false;
      _soundNode.Play();
    }
  }

  private Contact? _findContact() {
    Contact? best = null;
    var bestGap = float.MaxValue;

    for (var i = _candidates.Count - 1; i >= 0; i--) {
      var candidate = _candidates[i];
      if (!IsInstanceValid(candidate.Area)) {
        _candidates.RemoveAt(i);
        continue;
      }

      // Each end is what decides the other: the area point settles which part of the cube faces
      // it, and that part settles which part of the area is nearest. One pass each is enough.
      var areaPoint = _nearestPointOn(candidate, _cube.GlobalPosition);
      var cubePoint = _cube.ClosestSurfacePoint(areaPoint);
      areaPoint = _nearestPointOn(candidate, cubePoint);

      var gap = cubePoint.DistanceTo(areaPoint);
      if (gap >= bestGap || gap > REACH || gap < MIN_GAP) {
        continue;
      }
      // Asked of the area rather than of the one color name it was read as. A neutral surface is
      // tagged with all four colors at once and answers to every face, so naming its color picks
      // an arbitrary one of them and arcs the cube against something it is perfectly safe on.
      if (_cube.AcceptsColorOfAt(cubePoint, candidate.Area)) {
        continue;
      }

      var face = _faceAt(cubePoint);
      bestGap = gap;
      best = new Contact(
        candidate.Area, cubePoint, areaPoint, face.Tangent, face.HalfLength, candidate.ColorGroup);
    }
    return best;
  }

  // The face a point on the surface sits on, as the direction running along it and how far it
  // reaches either way from the cube's center.
  private (Vector2 Tangent, float HalfLength) _faceAt(Vector2 cubePoint) {
    var half = _cube.GetCollisionHalfExtents();
    var local = (cubePoint - _cube.GlobalPosition).Rotated(-_cube.GlobalRotation);
    // Whichever axis the point is pinned hardest against is the one pointing out of the cube, so
    // the other one runs along the face. A point on a corner is pinned on both and the deeper
    // pin takes it.
    var onSide = Mathf.Abs(local.X) / Mathf.Max(half.X, Mathf.Epsilon)
      >= Mathf.Abs(local.Y) / Mathf.Max(half.Y, Mathf.Epsilon);
    return onSide
      ? (Vector2.Down.Rotated(_cube.GlobalRotation), half.Y)
      : (Vector2.Right.Rotated(_cube.GlobalRotation), half.X);
  }

  private static Vector2 _nearestPointOn(Candidate candidate, Vector2 from) {
    if (candidate.Shape?.Shape is null) {
      return candidate.Area.GlobalPosition;
    }

    var basis = candidate.Shape.GlobalTransform;
    var local = basis.AffineInverse() * from;
    switch (candidate.Shape.Shape) {
      case RectangleShape2D rectangle: {
        var half = rectangle.Size * 0.5f;
        return basis * local.Clamp(-half, half);
      }
      case CircleShape2D circle: {
        var reach = Mathf.Min(local.Length(), circle.Radius);
        return basis * (local.IsZeroApprox() ? Vector2.Zero : local.Normalized() * reach);
      }
      default:
        return basis.Origin;
    }
  }

  private void _drawWires(Contact contact, float step) {
    _sinceScramble += step;
    if (_sinceScramble >= SCRAMBLE_INTERVAL) {
      _sinceScramble = 0.0f;
      _scramble();
    }

    for (var wire = 0; wire < WIRES; wire++) {
      // -1, 0, +1 for three wires: the outermost leave the face at the ends of its spread.
      var side = WIRES == 1 ? 0.0f : ((float)wire / (WIRES - 1) * 2.0f) - 1.0f;
      var offset = contact.FaceTangent * (side * contact.FaceHalfLength * FACE_SPREAD);
      var from = contact.CubePoint + offset;
      var to = contact.AreaPoint + (offset * ARRIVAL_SPREAD);

      var span = to - from;
      var across = span.Orthogonal() * SCATTER;
      var row = wire * (SEGMENTS + 1);
      for (var i = 0; i < _points.Length; i++) {
        var along = (float)i / SEGMENTS;
        _points[i] = from + (span * along) + (across * _scatter[row + i]);
      }
      _glowNodes[wire].Points = _points;
      _arcNodes[wire].Points = _points;
    }

    _tint(contact.ColorGroup);
    _setVisible(true);
  }

  private void _tint(string areaColorGroup) {
    if (areaColorGroup != _areaColorGroup) {
      _areaColorGroup = areaColorGroup;
      _areaColor = SkinManager.Instance.CurrentSkin.GetColor(
        GameSkin.ColorGroupToSkinColor(areaColorGroup), SkinColorIntensity.Basic);
    }

    var elapsed = BURST_DURATION - _burstLeft;
    var fade = Mathf.Clamp(
      Mathf.Min(elapsed / FADE_IN_DURATION, _burstLeft / FADE_OUT_DURATION), 0.0f, 1.0f);
    var color = new Color(_areaColor, fade * Opacity);
    for (var wire = 0; wire < WIRES; wire++) {
      _glowNodes[wire].DefaultColor = color;
      _arcNodes[wire].DefaultColor = color;
    }
  }

  private void _scramble() {
    for (var i = 0; i < _scatter.Length; i++) {
      // Alternating sides make each path zigzag rather than meander, and the envelope pinches it
      // to nothing at both ends so it leaves and lands square on the two surfaces it joins.
      var along = (float)(i % (SEGMENTS + 1)) / SEGMENTS;
      var side = (i & 1) == 0 ? 1.0f : -1.0f;
      _scatter[i] = side * _rng.RandfRange(0.35f, 1.0f) * Mathf.Sin(along * Mathf.Pi);
    }
  }

  private void _stop() {
    _setVisible(false);
    _burstLeft = 0.0f;
    _soundNode.Stop();
  }

  private void _setVisible(bool visible) {
    for (var wire = 0; wire < WIRES; wire++) {
      _glowNodes[wire].Visible = visible;
      _arcNodes[wire].Visible = visible;
    }
  }
}
