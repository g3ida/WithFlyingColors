namespace Wfc.Entities.World.Temple;

using Godot;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class TempleGem : Node2D {
  #region Constants
  private const float DURATION = 1.0f;
  #endregion Constants

  #region Signals
  [Signal]
  public delegate void MoveCompletedEventHandler(Node2D self);
  #endregion Signals

  #region Exports
  [Export]
  public string ColorGroup { get; private set; } = "blue";
  #endregion Exports

  public enum State {
    Idle,
    Moving,
    Moved
  }
  private State _currentState = State.Idle;
  private Tween? _tweener;

  #region Nodes
  [NodePath("PointLight2D")]
  private PointLight2D lightNode = default!;
  #endregion Nodes

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    lightNode.Visible = false;
  }

  public void SetColorGroup(string colorGroup) {
    ColorGroup = colorGroup;
    Color color = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(colorGroup),
      SkinColorIntensity.Basic
    );
    Color darkColor = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(colorGroup),
      SkinColorIntensity.Dark
    );
    GetNode<AnimatedSprite2D>("GemAnimatedSprite").Modulate = color;
    lightNode.Color = darkColor;
  }

  public void SetLightIntensity(float intensity) {
    lightNode.Energy = intensity;
  }

  public void MoveToPosition(Vector2 position, float waitTime, int easeType = 1) {
    if (_currentState == State.Idle) {
      _currentState = State.Moving;
      _moveTween(position, waitTime, easeType);
    }
  }

  private void _moveTween(Vector2 position, float waitTime, int easeType = 1) {
    _tweener?.Kill();
    _tweener = CreateTween();
    _tweener.Connect(
      Tween.SignalName.Finished,
      new Callable(this, nameof(_onTweenCompleted)),
      (uint)ConnectFlags.OneShot
    );
    _tweener.SetParallel(true);
    _tweener.TweenProperty(this, "global_position:x", position.X, DURATION)
           .SetTrans(Tween.TransitionType.Linear)
           .SetEase((Tween.EaseType)easeType)
           .SetDelay(waitTime);

    _tweener.TweenProperty(this, "global_position:y", position.Y, DURATION)
           .SetTrans(Tween.TransitionType.Circ)
           .SetEase((Tween.EaseType)easeType)
           .SetDelay(waitTime);
  }

  private void _onTweenCompleted() {
    if (_currentState == State.Moving) {
      _currentState = State.Moved;
      EmitSignal(nameof(MoveCompleted), this);
      lightNode.Visible = true;
    }
  }
}
