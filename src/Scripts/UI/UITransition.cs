using Godot;

public partial class UITransition : Control {
  [Signal]
  public delegate void enteredEventHandler();

  [Signal]
  public delegate void exitedEventHandler();

  public enum TransitionStates {
    ENTER_DELAY,
    ENTERING,
    ENTERED,
    EXIT_DELAY,
    EXITING,
    EXITED
  }

  private Control parent;
  private Vector2 displayPosition;
  private Vector2 hiddenPosition;
  private TransitionStates currentState = TransitionStates.ENTER_DELAY;
  private Tween tweener;

  [Export]
  public float time = 0.3f;

  [Export]
  public float delay = 0.25f;

  [Export]
  public Vector2 hidden_relative_position = Vector2.Zero;

  public override void _Ready() {
    parent = GetParent<Control>();

    parent.Connect(Control.SignalName.Ready, new Callable(this, nameof(_prepare)), flags: (uint)ConnectFlags.OneShot);
  }

  // API: enter(), exit()
  public void Enter() {
    EnterDelay();
  }

  public void Exit() {
    ExitDelay();
  }

  private void EnterDelay() {
    currentState = TransitionStates.ENTER_DELAY;
    StartTween(hiddenPosition, delay);
  }

  private void ReallyEnter() {
    currentState = TransitionStates.ENTERING;
    StartTween(displayPosition, time);
  }

  private void ExitDelay() {
    currentState = TransitionStates.EXIT_DELAY;
    StartTween(displayPosition, delay);
  }

  private void ReallyExit() {
    currentState = TransitionStates.EXITING;
    StartTween(hiddenPosition, time);
  }

  private void StartTween(Vector2 destinationPos, float duration) {
    tweener?.Kill();
    tweener = CreateTween();
    tweener.TweenProperty(parent, "position", destinationPos, duration)
          .SetTrans(Tween.TransitionType.Quad)
          .SetEase(Tween.EaseType.InOut);
    tweener.Connect(
      Tween.SignalName.Finished,
      new Callable(this, nameof(OnTweenCompleted)),
      flags: (uint)ConnectFlags.OneShot
    );
  }

  private void _prepare() {
    displayPosition = parent.Position;
    hiddenPosition = parent.Position + hidden_relative_position;
    parent.Position = hiddenPosition;
  }

  private void OnTweenCompleted() {
    if (currentState == TransitionStates.ENTER_DELAY) {
      ReallyEnter();
      EmitSignal(nameof(entered));
    }
    else if (currentState == TransitionStates.ENTERING) {
      currentState = TransitionStates.ENTERED;
    }
    else if (currentState == TransitionStates.EXIT_DELAY) {
      ReallyExit();
    }
    else if (currentState == TransitionStates.EXITING) {
      currentState = TransitionStates.EXITED;
      EmitSignal(nameof(exited));
    }
  }
}
