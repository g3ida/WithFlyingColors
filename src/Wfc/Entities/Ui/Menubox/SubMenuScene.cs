using Godot;
using System.Collections.Generic;
using System.Linq;
using Wfc.Core.Types;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;

namespace Wfc.Entities.Ui.Menubox
{
  [Tool]
  [ScenePath("res://Assets/Scenes/MainMenu/SubMenu.tscn")]
  public partial class SubMenuScene : Control
  {
    private readonly List<SubMenuItemScene> ButtonsNodes = [];

    [NodePath("VBoxContainer")]
    private VBoxContainer ButtonsContainerNode;

    [NodePath("VBoxContainer/Top")]
    private Control TopEdgeNode;

    public override void _Ready()
    {
      this.WireNodes();
      base._Ready();
      SetProcess(false);
    }

    // Called when the node enters the scene tree for the first time.
    // please call this method after the Node is ready.
    public void PopulateWith(IEnumerable<SubMenuButton> buttons, ColorGroup colorGroup)
    {
      if (!IsNodeReady())
      {
        throw new System.Exception("Node is not ready. Please call this method after the Node is ready.");
      }
      InitContainer();
      // skins
      var skin = SkinManager.Instance.CurrentSkin;
      var buttonColor = skin.GetColor(colorGroup.SkinColor, SkinColorIntensity.Basic);
      var edgeColor = skin.GetColor(colorGroup.SkinColor, SkinColorIntensity.VeryDark);
      TopEdgeNode.Modulate = edgeColor;

      var Scene = SceneHelpers.LoadScene<SubMenuItemScene>();
      float buttonsHeight = 0;
      float buttonsCustomMinimalSize = 0;

      foreach (var button in buttons)
      {
        var item = Scene.Instantiate<SubMenuItemScene>();
        item.Ready += () => item.PrepareWith(button, buttonColor);
        ButtonsNodes.Add(item);
        ButtonsContainerNode.AddChild(item);
        item.Owner = ButtonsContainerNode;

        buttonsHeight += item.Size.Y;
        buttonsCustomMinimalSize += item.CustomMinimumSize.Y;
      }
      Size = new Vector2(Size.X, Size.Y + buttonsHeight);
      CustomMinimumSize = new Vector2(CustomMinimumSize.X, CustomMinimumSize.Y + buttonsCustomMinimalSize);
      // set buttons focus
      SetFocusDependencies();
      SetFocusButton();
    }

    private void InitContainer()
    {
      ButtonsContainerNode.OffsetTop = 0;
      ButtonsContainerNode.OffsetBottom = 0;
    }

    private bool SetFocusButton(int index)
    {
      var button = ButtonsNodes[index];
      if (!button.ButtonInfo.IsDisabled)
      {
        button.ButtonGrabFocus();
        return true;
      }
      return false;
    }

    private void SetFocusButton()
    {
      for (int i = 0; i < ButtonsNodes.Count; i++)
      {
        if (SetFocusButton(i)) break;
      }
    }

    private void SetFocusDependencies()
    {
      List<int> activeIndexes = ButtonsNodes
        .Select((button, index) => new { button, index })
        .Where(x => !x.button.ButtonInfo.IsDisabled)
        .Select(x => x.index)
        .ToList();

      for (int i = 0; i < activeIndexes.Count - 1; i++)
      {
        var current = ButtonsNodes[activeIndexes[i]];
        var next = ButtonsNodes[activeIndexes[i + 1]];

        current.FocusNext = current.FocusNeighborBottom = next.GetPath();
        next.FocusPrevious = next.FocusNeighborTop = current.GetPath();
      }
    }
  }
}