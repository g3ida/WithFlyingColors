namespace Wfc.Entities.World.Player;

using Godot;
using Wfc.State;
using Wfc.Utils;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class PlayerDashingState : PlayerBaseState {
  private const float DASH_DURATION = 0.17f;
  private const float PERMISSIVENESS = 0.05f;
  private const float DASH_SPEED = 20 * Constants.WORLD_TO_SCREEN;
  private const float DASH_GHOST_INSTANCE_DELAY = 0.04f;

  private CountdownTimer dashTimer = new CountdownTimer();
  private CountdownTimer permissivenessTimer = new CountdownTimer();
  public Vector2 direction = Vector2.Zero;
  private bool dashDone = false;

  public PlayerDashingState() : base() {
    dashTimer.Set(DASH_DURATION, false);
    permissivenessTimer.Set(PERMISSIVENESS, false);
    this.baseState = PlayerStatesEnum.DASHING;
  }

  protected override void _Enter(Player player) {
    player.DashGhostTimerNode.WaitTime = DASH_GHOST_INSTANCE_DELAY;
    player.DashGhostTimerNode.Connect("timeout", new Callable(this, "_OnDashGhostTimerTimeout"));
    if (direction == Vector2.Zero) {
      permissivenessTimer.Reset();
      SetDashDirection(player);
      dashDone = false;
    }
    else {
      dashDone = true;
      permissivenessTimer.Stop();
    }

    dashTimer.Reset();
    player.CanDash = false;
    Global.Instance().Camera.GetNode<CameraShake>("CameraShake").Start();
    InstanceGhost(player);
    player.DashGhostTimerNode.Start();
  }

  protected override void _Exit(Player player) {
    if (dashDone) {
      player.Velocity = new Vector2(0, player.Velocity.Y);
    }
    dashTimer.Stop();
    permissivenessTimer.Stop();
    player.DashGhostTimerNode.Stop();
    player.DashGhostTimerNode.Disconnect("timeout", new Callable(this, "_OnDashGhostTimerTimeout"));
    direction = Vector2.Zero;
  }

  protected override BaseState<Player>? _PhysicsUpdate(Player player, float delta) {
    if (!dashDone && !permissivenessTimer.IsRunning()) {
      SetDashDirection(player);
      if (direction.LengthSquared() < 0.01f) {
        dashTimer.Stop();
      }
      else {
        dashDone = true;
        EventHandler.Instance.EmitPlayerDash(direction);
      }
    }

    if (dashDone) {
      if (Mathf.Abs(direction.X) > 0.01f) {
        player.Velocity = new Vector2(DASH_SPEED * direction.X, player.Velocity.Y);
      }
      if (Mathf.Abs(direction.Y) > 0.01f) {
        player.Velocity = new Vector2(player.Velocity.X, DASH_SPEED * direction.Y);
      }
    }

    if (!dashTimer.IsRunning()) {
      return player.StatesStore.GetState(PlayerStatesEnum.FALLING);
    }
    else {
      player.Velocity = new Vector2(player.Velocity.X, 0);
    }

    dashTimer.Step(delta);
    permissivenessTimer.Step(delta);

    return null;
  }

  private void SetDashDirection(Player player) {
    direction = Vector2.Zero;
    if (Godot.Input.IsActionPressed("move_right") && Godot.Input.IsActionPressed("move_left")) {
      direction.X = 0;
    }
    else if (Godot.Input.IsActionPressed("move_left")) {
      direction.X = -1;
    }
    else if (Godot.Input.IsActionPressed("move_right")) {
      direction.X = 1;
    }
    else if (Mathf.Abs(player.Velocity.X) > 0.1f) {
      direction.X = 1 * Mathf.Sign(player.Velocity.X);
    }
    else {
      direction.X = 0;
    }
    if (Godot.Input.IsActionPressed("down")) {
      direction.Y = 1;
    }
  }

  private void _OnDashGhostTimerTimeout() {
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
