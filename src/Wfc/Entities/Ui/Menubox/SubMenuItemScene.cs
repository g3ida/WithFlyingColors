using Godot;
using Wfc.Utils.Attributes;

namespace Wfc.Entities.Ui.Menubox
{

  [ScenePath("res://Assets/Scenes/MainMenu/SubMenuItem.tscn")]
  public partial class SubMenuItemScene : Control
  {
    [NodePath("Button")]
    private Button ButtonNode;
    public SubMenuButton ButtonInfo { get; private set; }

    public override void _Ready()
    {
      this.WireNodes();
      ButtonNode.Pressed += () => ButtonInfo.OnClick();
      ButtonNode.MouseEntered += ButtonGrabFocus;
      SetProcess(false);
    }

    public void PrepareWith(SubMenuButton buttonInfo, Color color)
    {
      if (!IsNodeReady())
      {
        throw new System.Exception("Node is not ready. Please call this method after the Node is ready.");
      }
      ButtonInfo = buttonInfo;
      ButtonNode.Text = buttonInfo.Text;
      ButtonNode.Modulate = color;
      ButtonNode.Disabled = buttonInfo.IsDisabled;
      ButtonNode.FocusMode = buttonInfo.IsDisabled ? FocusModeEnum.None : FocusModeEnum.All;
    }

    public void ButtonGrabFocus()
    {
      if (!ButtonInfo.IsDisabled)
      {
        ButtonNode.GrabFocus();
      }
    }

    public bool ButtonHasFocus()
    {
      return ButtonNode.HasFocus();
    }
  }
}