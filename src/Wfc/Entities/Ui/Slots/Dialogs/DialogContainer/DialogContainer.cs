namespace Wfc.Entities.Ui;

using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Event;
using Wfc.Core.Input;
using Wfc.Screens;
using Wfc.Screens.MenuManager;
using Wfc.Utils.Attributes;
using EventHandler = Wfc.Core.Event.EventHandler;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class DialogContainer : Control {
  private const float TWEEN_DURATION = 0.2f;

  private enum DialogStates {
    Showing,
    Shown,
    Hiding,
    Hidden
  }

  [Dependency]
  public IInputManager InputManager => this.DependOn<IInputManager>();
  [Export] public NodePath DialogNodePath = default!;
  private ColorRect ColorRectNode = default!;
  private GameMenu GameMenuNode = default!;
  private AcceptDialog DialogNode = default!;

  private int shownPosY;
  private int hiddenPosY;

  private DialogStates currentState = DialogStates.Hidden;
  //private List<Button> dialogButtons = new List<Button>();
  private Control? lastFocusOwner = null;
  private Tween? tweener;

  public override void _Notification(int what) => this.Notify(what);

  public void OnResolved() { }

  public override void _Ready() {
    base._Ready();
    ProcessMode = ProcessModeEnum.Always;
    ColorRectNode = GetNode<ColorRect>("ColorRect");
    GameMenuNode = GetParent<GameMenu>();
    DialogNode = GetNode<AcceptDialog>(DialogNodePath);

    shownPosY = DialogNode.Position.Y;
    hiddenPosY = shownPosY - 1000;

    HideDialog();

    DialogNode.Connect(AcceptDialog.SignalName.CloseRequested, new Callable(this, nameof(StartHidingDialog)));
    DialogNode.Connect(AcceptDialog.SignalName.Confirmed, new Callable(this, nameof(StartHidingDialog)));
    //dialogButtons = GetDialogButtons();
  }

  public override void _ExitTree() {
    DialogNode.Disconnect(AcceptDialog.SignalName.CloseRequested, new Callable(this, nameof(StartHidingDialog)));
    DialogNode.Disconnect(AcceptDialog.SignalName.Confirmed, new Callable(this, nameof(StartHidingDialog)));
  }

  public void ShowDialog() {
    if (_IsShownOrShowingState())
      return;

    GetTree().Paused = true;
    PrepareTween(shownPosY);
    currentState = DialogStates.Showing;
    ShowNodes();
    lastFocusOwner = GetViewport().GuiGetFocusOwner();
    //dialogButtons[0].GrabFocus();
  }

  private void ShowNodes() {
    Show();
    DialogNode.Show();
    ColorRectNode.Show();
    GameMenuNode.HandleBackEvent = false;
  }

  private void HideDialog() {
    DialogNode.Position = new Vector2I(DialogNode.Position.X, hiddenPosY);
    HideNodes();
    GetTree().Paused = false;
    lastFocusOwner?.GrabFocus();
    currentState = DialogStates.Hidden;
  }

  private void HideNodes() {
    Hide();
    DialogNode.Hide();
    ColorRectNode.Hide();
    GameMenuNode.HandleBackEvent = true;
  }

  public override void _Input(InputEvent @event) {
    if (_IsAcceptOrCancelPressed() && _IsShownOrShowingState()) {
      StartHidingDialog();
    }
  }

  private void PrepareTween(float targetPosY) {
    tweener?.Kill();
    tweener = CreateTween();
    tweener.Connect(
      Tween.SignalName.Finished,
      new Callable(this, nameof(OnTweenCompleted)),
      flags: (uint)ConnectFlags.OneShot
    );

    tweener.TweenProperty(DialogNode, "position:y", targetPosY, TWEEN_DURATION)
           .SetTrans(Tween.TransitionType.Linear)
           .SetEase(Tween.EaseType.InOut);
  }

  private void StartHidingDialog() {
    if (_IsHiddenOrHidingState()) {
      return;
    }

    EventHandler.Instance.EmitMenuActionPressed(MenuAction.ConfirmDialog);
    ShowNodes(); // just to make sure they are visible
    currentState = DialogStates.Hiding;
    PrepareTween(hiddenPosY);
  }

  private void OnTweenCompleted() {
    if (currentState == DialogStates.Hiding) {
      HideDialog();
    }
    else if (currentState == DialogStates.Showing) {
      currentState = DialogStates.Shown;
    }
  }

  private bool _IsAcceptOrCancelPressed() {
    return InputManager.IsJustPressed(IInputManager.Action.UICancel) || InputManager.IsJustPressed(IInputManager.Action.UIConfirm);
  }

  private bool _IsShownOrShowingState() {
    return currentState == DialogStates.Showing || currentState == DialogStates.Shown;
  }

  private bool _IsHiddenOrHidingState() {
    return currentState == DialogStates.Hidden || currentState == DialogStates.Hiding;
  }

  // private List<Button> GetDialogButtons()
  // {
  //     var buttons = new List<Button>();
  //     foreach (Node child in DialogNode.GetChildren())
  //     {
  //         if (child is HBoxContainer hBox)
  //         {
  //             foreach (Node subChild in hBox.GetChildren())
  //             {
  //                 if (subChild is Button button)
  //                 {
  //                     buttons.Add(button);
  //                 }
  //             }
  //             break;
  //         }
  //     }
  //     return buttons;
  // }
}
