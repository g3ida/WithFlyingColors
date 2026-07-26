namespace Wfc.Entities.World.Cutscenes;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Screens.Levels;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class Cutscene : Node2D {
  public override void _Notification(int what) => this.Notify(what);
  [Dependency]
  public IGameLevel GameLevel => this.DependOn<IGameLevel>();
  private const float DURATION = 0.1f;
  private const float EXPAND_SIZE = 100;
  private const float REDUCE_SIZE = 0;
  // Start and end have to agree on this id or _onCutsceneRequestEnd drops the
  // request and the input lock is never released.
  private const string CUTSCENE_ID = "CutScene";

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
    // Leaving the tree mid-cutscene would otherwise strand the input lock: this
    // node owns it and is the only thing that can release it.
    if (IsBusy()) {
      _currentState = CutsceneState.Disabled;
      _setPlayerInputDisabled(false);
    }
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
      _setPlayerInputDisabled(true);
      _showStripes();
    }
  }

  // The cutscene bus is process-wide but the player belongs to a level, so a
  // request can arrive while there is no valid player to lock.
  private static void _setPlayerInputDisabled(bool disabled) {
    var player = Global.Instance()?.Player;
    if (player != null && GodotObject.IsInstanceValid(player)) {
      player.HandleInputIsDisabled = disabled;
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
    if (_currentState == CutsceneState.Enabling) {
      _currentState = CutsceneState.Enabled;
    }
    else if (_currentState == CutsceneState.Disabling) {
      _currentState = CutsceneState.Disabled;
      canvasNode.Visible = false;
      _setPlayerInputDisabled(false);
    }
  }

  public async void ShowSomeNode(Node2D node, float duration = 7.0f, float moveSpeed = 3.2f) {
    // Re-entering would overwrite the saved camera state with the values the
    // previous run already replaced, so the first run's restore would put the
    // camera back onto the second run's focus node.
    if (IsBusy()) {
      return;
    }

    var cameraNode = GameLevel.CameraNode;
    var cameraLastFocus = cameraNode.FollowNode;
    var cameraLastSpeed = cameraNode.PositionSmoothingSpeed;
    EventHandler.Instance.EmitCutsceneRequestStart(CUTSCENE_ID);

    try {
      if (node != null) {
        cameraNode.FollowNode = node;
      }
      cameraNode.PositionSmoothingSpeed = moveSpeed;

      timerNode.WaitTime = duration * 0.6f;
      timerNode.Start();
      await ToSignal(timerNode, Timer.SignalName.Timeout);

      cameraNode.FollowNode = cameraLastFocus;
      timerNode.WaitTime = duration * 0.4f;
      timerNode.Start();
      await ToSignal(timerNode, Timer.SignalName.Timeout);
    }
    catch (ObjectDisposedException) {
      // The level was torn down mid-cutscene; fall through and still release
      // the input lock, or the next level starts with the player frozen.
    }
    finally {
      if (GodotObject.IsInstanceValid(cameraNode)) {
        cameraNode.FollowNode = cameraLastFocus;
        cameraNode.PositionSmoothingSpeed = cameraLastSpeed;
      }
      EventHandler.Instance.EmitCutsceneRequestEnd(CUTSCENE_ID);
    }
  }
}
