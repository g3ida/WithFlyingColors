namespace Wfc.Entities.Ui;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Input;
using Wfc.Core.Ui;
using Wfc.Screens.MenuManager;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

// Slides an AcceptDialog (or ConfirmationDialog) down over the screen and holds
// input while it is up.
//
// The dialog registers with the modal stack rather than reaching into the screen
// behind it, so the screen, its widgets and the settings focus manager all stand
// down for as long as it is shown. That registration is also what pauses the tree,
// which is why this node processes Always.
[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class DialogContainer : Control {
  private const float TWEEN_DURATION = 0.2f;
  private const int HIDDEN_OFFSET_Y = 1000;

  private enum DialogStates {
    Showing,
    Shown,
    Hiding,
    Hidden
  }

  #region Dependencies
  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public IInputManager InputManager => this.DependOn<IInputManager>();
  [Dependency]
  public IModalStack ModalStack => this.DependOn<IModalStack>();
  #endregion Dependencies

  [Export] public NodePath DialogNodePath = default!;

  private ColorRect _colorRectNode = default!;
  private AcceptDialog _dialogNode = default!;

  private int _shownPosY;
  private int _hiddenPosY;

  private DialogStates _currentState = DialogStates.Hidden;
  private Control? _lastFocusOwner;
  private Tween? _tweener;

  // Whether this dialog is the one holding the modal stack. Tracked rather than
  // inferred from the state so teardown can release the stack without having to
  // reason about which half of a tween it was interrupted in.
  private bool _holdsModal;

  public void OnResolved() { }

  public override void _Ready() {
    base._Ready();
    ProcessMode = ProcessModeEnum.Always;
    _colorRectNode = GetNode<ColorRect>("ColorRect");
    _dialogNode = GetNode<AcceptDialog>(DialogNodePath);

    _shownPosY = _dialogNode.Position.Y;
    _hiddenPosY = _shownPosY - HIDDEN_OFFSET_Y;

    _moveOffScreen();

    _dialogNode.Connect(AcceptDialog.SignalName.CloseRequested, new Callable(this, nameof(Dismiss)));
    _dialogNode.Connect(AcceptDialog.SignalName.Confirmed, new Callable(this, nameof(_onConfirmed)));
  }

  public override void _ExitTree() {
    base._ExitTree();
    // A screen torn down with a dialog still up would otherwise leave the tree
    // paused for the rest of the run.
    _releaseModal();
    _dialogNode.Disconnect(AcceptDialog.SignalName.CloseRequested, new Callable(this, nameof(Dismiss)));
    _dialogNode.Disconnect(AcceptDialog.SignalName.Confirmed, new Callable(this, nameof(_onConfirmed)));
  }

  public void ShowDialog() {
    if (_isShownOrShowing()) {
      return;
    }

    ModalStack.Push(this);
    _holdsModal = true;
    _lastFocusOwner = GetViewport().GuiGetFocusOwner();
    _currentState = DialogStates.Showing;
    _showNodes();
    _prepareTween(_shownPosY);
    _grabDialogFocus();
  }

  // A confirmation opens on its cancel button: these dialogs guard a slot wipe, so
  // the harmless answer is the one already under the player's thumb. Deferred
  // because the dialog window has only just been shown.
  private void _grabDialogFocus() {
    var button = (_dialogNode as ConfirmationDialog)?.GetCancelButton() ?? _dialogNode.GetOkButton();
    button?.CallDeferred(Control.MethodName.GrabFocus);
  }

  public override void _Input(InputEvent @event) {
    if (!_isShownOrShowing() || ModalStack.IsBlockedFor(this)) {
      return;
    }

    // Cancel only. UIConfirm is deliberately left alone so it reaches the focused
    // dialog button: swallowing it here meant Enter dismissed a confirmation
    // instead of confirming it, and there was no way to confirm without a mouse.
    if (InputManager.IsEventActionJustPressed(IInputManager.Action.UICancel, @event)) {
      Dismiss();
      GetViewport().SetInputAsHandled();
    }
  }

  // The player accepted. The screen's own handler is wired to the dialog's
  // confirmed signal in the scene and runs alongside this.
  private void _onConfirmed() {
    if (_isHiddenOrHiding()) {
      return;
    }
    EventHandler.Instance.EmitMenuActionPressed(MenuAction.ConfirmDialog);
    _startHiding();
  }

  // The player backed out, through UICancel or the window's own close.
  public void Dismiss() {
    if (_isHiddenOrHiding()) {
      return;
    }
    EventHandler.Instance.EmitMenuActionPressed(MenuAction.DismissDialog);
    _startHiding();
  }

  private void _startHiding() {
    _currentState = DialogStates.Hiding;
    _prepareTween(_hiddenPosY);
  }

  private void _showNodes() {
    Show();
    _dialogNode.Show();
    _colorRectNode.Show();
  }

  private void _hideNodes() {
    Hide();
    _dialogNode.Hide();
    _colorRectNode.Hide();
  }

  private void _moveOffScreen() {
    _dialogNode.Position = new Vector2I(_dialogNode.Position.X, _hiddenPosY);
    _hideNodes();
    _currentState = DialogStates.Hidden;
  }

  private void _onHidden() {
    _moveOffScreen();
    _releaseModal();
    // Confirming can navigate away, which frees the control that had focus before the
    // dialog opened well before this tween lands on it.
    if (_lastFocusOwner != null && IsInstanceValid(_lastFocusOwner)) {
      _lastFocusOwner.GrabFocus();
    }
    _lastFocusOwner = null;
  }

  private void _releaseModal() {
    if (!_holdsModal) {
      return;
    }
    _holdsModal = false;
    ModalStack.Pop(this);
  }

  private void _prepareTween(float targetPosY) {
    _tweener?.Kill();
    _tweener = CreateTween();
    _tweener.Connect(
      Tween.SignalName.Finished,
      new Callable(this, nameof(_onTweenCompleted)),
      flags: (uint)ConnectFlags.OneShot
    );

    _tweener.TweenProperty(_dialogNode, "position:y", targetPosY, TWEEN_DURATION)
           .SetTrans(Tween.TransitionType.Linear)
           .SetEase(Tween.EaseType.InOut);
  }

  private void _onTweenCompleted() {
    if (_currentState == DialogStates.Hiding) {
      _onHidden();
    }
    else if (_currentState == DialogStates.Showing) {
      _currentState = DialogStates.Shown;
    }
  }

  private bool _isShownOrShowing() => _currentState is DialogStates.Showing or DialogStates.Shown;

  private bool _isHiddenOrHiding() => _currentState is DialogStates.Hidden or DialogStates.Hiding;
}
