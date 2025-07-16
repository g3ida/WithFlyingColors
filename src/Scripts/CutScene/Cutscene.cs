using Godot;
using Wfc.Core.Event;
using EventHandler = Wfc.Core.Event.EventHandler;

public partial class Cutscene : Node2D {
  private const float DURATION = 0.1f;
  private const float EXPAND_SIZE = 100;
  private const float REDUCE_SIZE = 0;

  public enum CutsceneState {
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

  public override void _Ready() {
    canvasNode = GetNode<CanvasLayer>("CanvasLayer");
    topRectNode = GetNode<Control>("CanvasLayer/Control/TopRect");
    bottomRectNode = GetNode<Control>("CanvasLayer/Control/BottomRect");
    timerNode = GetNode<Timer>("Timer");

    bottomReducePosition = bottomRectNode.Position.Y;
    bottomExpandPosition = bottomRectNode.Position.Y - EXPAND_SIZE;
  }

  public override void _EnterTree() {
    EventHandler.Instance.Events.CutSceneRequestStart += OnCutsceneRequestStart;
    EventHandler.Instance.Events.CutSceneRequestEnd += OnCutsceneRequestEnd;
  }

  public override void _ExitTree() {
    EventHandler.Instance.Events.CutSceneRequestStart -= OnCutsceneRequestStart;
    EventHandler.Instance.Events.CutSceneRequestEnd -= OnCutsceneRequestEnd;
  }

  public bool IsBusy() {
    return currentState != CutsceneState.DISABLED;
  }

  private bool IsDisablingOrDisabledState() {
    return currentState == CutsceneState.DISABLED || currentState == CutsceneState.DISABLING;
  }

  private bool IsEnabledOrEnablingState() {
    return currentState == CutsceneState.ENABLED || currentState == CutsceneState.ENABLING;
  }

  private void OnCutsceneRequestStart(string id) {
    if (IsDisablingOrDisabledState()) {
      currentState = CutsceneState.ENABLING;
      currentCutsceneId = id;
      canvasNode.Visible = true;
      Global.Instance().Player.HandleInputIsDisabled = true;
      ShowStripes();
    }
  }

  private void OnCutsceneRequestEnd(string id) {
    if (IsEnabledOrEnablingState() && currentCutsceneId == id) {
      currentState = CutsceneState.DISABLING;
      HideStripes();
    }
  }

  private void RenewTween() {
    tweener?.Kill();
    tweener = CreateTween();
    tweener.Connect(
      Tween.SignalName.Finished,
      new Callable(this, nameof(OnTweenCompleted)),
      flags: (uint)ConnectFlags.OneShot
    );
  }

  private void ShowStripes() {
    RenewTween();
    StartTween(topRectNode, EXPAND_SIZE);
    StartBottomTween(bottomRectNode, bottomExpandPosition);
  }

  private void HideStripes() {
    RenewTween();
    StartTween(topRectNode, REDUCE_SIZE);
    StartBottomTween(bottomRectNode, bottomReducePosition);
  }

  private void StartTween(Control controlNode, float destSize) {
    tweener.TweenProperty(controlNode, "size:y", destSize, DURATION)
           .SetTrans(Tween.TransitionType.Quad)
           .SetEase(Tween.EaseType.InOut);
  }

  private void StartBottomTween(Control controlNode, float destPosition) {
    tweener.TweenProperty(controlNode, "position:y", destPosition, DURATION)
           .SetTrans(Tween.TransitionType.Quad)
           .SetEase(Tween.EaseType.InOut);
  }

  private void OnTweenCompleted() {
    if (currentState == CutsceneState.DISABLING) {
      currentState = CutsceneState.DISABLED;
      canvasNode.Visible = false;
      Global.Instance().Player.HandleInputIsDisabled = false;
    }
  }

  public async void ShowSomeNode(Node2D node, float duration = 7.0f, float moveSpeed = 3.2f) {
    var cameraLastFocus = Global.Instance().Camera.follow;
    var cameraLastSpeed = Global.Instance().Camera.PositionSmoothingSpeed;
    EventHandler.Instance.EmitCutsceneRequestStart("my_cutScene");

    if (node != null) {
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

    EventHandler.Instance.EmitCutsceneRequestEnd("my_cutScene");
    Global.Instance().Camera.PositionSmoothingSpeed = cameraLastSpeed;
  }
}
