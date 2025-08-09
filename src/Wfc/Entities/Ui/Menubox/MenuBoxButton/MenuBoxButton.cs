namespace Wfc.Entities.Ui.Menubox;

using System.Runtime.InteropServices;
using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;

public partial class MenuBoxButton : Control {
  private static readonly Color LABEL_COLOR = new Color("96464646");
  private static readonly Color LABEL_COLOR_HOVER = new Color("dc464646");
  private static readonly Color LABEL_COLOR_CLICK = new Color("f0464646");
  private static readonly Color LABEL_COLOR_DISABLED = new Color("46464646");

  [Signal] public delegate void pressedEventHandler();
  private string _text = string.Empty;
  [Export]
  public string text {
    get => _text;
    set {
      _text = value;
      // FIXME: node was not yet ready ? C# migration
      if (_labelNode != null) {
        _labelNode.Text = value;
        UpdateLabelColor();
      }
    }
  }

  private bool _disabled;
  [Export]
  public bool disabled {
    get => _disabled;
    set => SetDisabled(value);
  }

  #region Nodes
  [NodePath("CenterTexture/TextureButton")]
  private TextureButton _textureButtonNode = default!;
  [NodePath("CenterLabel/Label")]
  private Label _labelNode = default!;
  [NodePath("BlinkTimer")]
  private Timer _blinkTimer = default!;
  #endregion Nodes

  private bool _hovering = false;

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    text = _text;
  }

  private void _on_TextureButton_pressed() {
    _labelNode.Modulate = LABEL_COLOR_CLICK;
    _blinkTimer.Start();
    EmitSignal(nameof(pressed));
  }

  private void _on_TextureButton_mouse_entered() {
    _hovering = true;
    if (_disabled)
      return;
    _labelNode.Modulate = LABEL_COLOR_HOVER;
  }

  private void _on_TextureButton_mouse_exited() {
    _hovering = false;
    if (_disabled)
      return;
    _labelNode.Modulate = LABEL_COLOR;
  }

  private void _on_BlinkTimer_timeout() {
    _labelNode.Modulate = _hovering ? LABEL_COLOR_HOVER : LABEL_COLOR;
    if (_disabled)
      _labelNode.Modulate = LABEL_COLOR_DISABLED;
  }

  private void SetDisabled(bool value) {
    _disabled = value;
    if (_disabled) {
      _textureButtonNode.Disabled = true;
      _labelNode.Modulate = LABEL_COLOR_DISABLED;
    }
    else {
      _textureButtonNode.Disabled = false;
      _labelNode.Modulate = _hovering ? LABEL_COLOR_HOVER : LABEL_COLOR;
    }
  }

  private void UpdateLabelColor() {
    _labelNode.Modulate = _disabled ? LABEL_COLOR_DISABLED : LABEL_COLOR;
  }
}
