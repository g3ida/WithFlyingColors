using Godot;
using Wfc.Skin;

public partial class TempleGem : Node2D {
  // Enum for states
  public enum State {
    IDLE,
    MOVING,
    MOVED
  }

  // Constants
  private const float DURATION = 1.0f;

  // Signal
  [Signal]
  public delegate void MoveCompletedEventHandler(Node2D self);

  // FIXME: implement set get for this field
  // Variables
  [Export]
  public string color_group { get; private set; }

  private State currentState = State.IDLE;
  private Tween tweener;

  // Nodes
  private PointLight2D lightNode;

  public override void _Ready() {
    lightNode = GetNode<PointLight2D>("PointLight2D");
    lightNode.Visible = false;
  }

  public void SetColorGroup(string colorGroup) {
    color_group = colorGroup;
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
    if (currentState == State.IDLE) {
      currentState = State.MOVING;
      MoveTween(position, waitTime, easeType);
    }
  }

  private void MoveTween(Vector2 position, float waitTime, int easeType = 1) {
    tweener?.Kill();
    tweener = CreateTween();
    tweener.Connect(
      Tween.SignalName.Finished,
      new Callable(this, nameof(OnTweenCompleted)),
      (uint)ConnectFlags.OneShot
    );
    tweener.SetParallel(true);
    tweener.TweenProperty(this, "global_position:x", position.X, DURATION)
           .SetTrans(Tween.TransitionType.Linear)
           .SetEase((Tween.EaseType)easeType)
           .SetDelay(waitTime);

    tweener.TweenProperty(this, "global_position:y", position.Y, DURATION)
           .SetTrans(Tween.TransitionType.Circ)
           .SetEase((Tween.EaseType)easeType)
           .SetDelay(waitTime);
  }

  private void OnTweenCompleted() {
    if (currentState == State.MOVING) {
      currentState = State.MOVED;
      EmitSignal(nameof(MoveCompleted), this);
      lightNode.Visible = true;
    }
  }
}
