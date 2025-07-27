namespace Wfc.Entities.Ui.Slots;

using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Persistence;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class SaveSlotPanel : PanelContainer {

  public override void _Notification(int what) => this.Notify(what);
  [Dependency]
  public ISaveManager SaveManager => this.DependOn<ISaveManager>();

  [Signal]
  public delegate void PressedEventHandler(string action);

  private const float MOVE_DURATION = 0.1f;
  private const int FOCUS_SHIFT = 0;
  private static readonly Color FOCUS_OFF_BG_COLOR = new Color(0.1765f, 0.1765f, 0.1765f, 0.079f);
  private static readonly Color FOCUS_ON_BG_COLOR = new Color(0.1765f, 0.1765f, 0.1765f, 0.196f);

  public enum State { Init, Default, Focus, ActionShown }

  private const int MIN_WIDTH = 1160;

  private ImageTexture? _texture = null;
  private int _timestamp = -1;
  private string _description = "";
  private Color _bgColor = FOCUS_OFF_BG_COLOR;
  private bool _hasFocus = false;
  private bool _isDisabled = false;
  public int _id = 0;
  private State _currentState = State.Init;

  #region Nodes
  [NodePath("HBoxContainer/VBoxContainer/Description")]
  private Label _descriptionNode = default!;
  [NodePath("HBoxContainer/VBoxContainer/Timestamp")]
  private Label _timestampNode = default!;
  [NodePath("HBoxContainer")]
  private HBoxContainer _containerNode = default!;
  [NodePath("HBoxContainer/VBoxContainer")]
  private VBoxContainer _vBoxContainerNode = default!;
  [NodePath("HBoxContainer/VBoxContainer/SlotIndex")]
  private Label _slotIndexNode = default!;
  [NodePath("HBoxContainer/ActionButtons")]
  private SlotActionButtons _actionButtonsNode = default!;
  [NodePath("Button")]
  private Button _buttonNode = default!;
  [NodePath("AnimationPlayer")]
  private AnimationPlayer _animationPlayerNode = default!;
  #endregion Nodes

  private float _posX;

  public void OnResolved() { }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();

    _posX = Position.X;

    if (_texture != null) {
      SetTexture(_texture);
    }
    SetTimestamp(_timestamp);
    SetDescription(_description);
    SetSlotIndexLabel(_id);
    SetBgColor(_bgColor);
    SetState(State.Default);
    CustomMinimumSize = new Vector2(MIN_WIDTH, CustomMinimumSize.Y);
  }

  public ImageTexture? Texture2D {
    get => _texture;
    set => SetTexture(value);
  }

  public void SetTexture(ImageTexture? value) {
    _texture = value;
  }

  public string Description {
    get => _description;
    set => SetDescription(value);
  }

  public void SetDescription(string value) {
    _description = value;
    _descriptionNode.Text = _description;
  }

  public int SlotIndexLabel {
    get => _id;
    set => SetSlotIndexLabel(value);
  }

  public void SetSlotIndexLabel(int value) {
    _id = value;
    _actionButtonsNode.SlotIndex = _id;
    _slotIndexNode.Text = $"SLOT {_id + 1}";
  }

  public int Timestamp {
    get => _timestamp;
    set => SetTimestamp(value);
  }

  public void SetTimestamp(int value) {
    _timestamp = value;
    if (_timestamp == -1) {
      _timestampNode.Text = "----/--/-- --:--";
    }
    else {
      var time = Time.GetDatetimeDictFromUnixTime(_timestamp);
      _timestampNode.Text = $"{time["year"]}/{time["month"]:00}/{time["day"]:00} {time["hour"]:00}:{time["minute"]:00}";
    }
  }

  public Color BgColor {
    get => _bgColor;
    set => SetBgColor(value);
  }

  public void SetBgColor(Color value) {
    _bgColor = value;
  }

  private void _onButtonPressed() {
    if (_currentState == State.Focus) {
      SetState(State.ActionShown);
    }
    else if (_currentState == State.ActionShown) {
      SetState(State.Focus);
      EmitSignal(SaveSlotPanel.SignalName.Pressed, "focus");
    }
  }

  public override void _Process(double delta) {
    base._Process(delta);
    if (GetHasFocus()) {
      SetBgColor(FOCUS_ON_BG_COLOR);
      _animationPlayerNode.Play("Blink");
    }
    else {
      SetBgColor(FOCUS_OFF_BG_COLOR);
      _animationPlayerNode.Play("RESET");
    }
  }

  private void _onButtonMouseEntered() {
    _buttonNode.GrabFocus();
  }

  public new bool HasFocus {
    get => _hasFocus;
    set => SetHasFocus(value);
  }

  public void SetHasFocus(bool value) {
    if (value) {
      _buttonNode.GrabFocus();
    }
  }

  public bool GetHasFocus() {
    if (_buttonNode.HasFocus()) {
      if (_currentState == State.Default) {
        SetState(State.Focus);
      }
      return true;
    }
    else {
      if (_currentState == State.ActionShown && !_actionButtonsNode.ButtonsHasFocus()) {
        SetState(State.Default);
      }
      return false;
    }
  }

  public bool IsDisabled {
    get => _isDisabled;
    set => SetIsDisabled(value);
  }

  public void SetIsDisabled(bool value) {
    _isDisabled = value;
    _buttonNode.Disabled = value;
    _buttonNode.FocusMode = value ? Control.FocusModeEnum.None : Control.FocusModeEnum.All;
  }

  public void SetState(State newState) {
    if (newState == _currentState)
      return;

    switch (newState) {
      case State.Default:
        _animationPlayerNode.Play("RESET");
        _actionButtonsNode.HideButton();
        SetBgColor(FOCUS_OFF_BG_COLOR);
        HideButtonNode(false);
        break;
      case State.Focus:
        _animationPlayerNode.Play("Blink");
        SetBgColor(FOCUS_ON_BG_COLOR);
        HideButtonNode(false);
        _actionButtonsNode.HideButton();
        break;
      case State.ActionShown:
        if (_currentState == State.Default) {
          SetState(State.Focus);
        }
        HideButtonNode(true);
        _actionButtonsNode.ShowButton();
        break;
    }

    _currentState = newState;
  }

  private void HideButtonNode(bool hide) {
    _buttonNode.Visible = !hide;
    _buttonNode.Disabled = hide;
  }

  private void _on_ActionButtons_clear_button_pressed(int slotIndex) {
    EmitSignal(SaveSlotPanel.SignalName.Pressed, "delete");
  }

  private void _on_ActionButtons_select_button_pressed(int slotIndex) {
    EmitSignal(SaveSlotPanel.SignalName.Pressed, "select");
    HideActionButtons();
  }

  public void HideActionButtons() {
    SetHasFocus(true);
    SetState(State.Focus);
  }

  public void UpdateMetaData() {
    var metaData = SaveManager.GetSlotMetaData(_id);
    if (metaData != null) {
      SetTimestamp((int)metaData.SaveTimestamp);
      SetDescription($"LEVEL: 10 - Progress: {metaData.Progress}%");
    }
    else {
      SetTimestamp(-1);
      SetDescription("<EMPTY>");
    }
  }

  public void SetBorder(bool state) {
    var stylePath = state ? "res://Assets/Styles/greyPanelWithBorder.tres" : "res://Assets/Styles/greyPanelWithBorderTransparent.tres";
    var style = (StyleBox)GD.Load(stylePath);
    RemoveThemeStyleboxOverride("panel");
    AddThemeStyleboxOverride("panel", style);
  }
}
