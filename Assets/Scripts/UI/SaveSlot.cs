using Godot;
using System;

public class SaveSlot : PanelContainer
{
    [Signal]
    public delegate void pressed(string action);

    public enum State { INIT, DEFAULT, FOCUS, ACTIONS_SHOWN }

    private const float MOVE_DURATION = 0.1f;
    private const int FOCUS_SHIFT = 0;
    private static readonly Color FOCUS_OFF_BG_COLOR = new Color(0.1765f, 0.1765f, 0.1765f, 0.079f);
    private static readonly Color FOCUS_ON_BG_COLOR = new Color(0.1765f, 0.1765f, 0.1765f, 0.196f);
    private const int MIN_WIDTH = 1160;

    private ImageTexture _texture = null;
    private int _timestamp = -1;
    private string _description = "";
    private Color _bgColor = FOCUS_OFF_BG_COLOR;
    private bool _hasFocus;
    private bool _isDisabled = false;
    public int _id = 0;
    private State _currentState = State.INIT;

    private Label _descriptionNode;
    private Label _timestampNode;
    private HBoxContainer _containerNode;
    private VBoxContainer _vBoxContainerNode;
    private Label _slotIndexNode;
    private SlotActionButtons _actionButtonsNode;
    private Button _buttonNode;
    private AnimationPlayer _animationPlayerNode;

    private float _posX;

    public override void _Ready()
    {
        _descriptionNode = GetNode<Label>("HBoxContainer/VBoxContainer/Description");
        _timestampNode = GetNode<Label>("HBoxContainer/VBoxContainer/Timestamp");
        _containerNode = GetNode<HBoxContainer>("HBoxContainer");
        _vBoxContainerNode = GetNode<VBoxContainer>("HBoxContainer/VBoxContainer");
        _slotIndexNode = GetNode<Label>("HBoxContainer/VBoxContainer/SlotIndex");
        _actionButtonsNode = GetNode<SlotActionButtons>("HBoxContainer/ActionButtons");
        _buttonNode = GetNode<Button>("Button");
        _animationPlayerNode = GetNode<AnimationPlayer>("AnimationPlayer");

        _posX = RectPosition.x;

        SetTexture(_texture);
        SetTimestamp(_timestamp);
        SetDescription(_description);
        SetSlotIndexLabel(_id);
        SetBgColor(_bgColor);
        SetState(State.DEFAULT);
        RectMinSize = new Vector2(MIN_WIDTH, RectMinSize.y);
    }

    public ImageTexture Texture
    {
        get => _texture;
        set => SetTexture(value);
    }

    public void SetTexture(ImageTexture value)
    {
        _texture = value;
    }

    public string Description
    {
        get => _description;
        set => SetDescription(value);
    }

    public void SetDescription(string value)
    {
        _description = value;
        _descriptionNode.Text = _description;
    }

    public int SlotIndexLabel
    {
        get => _id;
        set => SetSlotIndexLabel(value);
    }

    public void SetSlotIndexLabel(int value)
    {
        _id = value;
        _actionButtonsNode.SlotIndex = _id;
        _slotIndexNode.Text = $"SLOT {_id + 1}";
    }

    public int Timestamp
    {
        get => _timestamp;
        set => SetTimestamp(value);
    }

    public void SetTimestamp(int value)
    {
        _timestamp = value;
        if (_timestamp == -1)
        {
            _timestampNode.Text = "----/--/-- --:--";
        }
        else
        {
            var time = Time.GetDatetimeDictFromUnixTime(_timestamp);
            _timestampNode.Text = $"{time["year"]}/{time["month"]:00}/{time["day"]:00} {time["hour"]:00}:{time["minute"]:00}";
        }
    }

    public Color BgColor
    {
        get => _bgColor;
        set => SetBgColor(value);
    }

    public void SetBgColor(Color value)
    {
        _bgColor = value;
    }

