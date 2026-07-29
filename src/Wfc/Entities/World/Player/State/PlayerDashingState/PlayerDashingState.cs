namespace Wfc.Entities.World.Player;

using Godot;
using Wfc.Core.Input;
using Wfc.Entities.World.Camera;
using Wfc.State;
using Wfc.Utils;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class PlayerDashingState : PlayerBaseState {
  private const float DASH_DURATION = 0.17f;
  private const float PERMISSIVENESS = 0.05f;
  private const float DASH_SPEED = 20 * Constants.WORLD_TO_SCREEN;

  private CountdownTimer _dashTimer = new CountdownTimer();
  private CountdownTimer _permissivenessTimer = new CountdownTimer();
  private bool _dashDone = false;
  private Vector2 _direction = Vector2.Zero;
  private float _elapsed = 0.0f;
  private float _committedAt = 0.0f;

  public PlayerDashingState(IPlayerStatesStore statesStore, IInputManager inputManager)
    : base(statesStore, inputManager) {
    _dashTimer.Set(DASH_DURATION, false);
    _permissivenessTimer.Set(PERMISSIVENESS, false);
  }

  protected override void _Enter(Player player) {
    _dashTimer.Reset();
    _elapsed = 0.0f;
    _committedAt = 0.0f;
    player.CanDash = false;

    if (_direction == Vector2.Zero) {
      _permissivenessTimer.Reset();
      _setDashDirection(player);
      _dashDone = false;
    }
    else {
      _dashDone = true;
      _permissivenessTimer.Stop();
      _commit(player);
    }
  }

  protected override void _Exit(Player player) {
    if (_dashDone) {
      player.Velocity = new Vector2(0, player.Velocity.Y);
    }
    _dashTimer.Stop();
    _permissivenessTimer.Stop();
    DashVisuals.End(player);
    _direction = Vector2.Zero;
  }

  protected override IState<Player>? _PhysicsUpdate(Player player, float delta) {
    if (!_dashDone && !_permissivenessTimer.IsRunning()) {
      _setDashDirection(player);
      if (_direction.LengthSquared() < 0.01f) {
        _dashTimer.Stop();
      }
      else {
        _dashDone = true;
        _commit(player);
      }
    }

    if (_dashDone) {
      if (Mathf.Abs(_direction.X) > 0.01f) {
        player.Velocity = new Vector2(DASH_SPEED * _direction.X, player.Velocity.Y);
      }
      if (Mathf.Abs(_direction.Y) > 0.01f) {
        player.Velocity = new Vector2(player.Velocity.X, DASH_SPEED * _direction.Y);
      }
      _stepVisuals(player);
    }

    if (!_dashTimer.IsRunning()) {
      return statesStore.GetState<PlayerFallingState>();
    }
    else if (HoldsHeightDuringDash(_direction)) {
      player.Velocity = new Vector2(player.Velocity.X, 0);
    }

    _dashTimer.Step(delta);
    _permissivenessTimer.Step(delta);
    _elapsed += delta;

    return null;
  }

  // A horizontal dash pins the cube at its current height by zeroing gravity every frame.
  // A down-dash's whole payload is a vertical velocity, so doing it there overwrote the
  // DASH_SPEED just assigned above: the cube hovered for DASH_DURATION and fell ~33px
  // instead of ~300, on every frame but the last.
  internal static bool HoldsHeightDuringDash(Vector2 direction) => Mathf.Abs(direction.Y) <= 0.01f;

  // Everything that announces the dash fires here rather than on entry, because on entry there
  // is not yet a direction to announce: a dash held inside the permissiveness window is still
  // waiting to find out which way it is going, and one that never finds a direction never
  // announces anything at all.
  private void _commit(Player player) {
    _committedAt = _elapsed;
    EventHandler.Instance.EmitPlayerDash(_direction);
    EventHandler.Instance.EmitCameraShakeRequest();
    // Each axis of a dash runs at DASH_SPEED of its own, so a diagonal one covers more ground
    // than a flat one and the trail has to be laid out over the distance actually covered.
    DashVisuals.Begin(player, _direction, DASH_SPEED * _travelWindow() * _direction.Length());
  }

  // The dash clock starts on entry but the cube does not move until a direction is settled, so
  // this is the part of DASH_DURATION that actually goes anywhere - and the only part the trail
  // may be laid out over.
  private float _travelWindow() => Mathf.Max(DASH_DURATION - _committedAt, MathUtils.EPSILON);

  private void _stepVisuals(Player player) =>
    DashVisuals.Step(player, _direction, _elapsed - _committedAt, _travelWindow());

  private void _setDashDirection(Player player) {
    _direction = Vector2.Zero;
    if (inputManager.IsPressed(IInputManager.Action.MoveRight) && inputManager.IsPressed(IInputManager.Action.MoveLeft)) {
      _direction.X = 0;
    }
    else if (inputManager.IsPressed(IInputManager.Action.MoveLeft)) {
      _direction.X = -1;
    }
    else if (inputManager.IsPressed(IInputManager.Action.MoveRight)) {
      _direction.X = 1;
    }
    else if (Mathf.Abs(player.Velocity.X) > 0.1f) {
      _direction.X = 1 * Mathf.Sign(player.Velocity.X);
    }
    else {
      _direction.X = 0;
    }
    if (inputManager.IsPressed(IInputManager.Action.Down)) {
      _direction.Y = 1;
    }
  }
}
