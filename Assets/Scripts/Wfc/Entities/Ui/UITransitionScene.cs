using Godot;

namespace Wfc.Entities.Ui
{
  public partial class UITransitionScene : Control
  {
    [Signal]
    public delegate void EnteredEventHandler();

    [Signal]
    public delegate void ExitedEventHandler();

    public enum TransitionStates
    {
      ENTER_DELAY,
      ENTERING,
      ENTERED,
      EXIT_DELAY,
      EXITING,
      EXITED
    }

    private Control _parent;
    private Vector2 _displayPosition;
    private Vector2 _hiddenPosition;
    private TransitionStates _currentState = TransitionStates.ENTER_DELAY;
    private Tween _tweener;

    [Export]
    public float Duration = 0.3f;

    [Export]
    public float Delay = 0.25f;

    [Export]
    public Vector2 HiddenRelativePosition = Vector2.Zero;

    public override void _Ready()
    {
      _parent = GetParent<Control>();
      _parent.Ready += _Prepare;
    }

    // API: enter(), exit()
    public void Enter()
    {
      _EnterDelay();
    }

    public void Exit()
    {
      _ExitDelay();
    }

    private void _EnterDelay()
    {
      _currentState = TransitionStates.ENTER_DELAY;
      _StartTween(_hiddenPosition, Delay);
    }

    private void _ReallyEnter()
    {
      _currentState = TransitionStates.ENTERING;
      _StartTween(_displayPosition, Duration);
    }

    private void _ExitDelay()
    {
      _currentState = TransitionStates.EXIT_DELAY;
      _StartTween(_displayPosition, Delay);
    }

    private void _ReallyExit()
    {
      _currentState = TransitionStates.EXITING;
      _StartTween(_hiddenPosition, Duration);
    }

    private void _StartTween(Vector2 destinationPos, float duration)
    {
      _tweener?.Kill();
      _tweener = CreateTween();
      _tweener.TweenProperty(_parent, "position", destinationPos, duration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.InOut);
      _tweener.Connect("finished", new Callable(this, nameof(_UpdateState)), flags: (uint)ConnectFlags.OneShot);
    }

    private void _Prepare()
    {
      _parent.Ready -= _Prepare;
      _displayPosition = _parent.Position;
      _hiddenPosition = _parent.Position + HiddenRelativePosition;
      _parent.Position = _hiddenPosition;
    }

    private void _UpdateState()
    {
      switch (_currentState)
      {
        case TransitionStates.ENTER_DELAY:
          _ReallyEnter();
          EmitSignal(nameof(Entered));
          break;
        case TransitionStates.ENTERING:
          _currentState = TransitionStates.ENTERED;
          break;
        case TransitionStates.EXIT_DELAY:
          _ReallyExit();
          break;
        case TransitionStates.EXITING:
          _currentState = TransitionStates.EXITED;
          EmitSignal(nameof(Exited));
          break;
      }
    }
  }
}
