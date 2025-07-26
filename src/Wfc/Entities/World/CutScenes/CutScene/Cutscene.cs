namespace Wfc.Entities.World.Cutscenes;

using Godot;
using Wfc.Core.Event;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

[ScenePath]
public partial class Cutscene : Node2D {
  private const float DURATION = 0.1f;
  private const float EXPAND_SIZE = 100;
  private const float REDUCE_SIZE = 0;

  public enum CutsceneState {
    Disabled,
    Enabling,
    Enabled,
    Disabling,
  }

  private string _currentCutsceneId = string.Empty;
  private CutsceneState _currentState = CutsceneState.Disabled;
  private Tween? _tweener = null;

  #region Nodes
  [NodePath("CanvasLayer")]
  private CanvasLayer canvasNode = default!;
  [NodePath("CanvasLayer/Control/TopRect")]
  private Control topRectNode = default!;
  [NodePath("CanvasLayer/Control/BottomRect")]
  private Control bottomRectNode = default!;
  [NodePath("Timer")]
  private Timer timerNode = default!;
  #endregion Nodes

  // Positions
  private float bottomReducePosition;
  private float bottomExpandPosition;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();

    bottomReducePosition = bottomRectNode.Position.Y;
    bottomExpandPosition = bottomRectNode.Position.Y - EXPAND_SIZE;
  }

  public override void _EnterTree() {
    base._EnterTree();
    EventHandler.Instance.Events.CutSceneRequestStart += _onCutsceneRequestStart;
    EventHandler.Instance.Events.CutSceneRequestEnd += _onCutsceneRequestEnd;
  }

  public override void _ExitTree() {
    base._ExitTree();
    EventHandler.Instance.Events.CutSceneRequestStart -= _onCutsceneRequestStart;
    EventHandler.Instance.Events.CutSceneRequestEnd -= _onCutsceneRequestEnd;
  }

  public bool IsBusy() {
    return _currentState != CutsceneState.Disabled;
  }

  private bool _isDisablingOrDisabledState() {
    return _currentState == CutsceneState.Disabled || _currentState == CutsceneState.Disabling;
  }

  private bool _isEnabledOrEnablingState() {
    return _currentState == CutsceneState.Enabled || _currentState == CutsceneState.Enabling;
  }

  private void _onCutsceneRequestStart(string id) {
    if (_isDisablingOrDisabledState()) {
      _currentState = CutsceneState.Enabling;
      _currentCutsceneId = id;
      canvasNode.Visible = true;
      Global.Instance().Player.HandleInputIsDisabled = true;
      _showStripes();
    }
  }

  private void _onCutsceneRequestEnd(string id) {
    if (_isEnabledOrEnablingState() && _currentCutsceneId == id) {
      _currentState = CutsceneState.Disabling;
      _hideStripes();
    }
  }

  private void _renewTween() {
    _tweener?.Kill();
    _tweener = CreateTween();
    _tweener.Connect(
      Tween.SignalName.Finished,
      new Callable(this, nameof(_onTweenCompleted)),
      flags: (uint)ConnectFlags.OneShot
    );
  }

  private void _showStripes() {
    _renewTween();
    StartTween(topRectNode, EXPAND_SIZE);
    StartBottomTween(bottomRectNode, bottomExpandPosition);
  }

  private void _hideStripes() {
    _renewTween();
    StartTween(topRectNode, REDUCE_SIZE);
    StartBottomTween(bottomRectNode, bottomReducePosition);
  }

  private void StartTween(Control controlNode, float destSize) {
    if (_tweener != null) {
      _tweener.TweenProperty(controlNode, "size:y", destSize, DURATION)
             .SetTrans(Tween.TransitionType.Quad)
             .SetEase(Tween.EaseType.InOut);
    }
  }

  private void StartBottomTween(Control controlNode, float destPosition) {
    if (_tweener != null) {
      _tweener.TweenProperty(controlNode, "position:y", destPosition, DURATION)
           .SetTrans(Tween.TransitionType.Quad)
           .SetEase(Tween.EaseType.InOut);
    }
  }

  private void _onTweenCompleted() {
    if (_currentState == CutsceneState.Disabling) {
      _currentState = CutsceneState.Disabled;
      canvasNode.Visible = false;
      Global.Instance().Player.HandleInputIsDisabled = false;
    }
  }

  public async void ShowSomeNode(Node2D node, float duration = 7.0f, float moveSpeed = 3.2f) {
    var cameraLastFocus = Global.Instance().Camera.FollowNode;
    var cameraLastSpeed = Global.Instance().Camera.PositionSmoothingSpeed;
    EventHandler.Instance.EmitCutsceneRequestStart("CutScene");

    if (node != null) {
      Global.Instance().Camera.FollowNode = node;
    }
    Global.Instance().Camera.PositionSmoothingSpeed = moveSpeed;

    timerNode.WaitTime = duration * 0.6f;
    timerNode.Start();
    await ToSignal(timerNode, Timer.SignalName.Timeout);

    Global.Instance().Camera.FollowNode = cameraLastFocus;
    timerNode.WaitTime = duration * 0.4f;
    timerNode.Start();
    await ToSignal(timerNode, Timer.SignalName.Timeout);

    EventHandler.Instance.EmitCutsceneRequestEnd("CutScene");
    Global.Instance().Camera.PositionSmoothingSpeed = cameraLastSpeed;
  }
}
