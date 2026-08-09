namespace Wfc.Entities.World.Paint;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Screens.Levels;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;
using Wfc.Utils.Layers;
using EventHandler = Wfc.Core.Event.EventHandler;

// A bucket big enough to flood the room it stands in. The cube arriving tips it, and what comes
// out runs downhill ahead of itself - pooling on every step it crosses, pouring off the far side
// of each one, and painting whatever it reaches whichever face was showing. The way out is to
// stay in front of it.
//
// The paint is a row of columns, each holding a depth over the floor beneath it, and the floor is
// read off the level rather than authored here: the flood finds the steps it is poured onto, so
// moving a platform under it changes where it pools without anything else being said.
//
// It is placed by the top-left of the stretch it may cover, which is also where its columns are
// measured from.
[Tool]
[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class PaintFlood : Node2D {
  #region Constants
  // How wide a column of paint is. Narrow enough that the lip of a step falls between two of them
  // rather than being rounded away, wide enough that a room's worth is a few hundred of them.
  private const float COLUMN_WIDTH = 12f;
  private const int MAX_COLUMNS = 640;

  // What gravity does to paint standing on a slope, and how much of its speed it keeps from one
  // tick to the next.
  //
  // The columns carry a speed rather than an amount handed over, and what crosses between two of
  // them is that speed times the depth behind it. A thin sheet running fast and a deep pool barely
  // moving are the same paint at two depths - carrying the amount directly makes the thin sheet
  // stall exactly where it should be racing, which is what leaves a flood looking like a canal.
  private const float GRAVITY = 380f;
  private const float DRAG = 4.2f;

  // The run the paint feels under itself, whatever it is actually standing on. The room is built
  // of level steps, and paint on a level floor is driven by nothing but the weight of its own
  // depth - which weakens as it thins, so a flood left to that alone settles into a puddle before
  // it has crossed the first step. This is the slope the room reads as having, given to the paint
  // rather than to the platforms: tilting the platforms themselves would cost the cube its
  // footing on every one of them, and the paint is the only thing that needed the slope.
  //
  // Against the drag it sets the speed the paint runs at on the flat, which is the speed of the
  // chase.
  private const float DOWNHILL = 405f;

  // The most of itself a column may hand over in one tick. This rather than the drag is what
  // holds a waterfall together: the slope under a column standing at the lip of a step is bounded
  // by nothing, and one that empties past itself in a tick takes the surface negative.
  private const float MAX_HANDOVER = 0.42f;

  // Below this the paint is a film - too thin to be worth drawing, and too thin to drown in.
  private const float MIN_DEPTH = 1.2f;
  private const float LETHAL_DEPTH = 7f;
  private const float HAIRLINE = 0.01f;

  // Paint standing over nothing is paint falling past the level, and paint that reaches the far
  // end of the run has left it. Both drain rather than piling up against the end of the array.
  private const float DRAIN_RATE = 4.2f;

  // How far the bucket goes over, and how long it takes to get there. A quarter turn and no
  // further: a bucket topples over the corner it stands on until it is lying on its side, and
  // anything past that is a bucket driven through the floor it is lying on. The pour starts
  // partway through - paint leaves a bucket as it passes the horizontal, not once it has landed.
  private const float TIP_ANGLE = Mathf.Pi / 2f;
  private const float TIP_DURATION = 0.7f;
  private const float POUR_DELAY = 0.34f;

  private const SkinColorIntensity PAINT = SkinColorIntensity.Basic;
  private const SkinColorIntensity PAINT_SHADE = SkinColorIntensity.Dark;
  #endregion Constants

  #region Dependencies
  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public IGameLevel GameLevel => this.DependOn<IGameLevel>();

  // Held rather than asked for each tick, and allowed to come up empty. A flood opened on its own
  // - the scene previewed, the node dropped somewhere to look at - has nobody to chase and no
  // camera to pull back, which is a preview rather than an error. There is no null fallback to
  // ask for, so the absence is read off the throw.
  public void OnResolved() {
    try {
      _level = GameLevel;
    }
    catch (ProviderNotFoundException) {
      _level = null;
    }
  }
  #endregion Dependencies

  #region Exports
  // The stretch of level the paint may cover, and how far below the origin it looks for something
  // to lie on. A run that ends short of the safe zone is what lets the player reach it.
  [Export]
  public float Width { get; set; } = 2400f;

  [Export]
  public float Depth { get; set; } = 900f;

  [Export(PropertyHint.Enum, "blue,pink,yellow,purple")]
  public string Group { get; set; } = ColorUtils.PURPLE;

  // Where the bucket stands and how wide its mouth is, in the flood's own pixels.
  [Export]
  public Vector2 SpoutOffset { get; set; } = new Vector2(120f, 0f);

  [Export]
  public float SpoutWidth { get; set; } = 96f;

  // How much paint comes out and over how long. Depth per second at the mouth, so a wider mouth
  // pours the same paint over more of the floor rather than more of it.
  [Export]
  public float PourRate { get; set; } = 900f;

  [Export]
  public float PourDuration { get; set; } = 3.5f;

  // What the flood is worth as a chase. Everything about how fast it travels is here, so a room
  // is tuned by this alone once the pour looks right.
  [Export(PropertyHint.Range, "0.2,3,0.05,or_greater")]
  public float Speed { get; set; } = 1f;

  // What the camera pulls back to while the chase is on, so the player can see what is behind
  // them and where they are going. Zero leaves the camera as the room framed it.
  [Export]
  public float CameraZoom { get; set; } = 1.45f;

  // The volume that sets it off, measured from the flood's own origin.
  [Export]
  public Vector2 TriggerOffset { get; set; } = new Vector2(320f, -160f);

  [Export]
  public Vector2 TriggerSize { get; set; } = new Vector2(160f, 320f);
  #endregion Exports

  #region Nodes
  [NodePath("Trigger")]
  private Area2D _triggerNode = default!;
  [NodePath("Trigger/TriggerShape")]
  private CollisionShape2D _triggerShapeNode = default!;
  [NodePath("Spout")]
  private BucketSprite _spoutNode = default!;
  [NodePath("Spray")]
  private CpuParticles2D _sprayNode = default!;
  #endregion Nodes

  #region Fields
  private enum State {
    Waiting,
    Tipping,
    Pouring,
    Draining,
  }

  private State _state = State.Waiting;
  private float _elapsed;
  private float _poured;

  private int _columns;
  private float[] _floor = System.Array.Empty<float>();
  private float[] _height = System.Array.Empty<float>();
  private float[] _velocity = System.Array.Empty<float>();
  private bool[] _isOverVoid = System.Array.Empty<bool>();
  // The drawn surface, smoothed off the simulated one. Kept apart so that smoothing never feeds
  // back into the paint it is smoothing.
  private float[] _drawn = System.Array.Empty<float>();
  private Vector2[] _outline = System.Array.Empty<Vector2>();
  private Color[] _outlineColors = System.Array.Empty<Color>();

  private bool _needsFloor = true;
  private bool _isFrozen;
  private bool _hasCaught;
  private float _zoomBefore;
  private float _mouthX;
  private bool _isSubscribed;
  private PhysicsRayQueryParameters2D? _rayQuery;
  private IGameLevel? _level;
  private Color _paintColor = Colors.White;
  private Color _shadeColor = Colors.Gray;
  #endregion Fields

  public override void _EnterTree() {
    base._EnterTree();
    if (Engine.IsEditorHint() || _isSubscribed) {
      return;
    }
    EventHandler.Instance.Events.CheckpointLoaded += _onCheckpointLoaded;
    EventHandler.Instance.Events.PlayerDying += _onPlayerDying;
    _isSubscribed = true;
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (!_isSubscribed) {
      return;
    }
    EventHandler.Instance.Events.CheckpointLoaded -= _onCheckpointLoaded;
    EventHandler.Instance.Events.PlayerDying -= _onPlayerDying;
    _isSubscribed = false;
  }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();

    _allocate();
    _applyColor();
    _applyTrigger();
    _spoutNode.Group = Group;
    _tipTo(0f);

    if (Engine.IsEditorHint()) {
      SetPhysicsProcess(false);
      QueueRedraw();
      return;
    }

    _triggerNode.CollisionLayer = 0;
    // Authored trigger volumes are easy to leave on the default layer, and the node that depends
    // on the contract is the one that gets to enforce it.
    _triggerNode.CollisionMask = PhysicsLayers.Player.Mask;
    _triggerNode.BodyEntered += _onBodyEntered;
  }

  public override void _PhysicsProcess(double delta) {
    if (Engine.IsEditorHint()) {
      return;
    }

    if (_needsFloor) {
      _readFloor();
      _needsFloor = false;
    }

    if (_state == State.Waiting || _isFrozen) {
      return;
    }

    var step = (float)delta;
    _elapsed += step;

    if (_state == State.Tipping && _elapsed >= POUR_DELAY) {
      _state = State.Pouring;
      _spoutNode.Empty();
      EventHandler.Instance.EmitPaintSpilled(GlobalPosition);
      EventHandler.Instance.EmitCameraShakeRequest(6f);
    }

    if (_state == State.Pouring) {
      _pour(step);
    }

    _flowStep(step);
    _drain(step);
    _catchPlayer();
    _followFront();
    QueueRedraw();
  }

  // Tipped from somewhere other than the volume that normally sets it off.
  public void Pour() {
    if (_state == State.Waiting) {
      _begin();
    }
  }

  public bool IsRunning => _state != State.Waiting;

  // How far along the run the paint has reached, in the flood's own pixels. The whole of the chase
  // is read off this one number.
  public float FrontX {
    get {
      for (var i = _columns - 1; i >= 0; i--) {
        if (_height[i] >= LETHAL_DEPTH) {
          return (i + 0.5f) * COLUMN_WIDTH;
        }
      }
      return 0f;
    }
  }

  public float DeepestDepth {
    get {
      var deepest = 0f;
      for (var i = 0; i < _columns; i++) {
        deepest = Mathf.Max(deepest, _height[i]);
      }
      return deepest;
    }
  }

  // Everything the flood is holding. The columns only ever hand paint to each other, so this
  // moves by the pour and the drain and by nothing else - which is the one property of the
  // simulation worth pinning, since a leak shows up as a chase that quietly runs out of steam.
  public float TotalPaint {
    get {
      var total = 0f;
      for (var i = 0; i < _columns; i++) {
        total += _height[i];
      }
      return total;
    }
  }

  // How deep the paint is standing at a point along the run.
  public float DepthAt(float x) => _columns == 0 ? 0f : _height[_columnAt(x)];

  public float WettedWidth {
    get {
      var wet = 0;
      for (var i = 0; i < _columns; i++) {
        if (_height[i] >= MIN_DEPTH) {
          wet++;
        }
      }
      return wet * COLUMN_WIDTH;
    }
  }

  // The bucket goes over on a tween, and everything after that is the paint's own. It is the one
  // part of the set piece that is a performance rather than a simulation.
  private void _begin() {
    _state = State.Tipping;
    _elapsed = 0f;
    _poured = 0f;
    _hasCaught = false;

    var tween = CreateTween();
    tween.TweenMethod(Callable.From((float turned) => _tipTo(turned)), 0f, TIP_ANGLE, TIP_DURATION)
      .SetTrans(Tween.TransitionType.Back)
      .SetEase(Tween.EaseType.In);

    if (CameraZoom > 0f && _level is not null) {
      _zoomBefore = _level.CameraNode.TargetZoom;
      _level.CameraNode.ZoomTo(CameraZoom);
    }
  }

  // Turned about the corner it stands on rather than about the middle of its base. A bucket spun
  // about its middle swings half of itself through the floor before it is halfway over; one
  // turned about its corner lifts off it, which is what toppling is.
  private void _tipTo(float turned) {
    var corner = new Vector2(_spoutNode.HalfWidth, 0f);
    _spoutNode.Rotation = turned;
    _spoutNode.Position = SpoutOffset + corner - corner.Rotated(turned);
    // The paint comes out of the mouth, and the mouth is halfway across the room by the time the
    // bucket has finished going over. Followed rather than assumed, so the stream stays with the
    // bucket instead of pouring out of the patch of floor it used to stand on.
    _mouthX = (_spoutNode.Position + new Vector2(0f, -_spoutNode.Height).Rotated(turned)).X;
  }

  private void _pour(float delta) {
    var budget = PourRate * PourDuration;
    if (_poured >= budget) {
      _state = State.Draining;
      _sprayNode.Emitting = false;
      return;
    }

    var added = Mathf.Min(PourRate * delta, budget - _poured);
    _poured += added;

    // Spread across the mouth rather than dropped down one column: a whole pour landing in one
    // place is a spike that the flow then has to knock down, which reads as a bounce.
    var first = _columnAt(_mouthX - (SpoutWidth / 2f));
    var last = _columnAt(_mouthX + (SpoutWidth / 2f));
    var share = added / Mathf.Max(last - first + 1, 1);
    for (var i = first; i <= last; i++) {
      _height[i] += share;
    }
  }

  // One pass of shallow water. The paint is pushed by the slope of its own surface, not of the
  // floor, which is what carries it over a step and lets it pile up behind anything standing in
  // its way - and what crosses between two columns is the speed it has picked up times the depth
  // standing behind it, so paint that has thinned out still runs at the speed it was going.
  private void _flowStep(float delta) {
    var drag = Mathf.Min(DRAG * delta, 1f);

    for (var i = 0; i < _columns - 1; i++) {
      // Downhill is a larger y, so a positive slope is paint standing higher on the left.
      var slope = (_surfaceOf(i + 1) - _surfaceOf(i)) / COLUMN_WIDTH;
      // Only where there is paint to carry: an interface the flood has not reached would
      // otherwise bank the run's push the whole time it stood dry, and spend it all in one tick
      // the moment the front arrived.
      var run = _height[i] > MIN_DEPTH ? DOWNHILL : 0f;

      _velocity[i] += ((GRAVITY * slope) + run) * Speed * delta;
      _velocity[i] -= _velocity[i] * drag;

      // Whichever side the paint is coming from is the side that has any to give.
      var behind = _velocity[i] > 0f ? _height[i] : _height[i + 1];
      var moved = behind * _velocity[i] * delta / COLUMN_WIDTH;
      var cap = behind * MAX_HANDOVER;
      moved = Mathf.Clamp(moved, -cap, cap);

      _height[i] -= moved;
      _height[i + 1] += moved;
    }
  }

  private void _drain(float delta) {
    var loss = Mathf.Min(DRAIN_RATE * delta, 1f);
    for (var i = 0; i < _columns; i++) {
      if (_isOverVoid[i]) {
        _height[i] -= _height[i] * loss;
      }
      if (_height[i] < 0f) {
        _height[i] = 0f;
      }
    }

    // The last column is the end of the run rather than a wall at the end of the array.
    _height[_columns - 1] -= _height[_columns - 1] * loss;
  }

  // Read against the columns rather than through an Area2D. The paint is a different depth over
  // every step it has reached, and one box drawn round the whole of it would drown a cube standing
  // dry on a step the paint has only lapped the foot of.
  private void _catchPlayer() {
    if (_hasCaught) {
      return;
    }

    var player = _level?.PlayerNode;
    if (player is null || player.IsDying()) {
      return;
    }

    var local = ToLocal(player.GlobalPosition);
    var column = _columnAt(local.X);
    if (local.X < 0f || local.X > Width || _height[column] < LETHAL_DEPTH) {
      return;
    }

    // How far the cube reaches below its own centre, which for a square is a turn away from being
    // its half extent.
    var half = player.GetCollisionHalfExtents();
    var turn = player.GlobalRotation;
    var reach = (half.X * Mathf.Abs(Mathf.Sin(turn))) + (half.Y * Mathf.Abs(Mathf.Cos(turn)));
    if (local.Y + reach < _surfaceOf(column)) {
      return;
    }

    _hasCaught = true;
    EventHandler.Instance.EmitPlayerDying(this, player.GlobalPosition, EntityType.Platform);
  }

  private void _followFront() {
    var front = -1;
    for (var i = _columns - 1; i >= 0; i--) {
      if (_height[i] >= LETHAL_DEPTH) {
        front = i;
        break;
      }
    }

    if (front < 0) {
      _sprayNode.Emitting = false;
      if (_state == State.Draining) {
        _finish();
      }
      return;
    }

    _sprayNode.Position = new Vector2((front + 0.5f) * COLUMN_WIDTH, _surfaceOf(front));
    _sprayNode.Emitting = _state != State.Draining || _height[front] > LETHAL_DEPTH * 2f;
  }

  // The paint is gone and the room is survivable again, so the camera goes back to what the level
  // framed. A respawn takes a different road: the camera restores itself on the same signal.
  private void _finish() {
    _state = State.Waiting;
    if (CameraZoom > 0f && _level is not null) {
      _level.CameraNode.ZoomTo(_zoomBefore);
    }
  }

  private void _onBodyEntered(Node2D body) {
    if (_state != State.Waiting || body != _level?.PlayerNode) {
      return;
    }
    _begin();
  }

  // Frozen rather than emptied: the flood the player died to is the one they see when the screen
  // comes back, and it is the reload that puts it away.
  private void _onPlayerDying(Node? area, Vector2 position, int entityType) => _isFrozen = true;

  private void _onCheckpointLoaded() {
    // Subscribed in _EnterTree, one step before _Ready: a reload arriving in between has no
    // columns to empty.
    if (!IsNodeReady()) {
      return;
    }
    _restore();
  }

  private void _restore() {
    System.Array.Clear(_height);
    System.Array.Clear(_velocity);
    _state = State.Waiting;
    _isFrozen = false;
    _hasCaught = false;
    _elapsed = 0f;
    _poured = 0f;
    _needsFloor = true;

    _tipTo(0f);
    _spoutNode.Fill();
    _sprayNode.Emitting = false;
    QueueRedraw();
  }

  private void _allocate() {
    _columns = Mathf.Clamp(Mathf.CeilToInt(Width / COLUMN_WIDTH), 2, MAX_COLUMNS);
    _floor = new float[_columns];
    _height = new float[_columns];
    _velocity = new float[_columns];
    _isOverVoid = new bool[_columns];
    _drawn = new float[_columns];
    // Down the surface and back along the floor, allocated once: the shape is rebuilt in place
    // every tick the flood is moving.
    _outline = new Vector2[_columns * 2];
    _outlineColors = new Color[_columns * 2];
  }

  private void _applyColor() {
    var skinColor = GameSkin.ColorGroupToSkinColor(Group);
    var skin = SkinManager.Instance.CurrentSkin;
    _paintColor = skin.GetColor(skinColor, PAINT);
    _shadeColor = skin.GetColor(skinColor, PAINT_SHADE);
    _sprayNode.Color = _paintColor;
  }

  private void _applyTrigger() {
    _triggerNode.Position = TriggerOffset;
    if (_triggerShapeNode.Shape is RectangleShape2D rectangle) {
      rectangle.Size = TriggerSize;
    }
  }

  // Where the level actually is under every column. One reused query: a room's worth is a few
  // hundred casts, and building one apiece allocates three engine objects a ray.
  private void _readFloor() {
    var space = GetWorld2D().DirectSpaceState;
    _rayQuery ??= new PhysicsRayQueryParameters2D {
      CollisionMask = PhysicsLayers.Platform.Mask,
      CollideWithAreas = false,
      CollideWithBodies = true,
    };

    for (var i = 0; i < _columns; i++) {
      var x = (i + 0.5f) * COLUMN_WIDTH;
      _rayQuery.From = ToGlobal(new Vector2(x, 0f));
      _rayQuery.To = ToGlobal(new Vector2(x, Depth));
      using var hit = space.IntersectRay(_rayQuery);

      _isOverVoid[i] = hit.Count == 0;
      _floor[i] = hit.Count == 0 ? Depth : ToLocal((Vector2)hit["position"]).Y;
    }
  }

  private float _surfaceOf(int column) => _floor[column] - _height[column];

  private int _columnAt(float x) =>
    Mathf.Clamp(Mathf.FloorToInt(x / COLUMN_WIDTH), 0, _columns - 1);

  public override void _Draw() {
    if (Engine.IsEditorHint()) {
      _drawGizmo();
      return;
    }

    if (_columns == 0) {
      return;
    }

    // Smoothed for drawing only. The columns carry the paint honestly, steps and all, but the
    // step between two of them is a pixel and a half of stairs down the face of a liquid.
    for (var i = 0; i < _columns; i++) {
      var left = _height[Mathf.Max(i - 1, 0)];
      var right = _height[Mathf.Min(i + 1, _columns - 1)];
      _drawn[i] = ((left + (_height[i] * 2f) + right) / 4f);
    }

    var deep = 0f;
    for (var i = 0; i < _columns; i++) {
      var x = (i + 0.5f) * COLUMN_WIDTH;
      var depth = _drawn[i] < MIN_DEPTH ? 0f : _drawn[i];
      deep = Mathf.Max(deep, depth);

      // Every column keeps a hair of thickness, dry ones included. The shape is one strip down the
      // surface and back along the floor, and a strip whose two edges meet has no area to divide
      // into triangles - one dry column and the whole flood fails to draw. A hundredth of a pixel
      // is below anything the rasteriser will put on screen.
      depth = Mathf.Max(depth, HAIRLINE);

      _outline[i] = new Vector2(x, _floor[i] - depth);
      _outlineColors[i] = _paintColor;
      // Back along the floor, so the shape closes on itself without a seam.
      var back = (_columns * 2) - 1 - i;
      _outline[back] = new Vector2(x, _floor[i]);
      _outlineColors[back] = _shadeColor;
    }

    if (deep < MIN_DEPTH) {
      return;
    }

    DrawPolygon(_outline, _outlineColors);
  }

  private void _drawGizmo() {
    var run = new Rect2(0f, 0f, Width, Depth);
    DrawRect(run, new Color(0.6f, 0.2f, 1f, 0.10f));
    DrawRect(run, new Color(0.6f, 0.2f, 1f, 0.55f), filled: false, width: 2f);
    DrawRect(
      new Rect2(TriggerOffset - (TriggerSize / 2f), TriggerSize),
      new Color(1f, 0.85f, 0.2f, 0.75f), filled: false, width: 2f);
  }
}
