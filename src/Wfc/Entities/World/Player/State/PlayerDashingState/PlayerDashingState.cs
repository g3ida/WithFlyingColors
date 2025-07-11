namespace Wfc.Entities.World.Player;

using Godot;
using Wfc.Core.Input;
using Wfc.State;
using Wfc.Utils;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class PlayerDashingState : PlayerBaseState {
  private const float DASH_DURATION = 0.17f;
  private const float PERMISSIVENESS = 0.05f;
  private const float DASH_SPEED = 20 * Constants.WORLD_TO_SCREEN;
  private const float DASH_GHOST_INSTANCE_DELAY = 0.04f;

  private CountdownTimer _dashTimer = new CountdownTimer();
  private CountdownTimer _permissivenessTimer = new CountdownTimer();
  private bool _dashDone = false;
  public Vector2 Direction = Vector2.Zero;

  public PlayerDashingState(IPlayerStatesStore statesStore, IInputManager inputManager)
    : base(statesStore, inputManager) {
    _dashTimer.Set(DASH_DURATION, false);
    _permissivenessTimer.Set(PERMISSIVENESS, false);
    this.baseState = PlayerStatesEnum.DASHING;
  }

  protected override void _Enter(Player player) {
    player.DashGhostTimerNode.WaitTime = DASH_GHOST_INSTANCE_DELAY;

    player.DashGhostTimerNode.Connect(
      Timer.SignalName.Timeout,
      new Callable(this, nameof(_onDashGhostTimerTimeout))
    );
    if (Direction == Vector2.Zero) {
      _permissivenessTimer.Reset();
      _setDashDirection(player);
      _dashDone = false;
    }
    else {
      _dashDone = true;
      _permissivenessTimer.Stop();
    }

    _dashTimer.Reset();
    player.CanDash = false;
    Global.Instance().Camera.GetNode<CameraShake>("CameraShake").Start();
    InstanceGhost(player);
    player.DashGhostTimerNode.Start();
  }

  protected override void _Exit(Player player) {
    if (_dashDone) {
      player.Velocity = new Vector2(0, player.Velocity.Y);
    }
    _dashTimer.Stop();
    _permissivenessTimer.Stop();
    player.DashGhostTimerNode.Stop();
    player.DashGhostTimerNode.Disconnect(
      Timer.SignalName.Timeout,
      new Callable(this, nameof(_onDashGhostTimerTimeout))
    );
    Direction = Vector2.Zero;
  }

  protected override IState<Player>? _PhysicsUpdate(Player player, float delta) {
    if (!_dashDone && !_permissivenessTimer.IsRunning()) {
      _setDashDirection(player);
      if (Direction.LengthSquared() < 0.01f) {
        _dashTimer.Stop();
      }
      else {
        _dashDone = true;
        EventHandler.Instance.EmitPlayerDash(Direction);
      }
    }

    if (_dashDone) {
      if (Mathf.Abs(Direction.X) > 0.01f) {
        player.Velocity = new Vector2(DASH_SPEED * Direction.X, player.Velocity.Y);
      }
      if (Mathf.Abs(Direction.Y) > 0.01f) {
        player.Velocity = new Vector2(player.Velocity.X, DASH_SPEED * Direction.Y);
      }
    }

    if (!_dashTimer.IsRunning()) {
      return player.StatesStore.GetState(PlayerStatesEnum.FALLING);
    }
    else {
      player.Velocity = new Vector2(player.Velocity.X, 0);
    }

    _dashTimer.Step(delta);
    _permissivenessTimer.Step(delta);

    return null;
  }

  private void _setDashDirection(Player player) {
    Direction = Vector2.Zero;
    if (inputManager.IsPressed(IInputManager.Action.MoveRight) && inputManager.IsPressed(IInputManager.Action.MoveLeft)) {
      Direction.X = 0;
    }
    else if (inputManager.IsPressed(IInputManager.Action.MoveLeft)) {
      Direction.X = -1;
    }
    else if (inputManager.IsPressed(IInputManager.Action.MoveRight)) {
      Direction.X = 1;
    }
    else if (Mathf.Abs(player.Velocity.X) > 0.1f) {
      Direction.X = 1 * Mathf.Sign(player.Velocity.X);
    }
    else {
      Direction.X = 0;
    }
    if (inputManager.IsPressed(IInputManager.Action.Down)) {
      Direction.Y = 1;
    }
  }

  private static void _onDashGhostTimerTimeout() {
    InstanceGhost(Global.Instance().Player);
  }

  private static void InstanceGhost(Player player) {
    Sprite2D ghost = SceneHelpers.InstantiateNode<DashGhost>();
    ghost.Scale = player.Scale;
    player.GetParent().AddChild(ghost);
    ghost.GlobalPosition = player.GlobalPosition;
    ghost.Texture = player.AnimatedSpriteNode.SpriteFrames.GetFrameTexture(player.AnimatedSpriteNode.Animation, player.AnimatedSpriteNode.Frame);
    ghost.Rotate(player.Rotation);
  }
}
