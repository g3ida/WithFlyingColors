namespace Wfc.Entities.Ui.Menubox;
using Godot;
using Wfc.Utils;
using Wfc.Utils.Attributes;
using Wfc.Core.Exceptions;

[ScenePath]
public partial class SubMenuItem : Control {
  [NodePath("Button")]
  private Button _buttonNode = null!;
  public SubMenuButton ButtonInfo { get; private set; } = null!;

  private SubMenuItem() { }

  public override void _Ready() {
    this.WireNodes();
    _buttonNode.Pressed += () => ButtonInfo.OnClick();
    _buttonNode.MouseEntered += ButtonGrabFocus;
    SetProcess(false);
  }

  public void PrepareWith(SubMenuButton buttonInfo, Color color) {
    if (!IsNodeReady()) {
      throw new GameExceptions.InvalidCallException("Node is not ready. Please call this method after the Node is ready.");
    }
    ButtonInfo = buttonInfo;
    _buttonNode.Text = buttonInfo.Text;
    _buttonNode.Modulate = color;
    _buttonNode.Disabled = buttonInfo.IsDisabled;
    _buttonNode.FocusMode = buttonInfo.IsDisabled ? FocusModeEnum.None : FocusModeEnum.All;
  }

  public void ButtonGrabFocus() {
    if (!ButtonInfo.IsDisabled) {
      _buttonNode.GrabFocus();
    }
  }

  public bool ButtonHasFocus() => _buttonNode.HasFocus();
}
