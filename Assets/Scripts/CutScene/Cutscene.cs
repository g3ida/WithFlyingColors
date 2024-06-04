using Godot;
using System;

public partial class Cutscene : Node2D
{
    private const float DURATION = 0.1f;
    private const float EXPAND_SIZE = 100;
    private const float REDUCE_SIZE = 0;

    public enum CutsceneState
    {
        DISABLED,
        ENABLING,
        ENABLED,
        DISABLING,
    }

    private string currentCutsceneId = null;
    private CutsceneState currentState = CutsceneState.DISABLED;
    private Tween tweener;

    // Nodes
    private CanvasLayer canvasNode;
    private Control topRectNode;
    private Control bottomRectNode;
    private Timer timerNode;

    // Positions
    private float bottomReducePosition;
    private float bottomExpandPosition;

    public override void _Ready()
    {
        canvasNode = GetNode<CanvasLayer>("CanvasLayer");
        topRectNode = GetNode<Control>("CanvasLayer/Control/TopRect");
        bottomRectNode = GetNode<Control>("CanvasLayer/Control/BottomRect");
        timerNode = GetNode<Timer>("Timer");

        bottomReducePosition = bottomRectNode.Position.Y;
        bottomExpandPosition = bottomRectNode.Position.Y - EXPAND_SIZE;
    }

    public override void _EnterTree()
    {
        Event.Instance().Connect("cutscene_request_start", new Callable(this, nameof(OnCutsceneRequestStart)));
        Event.Instance().Connect("cutscene_request_end", new Callable(this, nameof(OnCutsceneRequestEnd)));
    }

    public override void _ExitTree()
    {
        Event.Instance().Disconnect("cutscene_request_start", new Callable(this, nameof(OnCutsceneRequestStart)));
        Event.Instance().Disconnect("cutscene_request_end", new Callable(this, nameof(OnCutsceneRequestEnd)));
    }

    public bool IsBusy()
    {
        return currentState != CutsceneState.DISABLED;
    }

    private bool IsDisablingOrDisabledState()
    {
        return currentState == CutsceneState.DISABLED || currentState == CutsceneState.DISABLING;
    }

    private bool IsEnabledOrEnablingState()
    {
        return currentState == CutsceneState.ENABLED || currentState == CutsceneState.ENABLING;
    }

    private void OnCutsceneRequestStart(string id)
    {
        if (IsDisablingOrDisabledState())
        {
            currentState = CutsceneState.ENABLING;
            currentCutsceneId = id;
            canvasNode.Visible = true;
            Global.Instance().Player.handle_input_is_disabled = true;
            ShowStripes();
        }
    }

    private void OnCutsceneRequestEnd(string id)
    {
        if (IsEnabledOrEnablingState() && currentCutsceneId == id)
        {
            currentState = CutsceneState.DISABLING;
            HideStripes();
        }
    }

    private void RenewTween()
    {
        tweener?.Kill();
        tweener = CreateTween();
        tweener.Connect("finished", new Callable(this, nameof(OnTweenCompleted)), flags: (uint)ConnectFlags.OneShot);
    }

    private void ShowStripes()
    {
        RenewTween();
        StartTween(topRectNode, EXPAND_SIZE);
        StartBottomTween(bottomRectNode, bottomExpandPosition);
    }

    private void HideStripes()
    {
        RenewTween();
        StartTween(topRectNode, REDUCE_SIZE);
        StartBottomTween(bottomRectNode, bottomReducePosition);
    }

    private void StartTween(Control controlNode, float destSize)
    {
        tweener.TweenProperty(controlNode, "rect_size:y", destSize, DURATION)
               .SetTrans(Tween.TransitionType.Quad)
               .SetEase(Tween.EaseType.InOut);
    }

    private void StartBottomTween(Control controlNode, float destPosition)
    {
        tweener.TweenProperty(controlNode, "rect_position:y", destPosition, DURATION)
               .SetTrans(Tween.TransitionType.Quad)
               .SetEase(Tween.EaseType.InOut);
    }

    private void OnTweenCompleted()
    {
        if (currentState == CutsceneState.DISABLING)
        {
            currentState = CutsceneState.DISABLED;
            canvasNode.Visible = false;
            Global.Instance().Player.handle_input_is_disabled = false;
        }
    }

    public async void ShowSomeNode(Node2D node, float duration = 7.0f, float moveSpeed = 3.2f)
    {
        var cameraLastFocus = Global.Instance().Camera.follow;
        var cameraLastSpeed = Global.Instance().Camera.PositionSmoothingSpeed;
        Event.Instance().EmitCutsceneRequestStart("my_cutscene");

        if (node != null)
        {
            Global.Instance().Camera.follow = node;
        }
        Global.Instance().Camera.PositionSmoothingSpeed = moveSpeed;

        timerNode.WaitTime = duration * 0.6f;
        timerNode.Start();
        await ToSignal(timerNode, "timeout");

        Global.Instance().Camera.follow = cameraLastFocus;
        timerNode.WaitTime = duration * 0.4f;
        timerNode.Start();
        await ToSignal(timerNode, "timeout");

        Event.Instance().EmitCutsceneRequestEnd("my_cutscene");
        Global.Instance().Camera.PositionSmoothingSpeed = cameraLastSpeed;
    }
}
