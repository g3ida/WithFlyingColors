using Godot;
using System;
using System.Collections.Generic;

public partial class DialogContainer : Control {
  private const float TWEEN_DURATION = 0.2f;

  private enum DialogStates {
    SHOWING,
    SHOWN,
    HIDING,
    HIDDEN
  }

  [Export] public NodePath DialogNodePath;

  private ColorRect ColorRectNode;
  private Control GameMenuNode;
  private AcceptDialog DialogNode;

  private int shownPosY;
  private int hiddenPosY;

  private DialogStates currentState = DialogStates.HIDDEN;
  //private List<Button> dialogButtons = new List<Button>();
  private Control lastFocusOwner = null;
  private Tween tweener;

  public override void _Ready() {
    ProcessMode = ProcessModeEnum.Always;
    ColorRectNode = GetNode<ColorRect>("ColorRect");
    GameMenuNode = GetParent<Control>();
    DialogNode = GetNode<AcceptDialog>(DialogNodePath);

    shownPosY = DialogNode.Position.Y;
    hiddenPosY = shownPosY - 1000;

    HideDialog();
    DialogNode.Connect("close_requested", new Callable(this, nameof(StartHidingDialog)));
    DialogNode.Connect("confirmed", new Callable(this, nameof(StartHidingDialog)));
    //dialogButtons = GetDialogButtons();
  }

  public override void _ExitTree() {
    DialogNode.Disconnect("close_requested", new Callable(this, nameof(StartHidingDialog)));
    DialogNode.Disconnect("confirmed", new Callable(this, nameof(StartHidingDialog)));
  }

  public void ShowDialog() {
    if (IsShownOrShowingState())
      return;

    GetTree().Paused = true;
    PrepareTween(shownPosY);
    currentState = DialogStates.SHOWING;
    ShowNodes();
    lastFocusOwner = GetViewport().GuiGetFocusOwner();
    //dialogButtons[0].GrabFocus();
  }

  private void ShowNodes() {
    Show();
    DialogNode.Show();
    ColorRectNode.Show();
    // FIXME: fix this after migrating to C#
    GameMenuNode.Set("handle_back_event", false);
  }

  private void HideDialog() {
    DialogNode.Position = new Vector2I(DialogNode.Position.X, hiddenPosY);
    HideNodes();
    GetTree().Paused = false;
    lastFocusOwner?.GrabFocus();
    currentState = DialogStates.HIDDEN;
  }

  private void HideNodes() {
    Hide();
    DialogNode.Hide();
    ColorRectNode.Hide();
    GameMenuNode.Set("handle_back_event", true);
  }

  public override void _Input(InputEvent ev) {
    if (IsAcceptOrCancelPressed() && IsShownOrShowingState()) {
      StartHidingDialog();
    }
  }

  private void PrepareTween(float targetPosY) {
    tweener?.Kill();
    tweener = CreateTween();
    tweener.Connect("finished", new Callable(this, nameof(OnTweenCompleted)), flags: (uint)ConnectFlags.OneShot);

    tweener.TweenProperty(DialogNode, "position:y", targetPosY, TWEEN_DURATION)
           .SetTrans(Tween.TransitionType.Linear)
           .SetEase(Tween.EaseType.InOut);
  }

  private void StartHidingDialog() {
    if (IsHiddenOrHidingState())
      return;

    Event.Instance.EmitMenuButtonPressed(MenuButtons.CONFIRM_DIALOG);
    ShowNodes(); // just to make sure they are visible
    currentState = DialogStates.HIDING;
    PrepareTween(hiddenPosY);
  }

  private void OnTweenCompleted() {
    if (currentState == DialogStates.HIDING) {
      HideDialog();
    }
    else if (currentState == DialogStates.SHOWING) {
      currentState = DialogStates.SHOWN;
    }
  }

  private bool IsAcceptOrCancelPressed() {
    return Input.IsActionJustPressed("ui_cancel") || Input.IsActionJustPressed("ui_accept");
  }

  private bool IsShownOrShowingState() {
    return currentState == DialogStates.SHOWING || currentState == DialogStates.SHOWN;
  }

  private bool IsHiddenOrHidingState() {
    return currentState == DialogStates.HIDDEN || currentState == DialogStates.HIDING;
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
