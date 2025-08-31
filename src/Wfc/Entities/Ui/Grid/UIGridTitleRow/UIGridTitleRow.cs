namespace Wfc.Entities.Ui.Grid;

using System.Collections.Generic;
using Godot;
using Wfc.Core.Localization;
using Wfc.Core.Settings;
using Wfc.Entities.Ui;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class UIGridTitleRow : MarginContainer {
  [Export]
  public string Title { get; set; } = "";

  [NodePath("PanelContainer")]
  public PanelContainer _panelContainerNode = default!;
  [NodePath("PanelContainer/Content")]
  public CenterContainer _contentNode = default!;

  public void _setPanelStyle() {
    _panelContainerNode.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    var style = new StyleBoxFlat();
    style.BgColor = Colors.Transparent;
    style.ContentMarginTop = 5;
    style.ContentMarginBottom = 5;
    // Fixme: this hack is to fix line spacing between items in the parent grid.
    style.ExpandMarginTop = 4;
    style.BorderColor = Colors.Black;
    style.BorderWidthTop = 6;
    style.BorderWidthBottom = 6;
    _panelContainerNode.AddThemeStyleboxOverride("panel", style);
  }

  private void _addContent() {
    // Make this row stretch across the parent
    _contentNode.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    _contentNode.SizeFlagsVertical = SizeFlags.ShrinkCenter;

    // Create label
    var _label = new Label {
      Text = Title,
      HorizontalAlignment = HorizontalAlignment.Right,
      SizeFlagsHorizontal = SizeFlags.ExpandFill,
      SizeFlagsVertical = SizeFlags.ShrinkCenter
    };
    _contentNode.AddChild(_label);
  }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
    _setPanelStyle();
    _addContent();
  }
}
