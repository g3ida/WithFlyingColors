namespace Wfc.Entities.World.Enemies;

using Godot;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;

// A turret that launches homing missiles. Its own aim only has to be roughly right - the
// missile does the tracking - but the barrel stays inside the arc its mount allows, so a
// canon placed against a wall never fires through it.
[ScenePath]
public partial class SmartCanon : Node2D {
  #region Constants
  private const float ANGULAR_VELOCITY = 1.6f;
  private const float AIM_TOLERANCE = 20.0f * Mathf.Pi / 180.0f;
  // Zero rotation points the barrel straight up, which is how the scene rests it.
  private const float REST_DIRECTION = -Mathf.Pi / 2.0f;
  #endregion Constants

  #region Exports
  [Export]
  public NodePath ObjectToFollow { get; set; } = default!;
  [Export]
  public float Cooldown { get; set; } = 2.5f;
  [Export]
  public string ColorGroup { get; set; } = "blue";
  // How far the barrel may swing away from its rest heading, in degrees.
  [Export(PropertyHint.Range, "0,180,1")]
  public float AimSpread { get; set; } = 80.0f;
  // In world units. Beyond it the canon holds fire and lets the barrel drift back.
  [Export]
  public float Range { get; set; } = 9.0f;
  #endregion Exports

  #region Nodes
  [NodePath("Stand")]
  private Sprite2D _standNode = default!;
  [NodePath("Canon")]
  private Node2D _canonNode = default!;
  [NodePath("Canon/CanonSpr")]
  private Sprite2D _canonSpriteNode = default!;
  [NodePath("Canon/Muzzle")]
  private Marker2D _muzzleNode = default!;
  [NodePath("Canon/ShootAnimation")]
  private AnimationPlayer _shootAnimationNode = default!;
  [NodePath("Body/StandColorArea")]
  private Area2D _standColorAreaNode = default!;
  [NodePath("Canon/BarrelBody/CanonColorArea")]
  private Area2D _canonColorAreaNode = default!;
  [NodePath("ShootSound")]
  private AudioStreamPlayer2D _shootSoundNode = default!;
  [NodePath("CooldownTimer")]
  private Timer _cooldownTimerNode = default!;
  private Node2D? _followNode;
  #endregion Nodes

  private bool _canShoot = true;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _followNode = ObjectToFollow is null || ObjectToFollow.IsEmpty ? null : GetNodeOrNull<Node2D>(ObjectToFollow);
    AddToGroup(ColorGroup);
    _standColorAreaNode.AddToGroup(ColorGroup);
    _canonColorAreaNode.AddToGroup(ColorGroup);
    _updateColor();
    _cooldownTimerNode.WaitTime = Cooldown;
  }

  public override void _PhysicsProcess(double delta) {
    base._PhysicsProcess(delta);
    if (_followNode is null || !IsInstanceValid(_followNode)) {
      return;
    }

    var toTarget = _followNode.GlobalPosition - _canonNode.GlobalPosition;
    var offAim = Mathf.AngleDifference(_aimAngle(), toTarget.Angle());
    var range = Range * Constants.WORLD_TO_SCREEN;
    var inRange = toTarget.LengthSquared() < range * range;

    var spread = Mathf.DegToRad(AimSpread);
    var wanted = inRange ? Mathf.Clamp(_canonNode.Rotation + offAim, -spread, spread) : 0.0f;
    _canonNode.Rotation = Mathf.MoveToward(_canonNode.Rotation, wanted, ANGULAR_VELOCITY * (float)delta);

    if (_canShoot && inRange && Mathf.Abs(offAim) < AIM_TOLERANCE) {
      _shoot();
    }
  }

  // Where the barrel currently points, in global terms, so that a rotated mount aims just
  // as well as an upright one.
  private float _aimAngle() => _canonNode.GlobalRotation + REST_DIRECTION;

  private void _updateColor() {
    var color = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(ColorGroup),
      SkinColorIntensity.Basic
    );
    _standNode.Modulate = color;
    _canonSpriteNode.Modulate = color;
  }

  private async void _shoot() {
    _canShoot = false;
    _shootAnimationNode.Play("Shoot");
    await ToSignal(_shootAnimationNode, AnimationPlayer.SignalName.AnimationFinished);
    if (!IsInstanceValid(this)) {
      return;
    }

    _spawnMissile();
    _shootSoundNode.Play();
    _cooldownTimerNode.Start();
    await ToSignal(_cooldownTimerNode, Timer.SignalName.Timeout);
    if (IsInstanceValid(this)) {
      _canShoot = true;
    }
  }

  private void _spawnMissile() {
    var missile = SceneHelpers.InstantiateNode<Missile>();
    GetParent().AddChild(missile);
    missile.GlobalPosition = _muzzleNode.GlobalPosition;
    missile.SetColorGroup(ColorGroup);
    if (_followNode is not null) {
      missile.SetTarget(_followNode);
    }
    missile.Shoot(Vector2.FromAngle(_aimAngle()));
  }
}
