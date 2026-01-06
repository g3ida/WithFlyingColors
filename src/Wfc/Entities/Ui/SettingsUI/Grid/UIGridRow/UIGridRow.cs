namespace Wfc.Entities.Ui.SettingsUI.Grid;

using System.Collections.Generic;
using System.Diagnostics;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using Wfc.Core.Localization;
using Wfc.Core.Settings;
using Wfc.Entities.Ui;
using Wfc.Utils;
using Wfc.Utils.Attributes;

[ScenePath]
[Meta(typeof(IAutoNode))]
public partial class UIGridRow : PanelContainer {

  #region Dependencies
  public override void _Notification(int what) => this.Notify(what);

  [Dependency]
  public ILocalizationService LocalizationService => this.DependOn<ILocalizationService>();
  #endregion Dependencies


  private const string THEME_OVERRIDE_NAME = "panel";

  [Export]
  public TranslationKey TranslationKey { get; set; }

  [Export]
  public bool IsDark { get; set; }// alternate background

  [NodePath("Content")]
  public HBoxContainer _contentNode = default!;

  private Control? _attachedNode = null;
  private int _focusState = 0;
  private void _setStyle(bool hasFocus) {
    var style = new StyleBoxFlat();
    style.BgColor = hasFocus ? new Color(0f, 0f, 0f, 0.2f) : IsDark ? new Color(0f, 0f, 0f, 0.05f) : Colors.Transparent;
    style.ContentMarginTop = 5;
    style.ContentMarginBottom = 5;
    // Fixme: this hack is to fix line spacing between items in the parent grid.
    style.ExpandMarginTop = 4;
    if (HasThemeStyleboxOverride(THEME_OVERRIDE_NAME)) {
      RemoveThemeStyleboxOverride(THEME_OVERRIDE_NAME);
    }
    AddThemeStyleboxOverride(THEME_OVERRIDE_NAME, style);
  }

  private void _addContentLabel() {
    // Create label
    var _label = new Label {
      Text = LocalizationService.GetLocalizedString(TranslationKey),
      HorizontalAlignment = HorizontalAlignment.Right,
      SizeFlagsHorizontal = SizeFlags.ExpandFill,
      SizeFlagsVertical = SizeFlags.ShrinkCenter
    };
    _contentNode.AddChild(_label);
  }

  private void _addContentValue() {
    _attachedNode = _getAttachedNode();
    if (_attachedNode is Label label) {
      label.HorizontalAlignment = HorizontalAlignment.Left;
    }
    _attachedNode.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    _attachedNode.SizeFlagsVertical = SizeFlags.ShrinkCenter;
    _attachedNode.Reparent(_contentNode);
    _attachedNode.FocusEntered += _onAttachedNodeFocusEntered;
    _attachedNode.FocusExited += _onAttachedNodeFocusExited;
  }

  private void _onAttachedNodeFocusEntered() {
    _focusState++;
    _setStyle(hasFocus: true);
  }

  private void _onAttachedNodeFocusExited() {
    _focusState = Mathf.Max(0, _focusState - 1);
    if (_focusState == 0) {
      _setStyle(hasFocus: false);
    }
  }

  private void _onMouseEntered() {
    _focusState++;
    _setStyle(hasFocus: true);
    _attachedNode?.GrabFocus();
  }

  private void _onMouseExited() {
    _focusState = Mathf.Max(0, _focusState - 1);
    if (_focusState == 0) {
      _setStyle(hasFocus: false);
    }
  }

  private void _addContentSpacer() {
    // Spacer between label and value
    var spacer = new Control {
      CustomMinimumSize = new Vector2(40, 0),
      SizeFlagsHorizontal = SizeFlags.ShrinkCenter
    };
    _contentNode.AddChild(spacer);
  }

  private Control _getAttachedNode() {
    var children = GetChildren();
    Debug.Assert(children?.Count == 2, "UIGridRow should have 1 attached children");
    var vaueNode = children[0] == _contentNode ? children[1] : children[0];
    Debug.Assert(vaueNode is Control, "UIGridRow should have 1 attached children");
    return (vaueNode as Control)!;
  }

  // Gets the focusable control within this row.
  /// This is the control that should receive focus when navigating to this row.
  public Control? GetFocusableControl() {
    return _attachedNode;
  }

  public override void _Ready() {
    base._Ready();
    this.WireNodes();
  }

  public void OnResolved() {
    SizeFlagsHorizontal = SizeFlags.ExpandFill;
    SizeFlagsVertical = SizeFlags.Fill;
    // Set minimum height to ensure all rows have consistent size
    CustomMinimumSize = new Vector2(0, 70);
    _setStyle(hasFocus: false);
    // Make this row stretch across the parent
    _contentNode.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    _contentNode.SizeFlagsVertical = SizeFlags.ShrinkCenter;
    // Set content
    _addContentLabel();
    _addContentSpacer();
    _addContentValue();
  }
}