    private void _on_Button_pressed()
    {
        if (_currentState == State.FOCUS)
        {
            SetState(State.ACTIONS_SHOWN);
        }
        else if (_currentState == State.ACTIONS_SHOWN)
        {
            SetState(State.FOCUS);
            EmitSignal(nameof(pressed), "focus");
        }
    }

    public override void _Process(float delta)
    {
        if (GetHasFocus())
        {
            SetBgColor(FOCUS_ON_BG_COLOR);
            _animationPlayerNode.Play("Blink");
        }
        else
        {
            SetBgColor(FOCUS_OFF_BG_COLOR);
            _animationPlayerNode.Play("RESET");
        }
    }

    private void _on_Button_mouse_entered()
    {
        _buttonNode.GrabFocus();
    }

    public new bool HasFocus
    {
        get => _hasFocus;
        set => SetHasFocus(value);
    }

    public void SetHasFocus(bool value)
    {
        if (value)
        {
            _buttonNode.GrabFocus();
        }
    }

    public bool GetHasFocus()
    {
        if (_buttonNode.HasFocus())
        {
            if (_currentState == State.DEFAULT)
            {
                SetState(State.FOCUS);
            }
            return true;
        }
        else
        {
            if (_currentState == State.ACTIONS_SHOWN && !_actionButtonsNode.ButtonsHasFocus())
            {
                SetState(State.DEFAULT);
            }
            return false;
        }
    }

    public bool IsDisabled
    {
        get => _isDisabled;
        set => SetIsDisabled(value);
    }

    public void SetIsDisabled(bool value)
    {
        _isDisabled = value;
        _buttonNode.Disabled = value;
        _buttonNode.FocusMode = value ? Control.FocusModeEnum.None : Control.FocusModeEnum.All;
    }

    public void SetState(State newState)
    {
        if (newState == _currentState) return;

        switch (newState)
        {
            case State.DEFAULT:
                _animationPlayerNode.Play("RESET");
                _actionButtonsNode.HideButton();
                SetBgColor(FOCUS_OFF_BG_COLOR);
                HideButtonNode(false);
                break;
            case State.FOCUS:
                _animationPlayerNode.Play("Blink");
                SetBgColor(FOCUS_ON_BG_COLOR);
                HideButtonNode(false);
                _actionButtonsNode.HideButton();
                break;
            case State.ACTIONS_SHOWN:
                if (_currentState == State.DEFAULT)
                {
                    SetState(State.FOCUS);
                }
                HideButtonNode(true);
                _actionButtonsNode.ShowButton();
                break;
        }

        _currentState = newState;
    }

    private void HideButtonNode(bool hide)
    {
        _buttonNode.Visible = !hide;
        _buttonNode.Disabled = hide;
    }

    private void _on_ActionButtons_clear_button_pressed(int slotIndex)
    {
        EmitSignal(nameof(pressed), "delete");
    }

    private void _on_ActionButtons_select_button_pressed(int slotIndex)
    {
        EmitSignal(nameof(pressed), "select");
        HideActionButtons();
    }

    public void HideActionButtons()
    {
        SetHasFocus(true);
        SetState(State.FOCUS);
    }

    public void UpdateMetaData()
    {
        var metaData = SaveGame.Instance().GetSlotMetaData(_id);
        if (metaData != null)
        {
            SetTimestamp(Helpers.ParseSaveDataInt(metaData, "save_time"));
            SetDescription($"LEVEL: 10 - Progress: {metaData["progress"]}%");
        }
        else
        {
            SetTimestamp(-1);
            SetDescription("<EMPTY>");
        }
    }

    public void SetBorder(bool state)
    {
        var stylePath = state ? "res://Assets/Styles/greyPanelWithBorder.tres" : "res://Assets/Styles/greyPanelWithBorderTransparent.tres";
        var style = (StyleBox)GD.Load(stylePath);
        RemoveStyleboxOverride("panel");
        AddStyleboxOverride("panel", style);
    }
}
