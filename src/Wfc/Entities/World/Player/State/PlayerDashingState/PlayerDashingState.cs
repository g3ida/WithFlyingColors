namespace Wfc.Entities.World.Player;

using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.State;
using Wfc.Utils;

public partial class PlayerDashingState : PlayerBaseState {
  private const float DASH_DURATION = 0.17f;
  private const float PERMISSIVENESS = 0.05f;
  private const float DASH_SPEED = 20 * Constants.WORLD_TO_SCREEN;

  // How little of a frame's worth of dash the cube has to cover before the frame counts as
  // having been spent on something solid rather than on ground.
  private const float BLOCKED_TRAVEL_FRACTION = 0.35f;

  // What a dash that reached open air hands back to the run. The run damping bleeds it off over
  // the frames that follow, so the cube slides out of the dash rather than arriving stopped.
  private const float COAST_SPEED_FRACTION = 0.3f;

  private CountdownTimer _dashTimer = new CountdownTimer();
  private CountdownTimer _permissivenessTimer = new CountdownTimer();
  private bool _dashDone = false;
  private Vector2 _direction = Vector2.Zero;
  private float _elapsed = 0.0f;
  private float _committedAt = 0.0f;
  private float _lastDelta = 0.0f;
  private Vector2 _lastPosition = Vector2.Zero;
  private Vector2 _visualDirection = Vector2.Zero;
  private bool _impacted = false;

  public PlayerDashingState(IPlayerStatesStore statesStore, IInputManager inputManager)
    : base(statesStore, inputManager) {
    _dashTimer.Set(DASH_DURATION, false);
    _permissivenessTimer.Set(PERMISSIVENESS, false);
  }

  protected override void _Enter(Player player) {
    _dashTimer.Reset();
    _elapsed = 0.0f;
    _committedAt = 0.0f;
    _lastDelta = 0.0f;
    _impacted = false;
    player.CanDash = false;
    // A dash writes the whole of the cube's speed every tick it runs, so there is nothing left of
    // a push from the floor to keep apart from it.
    player.CarriedSpeed = 0.0f;

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
      _arrive(player);
    }
    else {
      DashVisuals.End(player);
    }
    _dashTimer.Stop();
    _permissivenessTimer.Stop();
    _direction = Vector2.Zero;
    _visualDirection = Vector2.Zero;
  }

  // The last frame of the dash is moved after this state has stopped being asked anything, so
  // whether it was the one that hit the wall is only knowable here.
  private void _arrive(Player player) {
    _watchForImpact(player);
    if (_impacted) {
      player.Velocity = new Vector2(0, player.Velocity.Y);
      return;
    }

    player.Velocity = new Vector2(DASH_SPEED * _direction.X * COAST_SPEED_FRACTION, player.Velocity.Y);
    DashVisuals.Coast(player);
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
      // Past the impact the stretch is being taken off the cube by the hit, and stepping it
      // here would put it straight back on for as long as the state has left to run.
      if (!_impacted) {
        _stepVisuals(player);
        _watchForImpact(player);
      }
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
    _lastDelta = delta;

    return null;
  }

  // The cube has hit something when a frame carries it a fraction of the ground the speed it was
  // given would have. Read off the move itself rather than asked of the body, so a wall, the
  // ceiling and the floor all answer the same, and measured a frame late because the move that
  // this state asks for happens after it has been asked for it.
  private void _watchForImpact(Player player) {
    if (_impacted || _elapsed <= _committedAt) {
      return;
    }

    var travelled = (player.GlobalPosition - _lastPosition).Dot(_direction.Normalized());
    _lastPosition = player.GlobalPosition;
    if (travelled >= DASH_SPEED * _direction.Length() * _lastDelta * BLOCKED_TRAVEL_FRACTION) {
      return;
    }

    _impacted = true;
    DashVisuals.Impact(player, _visualDirection);
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
    _lastPosition = player.GlobalPosition;
    _visualDirection = _visibleDirection(player, _direction);
    GameEvents.Instance.OnPlayerDashed(_direction);
    // Each axis of a dash runs at DASH_SPEED of its own, so a diagonal one covers more ground
    // than a flat one and the trail has to be laid out over the distance actually covered.
    DashVisuals.Begin(player, _visualDirection, DASH_SPEED * _travelWindow() * _visualDirection.Length());
  }

  // Where the dash can be seen to go, which is not always where it was aimed: the floor takes a
  // down-diagonal's vertical half whole, and a trail laid along the aim fans down into the ground
  // while the cube slides flat along the top of it. One the floor leaves nothing of keeps its
  // aim - that one really is the slam it was asked for.
  private static Vector2 _visibleDirection(Player player, Vector2 direction) =>
    player.IsOnFloor() && Mathf.Abs(direction.X) > 0.01f
      ? new Vector2(direction.X, 0.0f)
      : direction;

  // The dash clock starts on entry but the cube does not move until a direction is settled, so
  // this is the part of DASH_DURATION that actually goes anywhere - and the only part the trail
  // may be laid out over.
  private float _travelWindow() => Mathf.Max(DASH_DURATION - _committedAt, MathUtils.EPSILON);

  private void _stepVisuals(Player player) =>
    DashVisuals.Step(player, _visualDirection, _elapsed - _committedAt, _travelWindow());

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
    // Holding only down means straight down: leftover run speed must not bend an
    // aimed slam into a diagonal the player never asked for.
    else if (!inputManager.IsPressed(IInputManager.Action.Down) && Mathf.Abs(player.Velocity.X) > 0.1f) {
      _direction.X = Mathf.Sign(player.Velocity.X);
    }
    else {
      _direction.X = 0;
    }
    if (inputManager.IsPressed(IInputManager.Action.Down)) {
      _direction.Y = 1;
    }
  }
}
