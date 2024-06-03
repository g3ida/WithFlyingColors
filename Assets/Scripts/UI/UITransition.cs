using Godot;

public class UITransition : Control
{
    [Signal]
    public delegate void entered();

    [Signal]
    public delegate void exited();

    public enum TransitionStates
    {
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
    private SceneTreeTween tweener;

    [Export]
    public float time = 0.3f;

    [Export]
    public float delay = 0.25f;

    [Export]
    public Vector2 hidden_relative_position = Vector2.Zero;

    public override void _Ready()
    {
        parent = GetParent<Control>();
        parent.Connect("ready", this, nameof(_prepare), flags: (uint)ConnectFlags.Oneshot);
    }

    // API: enter(), exit()
    public void Enter()
    {
        GD.Print("enter");
        EnterDelay();
    }

    public void Exit()
    {
        ExitDelay();
    }

    private void EnterDelay()
    {
        currentState = TransitionStates.ENTER_DELAY;
        StartTween(hiddenPosition, delay);
    }

    private void ReallyEnter()
    {
        currentState = TransitionStates.ENTERING;
        StartTween(displayPosition, time);
    }

    private void ExitDelay()
    {
        currentState = TransitionStates.EXIT_DELAY;
        StartTween(displayPosition, delay);
    }

    private void ReallyExit()
    {
        currentState = TransitionStates.EXITING;
        StartTween(hiddenPosition, time);
    }

    private void StartTween(Vector2 destinationPos, float duration)
    {
        tweener?.Kill();
        tweener = CreateTween();
        tweener.Connect("finished", this, nameof(OnTweenCompleted), flags: (uint)ConnectFlags.Oneshot);
        tweener.TweenProperty(parent, "rect_position", destinationPos, duration)
              .SetTrans(Tween.TransitionType.Quad)
              .SetEase(Tween.EaseType.InOut);
    }

    private void _prepare()
    {
        displayPosition = parent.RectPosition;
        hiddenPosition = parent.RectPosition + hidden_relative_position;
        parent.RectPosition = hiddenPosition;
    }

    private void OnTweenCompleted()
    {
        if (currentState == TransitionStates.ENTER_DELAY)
        {
            ReallyEnter();
            EmitSignal(nameof(entered));
        }
        else if (currentState == TransitionStates.ENTERING)
        {
            currentState = TransitionStates.ENTERED;
        }
        else if (currentState == TransitionStates.EXIT_DELAY)
        {
            ReallyExit();
        }
        else if (currentState == TransitionStates.EXITING)
        {
            currentState = TransitionStates.EXITED;
            EmitSignal(nameof(exited));
        }
    }
}
