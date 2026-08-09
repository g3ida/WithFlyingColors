namespace Wfc.Entities.World.Paint;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using System.Collections.Generic;
using Godot;
using Wfc.Core.Input;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;
using EventHandler = Wfc.Core.Event.EventHandler;

// A bucket of paint standing on a surface, which the cube can walk into and shove along. Push it
// past the edge of what it is standing on and it slips, turns on that edge, comes down somewhere
// below and empties itself over whatever it lands on - and that run of surface is that colour
// from then on, to be crossed on the matching face like any other.
//
// The open tin is a surface of its colour too, so coming down in it on the wrong face is fatal
// while it is full. Only the paint is: the sides are a tin, and shoving one along is never a
// question of which face is pointing at it. An emptied one is a step for anybody.
//
// A level built around one has to leave the cube somewhere neutral to turn on: a painted run is a
// coloured surface, and turning a new face down while stood on one is fatal. Land the paint clear
// of the white the cube walks in on, and clear of the next splat, or the puzzle cannot be played
// rather than being hard.
[Tool]
[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class PaintBucket : CharacterBody2D, IPersistent {
  public override void _Notification(int what) => this.Notify(what);

  #region Dependencies
  [Dependency]
  public IInputManager InputManager => this.DependOn<IInputManager>();
  #endregion Dependencies

  // What the bucket is doing. It only ever goes forward through these: shoved around until it
  // hangs over an edge, turning on that edge, falling, and empty from then on.
  private enum State {
    Resting,
    Tipping,
    Falling,
    Spilled,
  }

  #region Constants
  // The cube's own fall, so a bucket coming down beside it belongs to the same world.
  private const float GRAVITY = 9.8f * Constants.WORLD_TO_SCREEN * 2.5f;

  // Slower than the cube can run, so pushing a bucket is walking at the bucket's pace rather than
  // the cube being held up by something it barely feels.
  //
  // The cube is always a tick of this behind the bucket while it shoves: the bucket moves first,
  // and the physics server tests the cube against where the bucket was at the top of the tick. The
  // body is narrower than the tin it draws by about that much, so the two are still drawn against
  // each other - which is why the collision shape is not the size of the sprite.
  private const float PUSH_SPEED = 190f;

  // How far off the cube can be and still be shoving. The two travel together once the shove is
  // under way, so this only has to cover the pixel or two between them.
  private const float PUSH_REACH = 10f;

  // How much faster than the bucket the cube is carried while it shoves. Being blocked by the
  // bucket is what holds it to the bucket's pace, and asking for more than that is what keeps it
  // pressed against it rather than trailing the gap it lost to the first collision.
  private const float PUSH_LEAN = 1.15f;

  // A shove is a scrape rather than a knock, so the sound is raised again for as long as it lasts.
  // Just short of the clip, so one runs into the next.
  private const float SHOVE_SOUND_INTERVAL = 0.5f;

  // A dash is not a shove. The cube arrives at ten times walking pace and the bucket is sent
  // skidding for it, bleeding the speed off over a long slide rather than stopping where the cube
  // does. What it costs to stop it is what decides how far it goes.
  private const float DASH_KICK_SPEED = 900f;
  private const float DASH_KICK_DRAG = 780f;

  // A bucket still carrying this much of a kick when it runs out of surface does not tip over the
  // edge - it leaves it. What was in it goes over the side and comes down well out from the wall,
  // turning over as it goes.
  private const float LAUNCH_MIN_SPEED = 300f;
  private const float LAUNCH_LIFT = 140f;
  private const float LAUNCH_SPIN = 7f;

  // A bucket kicked clean off the level is a puzzle with a piece missing. Past this far below where
  // it was authored it is put back, the same way a reload would put it back.
  private const float LOST_DROP = 2400f;

  // The topple: it begins as a slip that has barely started and winds up from there. The corner
  // carries it exactly as far as a quarter turn, which is where the bucket has swung clear of the
  // surface it was standing on - any further and it would be turning back underneath it.
  private const float TIP_INITIAL_SPEED = 0.5f;
  private const float TIP_ACCELERATION = 7.5f;
  private static readonly float TIP_HANDOFF = Mathf.Pi / 2f;

  // How far below the corner it turned on a contact has to be to count as the landing. Right up
  // to the handoff the bucket is still touching that corner, and the ground reported there is the
  // ledge it is in the middle of falling off.
  private const float LANDING_CLEARANCE = 12f;

  // How much of the turn it keeps once the corner has let go. Enough that a bucket goes on turning
  // as it drops rather than freezing on its side halfway down, and little enough that it comes
  // down on the side it went over on.
  private const float SPIN_CARRY = 0.35f;

  // The ground probes: how far above the base to start looking, how far below the base still
  // counts as standing on something, and how far in from the corners to ask. The inset is what
  // keeps a bucket pressed against a wall from reading the wall as its own edge.
  private const float PROBE_RISE = 4f;
  private const float PROBE_DROP = 14f;
  private const float PROBE_INSET = 3f;
  private const int EDGE_PROBE_STEPS = 6;

  // How far past the body the landing probe looks for the surface it came down on.
  private const float LANDING_PROBE = 24f;

  private const float IMPACT_SHAKE = 8f;
  #endregion Constants

  #region Exports
  // The paint it is full of. There is no neutral bucket: white paint would leave a splat that
  // reads as a colour puzzle and answers to every face.
  [Export(PropertyHint.Enum, "blue,pink,yellow,purple")]
  public string Group {
    get => _group;
    set {
      var previous = _group;
      _group = value;
      if (_isWired) {
        _spriteNode.Group = value;
        _paintAreaNode.RemoveFromGroup(previous);
        _paintAreaNode.AddToGroup(value);
      }
    }
  }
  private string _group = ColorUtils.PURPLE;

  // How wide a run of surface the paint covers where it lands. It has to be comfortably wider
  // than the cube, which is judged by the face it has down over the whole crossing.
  [Export]
  public float SplashWidth { get; set; } = 288f;
  #endregion Exports

  #region Nodes
  [NodePath("CollisionShape2D")]
  private CollisionShape2D _collisionShapeNode = default!;
  [NodePath("BucketSprite")]
  private BucketSprite _spriteNode = default!;
  [NodePath("PaintArea")]
  private Area2D _paintAreaNode = default!;
  #endregion Nodes

  #region Fields
  private State _state = State.Resting;
  private Vector2 _size = new Vector2(64f, 72f);
  private Vector2 _home;
  private float _homeRotation;
  private Node? _homeParent;
  private string _saveId = string.Empty;
  private Player.Player? _pusherNode;
  private float _shoveSoundIn;
  private float _kick;
  // Everything it put down: what stayed on the shelf, and whatever ran off either end of it.
  private readonly List<PaintSplat> _splats = [];
  private Vector2 _pivot;
  private Vector2 _arm;
  private float _tipAngle;
  private float _tipSpeed;
  private int _tipDirection = 1;
  private float _spin;
  private float _clearY;
  private bool _isSubscribed;
  // The exported setter fires while the scene is still loading, before there is a sprite to push
  // a colour into.
  private bool _isWired;
  private PhysicsRayQueryParameters2D? _rayQuery;
  #endregion Fields

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _isWired = true;
    if (_collisionShapeNode.Shape is RectangleShape2D rectangle) {
      _size = rectangle.Size;
    }
    _spriteNode.Group = Group;
    _paintAreaNode.AddToGroup(Group);
    _home = Position;
    _homeRotation = Rotation;
    _homeParent = GetParent();
    // Taken now, while it is still where the level put it. An emptied bucket goes to live under
    // whatever caught it so that it rides, and a saved entry filed under where it ended up is
    // filed under a name no freshly built level has.
    _saveId = GetPath();
    // Before the cube, whatever order the level put the two in. A shove is the bucket getting out
    // of the way and the cube walking into the room it left; the other way round, the cube walks
    // into the bucket and loses its speed to the collision every single tick.
    ProcessPhysicsPriority = -1;
    // The first thing a tick does is ask the input manager which way the cube is leaning, and
    // dependencies are not resolved until after this.
    SetPhysicsProcess(false);
  }

  public void OnResolved() => SetPhysicsProcess(true);

  public override void _EnterTree() {
    base._EnterTree();
    if (Engine.IsEditorHint() || _isSubscribed) {
      return;
    }
    EventHandler.Instance.Events.CheckpointLoaded += _onCheckpointLoaded;
    EventHandler.Instance.Events.CheckpointReached += _onCheckpointReached;
    _isSubscribed = true;
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (!_isSubscribed) {
      return;
    }
    EventHandler.Instance.Events.CheckpointLoaded -= _onCheckpointLoaded;
    EventHandler.Instance.Events.CheckpointReached -= _onCheckpointReached;
    _isSubscribed = false;
  }

  public override void _PhysicsProcess(double delta) {
    if (Engine.IsEditorHint()) {
      return;
    }
    switch (_state) {
      case State.Resting:
        _rest((float)delta);
        break;
      case State.Tipping:
        _tip((float)delta);
        break;
      case State.Falling:
        _fall((float)delta);
        break;
      default:
        break;
    }
  }

  public bool IsSpilled => _state == State.Spilled;

  public bool IsUpright => _state is State.Resting;

  public PaintSplat? Splat => _splats.Count > 0 ? _splats[0] : null;

  private void _rest(float delta) {
    _takeKick();
    _kick = Mathf.MoveToward(_kick, 0f, DASH_KICK_DRAG * delta);
    var shove = _pushSpeed(delta);
    // Whichever is carrying it: a bucket still skidding from a dash is not being walked along.
    var speed = Mathf.Abs(_kick) > Mathf.Abs(shove) ? _kick : shove;

    Velocity = new Vector2(speed, Velocity.Y + (GRAVITY * delta));
    MoveAndSlide();
    if (IsOnWall()) {
      _kick = 0f;
    }

    if (_groundBelow(GlobalPosition.X)) {
      return;
    }

    var side = _tippingSide();
    if (side != 0) {
      if (Mathf.Abs(_kick) >= LAUNCH_MIN_SPEED) {
        _launch(side);
      }
      else {
        _beginTipping(side);
      }
    }
    else if (!IsOnFloor()) {
      // Nothing under it at all, and so no edge to turn on: only the drop.
      _spin = 0f;
      _clearY = GlobalPosition.Y + LANDING_CLEARANCE;
      _state = State.Falling;
    }
  }

  private void _tip(float delta) {
    _tipSpeed += TIP_ACCELERATION * delta;
    _tipAngle += _tipSpeed * delta;

    var turned = _tipDirection * _tipAngle;
    Rotation = turned;
    GlobalPosition = _pivot + _arm.Rotated(turned);

    if (_tipAngle < TIP_HANDOFF) {
      return;
    }

    // The edge lets go of it: it leaves at the speed the corner was carrying it, which is the
    // turn read off the arm it was turning on.
    Velocity = _tipDirection * _tipSpeed * _arm.Rotated(turned + (Mathf.Pi / 2f));
    _spin = _tipDirection * _tipSpeed * SPIN_CARRY;
    _clearY = _pivot.Y + LANDING_CLEARANCE;
    _state = State.Falling;
  }

  private void _fall(float delta) {
    Velocity = new Vector2(Velocity.X, Velocity.Y + (GRAVITY * delta));
    MoveAndSlide();

    // A bucket that has left the edge goes on turning; one still leaning on it does not, or the
    // turn carries it back over the corner it just came off and it grinds its way down the face.
    if (!IsOnFloor()) {
      Rotation += _spin * delta;
      if (Position.Y > _home.Y + LOST_DROP) {
        _restore();
      }
      return;
    }

    var contact = _floorContact();
    // Everything short of a surface under the bucket itself is the ledge it is falling off, still
    // under one of its corners.
    if (contact.Surface is not null && contact.Point.Y > _clearY) {
      _spill(contact);
    }
  }

  private void _spill((Vector2 Point, Node? Surface) landing) {
    var (point, surface) = landing;
    var centre = new Vector2(ToGlobal(_collisionShapeNode.Position).X, point.Y);

    _spread(centre, surface);
    _spriteNode.Empty();
    _spriteNode.Impact();
    _settle(centre);
    _rideOn(surface);
    // Its paint is on the floor now, so what is left is a tin the cube can stand on whichever way
    // up it happens to be.
    _paintAreaNode.Monitorable = false;

    EventHandler.Instance.EmitPaintSpilled(centre);
    EventHandler.Instance.EmitCameraShakeRequest(IMPACT_SHAKE);

    _state = State.Spilled;
    // An empty bucket lying on a surface is scenery the cube can climb, and scenery does not
    // need a tick.
    SetPhysicsProcess(false);
  }

  // An emptied bucket belongs to whatever it came down on rather than to the room, the same way
  // the paint it spilled does. A surface that moves then takes both with it, and neither has to
  // know that it moves: the tin is carried by the very transform that carries the platform's own
  // collision, so there is nothing per-frame to keep the two in step and nothing to drift.
  private void _rideOn(Node? surface) {
    if (surface is not Node2D host || host == GetParent()) {
      return;
    }
    // Left exactly where it came to rest; what changes is only what it is measured against.
    Reparent(host, keepGlobalTransform: true);
  }

  // Which way the bucket falls: +1 for over its right-hand edge, -1 for its left, 0 while it is
  // still holding on at both corners or has already lost both.
  private int _tippingSide() {
    var half = (_size.X / 2f) - PROBE_INSET;
    var left = _groundBelow(GlobalPosition.X - half);
    var right = _groundBelow(GlobalPosition.X + half);
    return left == right ? 0 : left ? 1 : -1;
  }

  // A dashed bucket does not turn on the corner it left: it is already travelling faster than the
  // corner could carry it, so it goes off the end and turns over on the way down.
  private void _launch(int direction) {
    _tipDirection = direction;
    Velocity = new Vector2(_kick, -LAUNCH_LIFT);
    _spin = direction * LAUNCH_SPIN;
    _clearY = GlobalPosition.Y + LANDING_CLEARANCE;
    _state = State.Falling;
  }

  private void _beginTipping(int direction) {
    var half = (_size.X / 2f) - PROBE_INSET;
    var supported = GlobalPosition.X - (direction * half);
    _pivot = new Vector2(_edgeBetween(supported, GlobalPosition.X), GlobalPosition.Y);
    _arm = GlobalPosition - _pivot;
    _tipDirection = direction;
    _tipAngle = 0f;
    _tipSpeed = TIP_INITIAL_SPEED;
    _state = State.Tipping;
  }

  // The corner it turns on is the last of the surface, which is between the point that still has
  // ground under it and the one that has none. Halved down to it rather than stepped along:
  // starting the turn a few pixels inside the surface drives the bucket through it.
  private float _edgeBetween(float supported, float unsupported) {
    for (var step = 0; step < EDGE_PROBE_STEPS; step++) {
      var middle = (supported + unsupported) / 2f;
      if (_groundBelow(middle)) {
        supported = middle;
      }
      else {
        unsupported = middle;
      }
    }
    return supported;
  }

  private bool _groundBelow(float x) {
    var from = new Vector2(x, GlobalPosition.Y - PROBE_RISE);
    var to = from + new Vector2(0f, PROBE_RISE + PROBE_DROP);
    using var hit = GetWorld2D().DirectSpaceState.IntersectRay(_groundRay(from, to));
    return hit.Count > 0;
  }

  // One reused query: a resting bucket probes its own base every tick, and building one per probe
  // allocates three engine objects a ray.
  private PhysicsRayQueryParameters2D _groundRay(Vector2 from, Vector2 to) {
    _rayQuery ??= new PhysicsRayQueryParameters2D {
      Exclude = new Godot.Collections.Array<Rid> { GetRid() },
      CollisionMask = CollisionMask,
    };
    _rayQuery.From = from;
    _rayQuery.To = to;
    return _rayQuery;
  }

  // Where the bucket came down, probed from under its middle rather than read off the collision
  // that stopped it. A body that lands on one corner reports its contact there, and the paint
  // belongs under the bucket rather than under whichever corner touched first - and a landing the
  // floor snap caught leaves no slide collision behind to read at all.
  private (Vector2 Point, Node? Surface) _floorContact() {
    var centre = ToGlobal(_collisionShapeNode.Position);
    var half = Mathf.Max(_size.X, _size.Y) / 2f;
    // Its own middle first, since that is where the paint belongs; the two ends only for a bucket
    // that came down with its middle over the edge of what caught it.
    for (var probe = 0; probe < 3; probe++) {
      var offset = probe switch { 0 => 0f, 1 => PROBE_INSET - half, _ => half - PROBE_INSET };
      var from = new Vector2(centre.X + offset, centre.Y);
      using var hit = GetWorld2D().DirectSpaceState.IntersectRay(
        _groundRay(from, from + (Vector2.Down * (half + LANDING_PROBE)))
      );
      if (hit.Count > 0) {
        return (new Vector2(centre.X, hit["position"].AsVector2().Y), hit["collider"].As<Node>());
      }
    }
    return (GlobalPosition, null);
  }

  // Paint that stays: a bucket is emptied once and the room is authored around what it leaves.
  private void _spread(Vector2 where, Node? surface) =>
    PaintSpread.Lay(this, where, surface, Group, SplashWidth, life: 0f, _splats);

  private PaintSplat _makeSplat(Node2D host, Vector2 where, float width, bool dried) {
    var splat = SceneHelpers.InstantiateNode<PaintSplat>();
    splat.Setup(Group, width, dried);
    // Both before it is in the tree: physics interpolation is on for the whole project, and a
    // node given its transform after it is added draws its first frames sweeping in from its
    // parent's origin.
    splat.Position = where;
    // The paint is a size in pixels wherever it lands, and some of the older platforms are sized
    // by scaling them.
    var scale = host.GlobalScale;
    splat.Scale = new Vector2(
      Mathf.IsZeroApprox(scale.X) ? 1f : 1f / scale.X,
      Mathf.IsZeroApprox(scale.Y) ? 1f : 1f / scale.Y
    );

    host.AddChild(splat);
    return splat;
  }

  // Lay the bucket down on what it broke over, on whichever quarter turn it came down nearest -
  // short of standing back up on its base, which is not a bucket that has just emptied itself.
  private void _settle(Vector2 landing) {
    var quarter = Mathf.Pi / 2f;
    var rotation = Mathf.Round(Rotation / quarter) * quarter;
    if (Mathf.Abs(Mathf.Wrap(rotation, -Mathf.Pi, Mathf.Pi)) < quarter / 2f) {
      rotation = _tipDirection * quarter;
    }
    Rotation = rotation;

    // The node's origin is the middle of the bucket's base, so where the origin goes is read back
    // from where the body's own middle has to end up once it is lying down.
    var onItsSide = Mathf.Abs(Mathf.Sin(rotation)) > 0.5f;
    var halfHeight = onItsSide ? _size.X / 2f : _size.Y / 2f;
    var offset = _collisionShapeNode.Position.Rotated(rotation);

    GlobalPosition = new Vector2(landing.X - offset.X, landing.Y - halfHeight - offset.Y);
    Velocity = Vector2.Zero;
  }

  private float _pushSpeed(float delta) {
    if (!_isBeingShoved(out var pusher, out var towards)) {
      _shoveSoundIn = 0f;
      return 0f;
    }

    // The cube is given its speed back at the top of every tick of the shove, a little more than
    // the bucket is about to take, so the collision that holds it to the bucket's pace costs it
    // nothing. Without it the cube spends three ticks winding back up to speed after it first
    // walks into the bucket, and the handful of pixels it loses doing that stay as a gap between
    // the two for the rest of the shove.
    pusher.Velocity = new Vector2(towards * PUSH_SPEED * PUSH_LEAN, pusher.Velocity.Y);

    _shoveSoundIn -= delta;
    if (_shoveSoundIn <= 0f) {
      _shoveSoundIn = SHOVE_SOUND_INTERVAL;
      EventHandler.Instance.EmitBucketShoved(GlobalPosition);
    }

    return towards * PUSH_SPEED;
  }

  // A dash is taken on contact rather than on the move action: the cube arrives with whatever it
  // was holding, and what it does to the bucket is decided by how fast it got there.
  private void _takeKick() {
    if (_pusherNode is null || !IsInstanceValid(_pusherNode) || !_pusherNode.IsDashing()) {
      return;
    }
    var apart = GlobalPosition.X - _pusherNode.GlobalPosition.X;
    var towards = Mathf.Sign(apart);
    if (towards == 0f) {
      return;
    }
    if (Mathf.Abs(apart) > _pusherNode.GetCollisionHalfExtents().X + (_size.X / 2f) + PUSH_REACH) {
      return;
    }
    if (_pusherNode.GlobalPosition.Y < GlobalPosition.Y - _size.Y) {
      return;
    }

    if (Mathf.IsZeroApprox(_kick)) {
      EventHandler.Instance.EmitBucketShoved(GlobalPosition);
    }
    _kick = towards * DASH_KICK_SPEED;
  }

  private bool _isBeingShoved(out Player.Player pusher, out float towards) {
    pusher = null!;
    towards = 0f;
    if (_pusherNode is null || !IsInstanceValid(_pusherNode) || _pusherNode.IsDying()) {
      return false;
    }
    // A cube that is not taking input is not shoving anything: the intro walks it forward with the
    // move action still held, and a bucket standing in that walk would set off on its own.
    if (_pusherNode.HandleInputIsDisabled) {
      return false;
    }
    // Standing on the lid is not pushing. The sensor is a little larger than the bucket, so a cube
    // stood on top of it is inside the sensor as well.
    if (_pusherNode.GlobalPosition.Y < GlobalPosition.Y - _size.Y) {
      return false;
    }

    var apart = GlobalPosition.X - _pusherNode.GlobalPosition.X;
    towards = Mathf.Sign(apart);
    if (towards == 0f) {
      return false;
    }
    // Touching, rather than merely inside the sensor. The sensor is generous so that a shove that
    // has already started is not dropped over a pixel; starting one from that far out would have
    // the bucket move off before the cube reached it.
    if (Mathf.Abs(apart) > _pusherNode.GetCollisionHalfExtents().X + (_size.X / 2f) + PUSH_REACH) {
      return false;
    }

    var action = towards > 0f ? IInputManager.Action.MoveRight : IInputManager.Action.MoveLeft;
    if (!InputManager.IsPressed(action)) {
      return false;
    }

    pusher = _pusherNode;
    return true;
  }

  private void _onPushSensorBodyEntered(Node2D body) {
    if (body is Player.Player player) {
      _pusherNode = player;
    }
  }

  private void _onPushSensorBodyExited(Node2D body) {
    if (_pusherNode == body) {
      _pusherNode = null;
    }
  }

  // Where the bucket had got to when the player last reached a checkpoint, and the paint it had
  // already put down. A bucket is part of the puzzle, and a puzzle solved before the checkpoint
  // stays solved: sent back to make the same shove again, a player would be redoing work the
  // checkpoint said they were past.
  private void _onCheckpointReached(Vector2 position, string colorGroup) {
    if (!IsNodeReady()) {
      return;
    }
    _remembered = new Remembered(_state, GetParent(), Position, Rotation, _splats.ToArray());
  }

  // The position is the one it had inside that parent, which for a bucket riding a platform is the
  // only one still worth anything once the platform has been put back where it started too.
  private sealed record Remembered(
    State State, Node? Parent, Vector2 Position, float Rotation, PaintSplat[] Splats);

  private Remembered? _remembered;

  // A reload puts the bucket back where the checkpoint left it, with the paint it had spilled by
  // then still on the floor, and takes any it has spilled since with it.
  private void _onCheckpointLoaded() {
    if (!IsNodeReady()) {
      return;
    }
    _restore();
  }

  private void _restore() {
    var kept = _remembered?.Splats ?? System.Array.Empty<PaintSplat>();
    foreach (var splat in _splats) {
      if (IsInstanceValid(splat) && System.Array.IndexOf(kept, splat) < 0) {
        splat.QueueFree();
      }
    }
    _splats.Clear();
    foreach (var splat in kept) {
      if (IsInstanceValid(splat)) {
        _splats.Add(splat);
      }
    }

    // Back under whatever it belonged to then, before its position is read against it.
    var host = _remembered?.Parent ?? _homeParent;
    if (host is not null && IsInstanceValid(host) && host != GetParent()) {
      Reparent(host, keepGlobalTransform: false);
    }

    Position = _remembered?.Position ?? _home;
    Rotation = _remembered?.Rotation ?? _homeRotation;
    _state = _remembered?.State ?? State.Resting;
    Velocity = Vector2.Zero;
    _kick = 0f;
    _pusherNode = null;

    // An emptied bucket is a tin: no paint on it to be judged against, and nothing left for it to
    // do but be stood on.
    var emptied = _state == State.Spilled;
    if (emptied) {
      _spriteNode.Empty();
    }
    else {
      _spriteNode.Fill();
    }
    _paintAreaNode.Monitorable = !emptied;
    // Put back rather than travelling back: interpolation would otherwise draw the bucket
    // sweeping across the level from wherever it came to rest.
    ResetPhysicsInterpolation();
    SetPhysicsProcess(!emptied);
  }

  #region Persistence
  // What a saved game has to carry. The in-session snapshot points straight at the nodes it
  // remembers, which is all a death needs because the nodes live through one - but a game that was
  // closed and opened again has none of them, so this says where they were rather than which they
  // were. The paint is described well enough to be built again, since nothing in the level holds
  // it: it only ever existed because this bucket made it.
  private sealed record SplatData(string Host = "", float X = 0f, float Y = 0f, float Width = 0f);

  private sealed record SaveData(
    int State = 0,
    string Parent = "",
    float X = 0f,
    float Y = 0f,
    float Rotation = 0f,
    SplatData[]? Splats = null);

  public string GetSaveId() => _saveId.Length > 0 ? _saveId : GetPath();

  public string Save(ISerializer serializer) {
    // What the last checkpoint saw, not what is on screen: a saved game is resumed from the
    // checkpoint, so it has to carry what a death would have put back.
    var was = _remembered;
    var splats = new List<SplatData>();
    foreach (var splat in was?.Splats ?? System.Array.Empty<PaintSplat>()) {
      if (IsInstanceValid(splat) && splat.GetParent() is Node2D host) {
        splats.Add(new SplatData(host.GetPath(), splat.Position.X, splat.Position.Y, splat.Width));
      }
    }

    var at = was?.Position ?? _home;
    return serializer.Serialize(new SaveData(
      (int)(was?.State ?? State.Resting),
      (was?.Parent ?? _homeParent)?.GetPath() ?? string.Empty,
      at.X,
      at.Y,
      was?.Rotation ?? _homeRotation,
      splats.ToArray()));
  }

  public void Load(ISerializer serializer, string data) {
    var saved = serializer.Deserialize<SaveData>(data);
    if (saved is null) {
      return;
    }

    var splats = new List<PaintSplat>();
    foreach (var splat in saved.Splats ?? System.Array.Empty<SplatData>()) {
      if (GetNodeOrNull(splat.Host) is Node2D host) {
        splats.Add(_makeSplat(host, new Vector2(splat.X, splat.Y), splat.Width, dried: true));
      }
    }

    _remembered = new Remembered(
      (State)saved.State,
      GetNodeOrNull(saved.Parent),
      new Vector2(saved.X, saved.Y),
      saved.Rotation,
      splats.ToArray());
    // Adopted before the restore, or it would take the paint it has just put back for paint
    // spilled since the checkpoint and throw it away again.
    _splats.Clear();
    _splats.AddRange(splats);

    if (IsNodeReady()) {
      _restore();
      return;
    }
    Callable.From(_restore).CallDeferred();
  }
  #endregion Persistence
}
