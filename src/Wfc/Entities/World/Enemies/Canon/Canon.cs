namespace Wfc.Entities.World.Enemies;

using System;
using System.Collections.Generic;
using Godot;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class Canon : Node2D {
  #region Constants
  // Fast enough to hold a running player: the barrel has to close angle quicker than a cube at full
  // speed opens it, or the canon spends the whole crossing catching up and never lines a shot up.
  private const float ANGULAR_VELOCITY = 2.0f;
  private const float VIEW_LIMIT_1 = 179.0f * Mathf.Pi / 180.0f;
  private const float VIEW_LIMIT_2 = 1.0f * Mathf.Pi / 180.0f;
  private const float DISTANCE_LIMIT = 6.0f * Constants.WORLD_TO_SCREEN;
  // Wider than the barrel turns in a single tick, or a canon swinging to keep up steps over its own
  // firing window every frame and tracks the player without ever shooting at them.
  private const float SHOOT_PRECISION = 5.0f * Mathf.Pi / 180.0f;
  #endregion Constants

  #region Exports
  [Export]
  public string FollowNodeName { get; set; } = String.Empty;
  [Export]
  public NodePath ObjectToFollow { get; set; } = default!;
  [Export]
  public float cooldown { get; set; } = 1.5f;
  [Export]
  public string ColorGroup { get; set; } = "blue";
  #endregion Exports

  #region Nodes
  [NodePath("Stand")]
  private Node2D _standNode = default!;
  [NodePath("Canon")]
  private Node2D _canonNode = default!;
  [NodePath("Canon/Muzzle")]
  private Node2D _canonMuzzle = default!;
  [NodePath("Canon/ShootAnimation")]
  private AnimationPlayer _canonAnimation = default!;
  [NodePath("Body/StandColorArea")]
  private Node2D _standColorAreaNode = default!;
  [NodePath("Body/CanonColorArea")]
  private Node2D _canonColorAreaNode = default!;
  [NodePath("ShoutSound")]
  private AudioStreamPlayer2D _shootSound = default!;
  [NodePath("CooldownTimer")]
  private Timer _coolDownTimerNode = default!;
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
    UpdateColor();
    _coolDownTimerNode.WaitTime = cooldown;
  }

  private void UpdateColor() {
    Color color = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(ColorGroup),
      SkinColorIntensity.Basic
    );
    _standNode.Modulate = color;
    _canonNode.Modulate = color;
  }

  private Node2D SpawnBullet() {
    Node2D bullet = SceneHelpers.InstantiateNode<Bullet>();
    bullet.GlobalPosition = _canonMuzzle.GlobalPosition;
    GetParent().AddChild(bullet);
    bullet.Owner = GetParent();
    if (bullet is IBullet bulletScript) {
      bulletScript.SetColorGroup(ColorGroup);
    }
    return bullet;
  }

  private async void Shoot() {
    _canShoot = false;
    _canonAnimation.Play("Shoot");
    await ToSignal(_canonAnimation, AnimationPlayer.SignalName.AnimationFinished);
    if (!IsInstanceValid(this)) {
      return;
    }

    var bullet = SpawnBullet();
    if (bullet is IBullet bulletScript) {
      // Fired along the barrel as it stands now rather than where it was pointing when the recoil
      // started: the animation lasts long enough for a running player to have left that heading.
      bulletScript.Shoot(Vector2.FromAngle(_aimAngle()));
    }
    _shootSound.Play();
    _coolDownTimerNode.Start();
    await ToSignal(_coolDownTimerNode, Timer.SignalName.Timeout);
    if (IsInstanceValid(this)) {
      _canShoot = true;
    }
  }


  private static bool _canFollow(float targetAngle, float distanceSquared) {
    return !(targetAngle > VIEW_LIMIT_1 || targetAngle < VIEW_LIMIT_2) && distanceSquared < DISTANCE_LIMIT * DISTANCE_LIMIT;
  }

  // Where the barrel points, in global terms, so that a canon on a rotated mount aims as well as an
  // upright one.
  private float _aimAngle() => _canonNode.GlobalRotation + (Mathf.Pi / 2.0f);

  public override void _PhysicsProcess(double delta) {
    // A canon outlives what it was following - a level being torn down frees the player first - and
    // reading a freed node throws out of every physics tick from then on, which leaves the canon
    // standing there aiming at nothing for the rest of the level.
    if (_followNode is null || !IsInstanceValid(_followNode)) {
      return;
    }

    Vector2 direction = _followNode.GlobalPosition - _canonMuzzle.GlobalPosition;
    float targetAngle = direction.Angle();
    // Wrapped, and closed at a fixed rate. A raw difference against a barrel whose rotation has been
    // added to all game says the canon is nearly a whole turn out when it is a degree out, and
    // turning by a share of that difference approaches the aim without ever arriving: the canon
    // ended up staring at the player and never firing.
    float offAim = Mathf.AngleDifference(_aimAngle(), targetAngle);
    bool canFollow = _canFollow(targetAngle, direction.LengthSquared());

    if (canFollow) {
      float step = ANGULAR_VELOCITY * (float)delta;
      _canonNode.Rotate(Mathf.Clamp(offAim, -step, step));
    }

    // The same condition it tracks under, so what the canon is looking at is what it shoots at.
    if (canFollow && _canShoot && Mathf.Abs(offAim) < SHOOT_PRECISION) {
      Shoot();
    }
  }
}
