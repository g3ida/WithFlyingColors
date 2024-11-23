using Godot;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class PlayerJumpingState : PlayerBaseState {
  private const float TIME_UNTIL_FULL_JUMP_IS_CONSIDERED = 0.15f;
  private const float PERMISSIVENESS = 0.09f;
  private const float FACE_SEPARATOR_SCALE_FACTOR = 4.5f;
  private const float JUMP_FORCE = 1200f;

  private bool entered = false;
  private CountdownTimer jumpTimer = new CountdownTimer();
  private CountdownTimer permissivenessTimer = new CountdownTimer();
  public float touchJumpPower = 1.0f;

  public PlayerJumpingState() : base() {
    jumpTimer.Set(TIME_UNTIL_FULL_JUMP_IS_CONSIDERED, false);
    permissivenessTimer.Set(PERMISSIVENESS, false);
    this.baseState = PlayerStatesEnum.JUMPING;
  }

  protected override void _Enter(Player player) {
    entered = true;
    jumpTimer.Reset();
    EventHandler.Instance.EmitPlayerJumped();
    player.jumpParticlesNode.Emitting = true;
    player.ScaleCornersBy(FACE_SEPARATOR_SCALE_FACTOR);
  }

  protected override void _Exit(Player player) {
    entered = false;
    jumpTimer.Stop();
    permissivenessTimer.Stop();
    player.jumpParticlesNode.Emitting = false;
    player.ScaleCornersBy(1);
    touchJumpPower = 1.0f;
  }

  public override BaseState<Player> PhysicsUpdate(Player player, float delta) {
    return base.PhysicsUpdate(player, delta);
  }

  protected override BaseState<Player> _PhysicsUpdate(Player player, float delta) {
    if (entered) {
      entered = false;
      player.Velocity = new Vector2(player.Velocity.X, player.Velocity.Y - JUMP_FORCE * touchJumpPower);
    }
    else if (player.IsOnFloor()) {
      if (permissivenessTimer.IsRunning()) {
        return player.states_store.GetState(PlayerStatesEnum.JUMPING);
      }
      else {
        return player.states_store.GetState(PlayerStatesEnum.STANDING);
      }
    }

    if (JumpPressed(player)) {
      permissivenessTimer.Reset();
    }

    if (jumpTimer.IsRunning() && Input.IsActionJustReleased("jump")) {
      jumpTimer.Stop();
      if (player.Velocity.Y < 0) {
        player.Velocity = new Vector2(player.Velocity.X * 0.5f, player.Velocity.Y);
      }
    }

    jumpTimer.Step(delta);
    permissivenessTimer.Step(delta);
    return null;
  }

  public PlayerJumpingState WithJumpPower(float jumpPower) {
    touchJumpPower = jumpPower;
    return this;
  }
}
