namespace Wfc.Entities.World.Paint;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Screens.Levels;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Utils.Colors;
using Wfc.Utils.Layers;
using EventHandler = Wfc.Core.Event.EventHandler;

// A paint gun bolted to the ceiling, which follows the cube around the room and fires at it. What
// it throws is paint of its own colour: it coats whatever it hits, and the cube crossing that or
// meeting the shot in the air lives or dies by the face it has toward it, exactly as with anything
// else in the level that is a colour.
//
// It hangs upside down, so the whole of it reads from the ceiling downwards - a plate and a cone
// that never move, and under them a body that turns. The hose that feeds the body is the one part
// with no sprite of its own: it runs between something fixed and something that turns, so its
// shape is a consequence of where the gun is pointing rather than a picture that could be drawn
// once.
//
// The paint it leaves does not last. A bucket is emptied once and what it leaves is authored
// around; a gun fires all day, and a room whose floor is slowly painted end to end stops being a
// puzzle about colour.
[Tool]
[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class SplashGun : Node2D {
  #region Constants
  // The three tints the gun is cut from, lightest first. The shell is the housing, the trim is the
  // collar and the plate it hangs off, and the paint is what it is full of.
  private const SkinColorIntensity SHELL = SkinColorIntensity.Background;
  private const SkinColorIntensity TRIM = SkinColorIntensity.VeryLight;
  private const SkinColorIntensity PAINT = SkinColorIntensity.Basic;

  // The hose is rubber rather than paint, so it is the one part the level's colour does not reach.
  private static readonly StringName LevelParam = "u_level";

  private static readonly Color HOSE = new Color(0.925f, 0.941f, 0.945f);

  // Where the hose leaves the ceiling, and how much slack it hangs with. The slack is what makes it
  // read as a hose rather than a wire: a straight line between two moving points looks pulled tight
  // however far apart they are.
  private static readonly Vector2 HOSE_ANCHOR = new Vector2(104f, 10f);
  private const float HOSE_SLACK = 64f;
  private const int HOSE_POINTS = 16;

  // The gun points this way when it is not aiming at anything, which is the way the art is drawn.
  private static readonly Vector2 REST = Vector2.Left;

  // How many times the aim is walked round before it is used. Two is already past what can be seen.
  private const int AIM_PASSES = 6;
  private const float AIM_DAMPING = 0.5f;

  // Under this much off, the gun is pointed where it wants to be and is left alone. Without it the
  // last hundredth of a degree is chased every frame and the barrel never stops moving.
  private const float AIM_SETTLED = 0.4f;

  // How far up the cable the ink runs before the tank starts to show any. Without it the tank fills
  // from the first instant and the cable is decoration rather than where the ink is coming from.
  private const float INK_LEAD = 0.35f;
  #endregion Constants

  #region Dependencies
  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public IGameLevel GameLevel => this.DependOn<IGameLevel>();

  // Allowed to come up empty, so the gun can be opened on its own to look at.
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
  [Export(PropertyHint.Enum, "blue,pink,yellow,purple")]
  public string Group {
    get => _group;
    set {
      _group = value;
      _applyColor();
    }
  }
  private string _group = ColorUtils.PURPLE;

  // How far it will turn either way from where the art points, so a gun in a corner cannot swing
  // round into the wall it is bolted to.
  [Export(PropertyHint.Range, "-180,180,1")]
  public float MinAngle { get; set; } = -155f;

  [Export(PropertyHint.Range, "-180,180,1")]
  public float MaxAngle { get; set; } = -25f;

  // Degrees a second. Slow enough that running past it is a way of beating it rather than a thing
  // that never works.
  [Export(PropertyHint.Range, "10,720,5")]
  public float TurnSpeed { get; set; } = 150f;

  [Export(PropertyHint.Range, "0.2,8,0.1")]
  public float FireInterval { get; set; } = 1.7f;

  [Export]
  public float ShotSpeed { get; set; } = 820f;

  // How far away it bothers with the cube at all. Past this it sits still, which is also what tells
  // the player which rooms it owns.
  [Export]
  public float Range { get; set; } = 1500f;

  // How near the barrel has to be to on-target before it will fire. Without it a gun whose arc
  // cannot reach the cube keeps shooting at the wall it is aimed into, and one that has only just
  // noticed lets go while it is still swinging round.
  [Export(PropertyHint.Range, "1,45,1")]
  public float AimTolerance { get; set; } = 12f;

  // How wide a run of floor a shot paints, and how long that paint lasts.
  [Export]
  public float SplashWidth { get; set; } = 180f;

  [Export(PropertyHint.Range, "1,30,0.5")]
  public float PaintLife { get; set; } = 6f;

  // How many shots the tank holds, and how long it takes to fill again once it is dry. The wait is
  // the gun's cooldown, and the tank is what says so: a gun that simply stopped firing for a while
  // reads as broken, where one visibly drawing ink back up the cable reads as reloading.
  [Export(PropertyHint.Range, "1,10,1")]
  public int ShotsPerTank { get; set; } = 2;

  [Export(PropertyHint.Range, "0.3,10,0.1")]
  public float RefillTime { get; set; } = 2.2f;
  #endregion Exports

  #region Nodes
  [NodePath("Mount")]
  private Node2D _mountNode = default!;
  [NodePath("Mount/Hose")]
  private Line2D _hoseNode = default!;
  [NodePath("Mount/HoseInk")]
  private Line2D _hoseInkNode = default!;
  [NodePath("Arm/Tank")]
  private Sprite2D _tankNode = default!;
  [NodePath("Arm")]
  private Node2D _armNode = default!;
  [NodePath("Arm/PortCable")]
  private Sprite2D _portNode = default!;
  [NodePath("Arm/Muzzle")]
  private Marker2D _muzzleNode = default!;
  [NodePath("Fire")]
  private Timer _fireNode = default!;
  #endregion Nodes

  #region Fields
  private IGameLevel? _level;
  private bool _isWired;
  private PhysicsRayQueryParameters2D? _sightQuery;
  private readonly List<Sprite2D> _shell = [];
  private readonly List<Sprite2D> _trim = [];
  private readonly List<Sprite2D> _paint = [];

  // How much is left in the tank, as a share of full, and how far the ink drawn up the cable has
  // got while it is filling.
  private float _ink = 1f;
  private float _drawnUp;
  private bool _refilling;
  private bool _isSubscribed;
  private float _armRest;
  private Vector2 _tankHome;
  #endregion Fields

  public override void _EnterTree() {
    base._EnterTree();
    if (Engine.IsEditorHint() || _isSubscribed) {
      return;
    }
    EventHandler.Instance.Events.CheckpointLoaded += _onCheckpointLoaded;
    _isSubscribed = true;
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (!_isSubscribed) {
      return;
    }
    EventHandler.Instance.Events.CheckpointLoaded -= _onCheckpointLoaded;
    _isSubscribed = false;
  }

  // A retry meets the gun as the player first met it: tank full, barrel where it was left, and no
  // cooldown half spent. A gun caught mid-refill would hand the returning player a free run past it
  // that the attempt they died on never had.
  private void _onCheckpointLoaded() {
    if (!IsNodeReady()) {
      return;
    }
    _ink = 1f;
    _refilling = false;
    _drawnUp = 0f;
    _showInk();
    _armNode.Rotation = _armRest;
    // Put back rather than swung back: interpolation is on for the whole project, and the barrel
    // would otherwise be drawn sweeping round from wherever it was pointing when the player died.
    _armNode.ResetPhysicsInterpolation();
    _layHose();
    _fireNode.Start();
  }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _sortParts();
    _isWired = true;
    _tankHome = _tankNode.Position;
    _armRest = _armNode.Rotation;
    _applyColor();
    _showInk();
    _layHose();

    if (Engine.IsEditorHint()) {
      SetPhysicsProcess(false);
      return;
    }

    _fireNode.WaitTime = FireInterval;
    _fireNode.Timeout += _onFire;
    _fireNode.Start();
  }

  public override void _PhysicsProcess(double delta) {
    if (Engine.IsEditorHint()) {
      return;
    }
    _aim((float)delta);
    _refill((float)delta);
    _layHose();
  }

  // Ink coming back up the cable and into the tank. Nothing else is the cooldown - the gun is out
  // of paint until this finishes, and how far the ink has got is how long is left.
  private void _refill(float delta) {
    if (!_refilling) {
      return;
    }
    _drawnUp = Mathf.Min(_drawnUp + (delta / Mathf.Max(RefillTime, 0.01f)), 1f);
    // The tank fills behind the ink rather than with it, so the cable is seen to carry it there.
    _ink = Mathf.Max((_drawnUp - INK_LEAD) / (1f - INK_LEAD), 0f);
    _showInk();
    if (_ink >= 1f) {
      _refilling = false;
      _drawnUp = 0f;
    }
  }

  // What is left in the tank, shown by how much of it is ink. The sprite is cut to the ink rather
  // than squashed to it, so the level reads as a surface at a height instead of the whole tank
  // shrinking.
  private void _showInk() {
    var full = Mathf.Clamp(_ink, 0f, 1f);
    _tankNode.Visible = full > 0.01f;
    if (!_tankNode.Visible || _tankNode.Material is not ShaderMaterial ink) {
      return;
    }

    // How high the tank reaches in the world right now, which is not its own height once the gun
    // has turned: the corners swap places as it goes round, so they are all four asked.
    var size = _tankNode.Texture.GetSize();
    var top = float.MaxValue;
    var bottom = float.MinValue;
    for (var corner = 0; corner < 4; corner++) {
      var at = _tankNode.ToGlobal(new Vector2((corner & 1) * size.X, (corner >> 1) * size.Y)).Y;
      top = Mathf.Min(top, at);
      bottom = Mathf.Max(bottom, at);
    }
    ink.SetShaderParameter(LevelParam, bottom - ((bottom - top) * full));
  }

  // Which tint each part wears, decided by where it is rather than listed by name: the housing and
  // the barrel are the shell, whatever hangs off the ceiling is trim, and the rest is paint.
  private void _sortParts() {
    foreach (var part in new[] { "Body", "Barrel", "Nose" }) {
      _shell.Add(_armNode.GetNode<Sprite2D>(part));
    }
    foreach (var part in new[] { "Plate", "Ball" }) {
      _shell.Add(_mountNode.GetNode<Sprite2D>(part));
    }
    // The empty tank is the housing showing through, so it wears the housing's own tint.
    _shell.Add(_armNode.GetNode<Sprite2D>("TankShell"));
    foreach (var part in new[] { "Collar", "Neck" }) {
      _trim.Add(_mountNode.GetNode<Sprite2D>(part));
    }
    _trim.Add(_armNode.GetNode<Sprite2D>("Port"));
    foreach (var part in new[] { "Panel", "Splash", "Tank", "MuzzlePaint", "PortColor" }) {
      _paint.Add(_armNode.GetNode<Sprite2D>(part));
    }
  }

  private void _applyColor() {
    if (!_isWired) {
      return;
    }
    var skin = SkinManager.Instance.CurrentSkin;
    var color = GameSkin.ColorGroupToSkinColor(Group);

    foreach (var part in _shell) {
      part.Modulate = skin.GetColor(color, SHELL);
    }
    foreach (var part in _trim) {
      part.Modulate = skin.GetColor(color, TRIM);
    }
    foreach (var part in _paint) {
      part.Modulate = skin.GetColor(color, PAINT);
    }
    _hoseNode.DefaultColor = HOSE;
    _hoseInkNode.DefaultColor = skin.GetColor(color, PAINT);
  }

  // Turned toward the cube at a speed of its own rather than snapped onto it, so that outrunning
  // the barrel is something the player can do.
  private void _aim(float delta) {
    var player = _level?.PlayerNode;
    if (player is null) {
      return;
    }

    var toward = player.GlobalPosition - _armNode.GlobalPosition;
    if (toward.LengthSquared() > Range * Range) {
      return;
    }

    var want = _aimFor(player.GlobalPosition);
    var turn = Mathf.Wrap(want - _armNode.Rotation, -Mathf.Pi, Mathf.Pi);
    if (Mathf.Abs(turn) < Mathf.DegToRad(AIM_SETTLED)) {
      return;
    }
    var step = Mathf.DegToRad(TurnSpeed) * delta;
    _armNode.Rotation = Mathf.Clamp(
      _armNode.Rotation + Mathf.Clamp(turn, -step, step),
      Mathf.DegToRad(MinAngle),
      Mathf.DegToRad(MaxAngle));
  }

  // Where the gun has to be turned to put a shot on a point.
  //
  // Two things stop this being the direction from the joint to the cube. The shot leaves the muzzle,
  // which is most of the gun's length out from the joint and swings as it turns - aiming the joint
  // leaves the barrel's line running past the target, off by the whole of that offset. And the paint
  // falls on the way, so the barrel has to be lifted by however far it will drop.
  //
  // The muzzle's position depends on the angle, and the angle depends on where the muzzle is, so it
  // is settled by going round a few times rather than solved outright. It converges immediately -
  // each pass moves the muzzle by less than the last.
  private float _aimFor(Vector2 target) {
    var turned = _armNode.Rotation;
    var pivot = _armNode.GlobalPosition;
    var toward = target - pivot;
    for (var pass = 0; pass < AIM_PASSES; pass++) {
      var muzzle = pivot + _muzzleNode.Position.Rotated(turned + GlobalRotation);
      toward = target - muzzle;
      var solved = _throwAt(toward) - REST.Angle() - GlobalRotation;
      // Halfway to the answer each time rather than all the way. Going all the way lets the muzzle
      // swing past on every pass and come back on the next, and the gun sits there shaking between
      // two answers for ever instead of settling on one.
      turned += Mathf.Wrap(solved - turned, -Mathf.Pi, Mathf.Pi) * AIM_DAMPING;
    }
    return turned;
  }

  // The flatter of the two arcs that reach a point, or straight at it when none does - a gun that
  // cannot reach still looks like it is trying, and the shot falling short says so better than the
  // barrel pointing at the sky.
  private float _throwAt(Vector2 toward) {
    var across = Mathf.Abs(toward.X);
    var up = -toward.Y;
    var speed = ShotSpeed * ShotSpeed;
    var reach = (speed * speed) - (SplashShot.GRAVITY * ((SplashShot.GRAVITY * across * across) + (2f * up * speed)));
    if (across < 1f || reach < 0f) {
      return toward.Angle();
    }

    var climb = (speed - Mathf.Sqrt(reach)) / (SplashShot.GRAVITY * across);
    return new Vector2(Mathf.Sign(toward.X), -climb).Angle();
  }

  // The hose, from the fixed fitting on the ceiling down to the port on the body, hanging slack in
  // between. Both ends are read where they actually are, so the shape follows the gun round rather
  // than being a drawing that happens to fit one pose.
  // Whether there is anything to shoot at: near enough, actually in front of the barrel rather than
  // behind the arc it is allowed to swing through, and with nothing standing in the way. A gun that
  // fires at a cube it cannot reach paints the wall it is pointed at over and over.
  private bool _canReach() {
    // Nothing to fire while the tank is being filled: it is not ready until it is full, which is
    // what the player is watching the cable for.
    if (_refilling) {
      return false;
    }
    var player = _level?.PlayerNode;
    if (player is null || player.IsDying()) {
      return false;
    }

    var at = player.GlobalPosition;
    var toward = at - _armNode.GlobalPosition;
    if (toward.LengthSquared() > Range * Range) {
      return false;
    }

    // Whether it has finished turning onto its firing solution - not whether the barrel is pointed
    // straight at the cube, which it deliberately is not: it leads by however far the paint will
    // drop. Measured against the arc as well, so a cube the clamp will never let it reach is one it
    // keeps quiet about instead of firing past for ever.
    var wanted = Mathf.Clamp(_aimFor(at), Mathf.DegToRad(MinAngle), Mathf.DegToRad(MaxAngle));
    if (Mathf.Abs(Mathf.Wrap(wanted - _armNode.Rotation, -Mathf.Pi, Mathf.Pi)) > Mathf.DegToRad(AimTolerance)) {
      return false;
    }

    _sightQuery ??= new PhysicsRayQueryParameters2D {
      CollisionMask = PhysicsLayers.Platform.Mask,
      CollideWithAreas = false,
      CollideWithBodies = true,
    };
    _sightQuery.From = _muzzleNode.GlobalPosition;
    _sightQuery.To = at;
    using var blocked = GetWorld2D().DirectSpaceState.IntersectRay(_sightQuery);
    return blocked.Count == 0;
  }

  private void _layHose() {
    var from = HOSE_ANCHOR;
    var to = _mountNode.ToLocal(_portNode.GlobalPosition);
    // The slack hangs under the straight line between the ends, and there is less of it the further
    // apart they are - a hose pulled out straight has none left to give.
    var span = from.DistanceTo(to);
    // Less of it the further apart the ends are: a hose pulled out straight has no slack left to
    // give. It bows away from the gun rather than straight down, so the barrel never swings
    // through it.
    var sag = HOSE_SLACK * Mathf.Max(1f - (span / (HOSE_SLACK * 4f)), 0.2f);
    var bend = ((from + to) / 2f) + (new Vector2(0.55f, 1f).Normalized() * sag);

    var points = new Vector2[HOSE_POINTS];
    for (var i = 0; i < HOSE_POINTS; i++) {
      var along = (float)i / (HOSE_POINTS - 1);
      var back = 1f - along;
      points[i] = (back * back * from) + (2f * back * along * bend) + (along * along * to);
    }
    _hoseNode.Points = points;

    // The ink is the same curve, drawn as far along it as the ink has got. Backwards, because it
    // travels from the fitting to the tank and the last point is where it ends up.
    if (_drawnUp <= 0f) {
      _hoseInkNode.ClearPoints();
      return;
    }
    var reached = Mathf.Max(Mathf.CeilToInt(_drawnUp * HOSE_POINTS), 2);
    var run = new Vector2[reached];
    System.Array.Copy(points, run, reached);
    _hoseInkNode.Points = run;
  }

  private void _onFire() {
    if (!_canReach()) {
      return;
    }

    _ink -= 1f / Mathf.Max(ShotsPerTank, 1);
    if (_ink <= 0.001f) {
      _ink = 0f;
      _refilling = true;
      _drawnUp = 0f;
      GameEvents.Instance.OnPaintGunCooling(GlobalPosition);
    }
    _showInk();

    var shot = SceneHelpers.InstantiateNode<SplashShot>();
    shot.Setup(Group, SplashWidth, PaintLife);
    // Before it is in the tree: physics interpolation is on for the whole project, and a node given
    // its transform afterwards draws its first frames sweeping in from its parent's origin.
    shot.GlobalPosition = _muzzleNode.GlobalPosition;
    GetParent().AddChild(shot);
    shot.Fire(REST.Rotated(_armNode.GlobalRotation) * ShotSpeed);
    GameEvents.Instance.OnPaintGunFired(_muzzleNode.GlobalPosition);
  }
}
