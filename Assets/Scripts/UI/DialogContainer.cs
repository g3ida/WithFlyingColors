using Godot;
using System;
using System.Collections.Generic;

public class DialogContainer : Control
{
    private const float TWEEN_DURATION = 0.2f;

    private enum DialogStates
    {
        SHOWING,
        SHOWN,
        HIDING,
        HIDDEN
    }

    [Export] public NodePath DialogNodePath;

    private ColorRect ColorRectNode;
    private Control GameMenuNode;
    private Control DialogNode;

    private float shownPosY;
    private float hiddenPosY;

    private DialogStates currentState = DialogStates.HIDDEN;
    private List<Button> dialogButtons = new List<Button>();
    private Control lastFocusOwner = null;
    private SceneTreeTween tweener;

    public override void _Ready()
    {
        PauseMode = PauseModeEnum.Process;
        ColorRectNode = GetNode<ColorRect>("ColorRect");
        GameMenuNode = GetParent<Control>();
        DialogNode = GetNode<Control>(DialogNodePath);

        shownPosY = DialogNode.RectPosition.y;
        hiddenPosY = shownPosY - 1000;

        HideDialog();
        DialogNode.Connect("hide", this, nameof(StartHidingDialog));
        DialogNode.Connect("confirmed", this, nameof(StartHidingDialog));
        dialogButtons = GetDialogButtons();
    }

    public override void _ExitTree()
    {
        DialogNode.Disconnect("hide", this, nameof(StartHidingDialog));
        DialogNode.Disconnect("confirmed", this, nameof(StartHidingDialog));
    }

    public void ShowDialog()
    {
        if (IsShownOrShowingState())
            return;

        GetTree().Paused = true;
        PrepareTween(shownPosY);
        currentState = DialogStates.SHOWING;
        ShowNodes();
        lastFocusOwner = GetFocusOwner();
        dialogButtons[0].GrabFocus();
    }

    private void ShowNodes()
    {
        Show();
        DialogNode.Show();
        ColorRectNode.Show();
        // FIXME: fix this after migrating to C#
        GameMenuNode.Set("handle_back_event", false);
    }

    private void HideDialog()
    {
        DialogNode.RectPosition = new Vector2(DialogNode.RectPosition.x, hiddenPosY);
        HideNodes();
        GetTree().Paused = false;
        lastFocusOwner?.GrabFocus();
        currentState = DialogStates.HIDDEN;
    }

    private void HideNodes()
    {
        Hide();
        DialogNode.Hide();
        ColorRectNode.Hide();
        GameMenuNode.Set("handle_back_event", true);
    }

    public override void _Input(InputEvent @event)
    {
        if (IsAcceptOrCancelPressed() && IsShownOrShowingState())
        {
            StartHidingDialog();
        }
    }

    private void PrepareTween(float targetPosY)
    {
        tweener?.Kill();
        tweener = CreateTween();
        tweener.Connect("finished", this, nameof(OnTweenCompleted), flags: (uint)ConnectFlags.Oneshot);

        tweener.TweenProperty(DialogNode, "rect_position:y", targetPosY, TWEEN_DURATION)
               .SetTrans(Tween.TransitionType.Linear)
               .SetEase(Tween.EaseType.InOut);
    }

    private void StartHidingDialog()
    {
        if (IsHiddenOrHidingState())
            return;

        Event.Instance().EmitSignal("menu_button_pressed", MenuButtons.CONFIRM_DIALOG);
        ShowNodes(); // just to make sure they are visible
        currentState = DialogStates.HIDING;
        PrepareTween(hiddenPosY);
    }

    private void OnTweenCompleted()
    {
        if (currentState == DialogStates.HIDING)
        {
            HideDialog();
        }
        else if (currentState == DialogStates.SHOWING)
        {
            currentState = DialogStates.SHOWN;
        }
    }

    private bool IsAcceptOrCancelPressed()
    {
        return Input.IsActionJustPressed("ui_cancel") || Input.IsActionJustPressed("ui_accept");
    }

    private bool IsShownOrShowingState()
    {
        return currentState == DialogStates.SHOWING || currentState == DialogStates.SHOWN;
    }

    private bool IsHiddenOrHidingState()
    {
        return currentState == DialogStates.HIDDEN || currentState == DialogStates.HIDING;
    }

    private List<Button> GetDialogButtons()
    {
        var buttons = new List<Button>();
        foreach (Node child in DialogNode.GetChildren())
        {
            if (child is HBoxContainer hbox)
            {
                foreach (Node subChild in hbox.GetChildren())
                {
                    if (subChild is Button button)
                    {
                        buttons.Add(button);
                    }
                }
                break;
            }
        }
        return buttons;
    }
}
