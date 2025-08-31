namespace Wfc.Entities.Ui.Grid;

using System.Collections.Generic;
using Godot;
using Wfc.Core.Localization;
using Wfc.Core.Settings;
using Wfc.Entities.Ui;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
public partial class UIGridRow : PanelContainer {
  [Export]
  public string Label { get; set; } = "";

  [Export]
  public string Value { get; set; } = "";

  [Export]
  public bool IsDark { get; set; }// alternate background

  [NodePath("Content")]
  public HBoxContainer _contentNode = default!;

  public void _setStyle() {
    var style = new StyleBoxFlat();
    style.BgColor = IsDark ? new Color(0f, 0f, 0f, 0.05f) : Colors.Transparent;
    style.ContentMarginTop = 5;
    style.ContentMarginBottom = 5;
    // Fixme: this hack is to fix line spacing between items in the parent grid.
    style.ExpandMarginTop = 4;
    AddThemeStyleboxOverride("panel", style);
  }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();

    SizeFlagsHorizontal = SizeFlags.ExpandFill;

    _setStyle();


    // Make this row stretch across the parent
    _contentNode.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    _contentNode.SizeFlagsVertical = SizeFlags.ShrinkCenter;

    // Create label
    var _label = new Label {
      Text = Label,
      HorizontalAlignment = HorizontalAlignment.Right,
      SizeFlagsHorizontal = SizeFlags.ExpandFill,
      SizeFlagsVertical = SizeFlags.ShrinkCenter
    };
    _contentNode.AddChild(_label);

    // Spacer between label and value
    var spacer = new Control {
      CustomMinimumSize = new Vector2(40, 0),
      SizeFlagsHorizontal = SizeFlags.ShrinkCenter
    };
    _contentNode.AddChild(spacer);

    // Value control

    var _value = new Label {
      Text = Value,
      HorizontalAlignment = HorizontalAlignment.Left,
      SizeFlagsHorizontal = SizeFlags.ExpandFill,
      SizeFlagsVertical = SizeFlags.ShrinkCenter
    };
    _contentNode.AddChild(_value);
  }
}
