namespace Wfc.Entities.Ui.Menubox;
using Godot;
using System.Collections.Generic;
using System.Linq;
using Wfc.Core.Types;
using Wfc.Skin;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[Tool]
[ScenePath("res://Assets/Scenes/MainMenu/SubMenu.tscn")]
public partial class SubMenuScene : Control {
  private readonly List<SubMenuItemScene> _buttonsNodes = [];

  [NodePath("VBoxContainer")]
  private VBoxContainer _buttonsContainerNode = null!;

  [NodePath("VBoxContainer/Top")]
  private Control _topEdgeNode = null!;

  public override void _Ready() {
    this.WireNodes();
    base._Ready();
    SetProcess(false);
  }

  // Called when the node enters the scene tree for the first time.
  // please call this method after the Node is ready.
  public void PopulateWith(IEnumerable<SubMenuButton> buttons, ColorGroup colorGroup) {
    if (!IsNodeReady()) {
      throw new GameExceptions.InvalidCallException("Node is not ready. Please call this method after the Node is ready.");
    }
    InitContainer();
    // skins
    var skin = SkinManager.Instance.CurrentSkin;
    var buttonColor = skin.GetColor(colorGroup.SkinColor, SkinColorIntensity.Basic);
    var edgeColor = skin.GetColor(colorGroup.SkinColor, SkinColorIntensity.VeryDark);
    _topEdgeNode.Modulate = edgeColor;

    var scene = SceneHelpers.LoadScene<SubMenuItemScene>();
    float buttonsHeight = 0;
    float buttonsCustomMinimalSize = 0;

    foreach (var button in buttons) {
      var item = scene.Instantiate<SubMenuItemScene>();
      item.Ready += () => item.PrepareWith(button, buttonColor);
      _buttonsNodes.Add(item);
      _buttonsContainerNode.AddChild(item);
      item.Owner = _buttonsContainerNode;

      buttonsHeight += item.Size.Y;
      buttonsCustomMinimalSize += item.CustomMinimumSize.Y;
    }
    Size = new Vector2(Size.X, Size.Y + buttonsHeight);
    CustomMinimumSize = new Vector2(CustomMinimumSize.X, CustomMinimumSize.Y + buttonsCustomMinimalSize);
    // set buttons focus
    SetFocusDependencies();
    SetFocusButton();
  }

  private void InitContainer() {
    _buttonsContainerNode.OffsetTop = 0;
    _buttonsContainerNode.OffsetBottom = 0;
  }

  private bool SetFocusButton(int index) {
    var button = _buttonsNodes[index];
    if (!button.ButtonInfo.IsDisabled) {
      button.ButtonGrabFocus();
      return true;
    }
    return false;
  }

  private void SetFocusButton() {
    for (var i = 0; i < _buttonsNodes.Count; i++) {
      if (SetFocusButton(i)) {
        break;
      }
    }
  }

  private void SetFocusDependencies() {
    var activeIndexes = _buttonsNodes
      .Select((button, index) => new { button, index })
      .Where(x => !x.button.ButtonInfo.IsDisabled)
      .Select(x => x.index)
      .ToList();

    for (var i = 0; i < activeIndexes.Count - 1; i++) {
      var current = _buttonsNodes[activeIndexes[i]];
      var next = _buttonsNodes[activeIndexes[i + 1]];

      current.FocusNext = current.FocusNeighborBottom = next.GetPath();
      next.FocusPrevious = next.FocusNeighborTop = current.GetPath();
    }
  }
}