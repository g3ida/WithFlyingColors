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
  #endregion Constants

  #region Nodes
  [NodePath("CharacterBody2D")]
  private CharacterBody2D _bodyNode = default!;
  [NodePath("CharacterBody2D/MissileSpr")]
  private Sprite2D _spriteNode = default!;
  [NodePath("CharacterBody2D/ColorArea")]
  private Area2D _colorAreaNode = default!;
  #endregion Nodes

  private Node2D? _targetNode;
  private Vector2 _heading = Vector2.Up;
  private float _age;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
  }

  public void SetTarget(Node2D target) => _targetNode = target;

  public void Shoot(Vector2 shootDirection) {
    _heading = shootDirection.IsZeroApprox() ? Vector2.Up : shootDirection.Normalized();
    _faceHeading();
  }

  public void SetColorGroup(string groupName) {
    _colorAreaNode.AddToGroup(groupName);
    var color = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(groupName),
      SkinColorIntensity.Basic
    );
    _spriteNode.Modulate = color;
  }

  public override void _PhysicsProcess(double delta) {
    base._PhysicsProcess(delta);
    _age += (float)delta;
    if (_age >= LIFETIME) {
      QueueFree();
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

  // The sprite is drawn nose up, so it lags a quarter turn behind the heading angle.
  private void _faceHeading() => _spriteNode.Rotation = _heading.Angle() + (Mathf.Pi / 2.0f);

  private void _onColorAreaBodyEntered(Node body) {
    if (body == Global.Instance().Player && body is Player player && !player.IsDying()
        && !player.AcceptsColorOfAt(_colorAreaNode.GlobalPosition, _colorAreaNode)) {
      EventHandler.Instance.EmitPlayerDying(_colorAreaNode, player.GlobalPosition, EntityType.Bullet);
    }
    QueueFree();
  }
}
