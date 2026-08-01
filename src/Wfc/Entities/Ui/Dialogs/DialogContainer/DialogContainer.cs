namespace Wfc.Entities.Ui.Dialogs;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Input;
using Wfc.Core.Localization;
using Wfc.Core.Ui;
using Wfc.Screens.MenuManager;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

// Slides a ConfirmDialog down over the screen and holds input while it is up.
//
// The dialog registers with the modal stack rather than reaching into the screen
// behind it, so the screen, its widgets and the settings focus manager all stand
// down for as long as it is shown. That registration is also what pauses the tree,
// which is why this node processes Always.
//
// The dialog itself is an ordinary Control that fills the screen and centres its
// panel with anchors, so shown means position zero and hidden means one screen
// above - there is no window position to measure and nothing to drift.
[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class DialogContainer : Control {
  private const float TWEEN_DURATION = 0.2f;
  // Far enough up that no corner of the centred panel peeks into the screen.
  private const int HIDDEN_OFFSET_Y = 1500;

  private enum DialogStates {
    Showing,
    Shown,
    Hiding,
    Hidden
  }

  #region Dependencies
  // The dialog holds strings that were already translated when the screen was built,
  // so the engine's own auto-translation has nothing left to redo once the player
  // picks another language.
  public override void _Notification(int what) {
    this.Notify(what);
    if (what == NotificationTranslationChanged && _isWired) {
      _applyLocalizedText();
    }
  }

  [Dependency]
  public IInputManager InputManager => this.DependOn<IInputManager>();
  [Dependency]
  public IModalStack ModalStack => this.DependOn<IModalStack>();
  [Dependency]
  public ILocalizationService LocalizationService => this.DependOn<ILocalizationService>();
  #endregion Dependencies

  [Export] public NodePath DialogNodePath = default!;

  // Every dialog is worded in the player's language, so the wording belongs to the
  // container that puts one on screen rather than to the scene it sits in. The first
  // two are required - a container added without them shows the first entry in the
  // table, which is loud enough to be caught on the first run - while the third is
  // only read when the dialog has a cancel button to write it on.
  [Export] public TranslationKey DialogTextKey { get; set; }
  [Export] public TranslationKey ConfirmTextKey { get; set; }
  [Export] public TranslationKey CancelTextKey { get; set; }

  private ColorRect _colorRectNode = default!;
  private ConfirmDialog _dialogNode = default!;

  private DialogStates _currentState = DialogStates.Hidden;
  private Control? _lastFocusOwner;
  private Tween? _tweener;

  // Whether this dialog is the one holding the modal stack. Tracked rather than
  // inferred from the state so teardown can release the stack without having to
  // reason about which half of a tween it was interrupted in.
  private bool _holdsModal;

  // The dialog is only reachable from _Ready onwards, which is also the guard for
  // the translation notification: it arrives on screens still being built.
  private bool _isWired;

  public void OnResolved() => _applyLocalizedText();

  public override void _Ready() {
    base._Ready();
    ProcessMode = ProcessModeEnum.Always;
    _colorRectNode = GetNode<ColorRect>("ColorRect");
    _dialogNode = GetNode<ConfirmDialog>(DialogNodePath);
    _isWired = true;

    _moveOffScreen();

    _dialogNode.Confirmed += _onConfirmed;
    _dialogNode.Cancelled += Dismiss;
  }

  public override void _ExitTree() {
    base._ExitTree();
    // A screen torn down with a dialog still up would otherwise leave the tree
    // paused for the rest of the run.
    _releaseModal();
    if (_isWired) {
      _dialogNode.Confirmed -= _onConfirmed;
      _dialogNode.Cancelled -= Dismiss;
    }
  }

  private void _applyLocalizedText() {
    if (!_isWired) {
      return;
    }
    _dialogNode.SetText(LocalizationService.GetLocalizedString(DialogTextKey));
    _dialogNode.SetConfirmCaption(LocalizationService.GetLocalizedString(ConfirmTextKey));
    if (_dialogNode.ShowCancelButton) {
      _dialogNode.SetCancelCaption(LocalizationService.GetLocalizedString(CancelTextKey));
    }
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
    _prepareTween(0);
    _grabDialogFocus();
  }

  // A confirmation opens on its cancel button: these dialogs guard destructive
  // answers, so the harmless one is what a hasty press should land on. Deferred
  // because the dialog's buttons have only just been shown.
  private void _grabDialogFocus() =>
      Callable.From(_dialogNode.FocusDefaultButton).CallDeferred();

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
  // Confirmed signal in the scene and runs alongside this.
  private void _onConfirmed() {
    if (_isHiddenOrHiding()) {
      return;
    }
    EventHandler.Instance.EmitMenuActionPressed(MenuAction.ConfirmDialog);
    _startHiding();
  }

  // The player backed out, through UICancel or the dialog's cancel button.
  public void Dismiss() {
    if (_isHiddenOrHiding()) {
      return;
    }
    EventHandler.Instance.EmitMenuActionPressed(MenuAction.DismissDialog);
    _startHiding();
  }

  private void _startHiding() {
    _currentState = DialogStates.Hiding;
    _prepareTween(-HIDDEN_OFFSET_Y);
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
    _dialogNode.Position = new Vector2(_dialogNode.Position.X, -HIDDEN_OFFSET_Y);
    _hideNodes();
    _currentState = DialogStates.Hidden;
  }

  private void _onHidden() {
    _moveOffScreen();
    _releaseModal();
    // Confirming can navigate away, which frees the control that had focus before the
    // dialog opened well before this tween lands on it. It can also leave that
    // control unfocusable - an emptied slot's card - and grabbing focus for one of
    // those silently strands controller navigation with no focus at all.
    if (_lastFocusOwner != null && IsInstanceValid(_lastFocusOwner)
        && _lastFocusOwner.FocusMode != FocusModeEnum.None) {
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
