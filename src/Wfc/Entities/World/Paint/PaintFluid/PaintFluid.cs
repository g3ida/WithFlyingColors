namespace Wfc.Entities.World.Paint;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using System;
using System.Collections.Generic;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Persistence;
using Wfc.Core.Serialization;
using Wfc.Screens.Levels;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;
using Wfc.Utils.Layers;
using EventHandler = Wfc.Core.Event.EventHandler;

// A bucket big enough to flood the room it stands in, and the paint it lets go of, simulated as
// paint rather than drawn as a shape that moves.
//
// The paint is a few thousand particles under Position Based Fluids. Each one is pulled down by
// gravity and then shoved back out by its neighbours until the crowd is packed at the density
// paint likes to be at, which is what makes it pool where it is held, pour off an edge in an arc,
// break into a sheet where it thins, and pile back up where it lands. Nothing anywhere says what
// any of that should look like.
//
// It is placed by the top-left of the stretch it may cover, and it finds the floor it is poured
// onto by asking the level, so it runs down whatever steps are put under it.
[Tool]
[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class PaintFluid : Node2D, IPersistent {
  #region Constants
  private const int MAX_PARTICLES = 4200;

  // The most neighbours a particle is asked to keep track of. At the spacing paint settles to,
  // one has a dozen or so inside its reach; the rest of the room is a crowd that only forms where
  // the solve has gone wrong, and cutting it off there costs nothing and bounds the work.
  private const int CROWD = 40;

  // Room for every pair, listed once each. A particle keeps at most CROWD neighbours, and listing
  // a pair from one end only halves it.
  private const int MAX_PAIRS = MAX_PARTICLES * CROWD / 2;

  private const float COHESION_RANGE = 0.22f;

  // How near two particles have to be before there is no longer a direction between them worth
  // trusting, and how far apart they are then taken to be.
  //
  // A pair sitting on the same point cannot be left alone. Everything one particle does to another
  // is aimed along the line between them, and two particles on one point have no line - so a pair
  // skipped for having none stays where it is for ever, and every pair that lands there joins it.
  // The crowd ratchets into a single welded point that nothing in the solve can undo.
  private const float TOUCH_APART = 0.5f;
  private const float TOUCHING = TOUCH_APART * TOUCH_APART;


  // The field the surface is read out of, and how far a particle reaches into it. Coarser than the
  // particles on purpose: the surface wants to be smoother than the crowd under it.
  // How far above its own origin the field reaches. Once the bucket has gone over, its mouth is
  // higher than the top of the run, and paint thrown off a lip arcs above that again - none of
  // which a field starting at the origin can hold, so the pour comes out cut off square.
  private const float HEADROOM = 280f;

  private const float FIELD_CELL = 7f;
  private const float FIELD_REACH = 22f;
  private const float FIELD_SCALE = 3.1f;

  // How much field there has to be before the shader draws paint there. Must match the surface the
  // shader is cutting at, because everything measured off the field - where the flood has reached,
  // what it has coated - is answered in terms of what can actually be seen.
  private const float DRAWN_AT = 0.5f * FIELD_SCALE;

  // How many surfaces deep the level may be stacked under one column, and how far past one the
  // next is looked for so that the cast starts inside it rather than on its skin.
  private const int SURFACES_PER_COLUMN = 6;
  private const float SURFACE_STEP = 1f;

  // No surface under this height, which in a column that has any means inside one of them.
  private const float BURIED = -1f;

  // Just inside the column being moved to rather than exactly on its edge, which rounds the way
  // that would put the particle back in the one it is being pushed out of.
  private const float SKIN = 0.05f;

  // How far above the floor the paint is looked for before it counts as having touched it.
  //
  // Read off the drawn surface rather than off the particles: one particle raises the field enough
  // to stain at full strength but nowhere near enough to be drawn, so a single drop skittering
  // ahead of the flood would lay a finished coat over floor the paint has not reached. Kept to the
  // floor itself as well, because paint the field can see a little way above the surface is paint
  // flying over it rather than paint on it.
  private const int STAIN_LOOK = 1;

  // How much coat there is per drip at a density of one, in pixels. The density knob divides it.
  private const float STAIN_DRIP_SPACING = 34f;

  // How much a drip may swell or pinch partway down, and how far off plumb it may hang. Both are
  // small on purpose: paint runs downhill, and a drip with any real angle on it reads as blown
  // sideways rather than as paint.
  private const float STAIN_SWELL = 0.3f;
  private const float STAIN_LEAN = 0.07f;

  // How far the bucket goes over and how long it takes. A quarter turn and no further: a bucket
  // topples over the corner it stands on until it is lying on its side.
  private const float TIP_ANGLE = Mathf.Pi / 2f;
  private const float TIP_DURATION = 0.7f;
  private const float POUR_DELAY = 0.55f;

  // Just short of the clip, so one run of it goes into the next and the pour is heard as one
  // continuous sound rather than as a bucket emptying over and over.
  private const float POUR_SOUND_INTERVAL = 0.5f;

  private const SkinColorIntensity PAINT = SkinColorIntensity.Basic;
  private const SkinColorIntensity PAINT_SHADE = SkinColorIntensity.Dark;

  // Somewhere to send a pair that has none of its own. Eight of them so that a crowd which has
  // landed on one point comes apart in every direction at once rather than along a single seam,
  // and a power of two so the pick is a mask.
  private static readonly Vector2[] SPOKES = [
    new Vector2(1f, 0f),
    new Vector2(0.7071f, 0.7071f),
    new Vector2(0f, 1f),
    new Vector2(-0.7071f, 0.7071f),
    new Vector2(-1f, 0f),
    new Vector2(-0.7071f, -0.7071f),
    new Vector2(0f, -1f),
    new Vector2(0.7071f, -0.7071f),
  ];

  private static readonly StringName FieldParam = "u_field";
  private static readonly StringName ColorParam = "u_color";
  private static readonly StringName ShadeParam = "u_shade";
  private static readonly StringName StainParam = "u_stain";
  #endregion Constants

  #region Dependencies
  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public IGameLevel GameLevel => this.DependOn<IGameLevel>();

  // Held rather than asked for each tick, and allowed to come up empty: paint opened on its own
  // has nobody to chase and no camera to pull back, which is a preview rather than an error.
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
  [Export]
  public float Width { get; set; } = 2560f;

  [Export]
  public float Depth { get; set; } = 1100f;

  [Export(PropertyHint.Enum, "blue,pink,yellow,purple")]
  public string Group { get; set; } = ColorUtils.PURPLE;

  [Export]
  public Vector2 SpoutOffset { get; set; } = new Vector2(160f, 128f);

  [Export]
  public float SpoutWidth { get; set; } = 140f;

  // Particles a second, and for how long. Together they are how much paint is in the bucket.
  [Export]
  public float PourRate { get; set; } = 550f;

  [Export]
  public float PourDuration { get; set; } = 3.4f;

  // How hard it comes out. The paint runs on from this and from the slope it lands on, so this is
  // most of what decides whether the room can be outrun.
  [Export]
  public float PourSpeed { get; set; } = 210f;

  // The slope the room is on, in degrees. Paint on a level floor pools and stays there, which is
  // exactly what paint does - so a room built of level steps has to be told it is on a hill if
  // the paint is meant to run down it. It is given as the direction gravity pulls, so everything
  // the paint does on the way down is still the simulation's own: it pools behind a riser, pours
  // off a lip in an arc, and piles where it lands, all because of where down is.
  [Export(PropertyHint.Range, "0,25,0.5")]
  public float Tilt { get; set; } = 7.5f;

  [Export]
  public float CameraZoom { get; set; } = 1.5f;

  // Which pour this is. The paint comes out of the mouth with a little scatter on it, and left to
  // chance that scatter is a different flood every time - so a retry is a different room, and how
  // fast it comes down the stairs is not a thing anybody can tune. Seeded, a retry is the run the
  // player just lost to.
  [Export]
  public int PourSeed { get; set; } = 20260809;
  #endregion Exports

  #region Liquid
  // What the paint is made of. All of it is read fresh every tick except the two sizes, which the
  // grids are built from and so are taken when the room loads.
  //
  // How far apart particles sit at rest, and how far each one feels its neighbours. The pair is
  // also the price of the whole simulation: cost goes with how many it takes to fill the room,
  // which goes with the square of the spacing, while how the paint behaves goes with the ratio
  // between them - so coarser particles at the same ratio buy frame back for detail at the surface.
  [Export(PropertyHint.Range, "6,40,0.5")]
  public float ParticleSpacing { get; set; } = 14f;

  [Export(PropertyHint.Range, "10,70,0.5")]
  public float ParticleReach { get; set; } = 24f;

  [Export]
  public float Gravity { get; set; } = 1500f;

  // The fastest paint may travel, and with it how fast the flood comes down the room.
  //
  // It started as the cap that keeps a long fall in one piece - paint falling through air stops
  // speeding up long before it has fallen the height of a room, and without a limit the head of a
  // fall pulls away from the tail until the stream comes apart into a string of beads. It is also
  // the only thing that decides whether the room can be outrun. A body of paint let go spreads at
  // the speed a burst dam does, which goes with how deep it is and not with how steep the floor
  // under it is or how thick the paint is said to be: at any depth worth looking at, that is faster
  // than the cube can run. Raise it and the paint falls more nearly as it would; raise it far and
  // the room becomes unwinnable.
  [Export]
  public float TerminalSpeed { get; set; } = 220f;

  // How many times the crowd is shoved back apart per tick. This is what decides how much the
  // paint can be squashed: pressure is passed from one particle to the next one pass at a time, so
  // a deep pool needs several before the weight at the top is felt at the bottom, and paint solved
  // too few times holds far less than was poured into it. Each pass costs about as much as the
  // rest of a tick put together.
  [Export(PropertyHint.Range, "1,12,1")]
  public int SolverPasses { get; set; } = 4;

  // Softens the shove where a particle has too few neighbours to say anything reliable about how
  // packed it is - at a surface, in a falling sheet - which is where an unsoftened solve throws
  // particles off into space. Given as a multiple of what a particle's own neighbours amount to
  // when the paint is settled, so it means the same thing whatever size the particles are.
  [Export(PropertyHint.Range, "0.5,40,0.5")]
  public float Relaxation { get; set; } = 3f;

  // Paint pulls on itself. Without it the particles at a surface are pushed apart by nothing and
  // the paint frays into a haze instead of holding an edge and breaking into drops.
  [Export(PropertyHint.Range, "0,0.01,0.0001")]
  public float Cohesion { get; set; } = 0.0009f;

  // How much of its neighbours' motion a particle takes on. This is what makes paint thick: at
  // zero it behaves like water and splashes, and too high sets it solid.
  [Export(PropertyHint.Range, "0,0.4,0.005")]
  public float Thickness { get; set; } = 0.1f;

  [Export(PropertyHint.Range, "0,1,0.01")]
  public float Bounce { get; set; } = 0.1f;

  // What the floor rubs off a particle sliding along it, per tick. Per tick is the point: a number
  // that reads as gentle friction applied sixty times a second stops the paint where it lands.
  [Export(PropertyHint.Range, "0.9,1,0.001")]
  public float FloorGrip { get; set; } = 0.988f;

  // How far the paint stands off the surface it lies on. A particle held with its middle on the
  // floor is drawn half inside the platform, because what is drawn is a blob around a point.
  [Export]
  public float SurfaceLift { get; set; } = 9f;
  #endregion Liquid

  #region Stains
  // What the paint leaves on everything it runs over. The same coat an inked platform wears, said
  // in the same words, so that a surface the flood crossed and a surface somebody painted read as
  // the same thing - see FlatPlatform's Ink fields.
  [Export]
  public bool Stains { get; set; } = true;

  // How deep the coat lies on the surface, and how far the longest of its drips runs off the
  // underside.
  [Export(PropertyHint.Range, "0,80,1,or_greater")]
  public float StainDepth { get; set; } = 24f;

  [Export(PropertyHint.Range, "0,200,1,or_greater")]
  public float StainDripLength { get; set; } = 46f;

  // How many drips there are along the same stretch of coat, against the number it carries by
  // default. It says nothing about how deep the coat is.
  [Export(PropertyHint.Range, "0.2,4,0.05,or_greater")]
  public float StainDripDensity { get; set; } = 1f;

  // How much of the gap between two drips a drip actually fills. Independent of how many there
  // are, so a coat can be a few heavy runs or a close fringe of fine ones - and at one, each drip
  // runs into its neighbours and the fringe closes into a solid skirt.
  [Export(PropertyHint.Range, "0.1,1,0.05")]
  public float StainDripWidth { get; set; } = 1f;

  // Which run of drips. Two floods with the same seed dry the same way.
  [Export]
  public int StainSeed { get; set; }

  // How much paint has to be standing on a surface before it leaves anything on it. Read against
  // the same threshold the paint is drawn at, so the coat cannot appear where nothing is visible.
  //
  // Above the threshold the paint is merely drawn at, not equal to it: a column the flood has only
  // reached the edge of is one the surface is drawn over but nothing has actually run along, and a
  // coat laid there appears slightly ahead of the paint that is supposed to have left it.
  [Export(PropertyHint.Range, "0.1,3,0.05")]
  public float StainTrigger { get; set; } = 1.1f;
  #endregion Stains

  #region Exports

  [Export]
  public Vector2 TriggerOffset { get; set; } = new Vector2(860f, 176f);

  [Export]
  public Vector2 TriggerSize { get; set; } = new Vector2(200f, 340f);
  #endregion Exports

  #region Nodes
  [NodePath("Body")]
  private ColorRect _bodyNode = default!;
  [NodePath("Spout")]
  private BucketSprite _spoutNode = default!;
  [NodePath("Spray")]
  private CpuParticles2D _sprayNode = default!;
  [NodePath("Trigger")]
  private Area2D _triggerNode = default!;
  [NodePath("Trigger/TriggerShape")]
  private CollisionShape2D _triggerShapeNode = default!;
  #endregion Nodes

  #region Fields
  private enum State {
    Waiting,
    Tipping,
    Pouring,
    Spent,
  }

  private State _state = State.Waiting;
  private float _elapsed;
  private float _owed;
  private int _poured;

  private int _count;
  private readonly Vector2[] _at = new Vector2[MAX_PARTICLES];
  private readonly Vector2[] _going = new Vector2[MAX_PARTICLES];
  private readonly Vector2[] _bound = new Vector2[MAX_PARTICLES];
  private readonly Vector2[] _shove = new Vector2[MAX_PARTICLES];
  private readonly float[] _packed = new float[MAX_PARTICLES];
  private readonly float[] _give = new float[MAX_PARTICLES];

  // Which cell each particle is in, bucketed by counting sort so that finding neighbours is a walk
  // of nine cells rather than of every other particle.
  private int _cellsAcross;
  private int _cellsDown;
  private int[] _cellStart = System.Array.Empty<int>();
  private readonly int[] _inCell = new int[MAX_PARTICLES];

  // Who is near whom, found once a tick and read by every pass after. Position Based Fluids is
  // built on this: the crowd is allowed to move during the solve while the list of who is next to
  // whom is not, which is what makes every pass cost one search rather than a search apiece.
  //
  // Held as a flat list of pairs, each one listed once. Everything two particles do to each other
  // is equal and opposite - the same weight, the same push one way and the other - so finding a
  // pair from both ends is doing all of that arithmetic twice to arrive at the same numbers.
  private readonly int[] _pairA = new int[MAX_PAIRS];
  private readonly int[] _pairB = new int[MAX_PAIRS];
  private readonly float[] _pairWeight = new float[MAX_PAIRS];
  private readonly Vector2[] _pairPush = new Vector2[MAX_PAIRS];
  private int _pairs;

  private readonly Vector2[] _gradient = new Vector2[MAX_PARTICLES];
  private readonly float[] _spread = new float[MAX_PARTICLES];

  // The level under each column: the top of every surface stacked there, nearest first, and how
  // many of them there are. This is what the paint falls onto and what it runs into.
  private float[] _surface = System.Array.Empty<float>();
  private float[] _underside = System.Array.Empty<float>();
  private int[] _surfaceCount = System.Array.Empty<int>();
  private int _columns;

  // How far sideways a particle is allowed to be pushed to get out of a wall. Measured against the
  // reach, because that bounds how far into one a pass can drive it in the first place.
  private int _wallLook;

  private int _fieldAcross;
  private int _fieldDown;
  private float[] _field = System.Array.Empty<float>();
  // Everywhere paint has been. Never cleared while the room is running: paint dries where it lands
  // and the room wears the whole route the flood took, not just where it happens to be now.
  private float[] _stain = System.Array.Empty<float>();
  private bool[] _stained = System.Array.Empty<bool>();
  private byte[] _fieldBytes = System.Array.Empty<byte>();
  private ImageTexture? _fieldTexture;

  private float _reach = 24f;
  private float _spacing = 14f;
  private float _restPacked = 1f;
  private float _restSpread = 1f;
  private float _kernel;
  private float _kernelSlope;
  private float _cohesionAt;

  // Everything about the room that a retry from the last checkpoint has to come back to. Held on
  // the node because a death reloads the checkpoint rather than the level: the nodes live through
  // it, so what they were like is theirs to keep.
  private sealed class Remembered {
    public State State;
    public float Elapsed;
    public float Owed;
    public int Poured;
    public bool HasCaught;
    public float Sound;
    public ulong Scatter;
    public float ZoomBefore;
    public int Count;
    public Vector2[] At = System.Array.Empty<Vector2>();
    public Vector2[] Going = System.Array.Empty<Vector2>();
    public bool[] Stained = System.Array.Empty<bool>();
  }

  private Remembered? _remembered;
  private bool _pullBack;
  private bool _relayCoat;

  // One band per unbroken stretch of dried coat, so the paint the flood leaves can be landed on.
  private readonly List<Area2D> _coats = [];
  private bool _coatSpread;

  private bool _needsLevel = true;
  private bool _hasCaught;
  private float _zoomBefore;
  private float _mouthX;
  private float _dam;
  private float _sound;
  private bool _isSubscribed;
  private readonly RandomNumberGenerator _scatter = new();
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

  public override void _Ready() {
    base._Ready();
    this.WireNodes();

    _allocate();
    _applyColor();
    _applyTrigger();
    _spoutNode.Group = Group;
    _tipTo(0f);
    // An empty field before anything is poured. An unwritten sampler reads as white, which is a
    // room-sized rectangle of paint standing over the level from the moment it loads.
    _fill();

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

    if (_needsLevel) {
      _readLevel();
      _needsLevel = false;
    }

    if (_relayCoat) {
      _relayCoat = false;
      _relayCoats();
      _coatSpread = true;
      // Drawn here rather than left to the tick, because a room that has already been flooded is
      // sitting still: nothing else would put its coat on the screen.
      _fill();
    }

    if (_pullBack) {
      _pullBack = false;
      _level?.CameraNode.ZoomTo(CameraZoom);
    }

    if (_state == State.Waiting) {
      return;
    }

    var step = (float)delta;
    _elapsed += step;

    // Turned on the physics clock rather than by a tween. A tween runs on process frames, and the
    // paint runs on physics frames: however many of one happen per one of the other depends on
    // what the frame rate is doing, so the mouth would be somewhere slightly different every run
    // by the time the pour started - and a set piece the player retries has to be the same run.
    if (_elapsed <= TIP_DURATION) {
      _tipTo(TIP_ANGLE * _tipEase(_elapsed / TIP_DURATION));
    }

    if (_state == State.Tipping && _elapsed >= POUR_DELAY) {
      _state = State.Pouring;
      _spoutNode.Empty();
      GameEvents.Instance.OnPaintSpilled(GlobalPosition);
      GameEvents.Instance.RequestCameraShake(6f);
    }

    if (_state == State.Pouring) {
      _emit(step);
      _sound -= step;
      if (_sound <= 0f) {
        _sound = POUR_SOUND_INTERVAL;
        GameEvents.Instance.OnPaintPouring(GlobalPosition);
      }
    }

    _simulate(step);
    _fill();
    _catchPlayer();
    _followFront();
  }

  #region Simulation
  // One tick of Position Based Fluids. Gravity moves everything first and asks questions after:
  // the crowd is then shoved back apart until it is packed the way paint is packed, and the speed
  // each particle ends up with is however far the shoving actually moved it.
  private void _simulate(float delta) {
    if (_count == 0) {
      return;
    }

    var lean = Mathf.DegToRad(Tilt);
    var pull = new Vector2(Mathf.Sin(lean), Mathf.Cos(lean)) * (Gravity * delta);

    for (var i = 0; i < _count; i++) {
      _going[i] += pull;
      _bound[i] = _at[i] + (_going[i] * delta);
      _hold(ref _bound[i], _at[i].Y);
    }

    _bucket();
    _findPairs();

    for (var pass = 0; pass < SolverPasses; pass++) {
      _measure();
      _push();
      for (var i = 0; i < _count; i++) {
        _bound[i] += _shove[i];
        _hold(ref _bound[i], _at[i].Y);
      }
    }

    var fastest = TerminalSpeed * TerminalSpeed;
    for (var i = 0; i < _count; i++) {
      var moved = (_bound[i] - _at[i]) / delta;
      var speed = moved.LengthSquared();
      if (speed > fastest) {
        moved *= TerminalSpeed / MathF.Sqrt(speed);
      }
      _going[i] = moved;
      _at[i] = _bound[i];
    }

    _thicken();
    _land();
    _retire();
  }

  // How packed each particle is, and how much it may give to fix it. A particle with too few
  // neighbours to be sure is left mostly alone rather than shoved on a guess.
  private void _measure() {
    var alone = _kernel * _reach * _reach * _reach * _reach * _reach * _reach;
    var span = _reach * _reach;

    for (var i = 0; i < _count; i++) {
      _packed[i] = alone;
      _gradient[i] = Vector2.Zero;
      _spread[i] = 0f;
    }

    for (var pair = 0; pair < _pairs; pair++) {
      var a = _pairA[pair];
      var b = _pairB[pair];
      var away = _bound[a] - _bound[b];
      var apart = away.LengthSquared();
      if (apart >= span) {
        _pairWeight[pair] = -1f;
        continue;
      }
      if (apart < TOUCHING) {
        // On top of one another, so the direction between them is made up rather than measured.
        // Which one it is hardly matters; that there is one at all is what lets the pair come
        // apart again. Taken from the pair so that the same two always part the same way, because
        // a set piece the player retries has to be the run they just lost to.
        away = SPOKES[((a * 7) + (b * 13)) & (SPOKES.Length - 1)] * TOUCH_APART;
        apart = TOUCHING;
      }

      // One reciprocal square root does for both: the weight wants the square of the distance and
      // the push wants one over it, and neither wants the distance itself.
      var gap = span - apart;
      var weight = _kernel * gap * gap * gap;
      var inverse = 1f / MathF.Sqrt(apart);
      var edge = _reach - (apart * inverse);
      var push = away * (_kernelSlope * edge * edge * inverse);

      _pairWeight[pair] = weight;
      _pairPush[pair] = push;

      // Equal and opposite, which is the whole reason the pair is only listed once.
      _packed[a] += weight;
      _packed[b] += weight;
      _gradient[a] += push;
      _gradient[b] -= push;
      var pushed = push.LengthSquared();
      _spread[a] += pushed;
      _spread[b] += pushed;
    }

    var softened = Relaxation * _restSpread;
    for (var i = 0; i < _count; i++) {
      var crowding = (_packed[i] / _restPacked) - 1f;
      // Only ever a push apart. A particle with fewer neighbours than it would have deep inside the
      // paint is at a surface, not in a hole that wants filling - left to answer for it, every
      // particle on the outside is dragged inward and the whole body pulls itself smaller than the
      // paint that was poured. What holds a surface together is Cohesion, which is measured
      // between particles rather than against a density none of them can have out there.
      if (crowding <= 0f) {
        _give[i] = 0f;
        continue;
      }
      var spread = _spread[i] + _gradient[i].LengthSquared();
      _give[i] = -crowding / ((spread / (_restPacked * _restPacked)) + softened);
    }
  }

  // The shove itself: everything that is too close pushes apart, and a little extra push is added
  // between near neighbours so that paint at a surface still has something holding it together.
  private void _push() {
    for (var i = 0; i < _count; i++) {
      _shove[i] = Vector2.Zero;
    }

    for (var pair = 0; pair < _pairs; pair++) {
      var weight = _pairWeight[pair];
      if (weight < 0f) {
        continue;
      }
      var a = _pairA[pair];
      var b = _pairB[pair];

      // Squared twice rather than raised to a power: this is the innermost line in the whole
      // simulation, and a general pow here costs more than everything around it put together.
      var pull = weight / _cohesionAt;
      var pulled = pull * pull;
      var cohesion = -Cohesion * pulled * pulled;

      var shove = (_give[a] + _give[b] + cohesion) * _pairPush[pair];
      _shove[a] += shove;
      _shove[b] -= shove;
    }

    for (var i = 0; i < _count; i++) {
      _shove[i] /= _restPacked;
    }
  }

  // Paint is not water: a particle takes on some of what its neighbours are doing, which is what
  // stops a pour from atomising into spray the moment it leaves the bucket.
  private void _thicken() {
    for (var i = 0; i < _count; i++) {
      _shove[i] = Vector2.Zero;
    }

    for (var pair = 0; pair < _pairs; pair++) {
      // The last pass's spacing rather than this instant's. Thickness is a smooth weight over a
      // crowd, and the shove that moved them since is far smaller than the reach it is measured
      // against - so the square root it saves is worth more than the accuracy it costs.
      var weight = _pairWeight[pair];
      if (weight < 0f) {
        continue;
      }
      var a = _pairA[pair];
      var b = _pairB[pair];
      var borrowed = (_going[b] - _going[a]) * weight;
      _shove[a] += borrowed;
      _shove[b] -= borrowed;
    }

    var thickness = Thickness / _restPacked;
    for (var i = 0; i < _count; i++) {
      _going[i] += _shove[i] * thickness;
    }
  }

  #endregion Simulation

  #region Neighbours
  // Nine cells around each particle, written into one flat list of pairs. Nothing here allocates
  // and nothing is handed a callback: a lambda per particle per pass was once the whole cost of
  // this simulation, both in what it allocated and in calling through a delegate once per pair.
  //
  // A pair is listed from its lower-numbered end only. Each is then found once instead of twice,
  // and everything the passes do with it is applied to both ends at once.
  private void _findPairs() {
    _pairs = 0;
    var span = _reach * _reach;

    for (var i = 0; i < _count; i++) {
      var here = _bound[i];
      var cx = Mathf.Clamp((int)(here.X / _reach), 0, _cellsAcross - 1);
      var cy = Mathf.Clamp((int)(here.Y / _reach), 0, _cellsDown - 1);
      var found = 0;

      for (var oy = -1; oy <= 1 && found < CROWD; oy++) {
        var y = cy + oy;
        if (y < 0 || y >= _cellsDown) {
          continue;
        }
        for (var ox = -1; ox <= 1 && found < CROWD; ox++) {
          var x = cx + ox;
          if (x < 0 || x >= _cellsAcross) {
            continue;
          }
          var cell = (y * _cellsAcross) + x;
          var last = _cellStart[cell + 1];
          for (var slot = _cellStart[cell]; slot < last && found < CROWD; slot++) {
            var j = _inCell[slot];
            if (j <= i || _pairs >= MAX_PAIRS) {
              continue;
            }
            if (here.DistanceSquaredTo(_bound[j]) < span) {
              _pairA[_pairs] = i;
              _pairB[_pairs] = j;
              _pairs++;
              found++;
            }
          }
        }
      }
    }
  }

  // A counting sort into the grid, rebuilt from scratch every tick.
  private void _bucket() {
    System.Array.Clear(_cellStart);
    for (var i = 0; i < _count; i++) {
      _cellStart[_cellOf(i) + 1]++;
    }
    for (var cell = 1; cell < _cellStart.Length; cell++) {
      _cellStart[cell] += _cellStart[cell - 1];
    }

    // The running cursor is the start array itself, walked forward and then put back, which is
    // cheaper than a third array to walk it with.
    for (var i = 0; i < _count; i++) {
      var cell = _cellOf(i);
      _inCell[_cellStart[cell]] = i;
      _cellStart[cell]++;
    }
    for (var cell = _cellStart.Length - 1; cell > 0; cell--) {
      _cellStart[cell] = _cellStart[cell - 1];
    }
    _cellStart[0] = 0;
  }

  // The same cell size the search uses. Filing the crowd by one measure and looking it up by
  // another leaves every particle listed somewhere its neighbours never think to look.
  private int _cellOf(int i) {
    var x = Mathf.Clamp((int)(_bound[i].X / _reach), 0, _cellsAcross - 1);
    var y = Mathf.Clamp((int)(_bound[i].Y / _reach), 0, _cellsDown - 1);
    return (y * _cellsAcross) + x;
  }

  // How much a neighbour at this distance counts toward being packed, and which way it pushes.
  private float _weight(float distance) {
    var gap = (_reach * _reach) - (distance * distance);
    return _kernel * gap * gap * gap;
  }

  private Vector2 _slope(Vector2 away, float distance) {
    var edge = _reach - distance;
    return away * (_kernelSlope * edge * edge / distance);
  }
  #endregion Neighbours

  #region The level
  // Paint cannot be anywhere the level already is. What the level is, is read off it once as the
  // surfaces stacked under each column, so this is one lookup rather than a physics query per
  // particle per pass.
  //
  // Which surface holds a particle is answered from where it came from rather than from where it
  // has got to. A particle that crossed into a surface during a tick is above it as far as this is
  // concerned, so it is stopped by the one it was falling toward - and a particle that was already
  // below a surface is not answerable to it at all, however far out over its head it walks.
  private void _hold(ref Vector2 p, float fromY) {
    // The bucket is a solid thing lying on the floor, so nothing gets past it. Without this the
    // paint spreads back underneath and out the far side, which reads as the bucket leaking from
    // the wrong end.
    if (p.X < _dam) {
      p.X = _dam;
    }
    else if (p.X > Width) {
      p.X = Width;
    }

    var column = _sampleOf(p.X);
    var surface = _surfaceUnder(column, fromY);
    if (surface >= 0f) {
      var rest = surface - SurfaceLift;
      if (p.Y > rest) {
        p.Y = rest;
      }
      return;
    }

    _clearWall(ref p, column, fromY);
  }

  // The surface a particle at this height comes to rest on, or BURIED where it is inside the level
  // rather than standing on it.
  //
  // Each surface carries the solid behind it rather than being a bare line, because the two cannot
  // be told apart otherwise. Where one platform stands over another, paint works its way under the
  // overhang and along the top of the lower one, and a level read as lines has no way to say that
  // the air there has ended: the paint is told the next thing beneath it is the storey below and
  // pours down through the platform it is lying on.
  private float _surfaceUnder(int column, float y) {
    var slot = column * SURFACES_PER_COLUMN;
    var count = _surfaceCount[column];
    for (var i = 0; i < count; i++) {
      if (_surface[slot + i] >= y) {
        return _surface[slot + i];
      }
      if (_underside[slot + i] >= y) {
        return BURIED;
      }
    }
    // Nothing left under it: a hole, or the open air below the lowest thing standing in this
    // column. Either way somewhere the paint leaves the room by.
    return Depth;
  }

  // The top of whatever it is buried in, for paint too far inside to be against a face.
  private float _roofOver(int column, float y) {
    var slot = column * SURFACES_PER_COLUMN;
    for (var i = 0; i < _surfaceCount[column]; i++) {
      if (_surface[slot + i] < y && _underside[slot + i] >= y) {
        return _surface[slot + i];
      }
    }
    return _surface[slot];
  }

  // Paint that has run into the side of something rather than onto the top of it: out the nearest
  // way, and for anything a particle can cross in one tick that is sideways. So it stacks up
  // against the face and spills back the way it came, instead of being carried up onto the roof of
  // whatever it walked into - which is the one move that reads as no physics at all.
  private void _clearWall(ref Vector2 p, int column, float fromY) {
    for (var step = 1; step <= _wallLook; step++) {
      var left = column - step;
      var right = column + step;
      var toLeft = left >= 0 && _surfaceUnder(left, fromY) >= 0f
        ? p.X - (((left + 1) * FIELD_CELL) - SKIN)
        : float.MaxValue;
      var toRight = right < _columns && _surfaceUnder(right, fromY) >= 0f
        ? ((right * FIELD_CELL) + SKIN) - p.X
        : float.MaxValue;
      if (toLeft == float.MaxValue && toRight == float.MaxValue) {
        continue;
      }

      p.X = toLeft <= toRight ? p.X - toLeft : p.X + toRight;
      var rest = _surfaceUnder(_sampleOf(p.X), fromY) - SurfaceLift;
      if (p.Y > rest) {
        p.Y = rest;
      }
      return;
    }

    // Deeper in than any face it could have crossed in a tick, so it is not against one: up onto
    // the top of whatever it is buried in.
    p.Y = _roofOver(column, fromY) - SurfaceLift;
  }

  // What the floor takes out of the paint sliding over it. Not only the layer actually touching it:
  // a surface drags on the liquid for some way up into it, and the paint above that is held back in
  // turn by Thickness. Gripping only the particles in contact leaves a body of paint travelling as
  // one block on a frictionless skin, which is a flood that never slows down however thick the
  // paint is said to be.
  private void _land() {
    for (var i = 0; i < _count; i++) {
      var surface = _surfaceUnder(_sampleOf(_at[i].X), _at[i].Y);
      if (surface < 0f) {
        continue;
      }
      var above = (surface - SurfaceLift) - _at[i].Y;
      if (above > _spacing) {
        continue;
      }

      // Full at the surface and easing off through the layer, so the paint shears against the floor
      // rather than stopping in a slab.
      var bite = 1f - Mathf.Max(above, 0f) / _spacing;
      _going[i] = new Vector2(
        _going[i].X * Mathf.Lerp(1f, FloorGrip, bite),
        above > 0.5f ? _going[i].Y : Mathf.Min(_going[i].Y, 0f) * Bounce);
    }
  }

  // Paint that has run off the end of the room, or that is standing over a hole, has left.
  private void _retire() {
    for (var i = _count - 1; i >= 0; i--) {
      var gone = _at[i].X >= Width - 1f
        || _at[i].Y >= Depth - 1f
        || _at[i].Y > Depth - SurfaceLift - 2f;
      if (!gone) {
        continue;
      }
      _count--;
      _at[i] = _at[_count];
      _going[i] = _going[_count];
      _bound[i] = _bound[_count];
    }
  }

  private int _sampleOf(float x) =>
    Mathf.Clamp(Mathf.FloorToInt(x / FIELD_CELL), 0, _columns - 1);

  // Where the level actually is under every column, all the way down rather than only as far as
  // the first thing the room has standing there. A room built in tiers has paint running along a
  // floor with another floor over it, and a level read as one height per column cannot hold that:
  // the surface over the paint is the only one it can see.
  //
  // One reused query: a room's worth is a few hundred casts, and building one apiece allocates
  // three engine objects a ray. Each cast after the first starts inside the surface it has already
  // found, which the ray passes through, so it reports the next one down.
  private void _readLevel() {
    var space = GetWorld2D().DirectSpaceState;
    _rayQuery ??= new PhysicsRayQueryParameters2D {
      CollisionMask = PhysicsLayers.Platform.Mask,
      CollideWithAreas = false,
      CollideWithBodies = true,
    };

    for (var i = 0; i < _columns; i++) {
      var x = (i + 0.5f) * FIELD_CELL;
      var slot = i * SURFACES_PER_COLUMN;
      var from = 0f;
      var found = 0;

      while (found < SURFACES_PER_COLUMN && from < Depth) {
        _rayQuery.From = ToGlobal(new Vector2(x, from));
        _rayQuery.To = ToGlobal(new Vector2(x, Depth));
        using var hit = space.IntersectRay(_rayQuery);
        if (hit.Count == 0) {
          break;
        }

        var top = ToLocal((Vector2)hit["position"]).Y;
        // A cast that came back where the last one did has grazed a skin rather than found a new
        // surface, and taking it would fill the column with the same one over and over.
        if (found > 0 && top <= _surface[slot + found - 1]) {
          break;
        }

        _surface[slot + found] = top;
        found++;
        from = top + SURFACE_STEP;
      }

      // How far down each of them goes, found by casting back up at it from the clear air above
      // whatever is next below - the one place in the column that is certain to be outside it.
      for (var s = 0; s < found; s++) {
        var below = s + 1 < found ? _surface[slot + s + 1] - SURFACE_STEP : Depth;
        _rayQuery.From = ToGlobal(new Vector2(x, below));
        _rayQuery.To = ToGlobal(new Vector2(x, _surface[slot + s]));
        using var hit = space.IntersectRay(_rayQuery);
        // Nothing coming back means the cast began inside this one, so it reaches at least to there.
        _underside[slot + s] = hit.Count == 0 ? below : ToLocal((Vector2)hit["position"]).Y;
      }

      _surfaceCount[i] = found;
    }
  }
  #endregion The level

  #region The surface
  // Every particle laid into a coarse field, which the shader reads a surface out of. This is what
  // turns a crowd into one body of paint - the field between two close particles is raised by both
  // of them, so the surface closes over the gap instead of showing two discs.
  private void _fill() {
    System.Array.Clear(_field);
    var spread = FIELD_REACH / FIELD_CELL;
    var span = Mathf.CeilToInt(spread);

    for (var i = 0; i < _count; i++) {
      var at = new Vector2(_at[i].X, _at[i].Y + HEADROOM) / FIELD_CELL;
      var cx = Mathf.FloorToInt(at.X);
      var cy = Mathf.FloorToInt(at.Y);

      for (var y = cy - span; y <= cy + span; y++) {
        if (y < 0 || y >= _fieldDown) {
          continue;
        }
        for (var x = cx - span; x <= cx + span; x++) {
          if (x < 0 || x >= _fieldAcross) {
            continue;
          }
          var dx = (x + 0.5f) - at.X;
          var dy = (y + 0.5f) - at.Y;
          var apart = (dx * dx) + (dy * dy);
          var falloff = 1f - (apart / (spread * spread));
          if (falloff <= 0f) {
            continue;
          }
          _field[(y * _fieldAcross) + x] += falloff * falloff;
        }
      }
    }

    _stainWhereItRan();
    _wearTheCoat();

    for (var i = 0; i < _field.Length; i++) {
      _fieldBytes[i * 2] = (byte)(Mathf.Clamp(_field[i] / FIELD_SCALE, 0f, 1f) * 255f);
      _fieldBytes[(i * 2) + 1] = (byte)(Mathf.Clamp(_stain[i], 0f, 1f) * 255f);
    }

    // One image a frame rather than one a particle. The texture behind it is kept and written
    // through, so the only thing built per frame is the wrapper the engine wants to be handed.
    var image = Image.CreateFromData(_fieldAcross, _fieldDown, false, Image.Format.Rg8, _fieldBytes);
    if (_fieldTexture is null) {
      _fieldTexture = ImageTexture.CreateFromImage(image);
      ((ShaderMaterial)_bodyNode.Material).SetShaderParameter(FieldParam, _fieldTexture);
    }
    else {
      _fieldTexture.Update(image);
    }
  }
  // What paint running along a surface leaves on it: a coat lying on the surface itself and
  // running off its underside, rather than a blob centred on the paint that left it. Half of such
  // a blob stands proud of the platform, which reads as a lump sitting on top of the level rather
  // than as a surface that has been painted.
  private void _stainWhereItRan() {
    if (!Stains) {
      return;
    }
    var enough = StainTrigger * DRAWN_AT;

    // Every surface stacked under the column, not only the one on top: paint running along a floor
    // with another floor over it coats the one it is actually on.
    for (var x = 0; x < _columns; x++) {
      var slot = x * SURFACES_PER_COLUMN;
      var count = _surfaceCount[x];
      for (var s = 0; s < count; s++) {
        if (_stained[slot + s]) {
          continue;
        }

        var surface = _surface[slot + s] + HEADROOM;
        var top = Mathf.RoundToInt(surface / FIELD_CELL);

        // Only where there is paint standing on this surface deep enough to be drawn.
        var standing = 0f;
        for (var y = top - STAIN_LOOK; y <= top; y++) {
          if (y < 0 || y >= _fieldDown) {
            continue;
          }
          standing = Mathf.Max(standing, _field[(y * _fieldAcross) + x]);
        }
        if (standing < enough) {
          continue;
        }

        // The shape a column dries in is fixed by where it is, so it is worked out the once and the
        // column is never looked at again however long the paint keeps running over it.
        _stained[slot + s] = true;
        _coatSpread = true;
        _layCoat(x, _surface[slot + s]);
      }
    }
  }

  // The coat down one column: a pool lying on the surface with drips running off its underside,
  // cut the same way the paint on an inked platform is cut. Written as a distance rather than as
  // a height, so the drips come out rounded at the tip and soft at the edges instead of as a row
  // of square teeth every one of which is the same square tooth.
  private void _layCoat(int x, float on) {
    var across = (x + 0.5f) * FIELD_CELL;
    var surface = on + HEADROOM;
    var top = Mathf.RoundToInt(surface / FIELD_CELL);
    var until = Mathf.RoundToInt((surface + StainDepth + StainDripLength) / FIELD_CELL);

    // The pool's own underside is not a ruled line either.
    var pool = StainDepth * (0.82f + (0.18f * _stainHash(Mathf.Floor(across / 40f) + 3f)));

    for (var y = top; y <= until; y++) {
      if (y < 0 || y >= _fieldDown) {
        continue;
      }

      var below = ((y + 0.5f) * FIELD_CELL) - surface;
      var distance = Mathf.Min(below - pool, _dripDistance(across, below));
      // Softened over a cell, so the silhouette is a curve rather than the staircase the grid
      // would otherwise cut it into.
      var covered = Mathf.Clamp(0.5f - (distance / FIELD_CELL), 0f, 1f);
      if (covered <= 0f) {
        continue;
      }

      var cell = (y * _fieldAcross) + x;
      if (covered > _stain[cell]) {
        _stain[cell] = covered;
      }
    }
  }

  // How far a point is outside the nearest drip. Every drip is cut from its own hash: how far it
  // runs, how thick it is, whether it swells or pinches on the way down, and how far off plumb it
  // hangs - so no two of them are the same drip and none of them is the average of the rest.
  private float _dripDistance(float across, float below) {
    var spacing = STAIN_DRIP_SPACING / Mathf.Max(StainDripDensity, 0.1f);
    var id = Mathf.Floor(across / spacing);
    var nearest = float.MaxValue;

    for (var k = -1; k <= 1; k++) {
      var i = id + k;
      var centre = (i + 0.5f + (0.62f * (_stainHash(i) - 0.5f))) * spacing;
      var length = StainDripLength * (0.14f + (0.86f * _stainHash(i + 17f)));
      var radius = spacing * 0.5f * StainDripWidth * (0.34f + (0.32f * _stainHash(i + 41f)));

      var along = Mathf.Clamp(below / Mathf.Max(length, 0.001f), 0f, 1f);
      // Thinning as it runs and barely gathering again at the end, so a drip ends in a rounded tip
      // rather than the head of a mushroom.
      var width = radius * (1f - (0.28f * along) + (0.22f * Mathf.SmoothStep(0.62f, 1f, along)));
      width *= 1f + ((_stainHash(i + 5f) - 0.5f) * 2f * STAIN_SWELL * Mathf.Sin(along * Mathf.Pi));

      var lean = (_stainHash(i + 63f) - 0.5f) * 2f * STAIN_LEAN;
      var sideways = across - (centre + (lean * below));
      var beyond = Mathf.Max(below - length, 0f);
      nearest = Mathf.Min(nearest, Mathf.Sqrt((sideways * sideways) + (beyond * beyond)) - width);
    }

    return nearest;
  }

  // The coat as something that can be touched, not only something drawn. A platform somebody
  // painted wears an area in the ink's colour and answers to whichever face lands on it; ground the
  // flood ran over has to answer the same way, or the paint it leaves behind is scenery a player
  // walks through.
  //
  // One band per unbroken stretch of coat rather than one per column: the whole run is a few
  // hundred columns wide and the player is not.
  private void _wearTheCoat() {
    if (!_coatSpread) {
      return;
    }
    _coatSpread = false;

    var worn = 0;
    for (var s = 0; s < SURFACES_PER_COLUMN; s++) {
      var from = -1;
      var on = _surface[s];

      for (var x = 0; x <= _columns; x++) {
        var slot = (x * SURFACES_PER_COLUMN) + s;
        var here = x < _columns && s < _surfaceCount[x] && _stained[slot];
        // A run also ends where the ground under it steps, so a band lies along the surface it
        // belongs to instead of being stretched across a riser and left hanging in the air.
        var stepped = here && from >= 0 && Mathf.Abs(_surface[slot] - on) > FIELD_CELL;

        if (here && !stepped) {
          if (from < 0) {
            from = x;
          }
          on = _surface[slot];
          continue;
        }

        if (from >= 0) {
          worn = _wearBand(worn, from, x - 1, on);
        }
        from = here ? x : -1;
        on = here ? _surface[slot] : on;
      }
    }

    for (var i = worn; i < _coats.Count; i++) {
      _coats[i].Monitorable = false;
    }
  }

  private int _wearBand(int worn, int from, int to, float on) {
    while (worn >= _coats.Count) {
      // Layered and grouped like an inked platform, because it is meant to be the same surface: the
      // faces are what decide whether landing on it is survivable, and they only ask the group.
      var area = new Area2D {
        CollisionLayer = PhysicsLayers.Platform.Mask,
        CollisionMask = PhysicsLayers.BoxFace.Mask,
        Monitoring = false,
        Monitorable = false,
      };
      area.AddToGroup(Group);
      area.AddChild(new CollisionShape2D { Shape = new RectangleShape2D() });
      AddChild(area);
      _coats.Add(area);
    }

    var left = from * FIELD_CELL;
    var wide = ((to + 1) * FIELD_CELL) - left;
    // Deep enough to be landed on however thin the coat is drawn, and no deeper than the coat: a
    // band hanging below the surface is a kill nobody can see the reason for.
    var deep = Mathf.Max(StainDepth, FIELD_CELL);

    var coat = _coats[worn];
    coat.Position = new Vector2(left + (wide / 2f), on + (deep / 2f));
    coat.Monitorable = true;
    ((RectangleShape2D)coat.GetChild<CollisionShape2D>(0).Shape).Size = new Vector2(wide, deep);
    return worn + 1;
  }

  // The coat, worked out again from which columns dried. Everything about how one dries - how deep
  // it lies, how far its drips run, which way they lean - is fixed by where it is and the seed, so
  // which columns they were is the whole of what has to be remembered.
  private void _relayCoats() {
    System.Array.Clear(_stain);
    for (var x = 0; x < _columns; x++) {
      var slot = x * SURFACES_PER_COLUMN;
      for (var s = 0; s < _surfaceCount[x]; s++) {
        if (_stained[slot + s]) {
          _layCoat(x, _surface[slot + s]);
        }
      }
    }
  }

  private float _stainHash(float n) {
    var v = Mathf.Sin((n * 127.1f) + (StainSeed * 13.7f)) * 43758.5453f;
    return v - Mathf.Floor(v);
  }

  #endregion The surface

  #region The set piece
  public void Pour() {
    // A bucket that has already been emptied has nothing left to pour, however many times the
    // player comes back into the room. The flood ends by leaving the room at rest, so without
    // this a player who walked back into the trigger would set the whole thing off again - and so
    // would one who loaded a saved game taken after the room was already crossed.
    if (_state == State.Waiting && _poured == 0) {
      _begin();
    }
  }

  public bool IsRunning => _state != State.Waiting;

  public int ParticleCount => _count;

  // The lowest any of it has got, which is how a test tells paint lying on a floor from paint that
  // fell straight past one.
  public float LowestY {
    get {
      var lowest = 0f;
      for (var i = 0; i < _count; i++) {
        lowest = Mathf.Max(lowest, _at[i].Y);
      }
      return lowest;
    }
  }

  // How far along the run the flood has reached. Measured off the drawn body rather than off the
  // furthest particle: a fluid flings the odd drop a long way ahead of itself, and one of those is
  // neither what the player sees coming nor what catches them - taking the furthest particle
  // measures the spray and reports a front that can travel backwards when a drop lands.
  public float FrontX {
    get {
      var enough = StainTrigger * DRAWN_AT;
      for (var x = _columns - 1; x >= 0; x--) {
        var slot = x * SURFACES_PER_COLUMN;
        for (var s = 0; s < _surfaceCount[x]; s++) {
          var top = Mathf.RoundToInt((_surface[slot + s] + HEADROOM) / FIELD_CELL);
          for (var y = top - STAIN_LOOK; y <= top; y++) {
            if (y < 0 || y >= _fieldDown) {
              continue;
            }
            if (_field[(y * _fieldAcross) + x] >= enough) {
              return (x + 0.5f) * FIELD_CELL;
            }
          }
        }
      }
      return 0f;
    }
  }

  private void _begin() {
    _state = State.Tipping;
    _elapsed = 0f;
    _owed = 0f;
    _poured = 0;
    _sound = 0f;
    _hasCaught = false;
    _scatter.Seed = (ulong)PourSeed;

    if (CameraZoom > 0f && _level is not null) {
      _zoomBefore = _level.CameraNode.TargetZoom;
      _level.CameraNode.ZoomTo(CameraZoom);
    }
  }

  // Slow to leave, then over all at once, the way something heavy goes past its own balance.
  private static float _tipEase(float through) {
    var over = Mathf.Clamp(through, 0f, 1f);
    return over * over * ((2.2f * over) - 1.2f);
  }

  // Turned about the corner it stands on rather than about the middle of its base. A bucket spun
  // about its middle swings half of itself through the floor before it is halfway over.
  private void _tipTo(float turned) {
    var corner = new Vector2(_spoutNode.HalfWidth, 0f);
    _spoutNode.Rotation = turned;
    _spoutNode.Position = SpoutOffset + corner - corner.Rotated(turned);
    _mouthX = (_spoutNode.Position + new Vector2(0f, -_spoutNode.Height).Rotated(turned)).X;
    // Its back end, which is as far back as any of the paint may get.
    _dam = Mathf.Max(_spoutNode.Position.X, 0f);
  }

  private void _emit(float delta) {
    var budget = Mathf.FloorToInt(PourRate * PourDuration);
    if (_poured >= budget) {
      _state = State.Spent;
      _sprayNode.Emitting = false;
      return;
    }

    _owed += PourRate * delta;
    var mouth = _spoutNode.Position + new Vector2(0f, -_spoutNode.Height).Rotated(_spoutNode.Rotation);

    while (_owed >= 1f && _count < MAX_PARTICLES && _poured < budget) {
      _owed -= 1f;
      _poured++;
      var across = _scatter.RandfRange(-0.5f, 0.5f) * SpoutWidth;
      var along = _scatter.Randf() * _spacing;
      _at[_count] = new Vector2(
        Mathf.Clamp(mouth.X + across, 1f, Width - 1f), mouth.Y - along);
      _going[_count] = new Vector2(PourSpeed * 0.35f, PourSpeed);
      _bound[_count] = _at[_count];
      _count++;
    }
  }

  // Read against the particles rather than through an Area2D: the paint is a different depth over
  // every step it has reached, and one box drawn round the whole of it would drown a cube standing
  // dry on a step the paint has only lapped the foot of.
  private void _catchPlayer() {
    if (_hasCaught || _count == 0) {
      return;
    }

    var player = _level?.PlayerNode;
    if (player is null || player.IsDying()) {
      return;
    }

    var local = ToLocal(player.GlobalPosition);
    var half = player.GetCollisionHalfExtents();
    var turn = player.GlobalRotation;
    var reach = new Vector2(
      (half.X * Mathf.Abs(Mathf.Cos(turn))) + (half.Y * Mathf.Abs(Mathf.Sin(turn))),
      (half.X * Mathf.Abs(Mathf.Sin(turn))) + (half.Y * Mathf.Abs(Mathf.Cos(turn))));

    // A handful of them rather than one, so a single flung drop is spray on the paintwork and not
    // a death the player cannot see coming.
    var touching = 0;
    for (var i = 0; i < _count; i++) {
      var gap = (_at[i] - local).Abs() - reach;
      if (gap.X < _spacing && gap.Y < _spacing) {
        touching++;
        if (touching < 5) {
          continue;
        }
        _hasCaught = true;
        GameEvents.Instance.OnPlayerDying(this, player.GlobalPosition, EntityType.Platform);
        return;
      }
    }
  }

  private void _followFront() {
    if (_count == 0) {
      _sprayNode.Emitting = false;
      if (_state == State.Spent) {
        _finish();
      }
      return;
    }

    var lead = 0;
    for (var i = 1; i < _count; i++) {
      if (_at[i].X > _at[lead].X) {
        lead = i;
      }
    }
    _sprayNode.Position = _at[lead];
    _sprayNode.Emitting = _state == State.Pouring;
  }

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

  private void _onCheckpointLoaded() {
    // Subscribed in _EnterTree, one step before _Ready: a reload arriving in between has nothing
    // to put away.
    if (!IsNodeReady()) {
      return;
    }
    if (_remembered is null) {
      _reset();
      return;
    }
    _recall();
  }

  // The room as the player first met it, for a retry from before any of this happened.
  private void _reset() {
    _count = 0;
    _state = State.Waiting;
    _hasCaught = false;
    _elapsed = 0f;
    _owed = 0f;
    _poured = 0;
    _needsLevel = true;

    System.Array.Clear(_stain);
    System.Array.Clear(_stained);
    _coatSpread = false;
    foreach (var coat in _coats) {
      coat.Monitorable = false;
    }
    _tipTo(0f);
    _spoutNode.Fill();
    _sprayNode.Emitting = false;
    _fill();
  }

  // What the room was like when the player last reached a checkpoint. A set piece is only worth
  // retrying from as far as it was got to: sent back to a checkpoint beyond the flood, a player
  // who had already outrun it would find the bucket standing full again and the room dry, and be
  // made to run it a second time to reach the point they had already reached.
  private void _onCheckpointReached(Vector2 position, string colorGroup) {
    if (!IsNodeReady() || Engine.IsEditorHint()) {
      return;
    }

    _remembered ??= new Remembered {
      At = new Vector2[MAX_PARTICLES],
      Going = new Vector2[MAX_PARTICLES],
      Stained = new bool[_stained.Length],
    };

    _remembered.State = _state;
    _remembered.Elapsed = _elapsed;
    _remembered.Owed = _owed;
    _remembered.Poured = _poured;
    _remembered.HasCaught = _hasCaught;
    _remembered.Sound = _sound;
    // Where the scatter had got to, not the seed it started from: the pour is only the same run
    // twice if it carries on drawing from where it left off.
    _remembered.Scatter = _scatter.State;
    _remembered.ZoomBefore = _zoomBefore;
    _remembered.Count = _count;

    System.Array.Copy(_at, _remembered.At, _count);
    System.Array.Copy(_going, _remembered.Going, _count);
    System.Array.Copy(_stained, _remembered.Stained, _stained.Length);
  }

  private void _recall() {
    var was = _remembered!;
    // Whether the room has been measured yet. Coming back from a death it has; coming back from a
    // saved game the level has only just been built, so the coat waits for the first tick.
    var measured = !_needsLevel;
    _state = was.State;
    _elapsed = was.Elapsed;
    _owed = was.Owed;
    _poured = was.Poured;
    _hasCaught = was.HasCaught;
    _sound = was.Sound;
    _scatter.State = was.Scatter;
    _zoomBefore = was.ZoomBefore;
    _count = was.Count;
    _needsLevel = true;

    System.Array.Copy(was.At, _at, _count);
    System.Array.Copy(was.Going, _going, _count);
    System.Array.Copy(was.At, _bound, _count);
    System.Array.Copy(was.Stained, _stained, _stained.Length);

    if (measured) {
      _relayCoats();
    }
    else {
      _relayCoat = true;
    }

    // Both of these are read off how far through the pour it was rather than kept, because they
    // are only ever a function of it.
    _tipTo(_elapsed >= TIP_DURATION ? TIP_ANGLE : TIP_ANGLE * _tipEase(_elapsed / TIP_DURATION));
    // Interpolation is on for the whole project, so a bucket put back part-way over would
    // otherwise be drawn swinging there from wherever it was standing.
    _spoutNode.ResetPhysicsInterpolation();
    if (_state == State.Waiting || (_state == State.Tipping && _elapsed < POUR_DELAY)) {
      _spoutNode.Fill();
    }
    else {
      _spoutNode.Empty();
    }

    _sprayNode.Emitting = false;
    // The coat is worked out from which columns had dried, so it comes back with them.
    _coatSpread = true;
    _fill();

    // Pulled back again if the flood was still running, and left to the next tick so that whatever
    // the camera does about the reload itself has already happened by the time this lands.
    _pullBack = CameraZoom > 0f && _state is State.Tipping or State.Pouring;
  }
  #endregion The set piece

  #region Setting up
  private void _allocate() {
    // The kernels, worked out once. Both are the two-dimensional forms - the three-dimensional
    // ones every derivation is written in leave the paint far too loose to hold a surface.
    _reach = Mathf.Max(ParticleReach, 4f);
    _spacing = Mathf.Clamp(ParticleSpacing, 2f, _reach * 0.9f);

    _kernel = 4f / (Mathf.Pi * Mathf.Pow(_reach, 8f));
    _kernelSlope = -30f / (Mathf.Pi * Mathf.Pow(_reach, 5f));
    _cohesionAt = _weight(COHESION_RANGE * _reach);

    // How packed paint is when it is left alone, counted off a lattice at the spacing the
    // particles are poured at rather than guessed, and how much its neighbours pull on it there.
    // The second is what Relaxation is a multiple of, so that the knob means the same thing
    // whatever size the particles are: on its own it is a number with no scale of its own, and
    // anything but the right one either sets the paint solid or lets it squash to nothing.
    _restPacked = 0f;
    var restSlope = 0f;
    var reachInCells = Mathf.CeilToInt(_reach / _spacing);
    for (var y = -reachInCells; y <= reachInCells; y++) {
      for (var x = -reachInCells; x <= reachInCells; x++) {
        var distance = new Vector2(x, y).Length() * _spacing;
        if (distance >= _reach) {
          continue;
        }
        _restPacked += _weight(distance);
        if (distance > 0f) {
          restSlope += _slope(new Vector2(x, y) * _spacing, distance).LengthSquared();
        }
      }
    }
    _restSpread = restSlope / (_restPacked * _restPacked);

    _cellsAcross = Mathf.Max(Mathf.CeilToInt(Width / _reach), 1);
    _cellsDown = Mathf.Max(Mathf.CeilToInt((Depth + HEADROOM) / _reach), 1);
    _cellStart = new int[(_cellsAcross * _cellsDown) + 1];

    _fieldAcross = Mathf.Max(Mathf.CeilToInt(Width / FIELD_CELL), 1);
    _fieldDown = Mathf.Max(Mathf.CeilToInt((Depth + HEADROOM) / FIELD_CELL), 1);
    _field = new float[_fieldAcross * _fieldDown];
    _stain = new float[_field.Length];
    // Two channels: what is flowing, and what it left behind.
    _fieldBytes = new byte[_field.Length * 2];

    _columns = _fieldAcross;
    _surface = new float[_columns * SURFACES_PER_COLUMN];
    _underside = new float[_columns * SURFACES_PER_COLUMN];
    _surfaceCount = new int[_columns];
    _stained = new bool[_columns * SURFACES_PER_COLUMN];
    _wallLook = Mathf.Max(Mathf.CeilToInt(_reach / FIELD_CELL), 1);

    _bodyNode.Position = new Vector2(0f, -HEADROOM);
    _bodyNode.Size = new Vector2(Width, Depth + HEADROOM);
  }

  private void _applyColor() {
    var skinColor = GameSkin.ColorGroupToSkinColor(Group);
    var skin = SkinManager.Instance.CurrentSkin;
    _paintColor = skin.GetColor(skinColor, PAINT);
    _shadeColor = skin.GetColor(skinColor, PAINT_SHADE);
    _sprayNode.Color = _paintColor;

    if (_bodyNode.Material is ShaderMaterial material) {
      material.SetShaderParameter(ColorParam, _paintColor);
      material.SetShaderParameter(ShadeParam, _shadeColor);
    }
  }

  private void _applyTrigger() {
    _triggerNode.Position = TriggerOffset;
    if (_triggerShapeNode.Shape is RectangleShape2D rectangle) {
      rectangle.Size = TriggerSize;
    }
  }

  public override void _Draw() {
    if (!Engine.IsEditorHint()) {
      return;
    }
    var run = new Rect2(0f, 0f, Width, Depth);
    DrawRect(run, new Color(0.6f, 0.2f, 1f, 0.10f));
    DrawRect(run, new Color(0.6f, 0.2f, 1f, 0.55f), filled: false, width: 2f);
    DrawRect(
      new Rect2(TriggerOffset - (TriggerSize / 2f), TriggerSize),
      new Color(1f, 0.85f, 0.2f, 0.75f), filled: false, width: 2f);
  }
  #endregion Setting up

  #region Persistence
  // What a saved game carries. Not the paint that was in the air - a checkpoint is not put in the
  // middle of the chase, so what is worth keeping is whether the bucket has gone over and what the
  // flood dried onto on its way past. The coat itself is not written down, only which columns wear
  // it, because the rest of it follows from that and the seed.
  private sealed record SaveData(
    int State = 0,
    float Elapsed = 0f,
    float Owed = 0f,
    int Poured = 0,
    bool HasCaught = false,
    ulong Scatter = 0,
    float ZoomBefore = 0f,
    string Stained = "");

  public string GetSaveId() => GetPath();

  public string Save(ISerializer serializer) {
    var was = _remembered;
    return serializer.Serialize(new SaveData(
      (int)(was?.State ?? State.Waiting),
      was?.Elapsed ?? 0f,
      was?.Owed ?? 0f,
      was?.Poured ?? 0,
      was?.HasCaught ?? false,
      was?.Scatter ?? 0,
      was?.ZoomBefore ?? 0f,
      was is null ? string.Empty : _asBits(was.Stained)));
  }

  public void Load(ISerializer serializer, string data) {
    // The arrays it is read into are built when the room is, so there is nothing to read into yet
    // if the level is still being put together.
    if (!IsNodeReady()) {
      Callable.From(() => Load(serializer, data)).CallDeferred();
      return;
    }

    var saved = serializer.Deserialize<SaveData>(data);
    if (saved is null) {
      return;
    }

    _remembered = new Remembered {
      State = (State)saved.State,
      Elapsed = saved.Elapsed,
      Owed = saved.Owed,
      Poured = saved.Poured,
      HasCaught = saved.HasCaught,
      Scatter = saved.Scatter,
      ZoomBefore = saved.ZoomBefore,
      Count = 0,
      At = new Vector2[MAX_PARTICLES],
      Going = new Vector2[MAX_PARTICLES],
      Stained = new bool[_stained.Length],
    };
    _fromBits(saved.Stained, _remembered.Stained);
    _recall();
  }

  // One bit per column per surface. There are a few thousand of them and all but a handful are the
  // same answer, so writing them out as anything wordier would be most of the save file.
  private static string _asBits(bool[] bits) {
    var bytes = new byte[(bits.Length + 7) / 8];
    for (var i = 0; i < bits.Length; i++) {
      if (bits[i]) {
        bytes[i >> 3] |= (byte)(1 << (i & 7));
      }
    }
    return System.Convert.ToBase64String(bytes);
  }

  private static void _fromBits(string packed, bool[] into) {
    System.Array.Clear(into);
    if (packed.Length == 0) {
      return;
    }
    var bytes = System.Convert.FromBase64String(packed);
    for (var i = 0; i < into.Length && (i >> 3) < bytes.Length; i++) {
      into[i] = (bytes[i >> 3] & (1 << (i & 7))) != 0;
    }
  }
  #endregion Persistence
}
