namespace Wfc.Entities.World.Enemies;

using Godot;
using Wfc.Autoload;
using Wfc.Entities.World.Player;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

// A projectile that steers toward its target instead of flying straight. The turn rate is
// what keeps it fair: a missile committed to a heading has to swing wide to come back, so
// running past one and holding the line still shakes it off.
[ScenePath]
public partial class Missile : Node2D, IBullet {
  #region Constants
  private const float SPEED = 5.0f * Constants.WORLD_TO_SCREEN;
  private const float TURN_RATE = 1.8f;
  // Homing gives out well before the missile does, so a shot that has already been dodged
  // flies off instead of circling the player until its lifetime runs out.
  private const float HOMING_DURATION = 2.5f;
  private const float LIFETIME = 5.0f;
  // Comfortably longer than the exhaust's own lifetime at its full randomness, so nothing is
  // still on screen when the node goes.
  private const float TRAIL_FADE = 0.9f;
  #endregion Constants

  #region Nodes
  [NodePath("CharacterBody2D")]
  private CharacterBody2D _bodyNode = default!;
  [NodePath("CharacterBody2D/MissileSpr")]
  private Sprite2D _spriteNode = default!;
  [NodePath("CharacterBody2D/Exhaust")]
  private CpuParticles2D _exhaustNode = default!;
  [NodePath("CharacterBody2D/ColorArea")]
  private Area2D _colorAreaNode = default!;
  #endregion Nodes

  private Node2D? _targetNode;
  private Vector2 _heading = Vector2.Up;
  private float _age;
  private bool _isExpired;
  private float _fadeLeft;
  private bool _isSubscribed;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
  }

  // A respawn puts the level back, and a shot already in the air is part of what has to go: it
  // outlives the death it was fired before, and lands on a wall that has just been laid again or on
  // a player who has just been put back on their feet. Taken outright rather than expired: the trail
  // belongs to the run that has just ended.
  public override void _EnterTree() {
    base._EnterTree();
    if (_isSubscribed) {
      return;
    }
    EventHandler.Instance.Events.CheckpointLoaded += _onRespawn;
    _isSubscribed = true;
  }

  public override void _ExitTree() {
    base._ExitTree();
    if (!_isSubscribed) {
      return;
    }
    EventHandler.Instance.Events.CheckpointLoaded -= _onRespawn;
    _isSubscribed = false;
  }

  private void _onRespawn() => QueueFree();

  public void SetTarget(Node2D target) => _targetNode = target;

  public void Shoot(Vector2 shootDirection) {
    _heading = shootDirection.IsZeroApprox() ? Vector2.Up : shootDirection.Normalized();
    _faceHeading();
  }

  // Tinting the body rather than the sprite carries the group color into the exhaust too, so
  // the smoke reads as belonging to whichever canon fired it.
  public void SetColorGroup(string groupName) {
    _colorAreaNode.AddToGroup(groupName);
    var color = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(groupName),
      SkinColorIntensity.Basic
    );
    _bodyNode.Modulate = color;
  }

  public override void _PhysicsProcess(double delta) {
    base._PhysicsProcess(delta);
    if (_isExpired) {
      _fadeLeft -= (float)delta;
      if (_fadeLeft <= 0.0f) {
        QueueFree();
      }
      return;
    }

    _age += (float)delta;
    if (_age >= LIFETIME) {
      _expire();
      return;
    }

    if (_age < HOMING_DURATION && _targetNode is not null && IsInstanceValid(_targetNode)) {
      var toTarget = _targetNode.GlobalPosition - _bodyNode.GlobalPosition;
      if (!toTarget.IsZeroApprox()) {
        var maxTurn = TURN_RATE * (float)delta;
        var turn = Mathf.AngleDifference(_heading.Angle(), toTarget.Angle());
        _heading = _heading.Rotated(Mathf.Clamp(turn, -maxTurn, maxTurn));
      }
    }

    _faceHeading();
    _bodyNode.Velocity = _heading * SPEED;
    _bodyNode.MoveAndSlide();
  }

  // The missile is drawn nose up, so it lags a quarter turn behind the heading angle. Turning
  // the body rather than the sprite swings the exhaust round with it.
  private void _faceHeading() => _bodyNode.Rotation = _heading.Angle() + (Mathf.Pi / 2.0f);

  // The exhaust is emitted into world space, so freeing the missile outright would take smoke
  // with it that is already well behind. The missile goes invisible and inert instead, and
  // only leaves once the last of its trail has faded.
  private void _expire() {
    _isExpired = true;
    _fadeLeft = TRAIL_FADE;
    _exhaustNode.Emitting = false;
    _spriteNode.Hide();
  }

  private void _onColorAreaBodyEntered(Node body) {
    if (_isExpired) {
      return;
    }

    if (body == Global.Instance().Player && body is Player player && !player.IsDying()
        && !player.AcceptsColorOfAt(_colorAreaNode.GlobalPosition, _colorAreaNode)) {
      EventHandler.Instance.EmitPlayerDying(_colorAreaNode, player.GlobalPosition, EntityType.Bullet);
    }
    if (body is IShootable shootable) {
      shootable.OnShot(_colorAreaNode.GlobalPosition);
    }
    _expire();
  }
}
