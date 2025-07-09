namespace Wfc.Entities.Ui;

using Godot;

public partial class UITransition : Control {
  [Signal]
  public delegate void EnteredEventHandler();

  [Signal]
  public delegate void ExitedEventHandler();

  public enum TransitionStates {
    ENTER_DELAY,
    ENTERING,
    ENTERED,
    EXIT_DELAY,
    EXITING,
    EXITED
  }

  private Control _parent = null!;
  private Vector2 _displayPosition;
  private Vector2 _hiddenPosition;
  private TransitionStates _currentState = TransitionStates.ENTER_DELAY;
  private Tween? _tweener;

  [Export]
  public float Duration = 0.3f;

  [Export]
  public float Delay = 0.25f;

  [Export]
  public Vector2 HiddenRelativePosition = Vector2.Zero;

  public override void _Ready() {
    _parent = GetParent<Control>();
    _parent.Ready += _prepare;
  }

  // API: enter(), exit()
  public void Enter() => EnterDelay();

  public void Exit() => ExitDelay();

  private void EnterDelay() {
    _currentState = TransitionStates.ENTER_DELAY;
    _startTween(_hiddenPosition, Delay);
  }

  private void ReallyEnter() {
    _currentState = TransitionStates.ENTERING;
    _startTween(_displayPosition, Duration);
  }

  private void ExitDelay() {
    _currentState = TransitionStates.EXIT_DELAY;
    _startTween(_displayPosition, Delay);
  }

  private void ReallyExit() {
    _currentState = TransitionStates.EXITING;
    _startTween(_hiddenPosition, Duration);
  }

  private void _startTween(Vector2 destinationPos, float duration) {
    _tweener?.Kill();
    _tweener = CreateTween();
    _tweener.TweenProperty(
      _parent,
      "position",
      destinationPos,
      duration
    )
          .SetTrans(Tween.TransitionType.Quad)
          .SetEase(Tween.EaseType.InOut);

    _tweener.Connect(
      Tween.SignalName.Finished,
      new Callable(this, nameof(UpdateState)),
      flags: (uint)ConnectFlags.OneShot
    );
  }

  private void _prepare() {
    _parent.Ready -= _prepare;
    _displayPosition = _parent.Position;
    _hiddenPosition = _parent.Position + HiddenRelativePosition;
    _parent.Position = _hiddenPosition;
  }

  private void UpdateState() {
    switch (_currentState) {
      case TransitionStates.ENTER_DELAY:
        ReallyEnter();
        EmitSignal(nameof(Entered));
        break;
      case TransitionStates.ENTERING:
        _currentState = TransitionStates.ENTERED;
        break;
      case TransitionStates.EXIT_DELAY:
        ReallyExit();
        break;
      case TransitionStates.EXITING:
        _currentState = TransitionStates.EXITED;
        EmitSignal(nameof(Exited));
        break;
      case TransitionStates.ENTERED:
      case TransitionStates.EXITED:
      default:
        break;
    }
  }
}
