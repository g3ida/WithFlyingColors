namespace Wfc.Entities.Ui.Slots;

using System;
using Godot;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[Tool]
public partial class SlotButton : ColorRect {
  public enum State { Hidden, Hiding, Showing, Shown }

  #region Signals
  [Signal]
  public delegate void pressedEventHandler();
  #endregion Signals

  #region Constants
  public const int BUTTON_MAX_WIDTH = 110;
  public const int BUTTON_MIN_WIDTH = 0;
  public const float MOVE_DURATION = 0.15f;
  #endregion Constants

  #region Exports
  [Export]
  public string NodeColor = "pink";
  [Export]
  public Texture2D IconTexture = default!;
  [Export]
  public NodePath? FocusLeftNode;
  [Export]
  public NodePath? FocusRightNode;
  #endregion Exports

  #region Nodes
  [NodePath("Button")]
  private Button _buttonNode = default!;
  [NodePath("BlinkAnimationPlayer")]
  private AnimationPlayer _blinkAnimationPlayerNode = default!;
  #endregion Nodes

  private State _currentState = State.Hidden;
  private Tween? _buttonTween;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _buttonNode.FocusMode = Control.FocusModeEnum.All;

    _buttonNode.CustomMinimumSize = new Vector2(0, _buttonNode.CustomMinimumSize.Y);
    Visible = false;
    _buttonNode.Disabled = true;
    UpdateHeight();
    Color = SkinManager.Instance.CurrentSkin.GetColor(
      GameSkin.ColorGroupToSkinColor(NodeColor),
      SkinColorIntensity.Basic
    );
    _buttonNode.Icon = IconTexture;
  }

  private void SetFocusNextAndPrevious() {
    if (FocusLeftNode != null && !FocusLeftNode.IsEmpty) {
      var leftNode = GetNode<SlotButton>(FocusLeftNode);
      if (leftNode != null) {
        _buttonNode.FocusNeighborLeft = leftNode._buttonNode.GetPath();
      }
    }

    if (FocusRightNode != null && !FocusRightNode.IsEmpty) {
      var rightNode = GetNode<SlotButton>(FocusRightNode);
      if (rightNode != null) {
        _buttonNode.FocusNeighborRight = rightNode._buttonNode.GetPath();
      }
    }
  }

  public void ShowButton() {
    if (_currentState == State.Hiding || _currentState == State.Hidden) {
      Visible = true;
      _buttonNode.Disabled = false;
      _currentState = State.Showing;
      SetupTween(BUTTON_MAX_WIDTH);
      _buttonNode.FocusMode = FocusModeEnum.All;
      SetFocusNextAndPrevious();
    }
  }

  public bool ButtonHasFocus() {
    return _buttonNode.HasFocus();
  }

  public void HideButton() {
    if (_currentState == State.Showing || _currentState == State.Shown) {
      _currentState = State.Hiding;
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
    if (_currentState == State.Hiding) {
      _currentState = State.Hidden;
      Visible = false;
      _buttonNode.Disabled = true;
    }
    else if (_currentState == State.Showing) {
      _currentState = State.Shown;
    }
  }

  private void _onButtonPressed() {
    EmitSignal(nameof(pressed));
  }

  private void _onButtonMouseEntered() {
    GrabFocus();
  }
}
