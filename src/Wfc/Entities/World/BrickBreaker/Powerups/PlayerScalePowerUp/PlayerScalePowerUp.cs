namespace Wfc.Entities.World.BrickBreaker.Powerups;

using Godot;

// Both scale power-ups drive the same value on the player, so which of them is entitled to hand it
// back cannot be read off that value: for as long as a tween runs it matches neither one's target.
// Whichever power-up last started a tween owns the scale, until another takes it or it gives it back.
public abstract partial class PlayerScalePowerUp : PowerUpScript {
  private const float TWEEN_TIME = 0.7f;

  private static PlayerScalePowerUp? _scaleOwner = null;

  public abstract float ScaleFactor { get; }

  private Tween? _tweener = null;

  public override void _Ready() {
    base._Ready();
    SetProcess(false);
    _scaleOwner = this;
    _tweenScaleTo(ScaleFactor);
  }

  public override void _ExitTree() {
    base._ExitTree();
    _tweener?.Kill();
    _tweener = null;
    if (!IsStillRelevant()) {
      return;
    }
    _scaleOwner = null;
    _restorePlayerScale();
  }

  public override bool IsStillRelevant() => ReferenceEquals(_scaleOwner, this);

  private void _tweenScaleTo(float target) {
    var player = Global.Instance().Player;
    _tweener?.Kill();
    _tweener = CreateTween();
    _tweener.TweenProperty(
        player,
        Node2D.PropertyName.Scale.ToString(),
        new Vector2(target, target),
        TWEEN_TIME
    ).SetTrans(Tween.TransitionType.Linear
    ).SetEase(Tween.EaseType.InOut
    ).From(player.Scale);
  }

  // Also reached with the level coming down around it, where the player is already gone.
  private static void _restorePlayerScale() {
    var player = Global.Instance()?.Player;
    if (player is not null && GodotObject.IsInstanceValid(player)) {
      player.Scale = Vector2.One;
    }
  }
}
