namespace Wfc.Entities.World.Cutscenes;

using Chickensoft.Sync.Primitives;
using System;
using System.Threading.Tasks;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Screens.Levels;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class Cutscene : Node2D {
  private AutoChannel.Binding? _cutsceneBinding;

  public override void _Notification(int what) => this.Notify(what);
  [Dependency]
  public IGameLevel GameLevel => this.DependOn<IGameLevel>();

  [Dependency]
  public IGameRepo GameRepo => this.DependOn<IGameRepo>();
  private const float DURATION = 0.1f;
  private const float EXPAND_SIZE = 100;
  private const float REDUCE_SIZE = 0;
  // Start and end have to agree on this id or _onCutsceneRequestEnd drops the
  // request and the input lock is never released.
  private const string CUTSCENE_ID = "CutScene";
  // The share of a shot spent travelling out to what it is showing; the rest is
  // the way back, which still belongs to the shot.
  private const float TRAVEL_SHARE = 0.6f;

  public enum CutsceneState {
    Disabled,
    Enabling,
    Enabled,
    Disabling,
  }

  private string _currentCutsceneId = string.Empty;
  private CutsceneState _currentState = CutsceneState.Disabled;
  private Tween? _tweener = null;
  // Retires the shot in flight. A run whose generation has moved on writes nothing
  // more: not to the camera, and not to the cutscene a respawn has since started.
  private int _runGeneration;

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
    _cutsceneBinding ??= GameEvents.Instance.Channel.Bind()
      .On((in IGameEvents.CutsceneRequestStart m) => _onCutsceneRequestStart(m.Id))
      .On((in IGameEvents.CutsceneRequestEnd m) => _onCutsceneRequestEnd(m.Id))
      .On((in IGameEvents.CheckpointLoaded _) => _onCheckpointLoaded());
  }

  public override void _ExitTree() {
    base._ExitTree();
    _cutsceneBinding?.Dispose();
    _cutsceneBinding = null;
    // Leaving the tree mid-cutscene would otherwise strand the input lock: this
    // node owns it and is the only thing that can release it. The run goes with
    // it, so its ending cannot arrive in the next level.
    _runGeneration++;
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
  private void _setPlayerInputDisabled(bool disabled) {
    var player = GameRepo.Player.Value;
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

  // A respawn is a cut: the shot that was running is over, wherever it had got to. The
  // stripes come down and the player gets their input back now rather than whenever the
  // shot would have ended, and the run is retired so its own ending cannot arrive later
  // and close a cutscene the respawn has since started.
  private void _onCheckpointLoaded() {
    _runGeneration++;
    // The stripes and the timer are picked up in _Ready, one step after the subscription:
    // a reload arriving in between has no shot to call off yet.
    if (!IsNodeReady() || !IsBusy()) {
      return;
    }
    _tweener?.Kill();
    _tweener = null;
    _currentState = CutsceneState.Disabled;
    _currentCutsceneId = string.Empty;
    topRectNode.Size = new Vector2(topRectNode.Size.X, REDUCE_SIZE);
    bottomRectNode.Position = new Vector2(bottomRectNode.Position.X, bottomReducePosition);
    canvasNode.Visible = false;
    _setPlayerInputDisabled(false);
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

  // Borrows the camera to look at a node and hands it back. The camera owns the borrow
  // and can revoke it - a respawn does - so this never assigns to the camera directly:
  // the token is all this run has, and a revoked one writes nothing.
  public async void ShowSomeNode(Node2D node, float duration = 7.0f, float moveSpeed = 3.2f) {
    // Re-entering would leave two shots sharing one camera and one timer, and whichever
    // finished first would hand back a camera the other is still using.
    if (IsBusy() || !IsInstanceValid(node)) {
      return;
    }

    var run = ++_runGeneration;
    var cameraNode = GameLevel.CameraNode;
    var focus = cameraNode.BeginFocusOverride(node, moveSpeed);
    GameEvents.Instance.OnCutsceneRequestStart(CUTSCENE_ID);

    try {
      if (!await _waitFor(duration * TRAVEL_SHARE) || run != _runGeneration) {
        return;
      }
      cameraNode.ReturnFocus(focus);
      await _waitFor(duration * (1f - TRAVEL_SHARE));
    }
    finally {
      // A retired run has already had the camera taken off it and the input lock
      // released; ending here would close whatever cutscene is running now instead.
      if (run == _runGeneration) {
        if (IsInstanceValid(cameraNode)) {
          cameraNode.EndFocusOverride(focus);
        }
        GameEvents.Instance.OnCutsceneRequestEnd(CUTSCENE_ID);
      }
    }
  }

  // False once the level has been torn down under the shot: the caller stops there, and
  // the input lock is released by whoever is still alive to do it.
  private async Task<bool> _waitFor(float seconds) {
    try {
      timerNode.WaitTime = seconds;
      timerNode.Start();
      await ToSignal(timerNode, Timer.SignalName.Timeout);
      return true;
    }
    catch (ObjectDisposedException) {
      return false;
    }
  }
}
