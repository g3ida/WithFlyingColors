using System;
using Godot;
using Wfc.Skin;

[Tool]
public partial class SlotButton : ColorRect {
  public enum State { HIDDEN, HIDING, SHOWING, SHOWN }

  [Signal]
  public delegate void pressedEventHandler();

  public const int BUTTON_MAX_WIDTH = 110;
  public const int BUTTON_MIN_WIDTH = 0;
  public const float MOVE_DURATION = 0.15f;

  [Export]
  public string node_color = "pink";
  [Export]
  public Texture2D iconTexture;
  [Export]
  public NodePath focus_left_node;
  [Export]
  public NodePath focus_right_node;

  private Button _buttonNode;
  private AnimationPlayer _blinkAnimationPlayerNode;
  private State _currentState = State.HIDDEN;

  private Tween _buttonTween;

  public override void _Ready() {
    _buttonNode = GetNode<Button>("Button");
    _buttonNode.FocusMode = Control.FocusModeEnum.All;
    _blinkAnimationPlayerNode = GetNode<AnimationPlayer>("BlinkAnimationPlayer");

    _buttonNode.CustomMinimumSize = new Vector2(0, _buttonNode.CustomMinimumSize.Y);
    Visible = false;
    _buttonNode.Disabled = true;
    UpdateHeight();
    Color = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(node_color),
      SkinColorIntensity.Basic
    );
    _buttonNode.Icon = iconTexture;
  }

  private void SetFocusNextAndPrevious() {
    if (focus_left_node != null && !focus_left_node.IsEmpty) {
      var leftNode = GetNode<SlotButton>(focus_left_node);
      if (leftNode != null) {
        _buttonNode.FocusNeighborLeft = leftNode._buttonNode.GetPath();
      }
    }

    if (focus_left_node != null && !focus_right_node.IsEmpty) {
      var rightNode = GetNode<SlotButton>(focus_right_node);
      if (rightNode != null) {
        _buttonNode.FocusNeighborRight = rightNode._buttonNode.GetPath();
      }
    }
  }

  public void ShowButton() {
    if (_currentState == State.HIDING || _currentState == State.HIDDEN) {
      Visible = true;
      _buttonNode.Disabled = false;
      _currentState = State.SHOWING;
      SetupTween(BUTTON_MAX_WIDTH);
      _buttonNode.FocusMode = FocusModeEnum.All;
      SetFocusNextAndPrevious();
    }
  }

  public bool ButtonHasFocus() {
    return _buttonNode.HasFocus();
  }

  public void HideButton() {
    if (_currentState == State.SHOWING || _currentState == State.SHOWN) {
      _currentState = State.HIDING;
      SetupTween(BUTTON_MIN_WIDTH);
      _blinkAnimationPlayerNode.Play("RESET");
      _buttonNode.FocusMode = Control.FocusModeEnum.None;
    }
  }

  public new void GrabFocus() {
    _buttonNode.GrabFocus();
    _blinkAnimationPlayerNode.Play("Blink");
  }

  private void UpdateHeight() {
    // make the button height expand the whole container
    Size = new Vector2(Size.X, GetParent<Control>().Size.Y);
    _buttonNode.Size = new Vector2(_buttonNode.Size.X, Size.Y);
  }

  public override void _Process(double delta) {
    CustomMinimumSize = new Vector2(_buttonNode.CustomMinimumSize.X, CustomMinimumSize.Y);
    Size = new Vector2(_buttonNode.CustomMinimumSize.X, Size.Y);
    UpdateHeight();
    BlinkButtonIfNeeded();
  }

  private void BlinkButtonIfNeeded() {
    if (_buttonNode.HasFocus()) {
      if (_blinkAnimationPlayerNode.CurrentAnimation != "Blink") {
        _blinkAnimationPlayerNode.Play("Blink");
      }
    }
    else {
      if (_blinkAnimationPlayerNode.CurrentAnimation == "Blink") {
        _blinkAnimationPlayerNode.Play("RESET");
      }
    }
  }

  private void SetupTween(float sizeX) {
    _buttonTween?.Kill();
    _buttonTween = CreateTween();
    _buttonTween.TweenProperty(_buttonNode, "custom_minimum_size:x", sizeX, MOVE_DURATION)
        .SetEase(Tween.EaseType.InOut)
        .SetTrans(Tween.TransitionType.Linear);
    _buttonTween.Connect(Tween.SignalName.Finished, new Callable(this, nameof(OnTweenCompleted)), flags: (uint)ConnectFlags.OneShot);
  }

  private void OnTweenCompleted() {
    if (_currentState == State.HIDING) {
      _currentState = State.HIDDEN;
      Visible = false;
      _buttonNode.Disabled = true;
    }
    else if (_currentState == State.SHOWING) {
      _currentState = State.SHOWN;
    }
  }

  private void _on_Button_pressed() {
    EmitSignal(nameof(pressed));
  }

  private void _on_Button_mouse_entered() {
    GrabFocus();
  }
}
