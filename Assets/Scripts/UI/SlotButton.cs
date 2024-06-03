using Godot;
using System;

[Tool]
public class SlotButton : ColorRect
{
    public enum State { HIDDEN, HIDING, SHOWING, SHOWN }

    [Signal]
    public delegate void pressed();

    public const int BUTTON_MAX_WIDTH = 110;
    public const int BUTTON_MIN_WIDTH = 0;
    public const float MOVE_DURATION = 0.15f;

    [Export]
    public string node_color = "pink";
    [Export]
    public Texture iconTexture;
    [Export]
    public NodePath focus_left_node;
    [Export]
    public NodePath focus_right_node;

    private Button _buttonNode;
    private AnimationPlayer _blinkAnimationPlayerNode;
    private State _currentState = State.HIDDEN;

    private SceneTreeTween _buttonTween;

    public override void _Ready()
    {
        _buttonNode = GetNode<Button>("Button");
        _buttonNode.FocusMode = Control.FocusModeEnum.All;
        _blinkAnimationPlayerNode = GetNode<AnimationPlayer>("BlinkAnimationPlayer");

        _buttonNode.RectMinSize = new Vector2(0, _buttonNode.RectMinSize.y);
        Visible = false;
        _buttonNode.Disabled = true;
        UpdateHeight();
        Color = ColorUtils.GetBasicColor(ColorUtils.GetGroupColorIndex(node_color));
        _buttonNode.Icon = iconTexture;
    }

    private void SetFocusNextAndPrevious()
    {
        if (focus_left_node != null && !focus_left_node.IsEmpty())
        {
            var leftNode = GetNode<SlotButton>(focus_left_node);
            if (leftNode != null)
            {
                _buttonNode.FocusNeighbourLeft = leftNode._buttonNode.GetPath();
            }
        }

        if (focus_left_node != null && !focus_right_node.IsEmpty())
        {
            var rightNode = GetNode<SlotButton>(focus_right_node);
            if (rightNode != null)
            {
                _buttonNode.FocusNeighbourRight = rightNode._buttonNode.GetPath();
            }
        }
    }

    public void ShowButton()
    {
        if (_currentState == State.HIDING || _currentState == State.HIDDEN)
        {
            Visible = true;
            _buttonNode.Disabled = false;
            _currentState = State.SHOWING;
            SetupTween(BUTTON_MAX_WIDTH);
            _buttonNode.FocusMode = FocusModeEnum.All;
            SetFocusNextAndPrevious();
        }
    }

    public bool ButtonHasFocus()
    {
        return _buttonNode.HasFocus();
    }

    public void HideButton()
    {
        if (_currentState == State.SHOWING || _currentState == State.SHOWN)
        {
            _currentState = State.HIDING;
            SetupTween(BUTTON_MIN_WIDTH);
            _blinkAnimationPlayerNode.Play("RESET");
            _buttonNode.FocusMode = Control.FocusModeEnum.None;
        }
    }

    public new void GrabFocus()
    {
        _buttonNode.GrabFocus();
        _blinkAnimationPlayerNode.Play("Blink");
    }

    private void UpdateHeight()
    {
        // make the button height expand the whole container
        RectSize = new Vector2(RectSize.x, GetParent<Control>().RectSize.y);
        _buttonNode.RectSize = new Vector2(_buttonNode.RectSize.x, RectSize.y);
    }

    public override void _Process(float delta)
    {
        RectMinSize = new Vector2(_buttonNode.RectMinSize.x, RectMinSize.y);
        RectSize = new Vector2(_buttonNode.RectMinSize.x, RectSize.y);
        UpdateHeight();
        BlinkButtonIfNeeded();
    }

    private void BlinkButtonIfNeeded()
    {
        if (_buttonNode.HasFocus())
        {
            if (_blinkAnimationPlayerNode.CurrentAnimation != "Blink")
            {
                _blinkAnimationPlayerNode.Play("Blink");
            }
        }
        else
        {
            if (_blinkAnimationPlayerNode.CurrentAnimation == "Blink")
            {
                _blinkAnimationPlayerNode.Play("RESET");
            }
        }
    }

    private void SetupTween(float sizeX)
    {
        _buttonTween?.Kill();
        _buttonTween = CreateTween();
        _buttonTween.TweenProperty(_buttonNode, "rect_min_size:x", sizeX, MOVE_DURATION)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Linear);
        _buttonTween.Connect("finished", this, nameof(OnTweenCompleted), flags: (uint)ConnectFlags.Oneshot);
    }

    private void OnTweenCompleted()
    {
        if (_currentState == State.HIDING)
        {
            _currentState = State.HIDDEN;
            Visible = false;
            _buttonNode.Disabled = true;
        }
        else if (_currentState == State.SHOWING)
        {
            _currentState = State.SHOWN;
        }
    }

    private void _on_Button_pressed()
    {
        EmitSignal(nameof(pressed));
    }

    private void _on_Button_mouse_entered()
    {
        GrabFocus();
    }
}
