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
  private const float ANGULAR_VELOCITY = 0.5f;
  private const float VIEW_LIMIT_1 = 179.0f * Mathf.Pi / 180.0f;
  private const float VIEW_LIMIT_2 = 1.0f * Mathf.Pi / 180.0f;
  private const float DISTANCE_LIMIT = 6.0f * Constants.WORLD_TO_SCREEN;
  private const float SHOOT_PRECISION = 2.0f * Mathf.Pi / 180.0f;
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
  private Node2D _followNode = default!;
  #endregion Nodes

  private float _angle = 0f;
  private bool _canShoot = true;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _followNode = GetNode<Node2D>(ObjectToFollow);
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

  private async void Shoot(Vector2 direction) {
    _canShoot = false;
    _canonAnimation.Play("Shoot");
    await ToSignal(_canonAnimation, AnimationPlayer.SignalName.AnimationFinished);

    var bullet = SpawnBullet();
    if (bullet is IBullet bulletScript) {
      bulletScript.Shoot(direction);
    }
    _shootSound.Play();
    _coolDownTimerNode.Start();
    await ToSignal(_coolDownTimerNode, Timer.SignalName.Timeout);
    _canShoot = true;
  }


  private static bool _canFollow(float targetAngle, float distanceSquared) {
    return !(targetAngle > VIEW_LIMIT_1 || targetAngle < VIEW_LIMIT_2) && distanceSquared < DISTANCE_LIMIT * DISTANCE_LIMIT;
  }

  public override void _PhysicsProcess(double delta) {
    Vector2 direction = _followNode.GlobalPosition - _canonMuzzle.GlobalPosition;
    _angle = _canonNode.Rotation + Mathf.Pi / 2.0f;
    float targetAngle = direction.Angle();
    float rotationAmount = targetAngle - _angle;

    if (_canFollow(targetAngle, direction.LengthSquared())) {
      float amount = ANGULAR_VELOCITY * (float)delta;
      if (Mathf.Abs(rotationAmount) < Mathf.Abs(amount)) {
        amount = rotationAmount;
      }
      _canonNode.Rotate(rotationAmount * (float)delta);
    }

    if (Mathf.Abs(targetAngle - _angle) < SHOOT_PRECISION && _canShoot) {
      Shoot(direction.Normalized());
    }
  }
}
